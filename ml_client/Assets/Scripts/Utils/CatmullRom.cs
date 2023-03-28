using UnityEngine;

public static class CatmullRom
{
    // Interpolates between p1 and p2 using Catmull-Rom interpolation with tangent vectors t1 and t2
    public static Vector3 InterpolateT(Vector3 p1, Vector3 t1, Vector3 p2, Vector3 t2, float t)
    {
        float t2Squared = t * t;
        float t3 = t2Squared * t;

        Vector3 a = 2f * p1 - 2f * p2 + t1 + t2;
        Vector3 b = -3f * p1 + 3f * p2 - 2f * t1 - t2;
        Vector3 c = t1;
        Vector3 d = p1;

        return a * t3 + b * t2Squared + c * t + d;
    }

    // Interpolates between p1 and p2 using Catmull-Rom interpolation with no tangent vectors
    public static Vector3 Interpolate(Vector3 p1, Vector3 p2, float t)
    {
        return Interpolate(p1, (p2 - p1).normalized, p2, (p2 - p1).normalized, t);
    }

    // Interpolates between a series of control points using Catmull-Rom interpolation
    public static Vector3 Interpolate(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        Vector3 t1 = 0.5f * (p2 - p0);
        Vector3 t2 = 0.5f * (p3 - p1);

        return InterpolateT(p1, t1, p2, t2, t);
    }

    // Interpolates between a series of control points using Catmull-Rom interpolation
    public static Quaternion Interpolate(Quaternion q0, Quaternion q1, Quaternion q2, Quaternion q3, float t)
    {
        Vector3 p0 = q0.eulerAngles;
        Vector3 p1 = q1.eulerAngles;
        Vector3 p2 = q2.eulerAngles;
        Vector3 p3 = q3.eulerAngles;

        Vector3 t1 = 0.5f * (p2 - p0);
        Vector3 t2 = 0.5f * (p3 - p1);

        return Quaternion.Euler(Interpolate(p1, t1, p2, t2, t));
    }
}