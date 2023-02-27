using Riptide;
using Riptide.Utils;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public ushort Id { get; private set; }
    public bool IsLocal { get; private set; }

    [SerializeField] private PlayerAnimationManager animationManager;
    [SerializeField] private Transform camTransform;
    [SerializeField] private Interpolator interpolator;

    private string username;

    private void OnValidate()
    {
        if (animationManager == null)
            animationManager = GetComponent<PlayerAnimationManager>();
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        PlayerManager.Instance.list.Remove(Id);
    }

    private void Move(ushort tick, bool isTeleport, Vector3 newPosition, Vector3 forward)
    {
        interpolator.NewUpdate(tick, isTeleport, newPosition);

        if (!IsLocal)
            camTransform.forward = forward;

        animationManager.AnimateBasedOnSpeed();
    }

    #region Getter/Setter
    public void SetIsLocal(bool value)
    {
        IsLocal = value;
    }

    public void SetId(ushort value)
    {
        Id = value;
    }

    public void SetUsername(string value)
    {
        username = value;
    }
    #endregion

    #region Messages
    [MessageHandler((ushort)ServerToClientId.playerMovement)]
    private static void PlayerMovement(Message message)
    {
        if (PlayerManager.Instance.list.TryGetValue(message.GetUShort(), out Player player))
            player.Move(message.GetUShort(), message.GetBool(), message.GetVector3(), message.GetVector3());
    }
    #endregion
}