using Riptide;
using Riptide.Utils;
using System;
using UnityEngine;

public enum ServerToClientId : ushort
{
    sync = 1,
    playerSpawned,
    playerMovement,
    activeScene,
}

public enum ClientToServerId : ushort
{
    name = 1,
    input,
}

public class NetworkManager : MonoBehaviour
{
    #region Singleton Pattern
    private static NetworkManager _singleton;
    public static NetworkManager Instance
    {
        get => _singleton;
        set
        {
            if (_singleton == null)
                _singleton = value;
            else if (_singleton != value)
            {
                Debug.Log($"{nameof(NetworkManager)} instance already exists, destroying duplicate !");
                Destroy(value);
            }
        }
    }
    #endregion

    public Client Client { get; private set; }

    private ushort _serverTick;
    public ushort ServerTick
    {
        get => _serverTick;
        set
        {
            _serverTick = value;
            InterpolationTick = (ushort)(value - TicksBetweenPositionUpdates);
        }
    }
    public ushort InterpolationTick { get; private set; }
    private ushort _ticksBetweenPositionUpdates = 2;
    public ushort TicksBetweenPositionUpdates
    {
        get => _ticksBetweenPositionUpdates;
        private set
        {
            _ticksBetweenPositionUpdates = value;
            InterpolationTick = (ushort)(ServerTick - value);
        }
    }

    [SerializeField] private string ip = "127.0.0.1";
    [SerializeField] private ushort port = 7777;
    [SerializeField] private ushort tickDivergenceTolerance = 1;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(Instance);
    }

    public void Initialize()
    {
        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

        Client = new Client();
        Client.Connected += DidConnect;
        Client.ConnectionFailed += FailedToConnect;
        Client.ClientDisconnected += PlayerLeft;
        Client.Disconnected += DidDisconnect;

        ServerTick = TicksBetweenPositionUpdates;
    }

    public void StopManager()
    {
        Client.Disconnect();
        _singleton = null;
    }

    public void Connect()
    {
        Client.Connect($"{ip}:{port}");
    }

    private void DidConnect(object sender, EventArgs e)
    {
        UIManager.Instance.SendName();
        MainEntry.Instance.GoToNextFlow(SceneState.Game);
    }

    private void FailedToConnect(object sender, EventArgs e)
    {
        UIManager.Instance.BackToMain();
    }

    private void PlayerLeft(object sender, ClientDisconnectedEventArgs e)
    {
        if(Player.list.TryGetValue(e.Id, out Player player))
            Destroy(player.gameObject);
    }

    private void DidDisconnect(object sender, EventArgs e)
    {
        UIManager.Instance.BackToMain();
        foreach (Player player in Player.list.Values)
            Destroy(player.gameObject);

        MainEntry.Instance.GoToNextFlow(SceneState.Menu);
    }

    private void SetTick(ushort serverTick)
    {
        if (Mathf.Abs(ServerTick - serverTick) > tickDivergenceTolerance)
        {
            Debug.Log($"Client tick: {ServerTick} -> {serverTick}");
            ServerTick = serverTick;
        }
    }

    [MessageHandler((ushort)ServerToClientId.sync)]
    public static void Sync(Message message)
    {
        Instance.SetTick(message.GetUShort());
    }
}