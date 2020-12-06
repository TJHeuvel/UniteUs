using MLAPI;
using MLAPI.Messaging;
using UnityEngine;

class PlayerNetworkController : NetworkedBehaviour
{
    const ulong PlayerNetworkIdOffset = 100;
    [SerializeField] private PlayerController playerController;
    void Start()
    {
        MLAPI.Spawning.SpawnManager.SpawnNetworkedObjectLocally(NetworkedObject, PlayerNetworkIdOffset + playerController.NetworkPlayer.ID, true, false, playerController.NetworkPlayer.ID, null, false, 0, true, true);

        //Expose myself to everyone. 
        foreach (var player in LobbyManager.Instance.Players)
            NetworkedObject.NetworkShowLocal(player.ID);
    }
}