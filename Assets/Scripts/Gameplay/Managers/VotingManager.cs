using System;
using System.Collections;
using System.Collections.Generic;
using MLAPI;
using MLAPI.Messaging;
using UnityEngine;
using System.Linq;



/*
 * General networking remark:
 * 
 * I dont know a good pattern for this. I want both the client and server to do the same, i called it local
 * 
 * So BroadcastX is a client talking to server
 * serverX is a server-RPC
 * clientX is a client-RPC
 * localX is some shared behaviour?
 * 
 * Validation is also something i want to do on both sides, and is often the same, e.g. CanPlayerVote is the same logic.
 * 
 * Had a discussion on discord, use NetworkedVar instead!
 */
class VotingManager : NetworkedBehaviour
{
    public static VotingManager Instance { get; private set; }

    void OnEnable() => Instance = this;
    void OnDisable() => Instance = null;

    public delegate void VoteStartedDelegate(NetworkPlayer whoReported); //Use a delegate instead of Action to specify a variable name, which reads nicer. 
    public delegate void VoteCastDelegate(NetworkPlayer whoVoted, NetworkPlayer whoTheyVotedOn); //whatvotedon is null when its skip!
    public VoteStartedDelegate OnVotingStarted;
    public Action OnVotingEnded;
    public VoteCastDelegate OnPlayerVoteCasted;

    public bool IsVoting { get; private set; }
    public float VoteStartTime { get; private set; }
    public float VoteEndTime => VoteStartTime + LobbyManager.Instance.GameSettings.Value.VoteDuration;
    public float VoteTimeLeft => VoteEndTime - Time.time;

    /// <summary>
    /// Who voted what, key is who voted, value is what they voted on. If the value is null they voted to skip. If a player is not present in the dictionary, al
    /// </summary>
    private Dictionary<NetworkPlayer, NetworkPlayer> votes = new Dictionary<NetworkPlayer, NetworkPlayer>();


    /// <summary>
    /// For the given player, who did they vote on? Returns null for skip-vote
    /// </summary>
    /// <param name="player">Who voted</param>
    /// <returns>Who they voted on</returns>
    public NetworkPlayer GetPlayerVote(NetworkPlayer player)
    {
        //return the target vote, or null (meaning skip) when the player didnt vote
        return votes.TryGetValue(player, out var votedOnWho) ? votedOnWho : null;
    }

    
    //PlayerToVote is null to skip
    public void BroadcastPlayerVote(NetworkPlayer playerToVoteOn)
    {
        if (!IsVoting || votes.ContainsKey(LobbyManager.Instance.GetLocalPlayer())) return;

        InvokeServerRpc(serverCastVote, playerToVoteOn != null ? playerToVoteOn.ID : ulong.MaxValue);
    }

    [ServerRPC(RequireOwnership = false)]
    private void serverCastVote(ulong whoSenderVotedOn)
    {
        //Cant cast a vote when already voting, or we already voted
        if (!IsVoting || votes.ContainsKey(LobbyManager.Instance.GetPlayerById(ExecutingRpcSender))) return;

        //Maybe check if we didnt vote on a dead player?
        //This will go wrong in dedicated server!

        InvokeClientRpcOnEveryone(clientCastVote, ExecutingRpcSender, whoSenderVotedOn);
    }

    [ClientRPC]
    private void clientCastVote(ulong whoVotedID, ulong whoTheyVotedOnID)
    {
        NetworkPlayer whoVoted = LobbyManager.Instance.GetPlayerById(whoVotedID),
            whoTheyVotedOn = LobbyManager.Instance.GetPlayerById(whoTheyVotedOnID);

        votes[whoVoted] = whoTheyVotedOn;
        OnPlayerVoteCasted?.Invoke(whoVoted, whoTheyVotedOn);
    }


    /// <summary>
    /// The local player has found a body, or pressed the button. Broadcast that we want to start arguing. 
    /// </summary>
    public void BroadcastVoteStart()
    {
        if (IsVoting) return;

        InvokeServerRpc(serverStartVotePeriod);
    }

    [ServerRPC(RequireOwnership = false)]
    private void serverStartVotePeriod()
    {
        if (IsVoting) return; //dont start voting again if we already are. Could happen when two clients report at the same time

        localStartVotePeriod(ExecutingRpcSender);
        InvokeClientRpcOnEveryone(clientStartVotePeriod, ExecutingRpcSender);
    }
    [ClientRPC]
    private void clientStartVotePeriod(ulong reportPlayer)
    {
        //Maybe check if we are already voting? Dont know if thats possible, but we are networking so everything is possible.
        localStartVotePeriod(reportPlayer);
    }
    private void localStartVotePeriod(ulong reportPlayer)
    {
        votes.Clear();
        IsVoting = true;

        //This makes it that every player has a different vote-start-time. The server is earlier than the players, this can cause issues when you want to vote on the last second. The server will already have closed voting, rejecting any incoming votes. 
        //Maybe thats why there is a grace period in Among Us?
        VoteStartTime = Time.time;

        OnVotingStarted?.Invoke(LobbyManager.Instance.GetPlayerById(reportPlayer));

        StartCoroutine(waitForVotePeriodToEnd());
    }

    private IEnumerator waitForVotePeriodToEnd()
    {
        int alivePlayerCount = PlayerManager.Instance.Players.Count(p => p.IsAlive);

        //Wait for the time to run out, or for all alive players to have voted
        while (Time.time < VoteEndTime && votes.Count() < alivePlayerCount) yield return null;

        OnVotingEnded?.Invoke();
        IsVoting = false;
        //We dont want to clear votes here, because the UI still wants to show them even after voting has ended
    }
}
