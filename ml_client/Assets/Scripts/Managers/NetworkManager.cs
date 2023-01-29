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

    public Client Client { get; private set; }
    public Player localPlayer;

    private ushort _serverTick;
    public ushort ServerTick
    {
        get => _serverTick;
        private set
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

    // Shared
    private float timer;
    private float minTimeBetweenTicks;
    private const float SERVER_TICK_RATE = 30f;
    private const int BUFFER_SIZE = 1024;

    // Client specific
    private StatePayload[] stateBuffer;
    private InputPayload[] inputBuffer;
    private StatePayload latestServerState;
    private StatePayload lastProcessedState;
    private float horizontalInput;
    private float verticalInput;

    [SerializeField] private string ip;
    [SerializeField] private ushort port;
    [Space(10)]
    [SerializeField] private ushort tickDivergenceTolerance = 1;

    private void Awake()
    {
        Singleton = this;
    }

    private void Start()
    {
        minTimeBetweenTicks = 1f / SERVER_TICK_RATE;
        stateBuffer = new StatePayload[BUFFER_SIZE];
        inputBuffer = new InputPayload[BUFFER_SIZE];

        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

        Client = new Client();
        Client.Connected += DidConnect;
        Client.ConnectionFailed += FailedToConnect;
        Client.ClientDisconnected += PlayerLeft;
        Client.Disconnected += DidDisconnect;

        ServerTick = TicksBetweenPositionUpdates;
    }

    private void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");

        timer += Time.deltaTime;

        while (timer >= minTimeBetweenTicks)
        {
            timer -= minTimeBetweenTicks;
            Client.Update();

            if(localPlayer != null)
                HandleTick();

            ServerTick++;
        }
    }

    private void OnApplicationQuit()
    {
        Client.Disconnect();
    }

    public void Connect()
    {
        Client.Connect($"{ip}:{port}");
    }

    private void DidConnect(object sender, EventArgs e)
    {
        UIManager.Singleton.SendName();
    }

    private void FailedToConnect(object sender, EventArgs e)
    {
        UIManager.Singleton.BackToMain();
    }

    private void PlayerLeft(object sender, ClientDisconnectedEventArgs e)
    {
        if (Player.list.TryGetValue(e.Id, out Player player))
            Destroy(player.gameObject);
    }

    private void DidDisconnect(object sender, EventArgs e)
    {
        UIManager.Singleton.BackToMain();
        foreach (Player player in Player.list.Values)
            Destroy(player.gameObject);

        GameLogic.Singleton.UnloadActiveScene();
    }

    private void SetTick(ushort serverTick)
    {
        if (Mathf.Abs(ServerTick - serverTick) > tickDivergenceTolerance)
        {
            Debug.Log($"Client tick: {ServerTick} -> {serverTick}");
            ServerTick = serverTick;
        }
    }

    void HandleTick()
    {
        if (!latestServerState.Equals(default(StatePayload)) &&
            (lastProcessedState.Equals(default(StatePayload)) ||
            !latestServerState.Equals(lastProcessedState)))
        {
            HandleServerReconciliation();
        }

        int bufferIndex = ServerTick % BUFFER_SIZE;

        // Add payload to inputBuffer
        InputPayload inputPayload = new InputPayload();
        inputPayload.tick = ServerTick;
        inputPayload.inputDirection = new Vector3(horizontalInput, 0, verticalInput);
        inputBuffer[bufferIndex] = inputPayload;

        // Add payload to stateBuffer
        stateBuffer[bufferIndex] = ProcessMovement(inputPayload);

        // Send input payload to server
        SendInputPayLoad(inputPayload);
    }

    StatePayload ProcessMovement(InputPayload input)
    {
        // Should always be in sync with same function on Server
        Vector3 moveDirection = Vector3.Normalize(localPlayer.controller.camTransform.right * input.inputDirection.x + Vector3.Normalize(FlattenVector3(localPlayer.controller.camTransform.forward)) * input.inputDirection.y);
        moveDirection *= localPlayer.controller.moveSpeed * minTimeBetweenTicks;

        if (localPlayer.controller.inputs[5]) //sprint
            moveDirection *= 2f;

        if (localPlayer.controller.characterController.isGrounded)
        {
            localPlayer.controller.yVelocity = 0f;
            if (localPlayer.controller.inputs[4]) //jump
                localPlayer.controller.yVelocity = localPlayer.controller.jumpSpeed;
        }
        localPlayer.controller.yVelocity += localPlayer.controller.gravityAcceleration;

        moveDirection.y = localPlayer.controller.yVelocity;
        localPlayer.controller.characterController.Move(moveDirection);

        return new StatePayload()
        {
            tick = input.tick,
            position = localPlayer.transform.position,
        };
    }

    void HandleServerReconciliation()
    {
        lastProcessedState = latestServerState;

        int serverStateBufferIndex = latestServerState.tick % BUFFER_SIZE;
        float positionError = Vector3.Distance(latestServerState.position, stateBuffer[serverStateBufferIndex].position);

        if (positionError > 0.001f)
        {
            Debug.Log("We have to reconcile bro");
            // Rewind & Replay
            transform.position = latestServerState.position;

            // Update buffer at index of latest server state
            stateBuffer[serverStateBufferIndex] = latestServerState;

            // Now re-simulate the rest of the ticks up to the current tick on the client
            int tickToProcess = latestServerState.tick + 1;

            while (tickToProcess < ServerTick)
            {
                int bufferIndex = tickToProcess % BUFFER_SIZE;

                // Process new movement with reconciled state
                StatePayload statePayload = ProcessMovement(inputBuffer[bufferIndex]);

                // Update buffer with recalculated state
                stateBuffer[bufferIndex] = statePayload;

                tickToProcess++;
            }
        }
    }

    private Vector3 FlattenVector3(Vector3 vector)
    {
        vector.y = 0;
        return vector;
    }

    private void SendInputPayLoad(InputPayload inputPayload)
    {
        Message message = Message.Create(MessageSendMode.Unreliable, ClientToServerId.inputPayload);
        message.AddUShort(inputPayload.tick);
        message.AddVector2(inputPayload.inputDirection);
        NetworkManager.Singleton.Client.Send(message);
    }

    [MessageHandler((ushort)ServerToClientId.sync)]
    public static void Sync(Message message)
    {
        Singleton.SetTick(message.GetUShort());
    }

    [MessageHandler((ushort)ServerToClientId.statePayload)]
    public static void StatePayload(Message message)
    {
        StatePayload statePayload = new StatePayload()
        {
            tick = message.GetUShort(),
            position = message.GetVector3(),
        };
        Singleton.latestServerState = statePayload;
    }
}
