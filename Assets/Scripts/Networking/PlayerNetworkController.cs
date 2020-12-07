using System;
using System.IO;
using MLAPI;
using MLAPI.Messaging;
using UnityEngine;

class PlayerNetworkController : NetworkedBehaviour
{
    public Action<PlayerController> OnPlayerVoted; //WhoVoted, VotedOn
    public static Action<PlayerController> OnPlayerReported; //make up your mind if events are static or not!

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

        InvokeClientRpcOnEveryone(clientPlayerReported);
    }
    [ClientRPC]
    private void clientPlayerReported()
    {
        OnPlayerReported?.Invoke(playerController);

        //Clear current votes
        foreach (var p in PlayerManager.Instance.Players)
            p.NetworkController.PlayerVotedOn = null;
    }


    //todo: Validate a player can only vote once per round, locally and on the server. and its within vote period A bit tricky, playervotedon is null when skipped too
    /// <summary>
    /// Make a player vote on someone. ulong.maxvalue for skip
    /// </summary>
    /// <param name="whoToVoteOn"></param>
    public void BroadcastVote(ulong whoToVoteOn)
    {
        if (playerController.IsAlive)
            InvokeServerRpc(serverPlayerVoted, whoToVoteOn);
    }
    [ServerRPC(RequireOwnership = false)]
    private void serverPlayerVoted(ulong whoToVoteOn)
    {
        if (PlayerManager.Instance.GetPlayerById(ExecutingRpcSender).IsAlive)
            InvokeClientRpcOnEveryone(clientPlayerVoted, whoToVoteOn);
    }
    [ClientRPC]
    private void clientPlayerVoted(ulong votedOn)
    {
        PlayerVotedOn = PlayerManager.Instance.GetPlayerById(votedOn);
        OnPlayerVoted?.Invoke(PlayerVotedOn);
    }
    public PlayerController PlayerVotedOn { get; private set; } //todo: clear
}