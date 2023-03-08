using Riptide;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DoorManager : MonoBehaviour
{
    #region Singleton Pattern
    private static DoorManager _singleton;
    public static DoorManager Instance
    {
        get => _singleton;
        set
        {
            if (_singleton == null)
                _singleton = value;
            else if (_singleton != value)
            {
                Debug.Log($"{nameof(DoorManager)} instance already exists, destroying duplicate !");
                Destroy(value);
            }
        }
    }
    #endregion

    public static Dictionary<ushort, Door> list = new Dictionary<ushort, Door>();

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
        if (list.TryGetValue(_id, out Door door))
            door.open = !door.open;
            sendDoorOpened(_id);
    }
}