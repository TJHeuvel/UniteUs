using UnityEngine;
using UnityEngine.UI;

class HUD_Interaction : MonoBehaviour
{
    [SerializeField] private Button btnUse, btnKill, btnReport;

    public void SetUseButtonVisiblity(bool visible) => btnUse.gameObject.SetActive(visible);
    public void SetKillButtonVisibility(bool visible) => btnKill.gameObject.SetActive(visible);
    public void SetReportButtonVisibility(bool visible) => btnReport.gameObject.SetActive(visible);

    public void OnUseButtonClicked()
    {

    }
    public void OnKillButtonClicked()
    {

    }
    public void OnReportButtonClicked()
    {

    }
}
