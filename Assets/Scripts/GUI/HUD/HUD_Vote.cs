using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;

class HUD_Vote : MonoBehaviour
{
    public static HUD_Vote Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMPro.TextMeshProUGUI[] lblPlayerNames;
    [SerializeField] private Image[] imgPlayerSprites, imgPlayerDead;
    [SerializeField] private ToggleGroup tgglGroup;

    void Awake()
    {
        canvasGroup.alpha = 1f;
        Instance = this;
        gameObject.SetActive(false);

        PlayerNetworkController.OnPlayerVoted += onPlayerVoted;
    }

    void OnDestroy()
    {
        PlayerNetworkController.OnPlayerVoted -= onPlayerVoted;
        Instance = null;
    }


    public void OnVoteClicked()
    {
        int selectedToggle = tgglGroup.ActiveToggles().IndexOf(tgglGroup.GetFirstActiveToggle());

        //Max for skip
        ulong votedId = selectedToggle > PlayerManager.Instance.Players.Length ? ulong.MaxValue : (ulong)selectedToggle;


        foreach (var tgl in tgglGroup.ActiveToggles())
            tgl.interactable = false;

    }

    internal void Show(ulong clientThatReported)
    {
        gameObject.SetActive(true);
        int index = 0;

        foreach (var player in PlayerManager.Instance.Players.OrderBy(p => p.IsAlive))
        {
            lblPlayerNames[index].SetText(player.NetworkPlayer.Name);
            imgPlayerSprites[index].sprite = player.Sprite;
            imgPlayerDead[index].enabled = !player.IsAlive;

            lblPlayerNames[index].fontStyle = player.NetworkPlayer.ID == clientThatReported ? TMPro.FontStyles.Italic : TMPro.FontStyles.Normal;

            index++;
        }

        for (; index < lblPlayerNames.Length; index++)
            lblPlayerNames[index].transform.parent.gameObject.SetActive(false);

        foreach (var tgl in tgglGroup.ActiveToggles())
            tgl.interactable = true;
    }
    private void onPlayerVoted(PlayerController whoVoted, PlayerController votedWhat)
    {
        int whoPlayerRowIndex = lblPlayerNames.IndexOf(lbl => lbl.text == whoVoted.NetworkPlayer.Name);

    }
}
