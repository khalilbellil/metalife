using Riptide;
using System.Collections.Generic;

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

    public Dictionary<ushort, Door> list = new Dictionary<ushort, Door>();

    [MessageHandler((ushort)ServerToClientId.doorOpened)]
    private static void DoorOpened(Message message)
    {
        ushort _id = message.GetUShort();
        if (Instance.list.TryGetValue(_id, out Door door))
            door.open = !door.open;
    }
}