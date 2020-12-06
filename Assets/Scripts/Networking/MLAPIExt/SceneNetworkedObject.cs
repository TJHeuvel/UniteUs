using MLAPI;
using UnityEngine;

class SceneNetworkedObject : MonoBehaviour 
{
    [SerializeField] private NetworkedObject networkedObject;
    [SerializeField] private ulong networkId;

    void OnEnable()
    {
        if (networkedObject == null) return;
        MLAPI.Spawning.SpawnManager.SpawnNetworkedObjectLocally(networkedObject, networkId, true, false, NetworkingManager.Singleton.ServerClientId, null, false, 0, true, true);
    }
}