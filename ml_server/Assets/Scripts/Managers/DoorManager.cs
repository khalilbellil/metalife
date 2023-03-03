using Riptide;
using System.Collections.Generic;
using UnityEngine;

public class DoorManager
{
    #region Singleton Pattern
    private static DoorManager instance = null;
    private DoorManager() { }
    public static DoorManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new DoorManager();
            }
            return instance;
        }
    }
    #endregion

    public Dictionary<ushort, DoorController> list = new Dictionary<ushort, DoorController>();

    private static void sendDoorOpened(ushort id)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ServerToClientId.doorOpened);
        message.AddUShort(id);
        NetworkManager.Singleton.Server.SendToAll(message);
    }

    [MessageHandler((ushort)ClientToServerId.doorOpened)]
    private static void DoorOpened(ushort fromClientId, Message message)
    {
        ushort _id = message.GetUShort();
        if (Instance.list.TryGetValue(_id, out DoorController door))
            door.open = !door.open;
            sendDoorOpened(_id);
    }
}