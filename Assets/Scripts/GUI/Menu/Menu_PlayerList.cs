using System;
using System.Linq;
using System.Collections.Specialized;
using UnityEngine;

class Menu_PlayerList : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI[] lblPlayerNames;

    void OnEnable()
    {
        LobbyManager.Instance.OnPlayerAdded += onPlayerChanged;
        LobbyManager.Instance.OnPlayerRemoved += onPlayerChanged;
        refreshPlayerList();
    }

    void OnDisable()
    {
        LobbyManager.Instance.OnPlayerAdded -= onPlayerChanged;
        LobbyManager.Instance.OnPlayerRemoved -= onPlayerChanged;
    }

    private void onPlayerChanged(IPlayer obj)
    {
        refreshPlayerList();
    }
    private void refreshPlayerList()
    {
        for (int i = 0; i < lblPlayerNames.Length; i++)
        {
            string name = "Empty";
            if(i < LobbyManager.Instance.Players.Count)
                name = LobbyManager.Instance.Players.ElementAt(i).Value.Name;

            lblPlayerNames[i].SetText(name);   

        }
    }
}