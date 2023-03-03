using Riptide;
using Riptide.Utils;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class Player : MonoBehaviour
{
    public ushort Id { get; private set; }
    public string Username { get; private set; }

    public PlayerMovement Movement => movement;

    [SerializeField] private PlayerMovement movement;

    private void OnValidate()
    {
        if (movement == null)
            movement = GetComponent<PlayerMovement>();
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        PlayerManager.Instance.list.Remove(Id);
    }

    #region Getter/Setter
    public void SetId(ushort value)
    {
        Id = value;
    }

    public void SetUsername(string value)
    {
        Username = value;
    }
    #endregion

    #region Messages
    [MessageHandler((ushort)ClientToServerId.input)]
    private static void Input(ushort fromClientId, Message message)
    {
        if (PlayerManager.Instance.list.TryGetValue(fromClientId, out Player player))
            player.Movement.SetInput(message.GetBools(6), message.GetVector3());
    }
    #endregion
}