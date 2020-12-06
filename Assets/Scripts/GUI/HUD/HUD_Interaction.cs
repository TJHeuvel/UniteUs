using System;
using UnityEngine;
using UnityEngine.UI;

class HUD_Interaction : MonoBehaviour
{
    [SerializeField] private Button btnUse, btnKill, btnReport;

    void OnEnable()
    {
        PlayerInteractInTrigger.OnTriggerEnter += onEnteredTrigger;
        PlayerInteractInTrigger.OnTriggerExit += onExitedTrigger;
    }
    void OnDisable()
    {
        PlayerInteractInTrigger.OnTriggerEnter -= onEnteredTrigger;
        PlayerInteractInTrigger.OnTriggerExit -= onExitedTrigger;
    }

    private void onExitedTrigger(PlayerInteractInTrigger obj)
    {
        if (obj.Interaction == InteractionType.Use)
            SetUseButtonEnabled(false);
        else if (obj.Interaction == InteractionType.Kill)
            SetKillButtonEnabled(false);
        else if (obj.Interaction == InteractionType.Report)
            SetReportButtonEnabled(false);
    }

    private void onEnteredTrigger(PlayerInteractInTrigger obj)
    {
        if (obj.Interaction == InteractionType.Use)
            SetUseButtonEnabled(true);
        else if (obj.Interaction == InteractionType.Kill)
            SetKillButtonEnabled(true);
        else if (obj.Interaction == InteractionType.Report)
            SetReportButtonEnabled(true);
    }

    void Start()
    {
        SetUseButtonVisiblity(PlayerSpawnManager.Instance.LocalPlayer.Role == PlayerRole.Civilian);
        SetKillButtonVisibility(PlayerSpawnManager.Instance.LocalPlayer.Role == PlayerRole.Imposter);
        SetUseButtonEnabled(false);
        SetKillButtonEnabled(false);
        SetReportButtonEnabled(false);
    }

    public void SetUseButtonVisiblity(bool visible) => btnUse.gameObject.SetActive(visible);
    public void SetKillButtonVisibility(bool visible) => btnKill.gameObject.SetActive(visible);
    public void SetReportButtonVisibility(bool visible) => btnReport.gameObject.SetActive(visible);

    public void SetKillButtonEnabled(bool enabled) => btnKill.interactable = enabled;
    public void SetUseButtonEnabled(bool enabled) => btnUse.interactable = enabled;
    public void SetReportButtonEnabled(bool enabled) => btnReport.interactable = enabled;

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
