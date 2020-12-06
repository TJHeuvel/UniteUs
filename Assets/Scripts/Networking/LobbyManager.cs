using UnityEngine;
using MLAPI;
using MLAPI.Transports.UNET;
using System;
using System.Threading.Tasks;
using MLAPI.NetworkedVar.Collections;
using MLAPI.Messaging;
using UnityEngine.SceneManagement;

class LobbyManager : NetworkedBehaviour
{
    public event Action OnLobbyJoined, OnLobbyLeft;
    public bool IsInLobby => NetworkingManager.Singleton.IsListening;
    public bool IsGameStarted { get; private set; }

    public NetworkedList<NetworkPlayer> Players = new NetworkedList<NetworkPlayer>(
          new MLAPI.NetworkedVar.NetworkedVarSettings()
          {
              ReadPermission = MLAPI.NetworkedVar.NetworkedVarPermission.Everyone,
              WritePermission = MLAPI.NetworkedVar.NetworkedVarPermission.Everyone,
              SendTickrate = 5
          });

    public void StartGame()
    {
        IsGameStarted = true;
        if (IsServer)
            InvokeClientRpcOnEveryone(clientStartGame);
    }
    [ClientRPC]
    private void clientStartGame()
    {
        IsGameStarted = true;
        SceneManager.LoadScene("game");
    }


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

        if (NetworkingManager.Singleton.IsClient)
            NetworkingManager.Singleton.StopClient();
        else if (NetworkingManager.Singleton.IsHost)
        {
            NetworkingManager.Singleton.OnServerStarted -= onServerStarted;

            NetworkingManager.Singleton.StopHost();
        }
        NetworkingManager.Singleton.OnClientConnectedCallback -= onConnected;
        NetworkingManager.Singleton.OnClientDisconnectCallback -= onDisconnected;

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