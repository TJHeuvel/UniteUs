using MLAPI;
using UnityEngine;
using System.Linq;

class PlayerSpawnManager : NetworkedBehaviour
{
    const float TAU = Mathf.PI * 2;
    public static PlayerSpawnManager Instance { get; private set; }
    [SerializeField]
    private PlayerController localPlayerPrefab,
                            otherPlayerPrefab;
    [SerializeField] private Cinemachine.CinemachineVirtualCamera virtualCam;
    [SerializeField] private float spawnRadius = 10f;

#if UNITY_EDITOR
    [SerializeField] private bool dbgIsImposter;
#endif

    void OnEnable()
    {
        Instance = this;

#if UNITY_EDITOR

        //No lobby, must be a direct scene start. Attempt to make some sort of environment
        if (LobbyManager.Instance == null)
        {
            //i should probably just start a host
            gameObject.AddComponent<NetworkingManager>();
            gameObject.AddComponent<LobbyManager>();

            LocalPlayer = Instantiate(localPlayerPrefab);
            LocalPlayer.SetPlayer(new NetworkPlayer()
            {
                ID = 0,
                Name = "EditorPlayer",
                Role = dbgIsImposter ? PlayerRole.Imposter : PlayerRole.Civilian
            }, 0);
            virtualCam.Follow = LocalPlayer.transform;

        }
#endif
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
    public PlayerController LocalPlayer { get; private set; }

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
            {
                virtualCam.Follow = Players[i].transform;
                LocalPlayer = Players[i];
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}