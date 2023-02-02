using UnityEngine;

public class MainEntryCreator : MonoBehaviour
{
    public SceneState levelType;
    public static bool MS_CREATED = false;

    public void Awake()
    {
        if (!MS_CREATED)
        {
            //Create the DoNotDestroy object containing the mainscript
            GameObject msobj = new GameObject(this.ToString());
            msobj.name = "MainEntry";
            msobj.AddComponent<MainEntry>().Initialize(levelType);
            DontDestroyOnLoad(msobj);
            MS_CREATED = true;
        }
        Destroy(this);
    }
}