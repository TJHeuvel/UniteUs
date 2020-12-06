using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Text;
using UnityEngine.SceneManagement;

class HUD_GameOver : MonoBehaviour
{
    public static HUD_GameOver Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMPro.TextMeshProUGUI lblResult;

    void Awake()
    {
        canvasGroup.alpha = 1f;
        Instance = this;
        gameObject.SetActive(false);
    }
    void OnDestroy()
    {
        Instance = null;
    }

    public void ShowResult(bool impostersWon, ulong[] imposterIds)
    {
        gameObject.SetActive(true);

        StringBuilder sb = new StringBuilder();
        if (impostersWon)
            sb.Append("The imposters won the game!\n");
        else
            sb.Append("The civilians won the game!\n");

        sb.Append("The imposters were:");

        foreach (var uid in imposterIds)
            sb.Append($"{LobbyManager.Instance.GetPlayerById(uid).Name}, ");

        lblResult.SetText(sb);
    }
    public void OnBackClicked()
    {
        LobbyManager.Instance.LeaveLobby();
        SceneManager.LoadScene("menu");
    }
}
