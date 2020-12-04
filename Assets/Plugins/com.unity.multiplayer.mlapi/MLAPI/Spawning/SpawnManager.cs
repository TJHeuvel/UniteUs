using System;
using System.Collections.Generic;
using System.IO;
using MLAPI.Configuration;
using MLAPI.Exceptions;
using MLAPI.Hashing;
using MLAPI.Internal;
using MLAPI.Logging;
using MLAPI.Messaging;
using MLAPI.SceneManagement;
using MLAPI.Security;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;
using UnityEngine;

namespace MLAPI.Spawning
{
    /// <summary>
    /// Class that handles object spawning
    /// </summary>
    public static class SpawnManager
    {
        /// <summary>
        /// The currently spawned objects
        /// </summary>
        public static readonly Dictionary<ulong, NetworkedObject> SpawnedObjects = new Dictionary<ulong, NetworkedObject>();
        // Pending SoftSync objects
        internal static readonly Dictionary<ulong, NetworkedObject> pendingSoftSyncObjects = new Dictionary<ulong, NetworkedObject>();
        /// <summary>
        /// A list of the spawned objects
        /// </summary>
        public static readonly List<NetworkedObject> SpawnedObjectsList = new List<NetworkedObject>();
        /// <summary>
        /// The delegate used when spawning a networked object
        /// </summary>
        /// <param name="position">The position to spawn the object at</param>
        /// <param name="rotation">The rotation to spawn the object with</param>
        public delegate NetworkedObject SpawnHandlerDelegate(Vector3 position, Quaternion rotation);
        /// <summary>
        /// The delegate used when destroying networked objects
        /// </summary>
        /// <param name="networkedObject">The networked object to be destroy</param>
        public delegate void DestroyHandlerDelegate(NetworkedObject networkedObject);

        internal static readonly Dictionary<ulong, SpawnHandlerDelegate> customSpawnHandlers = new Dictionary<ulong, SpawnHandlerDelegate>();
        internal static readonly Dictionary<ulong, DestroyHandlerDelegate> customDestroyHandlers = new Dictionary<ulong, DestroyHandlerDelegate>();

        /// <summary>
        /// Registers a delegate for spawning networked prefabs, useful for object pooling
        /// </summary>
        /// <param name="prefabHash">The prefab hash to spawn</param>
        /// <param name="handler">The delegate handler</param>
        public static void RegisterSpawnHandler(ulong prefabHash, SpawnHandlerDelegate handler)
        {
            if (customSpawnHandlers.ContainsKey(prefabHash))
            {
                customSpawnHandlers[prefabHash] = handler;
            }
            else
            {
                customSpawnHandlers.Add(prefabHash, handler);
            }
        }

        /// <summary>
        /// Registers a delegate for destroying networked objects, useful for object pooling
        /// </summary>
        /// <param name="prefabHash">The prefab hash to destroy</param>
        /// <param name="handler">The delegate handler</param>
        public static void RegisterCustomDestroyHandler(ulong prefabHash, DestroyHandlerDelegate handler)
        {
            if (customDestroyHandlers.ContainsKey(prefabHash))
            {
                customDestroyHandlers[prefabHash] = handler;
            }
            else
            {
                customDestroyHandlers.Add(prefabHash, handler);
            }
        }

        /// <summary>
        /// Removes the custom spawn handler for a specific prefab hash
        /// </summary>
        /// <param name="prefabHash">The prefab hash of the prefab spawn handler that is to be removed</param>
        public static void RemoveCustomSpawnHandler(ulong prefabHash)
        {
            customSpawnHandlers.Remove(prefabHash);
        }

        /// <summary>
        /// Removes the custom destroy handler for a specific prefab hash
        /// </summary>
        /// <param name="prefabHash">The prefab hash of the prefab destroy handler that is to be removed</param>
        public static void RemoveCustomDestroyHandler(ulong prefabHash)
        {
            customDestroyHandlers.Remove(prefabHash);
        }

        internal static readonly Queue<ReleasedNetworkId> releasedNetworkObjectIds = new Queue<ReleasedNetworkId>();
        private static ulong networkObjectIdCounter;
        internal static ulong GetNetworkObjectId()
        {
            if (releasedNetworkObjectIds.Count > 0 && NetworkingManager.Singleton.NetworkConfig.RecycleNetworkIds && (Time.unscaledTime - releasedNetworkObjectIds.Peek().ReleaseTime) >= NetworkingManager.Singleton.NetworkConfig.NetworkIdRecycleDelay)
            {
                return releasedNetworkObjectIds.Dequeue().NetworkId;
            }
            else
            {
                networkObjectIdCounter++;
                return networkObjectIdCounter;
            }
        }

        /// <summary>
        /// Gets the prefab index of a given prefab hash
        /// </summary>
        /// <param name="hash">The hash of the prefab</param>
        /// <returns>The index of the prefab</returns>
        public static int GetNetworkedPrefabIndexOfHash(ulong hash)
        {
            for (int i = 0; i < NetworkingManager.Singleton.NetworkConfig.NetworkedPrefabs.Count; i++)
            {
                if (NetworkingManager.Singleton.NetworkConfig.NetworkedPrefabs[i].Hash == hash)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Returns the prefab hash for the networked prefab with a given index
        /// </summary>
        /// <param name="index">The networked prefab index</param>
        /// <returns>The prefab hash for the given prefab index</returns>
        public static ulong GetPrefabHashFromIndex(int index)
        {
            return NetworkingManager.Singleton.NetworkConfig.NetworkedPrefabs[index].Hash;
        }

        /// <summary>
        /// Returns the prefab hash for a given prefab hash generator
        /// </summary>
        /// <param name="generator">The prefab hash generator</param>
        /// <returns>The hash for the given generator</returns>
        public static ulong GetPrefabHashFromGenerator(string generator)
        {
            return generator.GetStableHash64();
        }

        /// <summary>
        /// Returns the local player object or null if one does not exist
        /// </summary>
        /// <returns>The local player object or null if one does not exist</returns>
        public static NetworkedObject GetLocalPlayerObject()
        {
            if (!NetworkingManager.Singleton.ConnectedClients.ContainsKey(NetworkingManager.Singleton.LocalClientId)) return null;
            return NetworkingManager.Singleton.ConnectedClients[NetworkingManager.Singleton.LocalClientId].PlayerObject;
        }

        /// <summary>
        /// Returns the player object with a given clientId or null if one does not exist
        /// </summary>
        /// <returns>The player object with a given clientId or null if one does not exist</returns>
        public static NetworkedObject GetPlayerObject(ulong clientId)
        {
            if (!NetworkingManager.Singleton.ConnectedClients.ContainsKey(clientId)) return null;
            return NetworkingManager.Singleton.ConnectedClients[clientId].PlayerObject;
        }

        internal static void RemoveOwnership(NetworkedObject netObject)
        {
            if (!NetworkingManager.Singleton.IsServer)
            {
                throw new NotServerException("Only the server can change ownership");
            }

            if (!netObject.IsSpawned)
            {
                throw new SpawnStateException("Object is not spawned");
            }

            for (int i = NetworkingManager.Singleton.ConnectedClients[netObject.OwnerClientId].OwnedObjects.Count - 1; i > -1; i--)
            {
                if (NetworkingManager.Singleton.ConnectedClients[netObject.OwnerClientId].OwnedObjects[i] == netObject)
                    NetworkingManager.Singleton.ConnectedClients[netObject.OwnerClientId].OwnedObjects.RemoveAt(i);
            }

			netObject._ownerClientId = null;

            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteUInt64Packed(netObject.NetworkId);
                    writer.WriteUInt64Packed(netObject.OwnerClientId);

                    InternalMessageSender.Send(MLAPIConstants.MLAPI_CHANGE_OWNER, "MLAPI_INTERNAL", stream, SecuritySendFlags.None, netObject);
                }
            }
        }

        internal static void ChangeOwnership(NetworkedObject netObject, ulong clientId)
        {
            if (!NetworkingManager.Singleton.IsServer)
            {
                throw new NotServerException("Only the server can change ownership");
            }

            if (!netObject.IsSpawned)
            {
                throw new SpawnStateException("Object is not spawned");
            }

            if (NetworkingManager.Singleton.ConnectedClients.ContainsKey(netObject.OwnerClientId))
            {
                for (int i = NetworkingManager.Singleton.ConnectedClients[netObject.OwnerClientId].OwnedObjects.Count - 1; i >= 0; i--)
                {
                    if (NetworkingManager.Singleton.ConnectedClients[netObject.OwnerClientId].OwnedObjects[i] == netObject)
                        NetworkingManager.Singleton.ConnectedClients[netObject.OwnerClientId].OwnedObjects.RemoveAt(i);
                }
            }

            NetworkingManager.Singleton.ConnectedClients[clientId].OwnedObjects.Add(netObject);
            netObject.OwnerClientId = clientId;

            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteUInt64Packed(netObject.NetworkId);
                    writer.WriteUInt64Packed(clientId);

                    InternalMessageSender.Send(MLAPIConstants.MLAPI_CHANGE_OWNER, "MLAPI_INTERNAL", stream, SecuritySendFlags.None, netObject);
                }
            }
        }

        // Only ran on Client
        internal static NetworkedObject CreateLocalNetworkedObject(bool softCreate, ulong instanceId, ulong prefabHash, ulong? parentNetworkId, Vector3? position, Quaternion? rotation)
        {
            NetworkedObject parent = null;

            if (parentNetworkId != null && SpawnedObjects.ContainsKey(parentNetworkId.Value))
            {
                parent = SpawnedObjects[parentNetworkId.Value];
            }
            else if (parentNetworkId != null)
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Cannot find parent. Parent objects always have to be spawned and replicated BEFORE the child");
            }

            if (!NetworkingManager.Singleton.NetworkConfig.EnableSceneManagement || NetworkingManager.Singleton.NetworkConfig.UsePrefabSync || !softCreate)
            {
                // Create the object
                if (customSpawnHandlers.ContainsKey(prefabHash))
                {
                    NetworkedObject networkedObject = customSpawnHandlers[prefabHash](position.GetValueOrDefault(Vector3.zero), rotation.GetValueOrDefault(Quaternion.identity));

                    if (parent != null)
                    {
                        networkedObject.transform.SetParent(parent.transform, true);
                    }

                    if (NetworkSceneManager.isSpawnedObjectsPendingInDontDestroyOnLoad)
                    {
                        GameObject.DontDestroyOnLoad(networkedObject.gameObject);
                    }

                    return networkedObject;
                }
                else
                {
                    int prefabIndex = GetNetworkedPrefabIndexOfHash(prefabHash);

                    if (prefabIndex < 0)
                    {
                        if (NetworkLog.CurrentLogLevel <= LogLevel.Error) NetworkLog.LogError("Failed to create object locally. [PrefabHash=" + prefabHash + "]. Hash could not be found. Is the prefab registered?");

                        return null;
                    }
                    else
                    {
                        GameObject prefab = NetworkingManager.Singleton.NetworkConfig.NetworkedPrefabs[prefabIndex].Prefab;

                        NetworkedObject networkedObject = ((position == null && rotation == null) ? MonoBehaviour.Instantiate(prefab) : MonoBehaviour.Instantiate(prefab, position.GetValueOrDefault(Vector3.zero), rotation.GetValueOrDefault(Quaternion.identity))).GetComponent<NetworkedObject>();

                        if (parent != null)
                        {
                            networkedObject.transform.SetParent(parent.transform, true);
                        }

                        if (NetworkSceneManager.isSpawnedObjectsPendingInDontDestroyOnLoad)
                        {
                            GameObject.DontDestroyOnLoad(networkedObject.gameObject);
                        }

                        return networkedObject;
                    }
                }
            }
            else
            {
                // SoftSync them by mapping
                if (!pendingSoftSyncObjects.ContainsKey(instanceId))
                {
                    // TODO: Fix this message
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Error) NetworkLog.LogError("Cannot find pending soft sync object. Is the projects the same?");
                    return null;
                }

                NetworkedObject networkedObject = pendingSoftSyncObjects[instanceId];
                pendingSoftSyncObjects.Remove(instanceId);

                if (parent != null)
                {
                    networkedObject.transform.SetParent(parent.transform, true);
                }

                return networkedObject;
            }
        }

        // Ran on both server and client
        internal static void SpawnNetworkedObjectLocally(NetworkedObject netObject, ulong networkId, bool sceneObject, bool playerObject, ulong? ownerClientId, Stream dataStream, bool readPayload, int payloadLength, bool readNetworkedVar, bool destroyWithScene)
        {
            if (netObject == null)
            {
                throw new ArgumentNullException(nameof(netObject), "Cannot spawn null object");
            }

            if (netObject.IsSpawned)
            {
                throw new SpawnStateException("Object is already spawned");
            }

            if (readNetworkedVar && NetworkingManager.Singleton.NetworkConfig.EnableNetworkedVar)
            {
                netObject.SetNetworkedVarData(dataStream);
                netObject.SetSyncedVarData(dataStream);
            }

            netObject.IsSpawned = true;

            netObject.IsSceneObject = sceneObject;
            netObject.NetworkId = networkId;

            netObject.DestroyWithScene = sceneObject || destroyWithScene;

            netObject._ownerClientId = ownerClientId;
            netObject.IsPlayerObject = playerObject;

            SpawnedObjects.Add(netObject.NetworkId, netObject);
            SpawnedObjectsList.Add(netObject);

            if (ownerClientId != null)
            {
                if (NetworkingManager.Singleton.IsServer)
                {
                    if (playerObject)
                    {
                        NetworkingManager.Singleton.ConnectedClients[ownerClientId.Value].PlayerObject = netObject;
                    }
                    else
                    {
                        NetworkingManager.Singleton.ConnectedClients[ownerClientId.Value].OwnedObjects.Add(netObject);
                    }
                }
                else if (playerObject && ownerClientId.Value == NetworkingManager.Singleton.LocalClientId)
                {
                    NetworkingManager.Singleton.ConnectedClients[ownerClientId.Value].PlayerObject = netObject;
                }
            }

            if (NetworkingManager.Singleton.IsServer)
            {
                for (int i = 0; i < NetworkingManager.Singleton.ConnectedClientsList.Count; i++)
                {
                    if (netObject.CheckObjectVisibility == null || netObject.CheckObjectVisibility(NetworkingManager.Singleton.ConnectedClientsList[i].ClientId))
                    {
                        netObject.observers.Add(NetworkingManager.Singleton.ConnectedClientsList[i].ClientId);
                    }
                }
            }

            netObject.ResetNetworkedStartInvoked();

            if (readPayload)
            {
                using (PooledBitStream payloadStream = PooledBitStream.Get())
                {
                    payloadStream.CopyUnreadFrom(dataStream, payloadLength);
                    dataStream.Position += payloadLength;
                    payloadStream.Position = 0;
                    netObject.InvokeBehaviourNetworkSpawn(payloadStream);
                }
            }
            else
            {
                netObject.InvokeBehaviourNetworkSpawn(null);
            }
        }

        internal static void SendSpawnCallForObject(ulong clientId, NetworkedObject netObject, Stream payload)
        {
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                WriteSpawnCallForObject(stream, clientId, netObject, payload);

                InternalMessageSender.Send(clientId, MLAPIConstants.MLAPI_ADD_OBJECT, "MLAPI_INTERNAL", stream, SecuritySendFlags.None, null);
            }
        }

        internal static void WriteSpawnCallForObject(Serialization.BitStream stream, ulong clientId, NetworkedObject netObject, Stream payload)
        {
            using (PooledBitWriter writer = PooledBitWriter.Get(stream))
            {
                writer.WriteBool(netObject.IsPlayerObject);
                writer.WriteUInt64Packed(netObject.NetworkId);
                writer.WriteUInt64Packed(netObject.OwnerClientId);

                NetworkedObject parent = null;

                if (!netObject.AlwaysReplicateAsRoot && netObject.transform.parent != null)
                {
                    parent = netObject.transform.parent.GetComponent<NetworkedObject>();
                }

                if (parent == null)
                {
                    writer.WriteBool(false);
                }
                else
                {
                    writer.WriteBool(true);
                    writer.WriteUInt64Packed(parent.NetworkId);
                }

                if (!NetworkingManager.Singleton.NetworkConfig.EnableSceneManagement || NetworkingManager.Singleton.NetworkConfig.UsePrefabSync)
                {
                    writer.WriteUInt64Packed(netObject.PrefabHash);
                }
                else
                {
                    writer.WriteBool(netObject.IsSceneObject == null ? true : netObject.IsSceneObject.Value);

                    if (netObject.IsSceneObject == null || netObject.IsSceneObject.Value)
                    {
                        writer.WriteUInt64Packed(netObject.NetworkedInstanceId);
                    }
                    else
                    {
                        writer.WriteUInt64Packed(netObject.PrefabHash);
                    }
                }

                if (netObject.IncludeTransformWhenSpawning == null || netObject.IncludeTransformWhenSpawning(clientId))
                {
                    writer.WriteBool(true);
                    writer.WriteSinglePacked(netObject.transform.position.x);
                    writer.WriteSinglePacked(netObject.transform.position.y);
                    writer.WriteSinglePacked(netObject.transform.position.z);

                    writer.WriteSinglePacked(netObject.transform.rotation.eulerAngles.x);
                    writer.WriteSinglePacked(netObject.transform.rotation.eulerAngles.y);
                    writer.WriteSinglePacked(netObject.transform.rotation.eulerAngles.z);
                } else
                {
                    writer.WriteBool(false);
                }

                writer.WriteBool(payload != null);

                if (payload != null)
                {
                    writer.WriteInt32Packed((int) payload.Length);
                }

                if (NetworkingManager.Singleton.NetworkConfig.EnableNetworkedVar)
                {
                    netObject.WriteNetworkedVarData(stream, clientId);
                    netObject.WriteSyncedVarData(stream, clientId);
                }

                if (payload != null) stream.CopyFrom(payload);
            }
        }

        internal static void UnSpawnObject(NetworkedObject netObject)
        {
            if (!netObject.IsSpawned)
            {
                throw new SpawnStateException("Object is not spawned");
            }

            if (!NetworkingManager.Singleton.IsServer)
            {
                throw new NotServerException("Only server unspawn objects");
            }

            OnDestroyObject(netObject.NetworkId, false);
        }

        // Makes scene objects ready to be reused
        internal static void ServerResetShudownStateForSceneObjects()
        {
            for (int i = 0; i < SpawnedObjectsList.Count; i++)
            {
                if ((SpawnedObjectsList[i].IsSceneObject != null && SpawnedObjectsList[i].IsSceneObject == true) || SpawnedObjectsList[i].DestroyWithScene)
                {
                    SpawnedObjectsList[i].IsSpawned = false;
                    SpawnedObjectsList[i].DestroyWithScene = false;
                    SpawnedObjectsList[i].IsSceneObject = null;
                }
            }
        }

        internal static void ServerDestroySpawnedSceneObjects()
        {
            for (int i = SpawnedObjectsList.Count - 1; i >= 0; i--)
            {
                if ((SpawnedObjectsList[i].IsSceneObject != null && SpawnedObjectsList[i].IsSceneObject == true) || SpawnedObjectsList[i].DestroyWithScene)
                {
                    if (customDestroyHandlers.ContainsKey(SpawnedObjectsList[i].PrefabHash))
                    {
                        customDestroyHandlers[SpawnedObjectsList[i].PrefabHash](SpawnedObjectsList[i]);
                        SpawnManager.OnDestroyObject(SpawnedObjectsList[i].NetworkId, false);
                    }
                    else
                    {
                        MonoBehaviour.Destroy(SpawnedObjectsList[i].gameObject);
                    }
                }
            }
        }

        internal static void DestroyNonSceneObjects()
        {
            NetworkedObject[] netObjects = MonoBehaviour.FindObjectsOfType<NetworkedObject>();

            for (int i = 0; i < netObjects.Length; i++)
            {
                if (netObjects[i].IsSceneObject != null && netObjects[i].IsSceneObject.Value == false)
                {
                    if (customDestroyHandlers.ContainsKey(netObjects[i].PrefabHash))
                    {
                        customDestroyHandlers[netObjects[i].PrefabHash](netObjects[i]);
                        SpawnManager.OnDestroyObject(netObjects[i].NetworkId, false);
                    }
                    else
                    {
                        MonoBehaviour.Destroy(netObjects[i].gameObject);
                    }
                }
            }
        }

        internal static void DestroySceneObjects()
        {
            NetworkedObject[] netObjects = MonoBehaviour.FindObjectsOfType<NetworkedObject>();

            for (int i = 0; i < netObjects.Length; i++)
            {
                if (netObjects[i].IsSceneObject == null || netObjects[i].IsSceneObject.Value == true)
                {
                    if (customDestroyHandlers.ContainsKey(netObjects[i].PrefabHash))
                    {
                        customDestroyHandlers[netObjects[i].PrefabHash](netObjects[i]);
                        SpawnManager.OnDestroyObject(netObjects[i].NetworkId, false);
                    }
                    else
                    {
                        MonoBehaviour.Destroy(netObjects[i].gameObject);
                    }
                }
            }
        }

        internal static void ServerSpawnSceneObjectsOnStartSweep()
        {
            NetworkedObject[] networkedObjects = MonoBehaviour.FindObjectsOfType<NetworkedObject>();

            for (int i = 0; i < networkedObjects.Length; i++)
            {
                if (networkedObjects[i].IsSceneObject == null)
                {
                    SpawnNetworkedObjectLocally(networkedObjects[i], GetNetworkObjectId(), true, false, null, null, false, 0, false, true);
                }
            }
        }

        internal static void ClientCollectSoftSyncSceneObjectSweep(NetworkedObject[] networkedObjects)
        {
            if (networkedObjects == null)
                networkedObjects = MonoBehaviour.FindObjectsOfType<NetworkedObject>();

            for (int i = 0; i < networkedObjects.Length; i++)
            {
                if (networkedObjects[i].IsSceneObject == null)
                {
                    pendingSoftSyncObjects.Add(networkedObjects[i].NetworkedInstanceId, networkedObjects[i]);
                }
            }
        }

        internal static void OnDestroyObject(ulong networkId, bool destroyGameObject)
        {
            if (NetworkingManager.Singleton == null)
                return;

            //Removal of spawned object
            if (!SpawnedObjects.ContainsKey(networkId))
                return;

			if (!SpawnedObjects[networkId].IsOwnedByServer && !SpawnedObjects[networkId].IsPlayerObject &&
			    NetworkingManager.Singleton.ConnectedClients.ContainsKey(SpawnedObjects[networkId].OwnerClientId))
            {
                //Someone owns it.
                for (int i = NetworkingManager.Singleton.ConnectedClients[SpawnedObjects[networkId].OwnerClientId].OwnedObjects.Count - 1; i > -1; i--)
                {
                    if (NetworkingManager.Singleton.ConnectedClients[SpawnedObjects[networkId].OwnerClientId].OwnedObjects[i].NetworkId == networkId)
                        NetworkingManager.Singleton.ConnectedClients[SpawnedObjects[networkId].OwnerClientId].OwnedObjects.RemoveAt(i);
                }
            }
            SpawnedObjects[networkId].IsSpawned = false;

            if (NetworkingManager.Singleton != null && NetworkingManager.Singleton.IsServer)
            {
                if (NetworkingManager.Singleton.NetworkConfig.RecycleNetworkIds)
                {
                    releasedNetworkObjectIds.Enqueue(new ReleasedNetworkId()
                    {
                        NetworkId = networkId,
                        ReleaseTime = Time.unscaledTime
                    });
                }

                if (SpawnedObjects[networkId] != null)
                {
                    using (PooledBitStream stream = PooledBitStream.Get())
                    {
                        using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                        {
                            writer.WriteUInt64Packed(networkId);

                            InternalMessageSender.Send(MLAPIConstants.MLAPI_DESTROY_OBJECT, "MLAPI_INTERNAL", stream, SecuritySendFlags.None, SpawnedObjects[networkId]);
                        }
                    }
                }
            }

            GameObject go = SpawnedObjects[networkId].gameObject;

            if (destroyGameObject && go != null)
            {
                if (customDestroyHandlers.ContainsKey(SpawnedObjects[networkId].PrefabHash))
                {
                    customDestroyHandlers[SpawnedObjects[networkId].PrefabHash](SpawnedObjects[networkId]);
                    SpawnManager.OnDestroyObject(networkId, false);
                }
                else
                {
                    MonoBehaviour.Destroy(go);
                }
            }

            SpawnedObjects.Remove(networkId);

            for (int i = SpawnedObjectsList.Count - 1; i > -1; i--)
            {
                if (SpawnedObjectsList[i].NetworkId == networkId)
                    SpawnedObjectsList.RemoveAt(i);
            }
        }
    }
}
