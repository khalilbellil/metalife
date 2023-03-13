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

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform camProxy;

    private float gravityAcceleration;
    private float moveSpeed;
    private float jumpSpeed;

    private bool[] inputs;
    private float yVelocity;
    private bool didTeleport;
    private bool sprintedBeforeJump = false;
    private bool walkingBeforeJump = false;

    private const int BUFFER_SIZE = 1024;
    private StatePayload[] stateBuffer;
    private Queue<InputPayload> inputQueue;

    private void OnValidate()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();
        if (player == null)
            player = GetComponent<Player>();
    }
    private void Start()
    {
        Initialize();
        inputs = new bool[6];
        stateBuffer = new StatePayload[BUFFER_SIZE];
        inputQueue = new Queue<InputPayload>();
    }
    private void FixedUpdate()
    {
        Move();
    }
    private void Initialize()
    {
        gravityAcceleration = NetworkManager.Singleton.gravity * Time.fixedDeltaTime 
            * Time.fixedDeltaTime * NetworkManager.Singleton.gravityMultiplier;
        moveSpeed = Convert.ToSingle(NetworkManager.Singleton.movementSpeed) * Time.fixedDeltaTime;
        jumpSpeed = Mathf.Sqrt(Convert.ToSingle(NetworkManager.Singleton.jumpHeight) * -2f 
            * gravityAcceleration);
    }
    private void Move()
    {
        // Process the input queue
        int bufferIndex = -1;
        while (inputQueue.Count > 0)
        {
            InputPayload inputPayload = inputQueue.Dequeue();

            bufferIndex = inputPayload.tick % BUFFER_SIZE;

            StatePayload statePayload = ProcessMovement(inputPayload);
            stateBuffer[bufferIndex] = statePayload;
        }

        if (bufferIndex != -1)
        {
            SendMovement(stateBuffer[bufferIndex]);
        }
    }
    StatePayload ProcessMovement(InputPayload input)
    {
        Transform camTransform = camProxy;
        // Should always be in sync with same function on Client
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
    public void Enabled(bool value)
    {
        enabled = value;
        controller.enabled = value;
    }
    public void Teleport(Vector3 toPosition)
    {
        bool isEnabled = controller.enabled;
        controller.enabled = false;
        transform.position = toPosition;
        controller.enabled = isEnabled;

        didTeleport = true;
    }
    private Vector2 GetInputDirection()
    {
        Vector2 inputDirection = Vector2.zero;
        if (controller.isGrounded)
        {
            if (inputs[0])
                inputDirection.y += 1;

            if (inputs[1])
                inputDirection.y -= 1;

            if (inputs[2])
                inputDirection.x -= 1;

            if (inputs[3])
                inputDirection.x += 1;
        }

        return inputDirection;
    }
    private Vector3 FlattenVector3(Vector3 vector)
    {
        vector.y = 0;
        return vector;
    }
    public void SetInput(bool[] inputs, Vector3 forward)
    {
        this.inputs = inputs;
        camProxy.forward = forward;
    }

    #region Messages
    private void SendMovement(StatePayload statePayload)
    {
        if (NetworkManager.Singleton.CurrentTick % 2 != 0)
            return;

        Message message = Message.Create(MessageSendMode.Unreliable, ServerToClientId.playerMovement);
        message.AddUShort(player.Id);
        message.AddUShort(statePayload.tick);
        message.AddVector3(statePayload.position);
        message.AddVector3(camProxy.forward);
        NetworkManager.Singleton.Server.SendToAll(message);
    }

    [MessageHandler((ushort)ClientToServerId.input)]
    private static void Input(ushort fromClientId, Message message)
    {
        if (PlayerManager.Instance.list.TryGetValue(fromClientId, out Player player))
        {
            InputPayload clientInputPayload = new InputPayload();
            clientInputPayload.tick = message.GetUShort();
            clientInputPayload.inputDirection = message.GetVector2();
            clientInputPayload.camForward = message.GetVector3();
            clientInputPayload.yRotation = message.GetFloat();
            clientInputPayload.jump = message.GetBool();
            clientInputPayload.sprint = message.GetBool();
            player.Movement.inputQueue.Enqueue(clientInputPayload);
        }
    }
    #endregion
}