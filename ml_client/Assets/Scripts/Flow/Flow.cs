using UnityEngine;

public class Flow
{
    protected bool initialized;

    public virtual void Initialize()
    {
        initialized = true;
    }

    public virtual void Update(float dt)
    {


    }

    public virtual void FixedUpdate(float dt)
    {


    }

    public virtual void EndFlow()
    {
        initialized = false;
    }
}