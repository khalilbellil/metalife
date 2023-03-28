using UnityEngine;
using UnityEngine.SceneManagement;

public enum SceneState { Game, Menu }

public class MainEntry : MonoBehaviour
{
    #region Singleton Pattern
    private static MainEntry _singleton;
    public static MainEntry Instance
    {
        get => _singleton;
        set
        {
            if (_singleton == null)
                _singleton = value;
            else if (_singleton != value)
            {
                Debug.Log($"{nameof(MainEntry)} instance already exists, destroying duplicate !");
                Destroy(value);
            }
        }
    }
    #endregion

    protected bool flowInitialized = false;
    SceneState currentState;

    public Flow curFlow;
    public static int sceneNb = 1;

    private void Awake()
    {
        Instance = this;
    }

    public void Initialize(SceneState cs)
    {
        //THIS IS THE FIRST POINT EVER ENTERED BY THIS PROGRAM. (Except for MainEntryCreator.cs, who creates this script and runs this function for the game to start)
        currentState = cs;
        curFlow = InitializeFlowScript(currentState, true);
    }

    // Start is called before the first frame update
    void Start()
    {
        if (flowInitialized)
        {
            if (curFlow == null)
                return; //This means Initialize hasnt been called yet, can happen in weird Awake/Update way (should not though, but be safe)
            curFlow.Initialize();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (flowInitialized)
        {
            if (curFlow == null)
                return; //This means Initialize hasnt been called yet, can happen in weird Awake/Update way (should not though, but be safe)
            curFlow.Update(Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        if (flowInitialized && curFlow != null && NetworkManager.Instance.Client != null)
        {
            NetworkManager.Instance.Client.Update();
            curFlow.FixedUpdate(Time.fixedDeltaTime);
            NetworkManager.Instance.ServerTick++;
        }
    }

    private void OnApplicationQuit()
    {
        NetworkManager.Instance.StopManager();
        _singleton = null;
    }

    public void StopManager(){
        GameObject.Destroy(gameObject);
        _singleton = null;
    }

    public Flow InitializeFlowScript(SceneState flowType, bool sceneAlreadyLoaded)
    {
        Flow newFlow;
        switch (flowType)
        {
            case SceneState.Game:
                newFlow = new GameFlow();
                break;
            case SceneState.Menu:
                newFlow = new MenuFlow();
                break;
            default:
                Debug.Log("Flow could not be loaded " + flowType);
                return null;
        }

        if (!sceneAlreadyLoaded)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded; //Clean any listener already on
            SceneManager.sceneLoaded += OnSceneLoaded; //Delay flow initialization until 
        }
        else
        {
            newFlow.Initialize();
            flowInitialized = true;
        }

        return newFlow;
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //Old scenes might still be listening, so doublecheck
        bool verified = false;
        switch (scene.name)
        {
            case "Game":
                verified = (currentState == SceneState.Game);
                break;
            case "Menu":
                verified = (currentState == SceneState.Menu);
                break;
            default:
                Debug.Log("Switch case not found: " + scene.name);
                break;
        }

        if (verified)
        {
            curFlow.Initialize();
            flowInitialized = true;
        }
        else
            Debug.Log("Unerror! Unverified scene!");
    }

    public void GoToNextFlow(SceneState nextState)
    {
        if (curFlow != null)
        {
            curFlow.EndFlow();
            flowInitialized = false;
        }
        //Assume Flow called Clean already
        //Load the next scene        
        switch (nextState)
        {
            case SceneState.Game:
                SceneManager.LoadScene("Scenes/Game");
                nextState = SceneState.Game;
                break;
            case SceneState.Menu:
                SceneManager.LoadScene("Scenes/Menu");
                nextState = SceneState.Menu;
                break;
            default:
                Debug.LogError("Unhandled Switch: " + nextState);
                return;
        }
        currentState = nextState;
        //Initialize the flow script for the scene
        curFlow = InitializeFlowScript(nextState, false);
    }
}