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

    public void FillInputPackage(InputPkg _toFill)
    {
        _toFill.deltaMouse.x = Input.GetAxis("Mouse X");
        _toFill.deltaMouse.y = Input.GetAxis("Mouse Y");
        _toFill.mousePosToRay = _toFill.MousePosToRay(Input.mousePosition);
        //if (PlayerManager.Instance.player)
        //    _toFill.aimingDirection = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - PlayerManager.Instance.player.transform.position).normalized;
        _toFill.leftMouseButtonPressed = Input.GetMouseButtonDown(0);
        _toFill.leftMouseButtonReleased = Input.GetMouseButtonUp(0);
        _toFill.leftMouseButtonHeld = Input.GetMouseButton(0);
        _toFill.rightMouseButtonPressed = Input.GetMouseButtonDown(1);
        _toFill.middleMouseButtonPressed = Input.GetMouseButtonDown(2);

        _toFill.anyKeyPressed = Input.anyKeyDown;
        _toFill.inventoryPressed = Input.GetButtonDown("Inventory");
        _toFill.interactPressed = Input.GetButtonDown("Interaction");
        _toFill.dirPressed.x = Input.GetAxis("Horizontal");
        _toFill.dirPressed.y = Input.GetAxis("Vertical");
        _toFill.dirPressed.Normalize();
        _toFill.jumpPressed = Input.GetButtonDown("Jump");
        _toFill.sprintPressed = Input.GetButtonDown("Sprint");
        _toFill.sprintUnPressed = Input.GetButtonUp("Sprint");
        _toFill.switchWeaponPressed = Input.GetButtonDown("Switch Weapon");
    }

    public void StopManager()
    {
        instance = null;
    }

    public class InputPkg
    {
        public Vector2 dirPressed;   //side to side and foward and back
        public Vector2 deltaMouse;   //the delta change of mouse position
        public Vector2 aimingDirection;
        public Ray mousePosToRay;
        public bool leftMouseButtonPressed;
        public bool leftMouseButtonReleased;
        public bool leftMouseButtonHeld;
        public bool rightMouseButtonPressed;
        public bool middleMouseButtonPressed;
        public bool jumpPressed;
        public bool sprintPressed;
        public bool sprintUnPressed;
        public bool anyKeyPressed;
        public bool inventoryPressed;
        public bool interactPressed;
        public bool switchWeaponPressed;

        public override string ToString()
        {
            return string.Format("DirPressed[{0}],DeltaMouse[{1}],JumpPressed[{2}],InventoryPressed[{3}],InteractPressed[{4}], SwitchWeaponPressed[{5}] ", dirPressed, deltaMouse, jumpPressed, inventoryPressed, interactPressed, switchWeaponPressed);
        }

        public Ray MousePosToRay(Vector3 _mousePos)
        {
            Ray ray = Camera.main.ScreenPointToRay(_mousePos);
            return ray;
        }
    }
}