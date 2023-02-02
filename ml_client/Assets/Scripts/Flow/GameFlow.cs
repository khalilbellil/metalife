using UnityEngine;

public class GameFlow : Flow
{
    public override void Initialize()
    {
        base.Initialize();
        InputManager.Instance.Initialize();
        UIManager.Instance.Initialize();
        //PlayerManager.Instance.Initialize();
    }

    public override void Update(float dt)
    {
        InputManager.Instance.UpdateManager();
        UIManager.Instance.UpdateManager(dt);
        //PlayerManager.Instance.UpdateManager(dt);
    }

    public override void FixedUpdate(float dt)
    {
        InputManager.Instance.FixedUpdateManager();
        UIManager.Instance.FixedUpdateManager(dt);
        //PlayerManager.Instance.FixedUpdateManager(dt);
    }

    public override void EndFlow()
    {
        InputManager.Instance.StopManager();
        //PlayerManager.Instance.StopManager();
        base.EndFlow();
    }
}