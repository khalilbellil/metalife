using Riptide;
using System;
using UnityEngine;

public struct InputPayload
{
    public ushort tick;
    public Vector2 inputDirection;
    public float mouseVertical;
    public float mouseHorizontal;
    public bool jump;
    public bool sprint;
}

public struct StatePayload
{
    public ushort tick;
    public Vector3 position;
    public Vector3 rotation;
}

public class PlayerController : MonoBehaviour
{
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform cameraTransform;
    public Player player;

#region Inputs & Physics
    private bool[] inputs;
    public ushort speed = 5; // player movement speed
    public ushort jumpHeight = 1; // jump height
    public float gravity = -9.81f; // gravity
    private float gravityAcceleration;
    private float moveSpeed;
    private float jumpSpeed;
    private bool didTeleport;
    private bool sprintedBeforeJump = false;
    private bool walkingBeforeJump = false;
    private Vector3 velocity;
    [SerializeField] private float sensitivity = 100f;
    [SerializeField] private float clampAngle = 85f;
    private float verticalRotation;
    private float horizontalRotation;
#endregion

#region Prediction & Reconciliation
    private const int BUFFER_SIZE = 1024;
    private StatePayload[] stateBuffer;
    private InputPayload[] inputBuffer;
    private StatePayload latestServerState;
    private StatePayload lastProcessedState;
    private bool isReconciling = false;
    private const float POSITION_THRESHOLD = 0.001f;
    private const float ROTATION_THRESHOLD = 5f; // Angle in degres
#endregion

    private void Initialize()
    {
        gravity = NetworkManager.Instance.gravity;
        gravityAcceleration = NetworkManager.Instance.gravity * Time.fixedDeltaTime * Time.fixedDeltaTime * NetworkManager.Instance.gravityMultiplier;
        moveSpeed = Convert.ToSingle(NetworkManager.Instance.movementSpeed) * Time.fixedDeltaTime;
        jumpSpeed = Mathf.Sqrt(Convert.ToSingle(NetworkManager.Instance.jumpHeight) * -2f * gravityAcceleration);
    }
    private void Start()
    {
        Initialize();
        inputs = new bool[6];
        stateBuffer = new StatePayload[BUFFER_SIZE];
        inputBuffer = new InputPayload[BUFFER_SIZE];

        verticalRotation = cameraTransform.localEulerAngles.x;
        horizontalRotation = transform.eulerAngles.y;
    }
    private void Update()
    {
        UpdateInputs();
    }
    private void FixedUpdate()
    {
        Move(GetInputDirection(), InputManager.Instance.inputPressed.deltaMouse.x, 
            InputManager.Instance.inputPressed.deltaMouse.y, 
            InputManager.Instance.inputPressed.jump, InputManager.Instance.inputPressed.sprint);

        for (int i = 0; i < inputs.Length; i++)
            inputs[i] = false;
    }
    private void Move(Vector2 inputDirection, float mouseHorizontal, float mouseVertical, bool jump, bool sprint)
    {
        if (!latestServerState.Equals(default(StatePayload)) &&
        (lastProcessedState.Equals(default(StatePayload)) ||
        !latestServerState.Equals(lastProcessedState)))
        {
            if(!isReconciling){
                isReconciling = true;
                HandleServerReconciliation();
                isReconciling = false;
            }
        }

        if (!isReconciling)
        {
            ushort currentTick = NetworkManager.Instance.ServerTick;
            int bufferIndex = currentTick % BUFFER_SIZE;

            // Add payload to inputBuffer
            InputPayload inputPayload = new InputPayload();
            inputPayload.tick = currentTick;
            inputPayload.inputDirection = inputDirection;
            inputPayload.mouseHorizontal = mouseHorizontal;
            inputPayload.mouseVertical = mouseVertical;
            inputPayload.jump = jump;
            inputPayload.sprint = sprint;
            inputBuffer[bufferIndex] = inputPayload;

            // Add payload to stateBuffer
            stateBuffer[bufferIndex] = ProcessMovement(inputPayload);

            // Send input to server
            SendInput(inputPayload);
        }
    }
    void HandleServerReconciliation()
    {
        lastProcessedState = latestServerState;

        int serverStateBufferIndex = latestServerState.tick % BUFFER_SIZE;
        float positionError = Vector3.Distance(latestServerState.position, stateBuffer[serverStateBufferIndex].position);
        float rotationError = Quaternion.Angle(Quaternion.Euler(latestServerState.rotation), Quaternion.Euler(stateBuffer[serverStateBufferIndex].rotation));

        if (positionError > 0.001f || rotationError > 0.001f)
        {
            Debug.Log("We have to reconcile bro");

            // Save the current transform state
            Vector3 currentPosition = transform.position;
            Quaternion currentRotation = transform.rotation;

            // Rewind to the server state
            transform.position = latestServerState.position;
            transform.rotation = Quaternion.Euler(latestServerState.rotation);

            // Replay the inputs from the last processed state
            int tickToProcess = lastProcessedState.tick + 1;
            while (tickToProcess <= latestServerState.tick)
            {
                int bufferIndex = tickToProcess % BUFFER_SIZE;

                // Process new movement with reconciled state
                StatePayload statePayload = ProcessMovement(inputBuffer[bufferIndex]);

                // Update buffer with recalculated state
                stateBuffer[bufferIndex] = statePayload;

                tickToProcess++;
            }
            int currentTick = NetworkManager.Instance.ServerTick;
            // Replay the inputs after the server state
            tickToProcess = latestServerState.tick + 1;
            while (tickToProcess < currentTick)
            {
                int bufferIndex = tickToProcess % BUFFER_SIZE;

                // Process new movement with current transform state
                StatePayload statePayload = ProcessMovement(inputBuffer[bufferIndex]);

                // Update buffer with recalculated state
                stateBuffer[bufferIndex] = statePayload;

                tickToProcess++;
            }

            // Set the transform state back to the server state
            transform.position = latestServerState.position;
            transform.rotation = Quaternion.Euler(latestServerState.rotation);
        }
    }
    StatePayload ProcessMovement(InputPayload input)
    {
        float deltaTime = Time.fixedDeltaTime;
        // Should always be in sync with same function on Server

        float moveHorizontal = input.inputDirection.x; // get horizontal movement input
        float moveVertical = input.inputDirection.y; // get vertical movement input

        // Rotate camera and player in the direction of movement
        verticalRotation += input.mouseVertical * sensitivity * deltaTime;
        horizontalRotation += input.mouseHorizontal * sensitivity * deltaTime;
        verticalRotation = Mathf.Clamp(verticalRotation, -clampAngle, clampAngle);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        transform.rotation = Quaternion.Euler(0f, horizontalRotation, 0f);

        // Create movement vector based on player direction
        Vector3 movement = (transform.forward * moveVertical + transform.right * moveHorizontal).normalized;

        // Apply gravity to velocity
        velocity.y += gravity * deltaTime;

        // Apply jump if player is on the ground
        if (controller.isGrounded && input.jump)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Apply movement to the CharacterController component
        controller.Move((movement * speed + velocity) * deltaTime);
        
        // Reset velocity if player is on the ground
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        return new StatePayload()
        {
            tick = input.tick,
            position = transform.position,
            rotation = transform.rotation.eulerAngles,
        };
    }
    public void ProcessServerState(StatePayload state)
    {
        if(!isReconciling)
            latestServerState = state;
    }
    private void UpdateInputs()
    {
        if (InputManager.Instance.inputPressed.forward)
            inputs[0] = true;

        if (InputManager.Instance.inputPressed.backward)
            inputs[1] = true;

        if (InputManager.Instance.inputPressed.left)
            inputs[2] = true;

        if (InputManager.Instance.inputPressed.right)
            inputs[3] = true;

        if (InputManager.Instance.inputPressed.jump)
            inputs[4] = true;

        if (InputManager.Instance.inputPressed.sprint)
            inputs[5] = true;
    }
    private Vector2 GetInputDirection()
    {
        Vector2 inputDirection = Vector2.zero;
        if (inputs[0])
            inputDirection.y += 1;

        if (inputs[1])
            inputDirection.y -= 1;

        if (inputs[2])
            inputDirection.x -= 1;

        if (inputs[3])
            inputDirection.x += 1;

        return inputDirection;
    }

    #region Messages
    private void SendInput(InputPayload inputPayload)
    {
        Message message = Message.Create(MessageSendMode.Unreliable, ClientToServerId.input);
        message.AddUShort(inputPayload.tick);
        message.AddVector2(inputPayload.inputDirection);
        message.AddFloat(inputPayload.mouseHorizontal);
        message.AddFloat(inputPayload.mouseVertical);
        message.AddBool(inputPayload.jump);
        message.AddBool(inputPayload.sprint);
        
        NetworkManager.Instance.Client.Send(message);
    }
    #endregion
}