using Riptide;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Door : MonoBehaviour
{
    public ushort id;
    bool trig;
    public bool open;
    public float smooth = 2.0f;
    public float DoorOpenAngle = 90.0f;
    private Vector3 defaulRot;
    private Vector3 openRot;
    private TMP_Text txt;

    void Start()
    {
        if (id != 0)
        {
            DoorManager.list.Add(id, this);
        }
        defaulRot = transform.eulerAngles;
        openRot = new Vector3(defaulRot.x, defaulRot.y + DoorOpenAngle, defaulRot.z);
        txt = UIManager.Instance.centerText;
    }

    void Update()
    {
        if (InputManager.Instance.inputPressed.interact && trig)
        {
            //open = !open;
            SendDoorOpened(open);
        }
        if (trig)
        {
            if (open)
            {
                txt.text = "Close F";
            }
            else
            {
                txt.text = "Open F";
            }
        }
    }
    private void FixedUpdate()
    {
        if (open)
        {
            transform.eulerAngles = Vector3.Slerp(transform.eulerAngles, openRot, Time.fixedDeltaTime * smooth);
        }
        else
        {
            transform.eulerAngles = Vector3.Slerp(transform.eulerAngles, defaulRot, Time.fixedDeltaTime * smooth);
        }
    }
    private void OnTriggerEnter(Collider coll)
    {
        if (coll.tag == "Player")
        {
            UIManager.Instance.centerText.gameObject.SetActive(true);
            trig = true;
        }
    }
    private void OnTriggerExit(Collider coll)
    {
        if (coll.tag == "Player")
        {
            UIManager.Instance.centerText.gameObject.SetActive(false);
            trig = false;
            txt.text = " ";
        }
    }

    public void SendDoorOpened(bool value)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerId.doorOpened);
        message.AddUShort(id);
        NetworkManager.Instance.Client.Send(message);
    }
}