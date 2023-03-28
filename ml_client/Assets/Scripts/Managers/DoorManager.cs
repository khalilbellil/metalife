using Riptide;
using System.Collections.Generic;
using System.Linq;
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

    public static Dictionary<ushort, Door> list = new Dictionary<ushort, Door>();

    public void Initialize(){

    }
    public void StopManager(){
        foreach(KeyValuePair<ushort, Door> kvp in list){
            GameObject.Destroy(kvp.Value.gameObject);
        }
        list.Clear();
    }

    [MessageHandler((ushort)ServerToClientId.doorOpened)]
    private static void DoorOpened(Message message)
    {
        ushort _id = message.GetUShort();
        if (list.TryGetValue(_id, out Door door))
            door.open = !door.open;
    }
}