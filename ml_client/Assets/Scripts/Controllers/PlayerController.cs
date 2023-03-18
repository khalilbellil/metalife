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
    public StatePayload latestServerState;
    private StatePayload lastProcessedState;
    public bool isReconciling = false;
    private const float POSITION_THRESHOLD = 0.001f;
    private const float ROTATION_THRESHOLD = 5f; // Angle in degres
#endregion

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
    private void Initialize()
    {
        gravityAcceleration = NetworkManager.Instance.gravity * Time.fixedDeltaTime * Time.fixedDeltaTime * NetworkManager.Instance.gravityMultiplier;
        moveSpeed = Convert.ToSingle(NetworkManager.Instance.movementSpeed) * Time.fixedDeltaTime;
        jumpSpeed = Mathf.Sqrt(Convert.ToSingle(NetworkManager.Instance.jumpHeight) * -2f * gravityAcceleration);
    }

    private void Move(Vector2 inputDirection, float mouseHorizontal, float mouseVertical, bool jump, 
        bool sprint)
    {
        if (!latestServerState.Equals(default(StatePayload)) &&
            (lastProcessedState.Equals(default(StatePayload)) ||
            !latestServerState.Equals(lastProcessedState)))
        {
            HandleServerReconciliation();
        }
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
    void HandleServerReconciliation()
    {
        if(!isReconciling){
            lastProcessedState = latestServerState;

            int serverStateBufferIndex = latestServerState.tick % BUFFER_SIZE;
            float positionError = (latestServerState.position - stateBuffer[serverStateBufferIndex].position).sqrMagnitude;
            float rotationError = Quaternion.Angle(Quaternion.Euler(latestServerState.rotation), Quaternion.Euler(stateBuffer[serverStateBufferIndex].rotation)) * Mathf.Deg2Rad;

            if (positionError > POSITION_THRESHOLD || rotationError > ROTATION_THRESHOLD)
            {
                Debug.Log("Reconciling player state");
                
                // Set the flag to indicate that we're reconciling
                isReconciling = true;

                // Reset player position and rotation to latest server state
                transform.position = latestServerState.position;
                transform.rotation = Quaternion.Euler(latestServerState.rotation);

                // Update the state buffer at the index of the latest server state
                stateBuffer[serverStateBufferIndex] = latestServerState;

                // Re-simulate movement for ticks between the latest server state and current client tick
                ushort currentClientTick = NetworkManager.Instance.ServerTick;
                for (int tickToProcess = latestServerState.tick + 1; tickToProcess < currentClientTick; tickToProcess++)
                {
                    int bufferIndex = tickToProcess % BUFFER_SIZE;
                    
                    // Process new movement with reconciled state
                    StatePayload statePayload = ProcessMovement(inputBuffer[bufferIndex]);
                    Debug.Log("Reconciliation done");
                    
                    // Update buffer with recalculated state
                    stateBuffer[bufferIndex] = statePayload;
                }

                // Reset the flag to indicate that we've finished reconciling
                isReconciling = false;
            }
        }
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