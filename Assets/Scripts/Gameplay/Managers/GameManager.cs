using System.Linq;
using MLAPI;
using MLAPI.Messaging;

class GameManager : NetworkedBehaviour
{
    public static GameManager Instance { get; private set; }
    void OnEnable()
    {
        Instance = this;
    }
    void OnDisable()
    {
        Instance = null;
    }

    public int TotalTaskCount { get; private set; } = 100;
    public int CompletedTaskCount { get; private set; }


    public void ServerCheckWinConditions()
    {
        if (!IsServer)
            throw new System.Exception("This method can only be called on the server!");

        int aliveImposterCount = 0, aliveCivilianCount = 0;
        foreach (var p in PlayerManager.Instance.Players)
        {
            if (!p.IsAlive) continue;

            if (LobbyManager.Instance.ServerImposters.Contains(p.NetworkPlayer))
                aliveImposterCount++;
            else
                aliveCivilianCount++;
        }

        if (aliveCivilianCount <= aliveImposterCount)
            InvokeClientRpcOnEveryone(onGameEnded, true, LobbyManager.Instance.ServerImposters.Select(u => u.ID).ToArray());
        else if (aliveImposterCount <= 0 || CompletedTaskCount >= TotalTaskCount)
            InvokeClientRpcOnEveryone(onGameEnded, false, LobbyManager.Instance.ServerImposters.Select(u => u.ID).ToArray());
    }
    [ClientRPC]
    private void onGameEnded(bool wonByImposters, ulong[] imposters) => HUD_GameOver.Instance.ShowResult(wonByImposters, imposters);
}