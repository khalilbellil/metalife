using Riptide;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLogic : MonoBehaviour
{
    private static GameLogic _singleton;
    public static GameLogic Singleton
    {
        get => _singleton;
        set
        {
            if (_singleton == null)
                _singleton = value;
            else if (_singleton != value)
            {
                Debug.Log($"{nameof(GameLogic)} instance already exists, destroying duplicate !");
                Destroy(value);
            }
        }
    }

    public bool IsGameInProgress => activeScene == 2;
    public GameObject PlayerPrefab => playerPrefab;

    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;

    private byte activeScene;

    private void Awake()
    {
        Singleton = this;
    }

    public void LoadScene(byte sceneBuildIndex)
    {
        StartCoroutine(LoadSceneInBackground(sceneBuildIndex));
    }

    private IEnumerator LoadSceneInBackground(byte sceneBuildIndex)
    {
        if (activeScene > 0)
            SceneManager.UnloadSceneAsync(activeScene);

        activeScene = sceneBuildIndex;
        SendNewActiveScene();

        AsyncOperation loadingScene = SceneManager.LoadSceneAsync(sceneBuildIndex, LoadSceneMode.Additive);
        while (!loadingScene.isDone)
            yield return new WaitForSeconds(0.25f);

        SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(sceneBuildIndex));
    //    foreach (Player player in Player.list.Values)
    //        player.Spawn();
    }

    private void SendActiveScene(ushort toClientId)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ServerToClientId.activeScene);
        message.AddByte(activeScene);
        NetworkManager.Singleton.Server.Send(message, toClientId);
    }

    private void SendNewActiveScene()
    {
        Message message = Message.Create(MessageSendMode.Reliable, ServerToClientId.activeScene);
        message.AddByte(activeScene);
        NetworkManager.Singleton.Server.SendToAll(message);
    }
}
