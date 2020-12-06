using UnityEngine;
using MLAPI;
using MLAPI.Transports.UNET;
using System;
using System.Threading.Tasks;
using MLAPI.NetworkedVar.Collections;
using MLAPI.Messaging;
using UnityEngine.SceneManagement;
using MLAPI.NetworkedVar;
using MLAPI.Serialization;
using System.IO;
using MLAPI.Serialization.Pooled;
using System.Linq;

//TODO: Reject players after the game started
//TODO: Properly handle disconnceting players also ingame
class LobbyManager : NetworkedBehaviour
{
    public event Action OnLobbyJoined, OnLobbyLeft;
    public bool IsInLobby => NetworkingManager.Singleton.IsListening;
    public bool IsGameStarted { get; private set; }

    public NetworkedList<NetworkPlayer> Players = new NetworkedList<NetworkPlayer>(
          new NetworkedVarSettings()
          {
              ReadPermission = NetworkedVarPermission.Everyone,
              WritePermission = NetworkedVarPermission.Everyone
          });
    public NetworkPlayer GetPlayerById(ulong playerId)
    {
        for (int i = 0; i < Players.Count; i++)
            if (Players[i].ID == playerId) return Players[i];
        return null;
    }
    public NetworkPlayer GetLocalPlayer() => GetPlayerById(NetworkingManager.Singleton.LocalClientId);

    [System.Serializable]
    public struct GameSettingsData : IBitWritable
    {
        public static GameSettingsData Default => new GameSettingsData() { ImposterCount = 1, MovementSpeed = 6 };
        public int ImposterCount;
        public float MovementSpeed;

        public void Read(Stream stream)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                ImposterCount = reader.ReadByte();
                MovementSpeed = reader.ReadSingle();
            }
        }
        public void Write(Stream stream)
        {
            using (PooledBitWriter writer = PooledBitWriter.Get(stream))
            {
                writer.WriteByte((byte)ImposterCount);
                writer.WriteSingle(MovementSpeed);
            }

        }
    }

    public NetworkedVar<GameSettingsData> GameSettings = new NetworkedVar<GameSettingsData>(new NetworkedVarSettings()
    {

        ReadPermission = NetworkedVarPermission.Everyone,
        WritePermission = NetworkedVarPermission.Everyone //todo: figure out if we can get the host only?

    }, GameSettingsData.Default);


    public static LobbyManager Instance { get; private set; }

    void OnEnable()
    {
        Instance = this;
        DontDestroyOnLoad(this);
    }
    async void OnDisable()
    {
        LeaveLobby();
        await System.Threading.Tasks.Task.Yield(); //wait one frame, then destroy. Solves people doing things in OnDestroy with us
        Instance = null;
    }
    void Update()
    {
        if (NetworkingManager.Singleton.IsListening)
            VarUpdate();
    }

    #region Lobby Creating and Leaving

    public void CreateLobby()
    {
        NetworkingManager.Singleton.OnClientConnectedCallback += onConnected;
        NetworkingManager.Singleton.OnClientDisconnectCallback += onDisconnected;
        NetworkingManager.Singleton.OnServerStarted += onServerStarted;

        NetworkingManager.Singleton.StartHost();
    }

    public async void JoinLobby(string address)
    {
        if (NetworkingManager.Singleton.TryGetComponent<UnetTransport>(out var transport))
            transport.ConnectAddress = address;
        
        NetworkingManager.Singleton.OnClientConnectedCallback += onConnected;
        NetworkingManager.Singleton.OnClientDisconnectCallback += onDisconnected;
        var tasks = NetworkingManager.Singleton.StartClient();

        while (!tasks.IsDone) await Task.Yield();
        //todo: error checking
        OnLobbyJoined?.Invoke();
    }

    public void LeaveLobby()
    {
        if (NetworkingManager.Singleton == null) return;

        Players.Clear();

        if (NetworkingManager.Singleton.IsHost)
        {
            NetworkingManager.Singleton.OnServerStarted -= onServerStarted;
            NetworkingManager.Singleton.StopHost();
        }
        else if (NetworkingManager.Singleton.IsClient)
        {
            NetworkingManager.Singleton.StopClient();
        }
                
        NetworkingManager.Singleton.OnClientConnectedCallback -= onConnected;
        NetworkingManager.Singleton.OnClientDisconnectCallback -= onDisconnected;

    }
    public void StartGame()
    {
        IsGameStarted = true;
        if (IsServer)
        {
            /*
             * First i tried setting the role when the game has started.
             * I had issues with the order of things, first i spawned a player and then tried to send a role to them.
             * Because of object visibility this wouldnt work, the object wasnt visible to other players yet. 
             *
             * This is also nice because we can assume that we know the roles when we load into the new scene
             */

            var imposters = Players.OrderBy(p => UnityEngine.Random.value).Take(GameSettings.Value.ImposterCount).ToArray();

            foreach (var player in Players)
            {
                if (imposters.Contains(player))
                    InvokeClientRpcOnClient(clientStartGameImposter, player.ID, imposters.Where(i => i != player).Select(p => p.ID).ToArray());
                else
                    InvokeClientRpcOnClient(clientStartGameCivilian, player.ID);
            }
        }
    }
    #endregion

    #region Callbacks
    private void onServerStarted()
    {
        onConnected(NetworkingManager.Singleton.LocalClientId);
        OnLobbyJoined?.Invoke();
    }

    private void onConnected(ulong id)
    {
        if (id == NetworkingManager.Singleton.LocalClientId)
        {
            string userName = SystemInfo.deviceName;
            if (Application.isEditor)
                userName += " (Editor)";

            Players.Add(new NetworkPlayer()
            {
                ID = id,
                Name = userName
            });
        }
    }

    private void onDisconnected(ulong id)
    {
        int index = 0;
        for (; index < Players.Count; index++)
            if (Players[index].ID == id)
                break;
        if (index < Players.Count) //make sure we know the player
            Players.RemoveAt(index);
    }

    #endregion

    #region RPC
    [ClientRPC]
    private void clientStartGameCivilian()
    {
        GetLocalPlayer().Role = PlayerRole.Civilian;
        IsGameStarted = true;
        SceneManager.LoadScene("game");
    }
    [ClientRPC]
    private void clientStartGameImposter(ulong[] otherImposters)
    {
        GetLocalPlayer().Role = PlayerRole.Imposter;
        foreach (var id in otherImposters)
            GetPlayerById(id).Role = PlayerRole.Imposter;

        IsGameStarted = true;
        SceneManager.LoadScene("game");
    }

    #endregion
#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(LobbyManager))]
    class LobbyManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var target = base.target as LobbyManager;
            if (Application.isPlaying)
                foreach (var p in target.Players)
                    GUILayout.Label(p.ToString());
        }
    }
#endif
}