using MLAPI;
using UnityEngine;

//TODO: Someone, maybe me, should start a lobby when its a scene-start
class PlayerSpawnManager : NetworkedBehaviour
{
    [SerializeField] private PlayerController localPlayerPrefab,
                            otherPlayerPrefab;
    [SerializeField] private Cinemachine.CinemachineVirtualCamera virtualCam;

    public PlayerController[] Players { get; private set; }
    
    public override void NetworkStart()
    {
        base.NetworkStart();

        Players = new PlayerController[LobbyManager.Instance.Players.Count];

        for (int i = 0; i < Players.Length; i++)
        {
            var networkPlayer = LobbyManager.Instance.Players[i];

            //TODO: Position them nicely. Since the index is guaranteed to be the same we can spawn people around
            Players[i] = Instantiate(networkPlayer.IsLocal ? localPlayerPrefab : otherPlayerPrefab);
            Players[i].SetPlayer(networkPlayer, i);

            if (networkPlayer.IsLocal)
                virtualCam.Follow = Players[i].transform;
        }
    }
}