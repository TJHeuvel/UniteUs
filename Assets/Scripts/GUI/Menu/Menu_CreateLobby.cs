using System;
using UnityEngine;

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
        LobbyManager.Instance.LeaveLobby();
    }
}