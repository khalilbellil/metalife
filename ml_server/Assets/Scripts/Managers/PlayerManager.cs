using Riptide;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager
{
    #region Singleton Pattern
    private static PlayerManager instance = null;
    private PlayerManager() { }
    public static PlayerManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new PlayerManager();
            }
            return instance;
        }
    }
    #endregion

    public Dictionary<ushort, Player> list = new Dictionary<ushort, Player>();

    public void Initialize()
    {
    }

    public void UpdateManager()
    {
    }

    public void FixedUpdateManager()
    {
    }

    public void StopManager()
    {
        instance = null;
    }

    public void Spawn(ushort id, string username)
    {
        foreach (Player otherPlayer in list.Values)
            SendSpawned(id, otherPlayer);

        Player player = GameObject.Instantiate(GameLogic.Singleton.PlayerPrefab, NetworkManager.Singleton.SpawnPoint.position, Quaternion.identity).GetComponent<Player>();
        player.name = $"Player {id} ({(string.IsNullOrEmpty(username) ? "Guest" : username)})";
        player.SetId(id);
        player.SetUsername(string.IsNullOrEmpty(username) ? $"Guest {id}" : username);

        SendSpawned(player);
        list.Add(id, player);
    }

    private void SendSpawned(Player player)
    {
        NetworkManager.Singleton.Server.SendToAll(AddSpawnData(Message.Create(MessageSendMode.Reliable, 
            ServerToClientId.playerSpawned), player));
    }

    private void SendSpawned(ushort toClientId, Player player)
    {
        NetworkManager.Singleton.Server.Send(AddSpawnData(Message.Create(MessageSendMode.Reliable, 
            ServerToClientId.playerSpawned), player), toClientId);
    }

    private Message AddSpawnData(Message message, Player player)
    {
        message.AddUShort(player.Id);
        message.AddString(player.Username);
        message.AddVector3(player.transform.position);
        return message;
    }

    [MessageHandler((ushort)ClientToServerId.loginInfo)]
    private static void Name(ushort fromClientId, Message message)
    {
        Instance.Spawn(fromClientId, message.GetString());
    }
}