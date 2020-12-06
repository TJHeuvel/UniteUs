using System;
using MLAPI;
using UnityEngine;

class Menu_JoinLobby : MonoBehaviour
{
    [SerializeField] private HashProvider hashProvider;
    [SerializeField] private RectTransform parentJoin;
    [SerializeField] private TMPro.TMP_InputField inptJoinHash;

    [SerializeField] private RectTransform parentLobby;
    void OnEnable()
    {
        parentJoin.gameObject.SetActive(true);
        parentLobby.gameObject.SetActive(false);

        LobbyManager.Instance.OnLobbyJoined += onLobbyJoined;
    }
    void OnDisable()
    {
        LobbyManager.Instance.OnLobbyJoined -= onLobbyJoined;

        if (!LobbyManager.Instance.IsGameStarted)
            LobbyManager.Instance.LeaveLobby();
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