using Riptide;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private static UIManager _singleton;
    public static UIManager Singleton
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

    [Header("Connect")]
    [SerializeField] private GameObject connectUI;
    [SerializeField] private InputField usernameField;

    private void Awake()
    {
        Singleton = this;
    }

    public void ConnectClicked()
    {
        usernameField.interactable = false;
        connectUI.SetActive(false);

        NetworkManager.Singleton.Connect();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void BackToMain()
    {
        usernameField.interactable = true;
        connectUI.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    #region Messages

    public void SendName()
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerId.name);
        message.AddString(usernameField.text);
        NetworkManager.Singleton.Client.Send(message);
    }

    #endregion
}
