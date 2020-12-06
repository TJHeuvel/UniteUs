using UnityEngine;
using MLAPI;
using MLAPI.Transports.UNET;
using System;
using MLAPI.Messaging;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using MLAPI.Serialization.Pooled;
using MLAPI.NetworkedVar.Collections;

class LobbyManager : NetworkedBehaviour
{
    public event Action OnLobbyJoined;
    //TODO: OnPlayerAdded is fired a bit funky when joining the game
    public event Action<IPlayer> OnPlayerAdded, OnPlayerRemoved;
    public SortedList<ulong, IPlayer> Players { get; private set; } = new SortedList<ulong, IPlayer>();
    
    public static LobbyManager Instance { get; private set; }

    void OnEnable()
    {
        Instance = this;
        DontDestroyOnLoad(this);
    }
    async void OnDisable()
    {
        LeaveLobby();
        await System.Threading.Tasks.Task.Yield(); //wait one frame, then destroy. Solves people doing things in OnDestroy with us
        Instance = null;
    }

    #region Lobby Creating and Leaving

    public void CreateLobby()
    {
        NetworkingManager.Singleton.OnClientConnectedCallback += onConnected;
        NetworkingManager.Singleton.OnClientDisconnectCallback += onDisconnected;
        NetworkingManager.Singleton.OnServerStarted += onServerStarted;

        NetworkingManager.Singleton.StartHost();
    }



    public async void JoinLobby(string address)
    {
        if (NetworkingManager.Singleton.TryGetComponent<UnetTransport>(out var transport))
            transport.ConnectAddress = address;
        NetworkingManager.Singleton.OnClientConnectedCallback += onConnected;
        NetworkingManager.Singleton.OnClientDisconnectCallback += onDisconnected;
        var tasks = NetworkingManager.Singleton.StartClient();

        while (!tasks.IsDone) await Task.Yield();
        //todo: error checking
        OnLobbyJoined?.Invoke();
    }

    public void LeaveLobby()
    {
        Players.Clear();

        if (NetworkingManager.Singleton == null) return;
        NetworkingManager.Singleton.OnClientConnectedCallback -= onConnected;
        NetworkingManager.Singleton.OnClientDisconnectCallback -= onDisconnected;

        if (NetworkingManager.Singleton.IsClient)
        {
            if (NetworkingManager.Singleton.IsConnectedClient)
                NetworkingManager.Singleton.StopClient();
        }
        else if (NetworkingManager.Singleton.IsHost)
        {
            NetworkingManager.Singleton.OnServerStarted -= onServerStarted;

            NetworkingManager.Singleton.StopHost();
        }
    }
    #endregion

    #region Callbacks
    private void onServerStarted()
    {
        onConnected(NetworkingManager.Singleton.LocalClientId);
        OnLobbyJoined?.Invoke();
    }

    private void onConnected(ulong id)
    {
        Debug.Log("Connected! " + id);

        string userName = SystemInfo.deviceName;
        if (Application.isEditor)
            userName += " (Editor)";

        //Add ourself
        if (IsClient && id == NetworkingManager.Singleton.LocalClientId)
            addPlayer(id, userName);

        //Ask the server to add us to everyone else
        InvokeServerRpc(serverIntroducePlayer, userName);
    }


    private void addPlayer(ulong id, string userName)
    {
        var p = new NetworkPlayer()
        {
            ID = id,
            Name = userName
        };
        Players.Add(id, p);
        OnPlayerAdded?.Invoke(p);
    }

    private void onDisconnected(ulong id)
    {
        var playerToRemove = Players[id];
        Players.Remove(id);
        OnPlayerRemoved?.Invoke(playerToRemove);

        if (IsServer)
            InvokeClientRpcOnEveryone(clientDisconnectedPlayer, id);
    }

    #endregion

    #region RPC

    [ServerRPC(RequireOwnership = false)]
    private void serverIntroducePlayer(string userName)
    {
        if (ExecutingRpcSender != NetworkingManager.Singleton.ServerClientId)
        {
            //first send back the existing players
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteInt32Packed(Players.Count);

                    foreach (var kvp in Players)
                    {
                        writer.WriteUInt64(kvp.Key);
                        writer.WriteString(kvp.Value.Name);
                    }

                    InvokeClientRpcOnClientPerformance(clientIntroduceAllPlayers, ExecutingRpcSender, stream);
                }
            }
        }
        //then add the new one

        //In case we are hosting, we already added it earlier in OnConnected
        if (!IsHost)
            addPlayer(ExecutingRpcSender, userName);

        //and let everyone else know
        InvokeClientRpcOnEveryoneExcept(clientIntroducePlayer, ExecutingRpcSender, ExecutingRpcSender, userName);
    }

    [ClientRPC]
    private void clientIntroduceAllPlayers(ulong serverId, Stream stream)
    {
        using (PooledBitReader reader = PooledBitReader.Get(stream))
        {
            int len = reader.ReadInt32Packed();

            for (int i = 0; i < len; i++)
            {
                ulong id = reader.ReadUInt64();
                string name = reader.ReadString().ToString();
                addPlayer(id, name);
            }
        }

    }

    [ClientRPC]
    private void clientIntroducePlayer(ulong id, string userName)
    {
        addPlayer(id, userName);
    }
    [ClientRPC]
    private void clientDisconnectedPlayer(ulong id)
    {
        OnPlayerRemoved?.Invoke(Players[id]);
        Players.Remove(id);
    }

    #endregion
#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(LobbyManager))]
    class LobbyManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var target = base.target as LobbyManager;
            if (Application.isPlaying)
                foreach (var p in target.Players)
                    GUILayout.Label(p.ToString());
        }
    }
#endif
}