using Riptide;
using Riptide.Utils;
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
    #region Singleton
    private static NetworkManager _singleton;
    public static NetworkManager Singleton
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

    public Server Server { get; private set; }
    public ushort CurrentTick { get; private set; } = 0;

    [SerializeField] private ushort port;
    [SerializeField] private ushort maxClientCount;

    [SerializeField] public Transform SpawnPoint;
    [SerializeField] public float gravity = -9.81f;
    [SerializeField] public ushort gravityMultiplier = 2;
    [SerializeField] public ushort movementSpeed = 5;
    [SerializeField] public ushort jumpHeight = 1;

    private void Awake()
    {
        Singleton = this;
    }

    private void Start()
    {
        Application.targetFrameRate = 60;

#if UNITY_EDITOR
        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);
#else
        System.Console.Title = "MetaLife Server 0.0.2.2.2";
        System.Console.Clear();
        Application.SetStackTraceLogType(UnityEngine.LogType.Log, StackTraceLogType.None);
        RiptideLogger.Initialize(Debug.Log, true);
#endif

        Server = new Server();
        Server.Start(port, maxClientCount);
        Server.ClientConnected += NewPlayerConnected;
        Server.ClientDisconnected += PlayerLeft;
    }

    private void FixedUpdate()
    {
        Server.Update();

        if (CurrentTick % 200 == 0)
            SendSync();

        CurrentTick++;
    }

    private void OnApplicationQuit()
    {
        Server.Stop();
    }

    private void NewPlayerConnected(object sender, ServerConnectedEventArgs e)
    {
        Message message = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.initData);
        message.AddFloat(gravity);
        message.AddUShort(gravityMultiplier);
        message.AddUShort(movementSpeed);
        message.AddUShort(jumpHeight);
        Server.Send(message, e.Client.Id);
        Debug.Log("InitData Sent");
    }

    private void PlayerLeft(object sender, ServerDisconnectedEventArgs e)
    {
        if (PlayerManager.Instance.list.TryGetValue(e.Client.Id, out Player player))
            Destroy(player.gameObject);
    }

    private void SendSync()
    {
        Message message = Message.Create(MessageSendMode.Unreliable, (ushort)ServerToClientId.sync);
        message.Add(CurrentTick);

        Server.SendToAll(message);
    }
}