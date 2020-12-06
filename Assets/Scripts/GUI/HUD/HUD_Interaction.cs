using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

class HUD_Interaction : MonoBehaviour
{
    [SerializeField] private Button btnUse, btnKill, btnReport;
    private List<PlayerInteractInTrigger> triggers = new List<PlayerInteractInTrigger>();


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
    void Start()
    {
        SetUseButtonVisiblity(PlayerManager.Instance.LocalPlayer.Role == PlayerRole.Civilian);
        SetKillButtonVisibility(PlayerManager.Instance.LocalPlayer.Role == PlayerRole.Imposter);
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


    //todo: get closest to current player?
    private PlayerInteractInTrigger getTargetOfType(InteractionType type) =>  triggers.FirstOrDefault(t => t.Interaction == type);

    public void OnUseButtonClicked()
    {

    }
    public void OnKillButtonClicked()
    {
        var killTarget = getTargetOfType(InteractionType.Kill).GetComponent<PlayerController>();

        killTarget.NetworkController.BroadcastPlayerKilled();

        SetKillButtonEnabled(false);
    }
    public void OnReportButtonClicked()
    {        
        PlayerManager.Instance.LocalPlayer.NetworkController.BroadcastPlayerReported();
        
        getTargetOfType(InteractionType.Report).enabled = false; //Players can only use the button once
    }


    private void onExitedTrigger(PlayerInteractInTrigger obj)
    {
        triggers.Remove(obj);

        SetUseButtonEnabled(triggers.Any(t => t.Interaction == InteractionType.Use));
        SetKillButtonEnabled(triggers.Any(t => t.Interaction == InteractionType.Kill));
        SetReportButtonEnabled(triggers.Any(t => t.Interaction == InteractionType.Report));
    }

    private void onEnteredTrigger(PlayerInteractInTrigger obj)
    {
        triggers.Add(obj);

        SetUseButtonEnabled(triggers.Any(t => t.Interaction == InteractionType.Use));
        SetKillButtonEnabled(triggers.Any(t => t.Interaction == InteractionType.Kill));
        SetReportButtonEnabled(triggers.Any(t => t.Interaction == InteractionType.Report));
    }

}
