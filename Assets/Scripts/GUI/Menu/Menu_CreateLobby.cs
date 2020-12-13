using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

class Menu_CreateLobby : MonoBehaviour
{
    [SerializeField] private HashProvider hashProvider;
    [SerializeField] private TMPro.TMP_InputField inptHash;
    [SerializeField] private TMPro.TextMeshProUGUI lblCurrentImposterCount, lblMovementSpeed, lblVoteDuration, lblTaskCount;
    
    void OnEnable()
    {
        inptHash.text = hashProvider.GetHashFromCurrentAddress();
        LobbyManager.Instance.GameSettings.OnValueChanged += onGameSettingsChanged;
        LobbyManager.Instance.CreateLobby();
    }

    private void onGameSettingsChanged(LobbyManager.GameSettingsData previousValue, LobbyManager.GameSettingsData newValue)
    {
        lblCurrentImposterCount.SetText(newValue.ImposterCount.ToString());
        lblMovementSpeed.SetText(newValue.MovementSpeed.ToString());
        lblVoteDuration.SetText(newValue.VoteDuration.ToString());
        lblTaskCount.SetText(newValue.TaskCount.ToString());
    }

    void OnDisable()
    {
        LobbyManager.Instance.GameSettings.OnValueChanged -= onGameSettingsChanged;

        if (!LobbyManager.Instance.IsGameStarted)
            LobbyManager.Instance.LeaveLobby();
    }


    public void OnImposterCountSliderChanged(float newValue)
    {
        var settings = LobbyManager.Instance.GameSettings.Value;
        settings.ImposterCount = Mathf.RoundToInt(newValue);
        LobbyManager.Instance.GameSettings.Value = settings;
    }
    public void OnMovementSpeedChanged(float newValue)
    {
        var settings = LobbyManager.Instance.GameSettings.Value;
        settings.MovementSpeed = Mathf.RoundToInt(newValue);
        LobbyManager.Instance.GameSettings.Value = settings;
    }

    public void OnVoteDurationChanged(float newValue)
    {
        var settings = LobbyManager.Instance.GameSettings.Value;
        settings.VoteDuration = Mathf.RoundToInt(newValue);
        LobbyManager.Instance.GameSettings.Value = settings;
    }
    public void OnTaskCountChanged(float newValue)
    {
        var settings = LobbyManager.Instance.GameSettings.Value;
        settings.TaskCount = Mathf.RoundToInt(newValue);
        LobbyManager.Instance.GameSettings.Value = settings;
    }
    public void OnStartGameClicked()
    {
        LobbyManager.Instance.StartGame();
    }
}