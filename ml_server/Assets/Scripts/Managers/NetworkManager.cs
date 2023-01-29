using Riptide;
using Riptide.Utils;
using System.Collections.Generic;
using UnityEngine;

public enum ServerToClientId : ushort
{
    sync = 1,
    playerSpawned,
    playerMovement,
    activeScene,
    statePayload,
}

public enum ClientToServerId : ushort
{
    name = 1,
    input,
    inputPayload,
}

public class NetworkManager : MonoBehaviour
{
    #region Singelton
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

    private float timer;
    private float minTimeBetweenTicks;
    private const float SERVER_TICK_RATE = 60f;
    public int BUFFER_SIZE { get; private set; } = 1024;
    [SerializeField] private ushort port;
    [SerializeField] private ushort maxClientCount;

    private void Awake()
    {
        Singleton = this;
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
        minTimeBetweenTicks = 1f / SERVER_TICK_RATE;

#if UNITY_EDITOR
        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);
#else
        System.Console.Title = "MetaLife Server 0.1";
        System.Console.Clear();
        Application.SetStackTraceLogType(UnityEngine.LogType.Log, StackTraceLogType.None);
        RiptideLogger.Initialize(Debug.Log, true);
#endif

        Server = new Server();
        Server.Start(port, maxClientCount);
        Server.ClientConnected += NewPlayerConnected;
        Server.ClientDisconnected += PlayerLeft;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        while (timer >= minTimeBetweenTicks)
        {
            timer -= minTimeBetweenTicks;
            Server.Update();

            HandleTick();

            if (CurrentTick % 200 == 0)
                SendSync();

            CurrentTick++;
        }
    }

    void HandleTick()
    {
        for (int i = 1; i < Player.list.Count; i++)
        {
            Player player = Player.list[(ushort)i];

            // Process the input queue
            int bufferIndex = -1;
            while (player.inputQueue.Count > 0)
            {
                InputPayload inputPayload = player.inputQueue.Dequeue();

                bufferIndex = inputPayload.tick % BUFFER_SIZE;

                StatePayload statePayload = ProcessMovement(inputPayload, player);
                player.stateBuffer[bufferIndex] = statePayload;
            }

            if (bufferIndex != -1)
            {
                SendStatePayload(player.stateBuffer[bufferIndex], player.Id);
            }
        }
    }

    StatePayload ProcessMovement(InputPayload input, Player player)
    {
        // Should always be in sync with same function on Client
        Vector3 moveDirection = Vector3.Normalize(player.Movement.camProxy.right * input.inputDirection.x + Vector3.Normalize(FlattenVector3(player.Movement.camProxy.forward)) * input.inputDirection.y);
        moveDirection *= player.Movement.moveSpeed * minTimeBetweenTicks;

        if (player.Movement.inputs[5]) //sprint
            moveDirection *= 2f;

        if (player.Movement.controller.isGrounded)
        {
            player.Movement.yVelocity = 0f;
            if (player.Movement.inputs[4]) //jump
                player.Movement.yVelocity = player.Movement.jumpSpeed;
        }
        player.Movement.yVelocity += player.Movement.gravityAcceleration;

        moveDirection.y = player.Movement.yVelocity;
        player.Movement.controller.Move(moveDirection);

        return new StatePayload()
        {
            tick = input.tick,
            position = player.transform.position,
        };
    }

    private Vector3 FlattenVector3(Vector3 vector)
    {
        vector.y = 0;
        return vector;
    }

    private void OnApplicationQuit()
    {
        Server.Stop();
    }

    private void NewPlayerConnected(object sender, ServerConnectedEventArgs e)
    {

    }

    private void PlayerLeft(object sender, ServerDisconnectedEventArgs e)
    {
        if (Player.list.TryGetValue(e.Client.Id, out Player player))
            Destroy(player.gameObject);
    }

    private void SendSync()
    {
        Message message = Message.Create(MessageSendMode.Unreliable, (ushort)ServerToClientId.sync);
        message.Add(CurrentTick);

        Server.SendToAll(message);
    }

    private void SendStatePayload(StatePayload statePayload, ushort id)
    {
        Message message = Message.Create(MessageSendMode.Unreliable, (ushort)ServerToClientId.statePayload);
        message.AddUShort(statePayload.tick);
        message.AddVector3(statePayload.position);
        Server.Send(message, id);
    }

    [MessageHandler((ushort)ClientToServerId.inputPayload)]
    private static void Input(ushort fromClientId, Message message)
    {
        if (Player.list.TryGetValue(fromClientId, out Player player))
        {
            InputPayload inputPayload = new InputPayload()
            {
                tick = message.GetUShort(),
                inputDirection = message.GetVector2(),
            };
            player.inputQueue.Enqueue(inputPayload);
        }
    }
}
