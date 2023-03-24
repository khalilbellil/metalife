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

        if (playerController == null){}
            playerController = GetComponent<PlayerController>();
    }

    private void Start()
    {
        //DontDestroyOnLoad(gameObject);
        playerController.player = this;
    }

    private void OnDestroy()
    {
        PlayerManager.Instance.list.Remove(Id);
    }

    public void Move(ushort tick, bool isTeleport, Vector3 newPosition, Vector3 newRotation)
    {
        //interpolator.NewUpdate(tick, isTeleport, newPosition, newRotation);
        interpolator.AddTransformUpdate(new TransformUpdate(tick, isTeleport, newPosition, newRotation));
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