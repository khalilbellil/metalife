using Riptide;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum TextColor
{
    white = 1,
    red,
    green,
    blue,
}

public class UIManager : MonoBehaviour
{
    #region Singleton Pattern
    private static UIManager _singleton;
    public static UIManager Instance
    {
        get => _singleton;
        set
        {
            if (_singleton == null)
                _singleton = value;
            else if (_singleton != value)
            {
                Debug.Log($"{nameof(UIManager)} instance already exists, destroying duplicate !");
                Destroy(value);
            }
        }
    }
    #endregion

    [SerializeField] private GameObject connectUI;
    [SerializeField] private InputField usernameField;
    [SerializeField] private InputField IpAddressField;
    [SerializeField] public TMP_Text centerText;
    [SerializeField] public TMP_Text pingText;
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private TMP_Text PositionText;
    [SerializeField] private TMP_Text RotationText;
    [SerializeField] private TMP_Text TickText;
    [SerializeField] private GameObject escapeMenu;
    [SerializeField] private Toggle displayPositionToggle;
    [SerializeField] private Toggle displayRotationToggle;
    [SerializeField] private Button disconnectButton;
    [SerializeField] private Button quitGameButton;
    [SerializeField] private TMP_Text connectionStatusText;
    private bool isCursorVisible = true;
    private bool isInGame = false;

    private void Awake()
    {
        if(Instance == null){
            Instance = this;
            DontDestroyOnLoad(Instance);
        }
    }
    public void Initialize()
    {

    }
    public void UpdateManager(float dt)
    {
        if(isInGame){
            if (Input.GetKeyDown(KeyCode.Escape)){
                ToggleCursorMode();
                escapeMenu.SetActive(!escapeMenu.activeSelf);
            }
            if (Input.GetKeyDown(KeyCode.BackQuote)){
                ToggleCursorMode();
            }
            UpdatePingText();
        }
    }
    public void FixedUpdateManager(float dt)
    {

    }
    public void StopManager()
    {
        GameObject.Destroy(gameObject);
        _singleton = null;
    }
    public void ConnectClicked()
    {
        UpdateConnectionStatusText("Loading...");
        NetworkManager.Instance.Connect(IpAddressField.text, "7777");
    }
    public void DisconnectClicked()
    {
        BackToMain();
    }
    public void QuitGameClicked()
    {
        Application.Quit();
    }
    public void BackToMain()
    {
        if(connectUI)
            connectUI.SetActive(true);
        if(escapeMenu)
            escapeMenu.SetActive(false);
        isInGame = false;
        NetworkManager.Instance.StopManager();
        MainEntry.Instance.GoToNextFlow(SceneState.Menu);
        StopManager();
    }
    public void InitializeGameUI(int id, string username){
        isInGame = true;
        usernameText.SetText(string.IsNullOrEmpty(username) ? "Guest" : username + " (" + id.ToString() + ")");
        usernameText.gameObject.SetActive(true);
        pingText.gameObject.SetActive(true);
    }
    private void ToggleCursorMode()
    {
        isCursorVisible = !isCursorVisible;
        Cursor.visible = isCursorVisible;
        Cursor.lockState = isCursorVisible ? CursorLockMode.None : CursorLockMode.Locked;
    }
    public void ToggleMenuUI(){
        connectUI.SetActive(!connectUI.activeSelf);
        ToggleCursorMode();
    }
    public void UpdateConnectionStatusText(string text = "", TextColor color = TextColor.white){
        GameObject go = connectionStatusText.gameObject;
        if(text == ""){
            connectionStatusText.SetText(text);
            go.SetActive(false);
        }else{
            Color c = new Color(255,255,255,255);
            switch (color)
            {
                case TextColor.white:
                    break;
                case TextColor.red:
                    c = new Color(255,0,0,255);
                    break;
                case TextColor.green:
                    c = new Color(0,255,0,255);
                    break;
                case TextColor.blue:
                    c = new Color(0,0,255,255);
                    break;
                default:
                    // code block
                    break;
            }
            connectionStatusText.SetText(text);
            connectionStatusText.color = c;
            go.SetActive(true);
        }
    }
    public void TogglePositionText(){
        PositionText.gameObject.SetActive(!PositionText.gameObject.activeSelf);
    }
    public void ToggleRotationText(){
        RotationText.gameObject.SetActive(!RotationText.gameObject.activeSelf);
    }
    public void SetPositionText(string position){
        PositionText.SetText("Pos: " + position);
    }
    public void SetRotationText(string rotation){
        RotationText.SetText("Y Euler Angle: " + rotation);
    }
    public bool PositionTextIsOn(){
        return PositionText.gameObject.activeSelf;
    }
    public bool RotationTextIsOn(){
        return RotationText.gameObject.activeSelf;
    }
    public void ToggleTickText(){
        TickText.gameObject.SetActive(!TickText.gameObject.activeSelf);
    }
    public void SetTickText(string text){
        TickText.SetText("Tick: " + text);
    }
    public bool TickStatusIsOn(){
        return TickText.gameObject.activeSelf;
    }
    private void UpdatePingText(){
        int ping = Mathf.RoundToInt(NetworkManager.Instance.Client.RTT);
        if(ping > 99){
            pingText.color = Color.red;
        }else{
            pingText.color = Color.green;
        }
        pingText.SetText(ping + "ms");
    }

    #region Messages
    public void SendLoginInfo()
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerId.loginInfo);
        message.AddString(usernameField.text);
        NetworkManager.Instance.Client.Send(message);
    }
    #endregion
}