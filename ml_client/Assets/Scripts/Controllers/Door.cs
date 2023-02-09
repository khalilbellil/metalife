using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Door : MonoBehaviour
{

    bool trig, open;
    public float smooth = 2.0f;
    public float DoorOpenAngle = 90.0f;
    private Vector3 defaulRot;
    private Vector3 openRot;
    public TMP_Text txt;

    void Start()
    {
        defaulRot = transform.eulerAngles;
        openRot = new Vector3(defaulRot.x, defaulRot.y + DoorOpenAngle, defaulRot.z);
        txt = UIManager.Instance.centerText;
    }

    void fUpdate()
    {
        if (open)
        {
            transform.eulerAngles = Vector3.Slerp(transform.eulerAngles, openRot, Time.deltaTime * smooth);
        }
        else
        {
            transform.eulerAngles = Vector3.Slerp(transform.eulerAngles, defaulRot, Time.deltaTime * smooth);
        }
        if (InputManager.Instance.inputPressed.interact && trig)
        {
            open = !open;
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
    private void OnTriggerEnter(Collider coll)
    {
        Debug.Log("TriggerEnter");
        if (coll.tag == "Player")
        {
            txt.alpha = 255f;
            trig = true;
        }
    }
    private void OnTriggerExit(Collider coll)
    {
        Debug.Log("TriggerExit");
        if (coll.tag == "Player")
        {
            txt.alpha = 0f;
            trig = false;
            txt.text = " ";
        }
    }
}