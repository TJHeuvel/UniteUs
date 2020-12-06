using MLAPI;
using UnityEngine;
using System.Linq;

//TODO: Someone, maybe me, should start a lobby when its a scene-start
class PlayerSpawnManager : NetworkedBehaviour
{
    public static PlayerSpawnManager Instance { get; private set; }
    [SerializeField]
    private PlayerController localPlayerPrefab,
                            otherPlayerPrefab;
    [SerializeField] private Cinemachine.CinemachineVirtualCamera virtualCam;
    [SerializeField] private float spawnRadius = 10f;

    const float TAU = Mathf.PI * 2;

    void OnEnable()
    {
        Instance = this;
    }
    void OnDisable()
    {
        Instance = null;
    }

    public PlayerController[] Players { get; private set; }

    public PlayerController GetPlayerById(ulong playerId)
    {
        for (int i = 0; i < Players.Length; i++)
            if (Players[i].NetworkPlayer.ID == playerId) return Players[i];
        return null;
    }

    public override void NetworkStart()
    {
        base.NetworkStart();

        Players = new PlayerController[LobbyManager.Instance.Players.Count];

        for (int i = 0; i < Players.Length; i++)
        {
            var networkPlayer = LobbyManager.Instance.Players[i];

            //TODO: Position them nicely. Since the index is guaranteed to be the same we can spawn people around
            Vector3 position = transform.position;
            float radius = (float)i / Players.Length * TAU;
            position.x += Mathf.Sin(radius) * spawnRadius;
            position.x += Mathf.Cos(radius) * spawnRadius;


            Players[i] = Instantiate(networkPlayer.IsLocal ? localPlayerPrefab : otherPlayerPrefab, position, Quaternion.identity);
            Players[i].SetPlayer(networkPlayer, i);

            if (networkPlayer.IsLocal)
                virtualCam.Follow = Players[i].transform;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}