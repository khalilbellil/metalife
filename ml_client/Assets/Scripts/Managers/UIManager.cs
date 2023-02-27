using Riptide;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(Instance);
    }

    public void Initialize()
    {

    }

    public void UpdateManager(float dt)
    {

    }

    public void FixedUpdateManager(float dt)
    {

    }

    public void StopManager()
    {
        _singleton = null;
    }

    public void ConnectClicked()
    {
        usernameField.interactable = false;
        connectUI.SetActive(false);

        NetworkManager.Instance.Connect(IpAddressField.text, "7777");

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void BackToMain()
    {
        if(usernameField)
            usernameField.interactable = true;
        if(connectUI)
            connectUI.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
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