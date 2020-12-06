using MLAPI;
using UnityEngine;

class PlayerNetworkController : MonoBehaviour 
{
    const ulong PlayerNetworkIdOffset = 100;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private NetworkedObject networkedObject;
    void Start()
    {        
        MLAPI.Spawning.SpawnManager.SpawnNetworkedObjectLocally(networkedObject, PlayerNetworkIdOffset + playerController.Player.ID, true, false, playerController.Player.ID, null, false, 0, true, true);
    }
}