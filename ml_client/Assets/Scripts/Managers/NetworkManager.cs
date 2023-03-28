using Riptide;
using Riptide.Utils;
using System;
using UnityEngine;

public enum ServerToClientId : ushort
{
    sync = 1,
    playerSpawned,
    playerMovement,
    doorOpened,
    initData
}

public enum ClientToServerId : ushort
{
    loginInfo = 1,
    input,
    doorOpened
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

    [SerializeField] private string default_ip = "127.0.0.1";
    [SerializeField] private ushort default_port = 7777;
    [SerializeField] private ushort tickDivergenceTolerance = 1;

#region InitData
    [NonSerialized] public float gravity;
    [NonSerialized] public ushort gravityMultiplier;
    [NonSerialized] public ushort movementSpeed;
    [NonSerialized] public ushort jumpHeight;
#endregion

    private void Awake()
    {
        if(Instance == null){
            Instance = this;
            DontDestroyOnLoad(Instance);
        }
    }
    private void Start() {
        Initialize();
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
        if(Client != null)
            Client.Disconnect();
        GameObject.Destroy(gameObject);
        _singleton = null;
    }

    public void Connect(string ip, string port)
    {
        if (string.IsNullOrEmpty(ip))
            ip = default_ip;
        if (string.IsNullOrEmpty(port))
            ip = default_port.ToString();

        Client.Connect($"{ip}:{port}");
    }
    private void FailedToConnect(object sender, EventArgs e)
    {
        Debug.Log("FailedToConnect " + e);
        UIManager.Instance.UpdateConnectionStatusText("Connection to host failed !", TextColor.red);
    }
    private void DidConnect(object sender, EventArgs e)
    {
        UIManager.Instance.ToggleMenuUI();
        MainEntry.Instance.GoToNextFlow(SceneState.Game);
    }
    private void DidDisconnect(object sender, EventArgs e)
    {
        
    }
    private void PlayerLeft(object sender, ClientDisconnectedEventArgs e)
    {
        if(PlayerManager.Instance.list.TryGetValue(e.Id, out Player player))
            Destroy(player.gameObject);
    }

    private void SetTick(ushort serverTick)
    {
        if (Mathf.Abs(ServerTick - serverTick) > tickDivergenceTolerance)
        {
            Debug.Log($"Client tick correction: {ServerTick} -> {serverTick}");
            ServerTick = serverTick;
        }
    }

#region Messages
    [MessageHandler((ushort)ServerToClientId.sync)]
    public static void Sync(Message message)
    {
        Instance.SetTick(message.GetUShort());
    }

    [MessageHandler((ushort)ServerToClientId.initData)]
    public static void InitData(Message message)
    {
        _singleton.gravity = message.GetFloat();
        _singleton.gravityMultiplier = message.GetUShort();
        _singleton.movementSpeed = message.GetUShort();
        _singleton.jumpHeight = message.GetUShort();
    }
#endregion
}