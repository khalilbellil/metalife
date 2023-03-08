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
    [SerializeField] public PlayerController playerController;

    private string username;

    private void OnValidate()
    {
        if (animationManager == null)
            animationManager = GetComponent<PlayerAnimationManager>();

        if (playerController == null)
            playerController = GetComponent<PlayerController>();
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        PlayerManager.Instance.list.Remove(Id);
    }

    public void Move(ushort tick, bool isTeleport, Vector3 newPosition, Vector3 forward)
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
    #endregion
}