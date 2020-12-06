using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

class Menu_CreateLobby : MonoBehaviour
{
    [SerializeField] private HashProvider hashProvider;
    [SerializeField] private TMPro.TMP_InputField inptHash;
    [SerializeField] private TMPro.TextMeshProUGUI lblCurrentImposterCount, lblMovementSpeed;
    [SerializeField] private Slider sldrImposterCount, sldrMovementSpeed;

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

    public void OnStartGameClicked()
    {
        LobbyManager.Instance.StartGame();
    }
}