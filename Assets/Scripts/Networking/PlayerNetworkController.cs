using System;
using System.IO;
using MLAPI;
using MLAPI.Messaging;
using UnityEngine;

class PlayerNetworkController : NetworkedBehaviour
{
    public static Action<PlayerController, PlayerController> OnPlayerVoted;

    const ulong PlayerNetworkIdOffset = 100;
    [SerializeField] private PlayerController playerController;
    void Start()
    {
        MLAPI.Spawning.SpawnManager.SpawnNetworkedObjectLocally(NetworkedObject, PlayerNetworkIdOffset + playerController.NetworkPlayer.ID, true, false, playerController.NetworkPlayer.ID, null, false, 0, true, true);

        //Expose myself to everyone. 
        foreach (var player in LobbyManager.Instance.Players)
            NetworkedObject.NetworkShowLocal(player.ID);
    }

    public void BroadcastPlayerKilled()
    {
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

    public void BroadcastPlayerReported()
    {
        InvokeServerRpc(serverPlayerReported);
    }
    [ServerRPC(RequireOwnership = false)]
    private void serverPlayerReported()
    {
        if (IsHost && HUD_Vote.Instance.gameObject.activeInHierarchy) return; //We're voting (TODO: Implement on server too)
        if (!playerController.IsAlive) return; //The player has died since pressing report.

        InvokeClientRpcOnEveryone(clientPlayerReported, ExecutingRpcSender);
    }

    [ClientRPC]
    private void clientPlayerReported(ulong clientThatReported)
    {
        HUD_Vote.Instance.Show(clientThatReported);
    }



    public void BroadcastVote(ulong votedId)
    {
        InvokeServerRpc(serverPlayerVoted, votedId);
    }
    [ServerRPC(RequireOwnership = false)]
    private void serverPlayerVoted(ulong voteId)
    {
        InvokeClientRpcOnEveryone(clientPlayerVoted, ExecutingRpcSender, voteId);
    }
    [ClientRPC]
    private void clientPlayerVoted(ulong whoVoted, ulong votedWhat)
    {
        OnPlayerVoted?.Invoke(PlayerManager.Instance.GetPlayerById(whoVoted), PlayerManager.Instance.GetPlayerById(votedWhat));
    }
}