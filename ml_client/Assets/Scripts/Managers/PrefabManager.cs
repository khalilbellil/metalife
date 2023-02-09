using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabManager : MonoBehaviour
{
    #region Singleton Pattern
    private static PrefabManager _singleton;
    public static PrefabManager Instance
    {
        get => _singleton;
        set
        {
            if (_singleton == null)
                _singleton = value;
            else if (_singleton != value)
            {
                Debug.Log($"{nameof(PrefabManager)} instance already exists, destroying duplicate !");
                Destroy(value);
            }
        }
    }
    #endregion

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(Instance);
    }

    [SerializeField] public string PlayerPath = "Prefabs/Characters/Player";
    public GameObject PlayerPrefab;
    [SerializeField] public string LocalPlayerPath = "Prefabs/Characters/LocalPlayer";
    public GameObject LocalPlayerPrefab;

    public void Initialize()
    {
        if(PlayerPath != null && PlayerPath != "")
            PlayerPrefab = Resources.Load(PlayerPath) as GameObject;

        if (LocalPlayerPath != null && LocalPlayerPath != "")
            LocalPlayerPrefab = Resources.Load(LocalPlayerPath) as GameObject;
    }

    public GameObject Spawn(Vector3 position, Quaternion rotation)
    {
        return Instantiate(LocalPlayerPrefab, position, rotation);
    }

    public void StopManager()
    {
    }
}