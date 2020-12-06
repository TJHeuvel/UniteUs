using UnityEngine;
using UnityEngine.UI;

class HUD_Task : MonoBehaviour
{
    public static HUD_Task Instance { get; private set; }
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMPro.TextMeshProUGUI lblTaskContent;
    [SerializeField] private Button btnAnswer;

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
}
