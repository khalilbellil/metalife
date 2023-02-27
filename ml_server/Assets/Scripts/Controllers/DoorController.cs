using UnityEngine;

public class DoorController : MonoBehaviour
{
    public ushort id = 0;
    bool trig;
    public bool open;
    public float smooth = 2.0f;
    public float DoorOpenAngle = 90.0f;
    private Vector3 defaulRot;
    private Vector3 openRot;

    void Start()
    {
        if(id != 0)
        {
            DoorManager.Instance.list.Add(id, this);
        }
        defaulRot = transform.eulerAngles;
        openRot = new Vector3(defaulRot.x, defaulRot.y + DoorOpenAngle, defaulRot.z);
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
            trig = true;
        }
    }
    private void OnTriggerExit(Collider coll)
    {
        if (coll.tag == "Player")
        {
            trig = false;
        }
    }
}