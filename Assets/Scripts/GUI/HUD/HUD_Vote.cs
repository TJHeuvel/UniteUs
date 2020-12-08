using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

class HUD_Vote : MonoBehaviour
{
    public static HUD_Vote Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private ToggleGroup tgglGroup;
    [SerializeField] private Button btnVote;
    [SerializeField] private TMPro.TextMeshProUGUI lblTimeLeft;

    [SerializeField] private HUD_Vote_Player[] playersRows;
    [SerializeField] private Image[] imgSkipVotes;

    void OnValidate()
    {
        playersRows = GetComponentsInChildren<HUD_Vote_Player>();
    }

    void Awake()
    {
        canvasGroup.alpha = 1f;
        Instance = this;        
    }

    void Start()
    {
        VotingManager.Instance.OnVotingStarted += Show;
        VotingManager.Instance.OnVotingEnded += onVotePeriodEnded;
        gameObject.SetActive(false);        
    }

    void OnDestroy()
    {
        if (VotingManager.Instance == null) return;
        VotingManager.Instance.OnVotingStarted -= Show;
        VotingManager.Instance.OnVotingEnded -= onVotePeriodEnded;
        Instance = null;
    }


    void Update()
    {        
        lblTimeLeft.text = $"Time left: {VotingManager.Instance.VoteTimeLeft:0}s";
    }
    public void OnVoteClicked()
    {
        int selectedToggle = tgglGroup.ActiveToggles().IndexOf(tgglGroup.GetFirstActiveToggle());

        //Null when skipped
        NetworkPlayer votedOn = selectedToggle > playersRows.Length ? null : playersRows[selectedToggle].TargetPlayer.NetworkPlayer;
        
        VotingManager.Instance.BroadcastPlayerVote(votedOn);        

        foreach (var tgl in tgglGroup.ActiveToggles())
            tgl.interactable = false;
        btnVote.interactable = false;
    }

    public void Show(NetworkPlayer clientThatReported)
    {
        gameObject.SetActive(true);
        int index = 0;
        btnVote.interactable = true;

        foreach (var player in PlayerManager.Instance.Players.OrderByDescending(p => p.IsAlive))
            playersRows[index++].SetInfo(player, player.NetworkPlayer == clientThatReported);

        for (; index < playersRows.Length; index++)
            playersRows[index].SetInfo(null);

        foreach (var tgl in tgglGroup.ActiveToggles())
            tgl.interactable = true;

        for (int i = 0; i < imgSkipVotes.Length; i++)
            imgSkipVotes[i].enabled = false;

    }

    private void onVotePeriodEnded()
    {        
        btnVote.interactable = false;

        foreach(var row in playersRows.Where(p => p.TargetPlayer != null))
        {
            //Find all players that voted on the player that the row is showing.
            //A LINQ Group is probably better suited
            StartCoroutine(row.ShowVoted(PlayerManager.Instance.Players.Where(p => VotingManager.Instance.GetPlayerVote(p.NetworkPlayer) == row.TargetPlayer.NetworkPlayer)));
        }

        StartCoroutine(showVoteSkipped(PlayerManager.Instance.Players.Where(p => VotingManager.Instance.GetPlayerVote(p.NetworkPlayer) == null)));

        StartCoroutine(waitAndClose());
    }

    private IEnumerator waitAndClose()
    {
        yield return new WaitForSeconds(5f);
        gameObject.SetActive(false);
    }

    private IEnumerator showVoteSkipped(IEnumerable<PlayerController> whoVotedSkipped)
    {
        var wait = new WaitForSeconds(.2f);
        yield return wait;

        foreach (var player in whoVotedSkipped)
        {
            imgSkipVotes[player.PlayerIndex].enabled = true;
            yield return wait;
        }
    }
}
