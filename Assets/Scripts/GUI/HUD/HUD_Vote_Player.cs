using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

class HUD_Vote_Player : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI lblPlayerName;
    [SerializeField] private Image imgPlayerSprite, imgPlayerDied, imgPlayerVoted, imgReporter;
    [SerializeField] private Toggle tgglSelectVote;

    [SerializeField] private Image[] imgPlayersVotedForMe;

    public PlayerController TargetPlayer { get; private set; }
    public bool IsSelected => gameObject.activeInHierarchy && tgglSelectVote.isOn;
    public void SetInfo(PlayerController player, bool wasPlayerThatReported = false)
    {
        if (player == null)
        {
            gameObject.SetActive(false);
            return;
        }

        TargetPlayer = player;

        gameObject.SetActive(true);
        lblPlayerName.text = player.NetworkPlayer.Name;
        lblPlayerName.fontStyle = wasPlayerThatReported ? TMPro.FontStyles.Italic : TMPro.FontStyles.Normal;

        imgPlayerSprite.sprite = player.Sprite;
        imgPlayerDied.enabled = !player.IsAlive;

        imgPlayerVoted.enabled = false;
        imgPlayerVoted.enabled = false;
        tgglSelectVote.isOn = false;

        for (int i = 0; i < imgPlayersVotedForMe.Length; i++)
            imgPlayersVotedForMe[i].enabled = false;
        VotingManager.Instance.OnPlayerVoteCasted += onPlayerVoted;
    }


    void OnDisable()
    {
        if (TargetPlayer != null && VotingManager.Instance != null)
            VotingManager.Instance.OnPlayerVoteCasted -= onPlayerVoted;
    }

    private void onPlayerVoted(NetworkPlayer whoVoted, NetworkPlayer whoTheyVotedOn)
    {
        if (whoVoted == TargetPlayer.NetworkPlayer)
            imgPlayerVoted.enabled = true;
    }

    public IEnumerator ShowVoted(IEnumerable<PlayerController> whoVotedOnMe)
    {
        var wait = new WaitForSeconds(.2f);
        yield return wait;

        foreach (var player in whoVotedOnMe)
        {
            imgPlayersVotedForMe[player.PlayerIndex].enabled = true;
            yield return wait;
        }
    }
}