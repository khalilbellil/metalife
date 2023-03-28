using Riptide;
using System;
using System.Collections;
using System.Collections.Generic;
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
    public Queue<InputPayload> inputQueue;

    public ushort speed = 5; // player movement speed
    public ushort jumpHeight = 2; // jump height
    public float gravity = -9.81f; // gravity
    private Vector3 velocity;
    [SerializeField] private float sensitivity = 100f;
    [SerializeField] private float clampAngle = 85f;

    private float verticalRotation;
    private float horizontalRotation;

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

        Debug.DrawRay(camProxy.position, camProxy.forward * 2f, Color.green);
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
        float fixedDeltaTime = Time.fixedDeltaTime;
        Transform cameraTransform = camProxy;
        // Should always be in sync with same function on Client

        float moveHorizontal = input.inputDirection.x; // get horizontal movement input
        float moveVertical = input.inputDirection.y; // get vertical movement input

        // Rotate camera and player in the direction of movement
        verticalRotation += input.mouseVertical * sensitivity * fixedDeltaTime;
        horizontalRotation += input.mouseHorizontal * sensitivity * fixedDeltaTime;
        verticalRotation = Mathf.Clamp(verticalRotation, -clampAngle, clampAngle);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        transform.rotation = Quaternion.Euler(0f, horizontalRotation, 0f);

        // Create movement vector based on player direction
        Vector3 movement = (transform.forward * moveVertical + transform.right * moveHorizontal).normalized;

        // Apply gravity to velocity
        velocity.y += gravity * fixedDeltaTime;

        // Apply jump if player is on the ground
        if (controller.isGrounded && input.jump)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Apply movement to the CharacterController component
        controller.Move((movement * speed + velocity) * fixedDeltaTime);
        Physics.Simulate(fixedDeltaTime);

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
        message.AddVector3(statePayload.rotation);
        NetworkManager.Singleton.Server.SendToAll(message);
    }
    #endregion
}