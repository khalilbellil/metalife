using Riptide;
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
        UIManager.Instance.SendLoginInfo(); //Send Player identity to server then spawn
    }

    public void UpdateManager(float dt)
    {
    }

    public void FixedUpdateManager(float dt)
    {
    }

    public void StopManager()
    {
        foreach (Player player in PlayerManager.Instance.list.Values)
            GameObject.Destroy(player.gameObject);
        list.Clear();
        instance = null;
    }

    public void Spawn(ushort id, string username, Vector3 position)
    {
        Player player;
        if (id == NetworkManager.Instance.Client.Id)
        {

            player = GameObject.Instantiate(PrefabManager.Instance.LocalPlayerPrefab, position, Quaternion.identity).GetComponent<Player>();
            player.SetIsLocal(true);
        }
        else
        {
            player = GameObject.Instantiate(PrefabManager.Instance.PlayerPrefab, position, Quaternion.identity).GetComponent<Player>();
            player.SetIsLocal(false);
        }

        player.name = $"Player {id} ({(string.IsNullOrEmpty(username) ? "Guest" : username)})";
        player.SetId(id);
        player.SetUsername(username);
        list.Add(id, player);

        UIManager.Instance.InitializeGameUI(id, username);
    }

    #region Messages
    [MessageHandler((ushort)ServerToClientId.playerSpawned)]
    private static void SpawnPlayer(Message message)
    {
        Instance.Spawn(message.GetUShort(), message.GetString(), message.GetVector3());
    }

    [MessageHandler((ushort)ServerToClientId.playerMovement)]
    private static void PlayerMovement(Message message)
    {
        ushort playerId = message.GetUShort();
        if (PlayerManager.Instance.list.TryGetValue(playerId, out Player player))
        {
            StatePayload serverState = new StatePayload();
            serverState.tick = message.GetUShort();
            serverState.position = message.GetVector3();
            serverState.rotation = message.GetVector3();
            if (playerId == NetworkManager.Instance.Client.Id)
            {
               player.playerController.ProcessServerState(serverState);
            }else{
                player.Move(serverState.tick, false, serverState.position, serverState.rotation);
            }
        }
    }
    #endregion
}