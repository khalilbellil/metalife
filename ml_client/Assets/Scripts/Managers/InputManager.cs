using UnityEngine;

public class InputManager
{
    #region Singleton Pattern
    private static InputManager instance = null;
    private InputManager() { }
    public static InputManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new InputManager();
            }
            return instance;
        }
    }
    #endregion

    public InputPkg fixedInputPressed; //Every fixed update we fill this
    public InputPkg inputPressed;      //Every update we fill this

    public void Initialize()
    {
        fixedInputPressed = new InputPkg();
        inputPressed = new InputPkg();
    }

    public void UpdateManager()
    {
        FillInputPackage(inputPressed);
    }

    public void FixedUpdateManager()
    {

        FillInputPackage(fixedInputPressed);
    }

    public void StopManager()
    {
        instance = null;
    }

    public void FillInputPackage(InputPkg _toFill)
    {
        _toFill.deltaMouse.x = Input.GetAxis("Mouse X");
        _toFill.deltaMouse.y = Input.GetAxis("Mouse Y");
        if (Camera.main)
            _toFill.mousePosToRay = MousePosToRay(Input.mousePosition);
        else
            Debug.LogWarning("You need to tag a Main Camera !");
        //if (PlayerManager.Instance.player)
        //    _toFill.aimingDirection = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - PlayerManager.Instance.player.transform.position).normalized;
        _toFill.leftMouseButtonPressed = Input.GetMouseButtonDown(0);
        _toFill.leftMouseButtonReleased = Input.GetMouseButtonUp(0);
        _toFill.leftMouseButtonHeld = Input.GetMouseButton(0);
        _toFill.rightMouseButtonPressed = Input.GetMouseButtonDown(1);
        _toFill.middleMouseButtonPressed = Input.GetMouseButtonDown(2);

        _toFill.forward = Input.GetKey(KeyCode.W);
        _toFill.backward = Input.GetKey(KeyCode.S);
        _toFill.left = Input.GetKey(KeyCode.A);
        _toFill.right = Input.GetKey(KeyCode.D);
        _toFill.jump = Input.GetKey(KeyCode.Space);
        _toFill.sprint = Input.GetKey(KeyCode.LeftShift);

        _toFill.inventory = Input.GetKeyDown(KeyCode.I);
        _toFill.interact = Input.GetKeyDown(KeyCode.F);
        _toFill.switchWeapon = Input.GetKeyDown(KeyCode.Alpha1);

        _toFill.anyKey = Input.anyKeyDown;
    }
    public Ray MousePosToRay(Vector3 _mousePos)
    {
        Ray ray = Camera.main.ScreenPointToRay(_mousePos);
        return ray;
    }

    public class InputPkg
    {
        public Vector2 deltaMouse;   //the delta change of mouse position
        public Vector2 aimingDirection;
        public Ray mousePosToRay;
        public bool leftMouseButtonPressed;
        public bool leftMouseButtonReleased;
        public bool leftMouseButtonHeld;
        public bool rightMouseButtonPressed;
        public bool middleMouseButtonPressed;
        public bool forward;
        public bool backward;
        public bool left;
        public bool right;
        public bool jump;
        public bool sprint;
        public bool anyKey;
        public bool inventory;
        public bool interact;
        public bool switchWeapon;

        public override string ToString()
        {
            return string.Format("DeltaMouse[{0}],JumpPressed[{1}],InventoryPressed[{2}],InteractPressed[{3}], SwitchWeaponPressed[{4}] ", deltaMouse, jump, inventory, interact, switchWeapon);
        }
    }
}