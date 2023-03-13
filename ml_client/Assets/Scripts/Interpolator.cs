using System.Collections.Generic;
using UnityEngine;

public class Interpolator : MonoBehaviour
{
    [SerializeField] private float timeElapsed = 0f;
    [SerializeField] private float timeToReachTarget = 0.05f;
    [SerializeField] private float movementThreshold = 0.05f;

    private readonly List<TransformUpdate> futureTransformUpdates = new List<TransformUpdate>();

    private float squareMovementThreshold;
    private TransformUpdate to;
    private TransformUpdate from;
    private TransformUpdate previous;

    private void Start()
    {
        squareMovementThreshold = movementThreshold * movementThreshold;
        to = new TransformUpdate(NetworkManager.Instance.ServerTick, false, transform.position);
        from = new TransformUpdate(NetworkManager.Instance.InterpolationTick, false, transform.position);
        previous = new TransformUpdate(NetworkManager.Instance.InterpolationTick, false, transform.position);
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < futureTransformUpdates.Count; i++)
        {
            if (NetworkManager.Instance.ServerTick >= futureTransformUpdates[i].Tick)
            {
                if (futureTransformUpdates[i].IsTeleport)
                {
                    to = futureTransformUpdates[i];
                    from = to;
                    previous = to;
                    transform.position = to.Position;
                }
                else
                {
                    previous = to;
                    to = futureTransformUpdates[i];
                    from = new TransformUpdate(NetworkManager.Instance.InterpolationTick, false, 
                        transform.position);
                }

                futureTransformUpdates.RemoveAt(i);
                i--;
                timeElapsed = 0f;
                //timeToReachTarget = (to.Tick - from.Tick) * Time.fixedDeltaTime;
                timeToReachTarget = Mathf.Max((to.Tick - from.Tick), 0.5f) * Time.fixedDeltaTime;
            }
        }

        timeElapsed += Time.deltaTime;
        InterpolatePosition(timeElapsed / timeToReachTarget);
        InterpolateRotation(timeElapsed / timeToReachTarget);
    }

    private void InterpolatePosition(float lerpAmount)
    {
        if ((to.Position - previous.Position).sqrMagnitude < squareMovementThreshold)
        {
            if (to.Position != from.Position)
                transform.position = Vector3.Lerp(from.Position, to.Position, lerpAmount);

            return;
        }

        transform.position = Vector3.LerpUnclamped(from.Position, to.Position, lerpAmount);
    }

    private void InterpolateRotation(float lerpAmount)
    {
        if ((to.Rotation - previous.Rotation).sqrMagnitude < squareMovementThreshold)
        {
            if (to.Rotation != from.Rotation)
                transform.rotation = Quaternion.Lerp(Quaternion.Euler(from.Rotation),
                    Quaternion.Euler(to.Rotation), lerpAmount);
            return;
        }

        transform.rotation = Quaternion.LerpUnclamped(Quaternion.Euler(from.Rotation),
                    Quaternion.Euler(to.Rotation), lerpAmount);
    }

    public void NewUpdate(ushort tick, bool isTeleport, Vector3 position, Vector3 rotation)
    {
        if (tick <= NetworkManager.Instance.InterpolationTick && !isTeleport)
            return;

        for (int i = 0; i < futureTransformUpdates.Count; i++)
        {
            if (tick < futureTransformUpdates[i].Tick)
            {
                futureTransformUpdates.Insert(i, new TransformUpdate(tick, isTeleport, position, rotation));
                return;
            }
        }

        futureTransformUpdates.Add(new TransformUpdate(tick, isTeleport, position, rotation));
    }
}