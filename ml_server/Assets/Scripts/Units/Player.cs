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
}