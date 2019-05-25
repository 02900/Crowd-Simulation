using UnityEngine;

public static class ExtensionMethods
{
    public static Vector2 ToXZ(this Vector3 v)
    {
        return new Vector2(v.x, v.z);
    }

    public static Vector2 ToXY(this Vector3 v)
    {
        return new Vector2(v.x, v.y);
    }

    public static Vector3 Vector2ToVector3 (Vector2 v)
    {
        return new Vector3(v.x, 0, v.y);
    }
    public static Vector2 RotateVector(Vector2 v, float angle)
    {
        float radian = angle * Mathf.Deg2Rad;
        float x = v.x * Mathf.Cos(radian) - v.y * Mathf.Sin(radian);
        float y = v.x * Mathf.Sin(radian) + v.y * Mathf.Cos(radian);
        return new Vector2(x,y);
    } 

    //Where is p in relation to a-b
    // < 0 -> to the right
    // = 0 -> on the line
    // > 0 -> to the left
    public static float IsAPointLeftOfVectorOrOnTheLine(Vector2 a, Vector2 b, Vector2 p)
    {
        float determinant = (a.x - p.x) * (b.y - p.y) - (a.y - p.y) * (b.x - p.x);
        return determinant;
    }

    public static Vector3 Orientation2Vector(float orientation, float angleIsGlobal = 0)
    {
        orientation += angleIsGlobal;
        Vector3 vector = Vector3.zero;
        vector.x = Mathf.Sin(orientation * Mathf.Deg2Rad) * 1.0f;
        vector.z = Mathf.Cos(orientation * Mathf.Deg2Rad) * 1.0f;
        return vector.normalized;
    }

    // returns -1 when to the left, 1 to the right, and 0 for forward/backward
    public static int AngleDir(Vector3 fwd, Vector3 targetDir, Vector3 up)
    {
        Vector3 perp = Vector3.Cross(fwd, targetDir);
        float dir = Vector3.Dot(perp, up);

        if (dir > 0) return 1;
        else if (dir < 0) return -1;
        else return 0;
    }

    public static int AngleDir2D(Vector2 A, Vector2 B, Vector2 right)
    {
        Vector2 dir = A - B;
        float dot = Vector2.Dot(dir, right);
        return dot > 0 ? 1 : dot < 0? -1 : 0;
    }

    public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
    {
        return RotatePointAroundPivot(point, pivot, Quaternion.Euler(angles));
    }

    public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
    {
        return rotation * (point - pivot) + pivot;
    }
}