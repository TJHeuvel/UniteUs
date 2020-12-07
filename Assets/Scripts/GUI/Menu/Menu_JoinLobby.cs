using UnityEngine;

class Menu_JoinLobby : MonoBehaviour
{
    [SerializeField] private HashProvider hashProvider;
    [SerializeField] private RectTransform parentJoin, parentLobby;
    [SerializeField] private TMPro.TMP_InputField inptJoinHash;
    [SerializeField] private TMPro.TextMeshProUGUI lblGameSettings;

    void OnEnable()
    {
        parentJoin.gameObject.SetActive(true);
        parentLobby.gameObject.SetActive(false);

        LobbyManager.Instance.GameSettings.OnValueChanged += onGameSettingsChanged;
        LobbyManager.Instance.OnLobbyJoined += onLobbyJoined;
    }

    void OnDisable()
    {
        LobbyManager.Instance.OnLobbyJoined -= onLobbyJoined;
        LobbyManager.Instance.GameSettings.OnValueChanged -= onGameSettingsChanged;

        if (!LobbyManager.Instance.IsGameStarted)
            LobbyManager.Instance.LeaveLobby();
    }

    private void onGameSettingsChanged(LobbyManager.GameSettingsData previousValue, LobbyManager.GameSettingsData newValue)
    {
        lblGameSettings.SetText(newValue.ToString());
    }
    private void onLobbyJoined()
    {
        parentJoin.gameObject.SetActive(false);
        parentLobby.gameObject.SetActive(true);
    }

    public void OnJoinClicked()
    {
        LobbyManager.Instance.JoinLobby(hashProvider.GetAddressFromHash(inptJoinHash.text));
    }
    public void OnJoinLocalClicked()
    {
        LobbyManager.Instance.JoinLobby("127.0.0.1");
    }

}