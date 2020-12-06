using System;
using System.Linq;
using System.Collections.Specialized;
using UnityEngine;

class Menu_PlayerList : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI[] lblPlayerNames;

    void OnEnable()
    {
        LobbyManager.Instance.Players.OnListChanged += onPlayersChanged;

        refreshPlayerList();
    }


    void OnDisable()
    {
        LobbyManager.Instance.Players.OnListChanged -= onPlayersChanged;
    }


    private void onPlayersChanged(MLAPI.NetworkedVar.Collections.NetworkedListEvent<NetworkPlayer> changeEvent)
    {
        refreshPlayerList();
    }

    private void refreshPlayerList()
    {
        for (int i = 0; i < lblPlayerNames.Length; i++)
        {
            string name = "Empty";
            if (i < LobbyManager.Instance.Players.Count)
                name = LobbyManager.Instance.Players[i].Name;

            lblPlayerNames[i].SetText(name);

        }
    }
}