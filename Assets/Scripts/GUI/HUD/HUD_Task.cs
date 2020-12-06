using UnityEngine;
using UnityEngine.UI;

class HUD_Task : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMPro.TextMeshProUGUI lblTaskContent;
    [SerializeField] private Button btnAnswer;

    void Awake()
    {
        canvasGroup.alpha = 0f;
    }
}
