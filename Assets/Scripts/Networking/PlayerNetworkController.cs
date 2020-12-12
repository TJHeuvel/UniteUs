using System;
using System.IO;
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

    //TODO: Use Syncvars?
    public void BroadcastPlayerKilled()
    {
        if (IsServer)
            serverPlayerDied();
        else
            InvokeServerRpc(serverPlayerDied);
    }
    [ServerRPC(RequireOwnership = false)]
    private void serverPlayerDied()
    {
        //TODO: More validation, are both clients close?
        if (!playerController.IsAlive) return;

        playerController.Die();
        GameManager.Instance.ServerCheckWinConditions();

        InvokeClientRpcOnEveryone(clientPlayerDied);
    }

    [ClientRPC]
    private void clientPlayerDied() => playerController.Die();

}