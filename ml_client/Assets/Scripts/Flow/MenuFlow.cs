using UnityEngine;
using UnityEngine.UI;

public class MenuFlow : Flow
{
    public override void Initialize()
    {
        base.Initialize();
        InputManager.Instance.Initialize();
        UIManager.Instance.Initialize();
        PrefabManager.Instance.Initialize();
    }

    public override void Update(float dt)
    {
        InputManager.Instance.UpdateManager();
        UIManager.Instance.UpdateManager(dt);
    }

    public override void FixedUpdate(float dt)
    {
        InputManager.Instance.FixedUpdateManager();
        UIManager.Instance.FixedUpdateManager(dt);
    }

    public override void EndFlow()
    {
        base.EndFlow();
    }
}