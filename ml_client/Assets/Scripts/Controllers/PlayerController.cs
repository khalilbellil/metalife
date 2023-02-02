using Riptide;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Transform camTransform;

    private bool[] inputs;

    private void Start()
    {
        inputs = new bool[6];
    }

    private void Update()
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

    private void FixedUpdate()
    {
        SendInput();

        for (int i = 0; i < inputs.Length; i++)
            inputs[i] = false;
    }

    #region Messages

    private void SendInput()
    {
        Message message = Message.Create(MessageSendMode.Unreliable, ClientToServerId.input);
        message.AddBools(inputs, false);
        message.AddVector3(camTransform.forward);
        NetworkManager.Instance.Client.Send(message);
    }
    #endregion
}
