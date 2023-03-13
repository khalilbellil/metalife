using Riptide;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct InputPayload
{
    public ushort tick;
    public Vector2 inputDirection;
    public Vector3 camForward;
    public float yRotation;
    public bool jump;
    public bool sprint;
}

public struct StatePayload
{
    public ushort tick;
    public Vector3 position;
}

public class PlayerController : MonoBehaviour
{
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform camTransform;

    private float gravityAcceleration;
    private float moveSpeed;
    private float jumpSpeed;

    private bool[] inputs;
    private float yVelocity, zVelocity;
    private bool didTeleport;
    private bool sprintedBeforeJump = false;
    private bool walkingBeforeJump = false;

    private const int BUFFER_SIZE = 1024;
    private StatePayload[] stateBuffer;
    private InputPayload[] inputBuffer;
    public StatePayload latestServerState;
    private StatePayload lastProcessedState;

    private void Start()
    {
        Initialize();
        inputs = new bool[6];
        stateBuffer = new StatePayload[BUFFER_SIZE];
        inputBuffer = new InputPayload[BUFFER_SIZE];
    }
    private void Update()
    {
        UpdateInputs();
    }
    private void FixedUpdate()
    {
        Move(GetInputDirection(), camTransform.forward, InputManager.Instance.inputPressed.jump, 
            InputManager.Instance.inputPressed.sprint);

        for (int i = 0; i < inputs.Length; i++)
            inputs[i] = false;
    }
    private void Initialize()
    {
        gravityAcceleration = NetworkManager.Instance.gravity * Time.fixedDeltaTime * Time.fixedDeltaTime * NetworkManager.Instance.gravityMultiplier;
        moveSpeed = Convert.ToSingle(NetworkManager.Instance.movementSpeed) * Time.fixedDeltaTime;
        jumpSpeed = Mathf.Sqrt(Convert.ToSingle(NetworkManager.Instance.jumpHeight) * -2f * gravityAcceleration);
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
    private void Move(Vector2 inputDirection, Vector3 camForward, bool jump, bool sprint)
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
        inputPayload.camForward = camForward;
        inputPayload.yRotation = transform.rotation.eulerAngles.y;
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
        // Should always be in sync with same function on Server
        camTransform.rotation = Quaternion.LookRotation(input.camForward); // cam rotation sync
        transform.localEulerAngles = new Vector3(0, input.yRotation, 0); // player rotation sync

        Vector3 moveDirection;
        if (!controller.isGrounded)
        {
            Vector2 inputDirection = Vector2.zero;
            if (walkingBeforeJump)
            {
                inputDirection.y = 1 * Time.fixedDeltaTime; // apply forward jump force
            }
            moveDirection = Vector3.Normalize(camTransform.right * inputDirection.x
            + Vector3.Normalize(FlattenVector3(camTransform.forward)) * inputDirection.y);
            if (sprintedBeforeJump)
            {
                moveDirection *= 2f;
            }
        }
        else
        {
            moveDirection = Vector3.Normalize(camTransform.right * input.inputDirection.x
            + Vector3.Normalize(FlattenVector3(camTransform.forward)) * input.inputDirection.y);
            moveDirection *= moveSpeed;

            sprintedBeforeJump = false;
            walkingBeforeJump = false;
            if (input.inputDirection.y >= 1 && input.jump)
                walkingBeforeJump = true;
            if (input.jump && input.sprint)
                sprintedBeforeJump = true;
            yVelocity = 0f;

            if (input.sprint)
                moveDirection *= 1.4f;

            if (input.jump)
            {
                yVelocity = jumpSpeed;
                if (input.sprint)
                {
                    yVelocity *= 1.3f; // apply upward jump force
                }
            }
        }
        yVelocity += gravityAcceleration;
        moveDirection.y = yVelocity;

        controller.Move(moveDirection);

        return new StatePayload()
        {
            tick = input.tick,
            position = transform.position,
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

            while (tickToProcess < NetworkManager.Instance.ServerTick)
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
    private Vector3 FlattenVector3(Vector3 vector)
    {
        vector.y = 0;
        return vector;
    }

    #region Messages
    private void SendInput(InputPayload inputPayload)
    {
        Message message = Message.Create(MessageSendMode.Unreliable, ClientToServerId.input);
        message.AddUShort(inputPayload.tick);
        message.AddVector2(inputPayload.inputDirection);
        message.AddVector3(inputPayload.camForward);
        message.AddFloat(inputPayload.yRotation);
        message.AddBool(inputPayload.jump);
        message.AddBool(inputPayload.sprint);
        
        NetworkManager.Instance.Client.Send(message);
    }
    #endregion
}