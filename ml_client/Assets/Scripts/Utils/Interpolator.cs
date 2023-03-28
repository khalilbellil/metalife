using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interpolator : MonoBehaviour
{
    private TransformUpdate to;
    private TransformUpdate from;
    private TransformUpdate previous;
    private float timeToReachTarget = 0.2f;

    private Queue<TransformUpdate> futureTransformUpdates = new Queue<TransformUpdate>();

    private void Start()
    {
        to = new TransformUpdate(NetworkManager.Instance.ServerTick, false, transform.position, transform.rotation.eulerAngles);
        from = new TransformUpdate(NetworkManager.Instance.InterpolationTick, false, transform.position, transform.rotation.eulerAngles);
        previous = new TransformUpdate(NetworkManager.Instance.InterpolationTick, false, transform.position, transform.rotation.eulerAngles);
    }

    private void FixedUpdate()
    {
        // If there are no more future updates, return
        if (futureTransformUpdates.Count == 0)
        {
            return;
        }

        // Check if we've reached the next update in the queue
        if (NetworkManager.Instance.ServerTick >= futureTransformUpdates.Peek().Tick)
        {
            // Dequeue the next update
            var nextUpdate = futureTransformUpdates.Dequeue();

            // If this is a teleport, update the position and rotation immediately
            if (nextUpdate.IsTeleport)
            {
                to = nextUpdate;
                from = to;
                previous = to;
                transform.position = to.Position;
                transform.rotation = Quaternion.Euler(to.Rotation);
            }
            // Otherwise, interpolate between the current and next positions and rotations
            else
            {
                previous = to;
                to = nextUpdate;
                from = new TransformUpdate(NetworkManager.Instance.InterpolationTick, false,
                    transform.position, transform.rotation.eulerAngles);

                timeToReachTarget = Mathf.Max((to.Tick - from.Tick) * Time.fixedDeltaTime, 0.2f);

                // Start the coroutine to interpolate the position and rotation
                StartCoroutine(InterpolateCoroutine());
            }
        }
    }

    private IEnumerator InterpolateCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < timeToReachTarget)
        {
            // Interpolate the position and rotation
            InterpolatePosition(elapsed / timeToReachTarget);
            InterpolateRotation(elapsed / timeToReachTarget);

            // Wait for the next fixed frame
            yield return new WaitForFixedUpdate();

            // Update the elapsed time
            elapsed += Time.fixedDeltaTime;
        }

        // Set the final position and rotation to the target values
        transform.position = to.Position;
        transform.rotation = Quaternion.Euler(to.Rotation);
    }

    private void InterpolatePosition(float t)
    {
        // Use Catmull-Rom interpolation for smoother movement
        transform.position = CatmullRom.Interpolate(previous.Position, from.Position, to.Position, to.Position, t);
    }

    private void InterpolateRotation(float t)
    {
        // Use Catmull-Rom interpolation for smoother rotation
        transform.rotation = Quaternion.Euler(CatmullRom.Interpolate(previous.Rotation, from.Rotation, to.Rotation, to.Rotation, t));
    }

    public void AddTransformUpdate(TransformUpdate update)
    {
        futureTransformUpdates.Enqueue(update);
    }
}