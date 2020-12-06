using System;
using UnityEngine;
using UnityEngine.SceneManagement;

class Menu_CreateLobby : MonoBehaviour
{
    [SerializeField] private HashProvider hashProvider;
    [SerializeField] private TMPro.TMP_InputField inptHash;

    void OnEnable()
    {
        inptHash.text = hashProvider.GetHashFromCurrentAddress();
        LobbyManager.Instance.CreateLobby();
    }


    void OnDisable()
    {
        if (!LobbyManager.Instance.IsGameStarted)
            LobbyManager.Instance.LeaveLobby();
    }

    public void OnStartGameClicked()
    {
        LobbyManager.Instance.StartGame();
    }
}