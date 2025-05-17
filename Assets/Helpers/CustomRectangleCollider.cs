using UnityEditor;
using UnityEngine;

public class CustomRectangleCollider : MonoBehaviour
{
    public Vector2 Center;
    public Vector2 Size;

    private bool scaleUpdated = false;
    private Vector2 _realScale = new Vector2(1, 1);
    public Vector2 RealScale
    {
        get
        {
            if (!scaleUpdated)
            {
                _realScale = HelperMethods.FullScale(transform);
                scaleUpdated = true;
            }
            return _realScale;
        }
    }

    private bool cornerUpdated = false;
    private Vector2[] _corners = new Vector2[4] {
        Vector2.zero,
        Vector2.zero,
        Vector2.zero,
        Vector2.zero
    };
    public Vector2[] Corners
    {
        get
        {
            if (!cornerUpdated)
                UpdateCorners();
            return _corners;
        }
    }

    public void LateUpdate()
    {
        cornerUpdated = false;
        scaleUpdated = false;
    }

    private void UpdateCorners()
    {
        // Size
        var realSize = Size * RealScale;

        // Angles
        var localAngle = Mathf.Atan(realSize.y / realSize.x);
        var objAngle = transform.rotation.eulerAngles.z * Mathf.Deg2Rad;
        var angle = localAngle + objAngle;

        // Center
        var centerDist = Mathf.Sqrt(Center.x * Center.x * RealScale.x * RealScale.x + Center.y * Center.y * RealScale.y * RealScale.y);
        var realCenter = centerDist * new Vector2(Mathf.Cos(objAngle + 90 * Mathf.Deg2Rad),
                                                  Mathf.Sin(objAngle + 90 * Mathf.Deg2Rad));
        var absoluteCenter = (Vector2)transform.position + realCenter;

        // Corners
        var length = Mathf.Sqrt(realSize.x * realSize.x + realSize.y * realSize.y) / 2;
        var cos13 = Mathf.Cos(angle) * length;
        var sin13 = Mathf.Sin(angle) * length;
        var cos24 = Mathf.Cos(180 * Mathf.Deg2Rad - localAngle + objAngle) * length;
        var sin24 = Mathf.Sin(180 * Mathf.Deg2Rad - localAngle + objAngle) * length;

        Corners[0] = absoluteCenter + new Vector2(cos13, sin13);
        Corners[1] = absoluteCenter + new Vector2(cos24, sin24);
        Corners[2] = absoluteCenter + new Vector2(-cos13, -sin13);
        Corners[3] = absoluteCenter + new Vector2(-cos24, -sin24);

        cornerUpdated = true;
    }

    public bool IsTouching(CustomRectangleCollider other)
    {
        bool IsBetween(Vector2 p, Vector2 a, Vector2 b) =>
                ((p.x >= a.x && p.x <= b.x) || (p.x <= a.x && p.x >= b.x)) &&
                ((p.y >= a.y && p.y <= b.y) || (p.y <= a.y && p.y >= b.y));
        for (int i = 0; i < 4; i++)
        {
            if (IsBetween(Corners[i], other.Corners[0], other.Corners[3]))
                return true;

            if (IsBetween(other.Corners[i], Corners[0], Corners[3]))
                return true;
        }

        return false;
    }

    // public bool IsTouching(CustomRectangleCollider other)
    // {
    //     // Scale
    //     var realScale = HelperMethods.FullScale(transform);
    //     // Other Scale
    //     var otherRealScale = HelperMethods.FullScale(other.transform);

    //     // Size
    //     var realSize = Size * realScale;
    //     // Other Size
    //     var otherRealSize = other.Size * otherRealScale;

    //     // Angles
    //     var localAngle = Mathf.Atan(realSize.y / realSize.x);
    //     var objAngle = transform.rotation.eulerAngles.z * Mathf.Deg2Rad;
    //     var angle = localAngle + objAngle;
    //     // Other Angles
    //     var otherLocalAngle = Mathf.Atan(otherRealSize.y / otherRealSize.x);
    //     var otherObjAngle = other.transform.rotation.eulerAngles.z * Mathf.Deg2Rad;
    //     var otherAngle = otherLocalAngle + otherObjAngle;

    //     // Center
    //     var centerDist = Mathf.Sqrt(Center.x * Center.x * realScale.x * realScale.x + Center.y * Center.y * realScale.y * realScale.y);
    //     var realCenter = centerDist * new Vector2(Mathf.Cos(objAngle + 90 * Mathf.Deg2Rad),
    //                                               Mathf.Sin(objAngle + 90 * Mathf.Deg2Rad));
    //     var absoluteCenter = (Vector2)transform.position + realCenter;
    //     // Other Center
    //     var otherCenterDist = Mathf.Sqrt(other.Center.x * other.Center.x * otherRealScale.x * otherRealScale.x +
    //                                      other.Center.y * other.Center.y * otherRealScale.y * otherRealScale.y);
    //     var otherRealCenter = otherCenterDist * new Vector2(Mathf.Cos(otherObjAngle + 90 * Mathf.Deg2Rad),
    //                                                         Mathf.Sin(otherObjAngle + 90 * Mathf.Deg2Rad));
    //     var otherAbsoluteCenter = (Vector2)transform.position + otherRealCenter;

    //     // Corners
    //     var length = Mathf.Sqrt(realSize.x * realSize.x + realSize.y * realSize.y) / 2;
    //     var cos13 = Mathf.Cos(angle) * length;
    //     var sin13 = Mathf.Sin(angle) * length;
    //     var cos24 = Mathf.Cos(180 * Mathf.Deg2Rad - localAngle + objAngle) * length;
    //     var sin24 = Mathf.Sin(180 * Mathf.Deg2Rad - localAngle + objAngle) * length;
    //     Vector2[] corners = new Vector2[4] {
    //         absoluteCenter + new Vector2(cos13, sin13),
    //         absoluteCenter + new Vector2(cos24, sin24),
    //         absoluteCenter + new Vector2(-cos13, -sin13),
    //         absoluteCenter + new Vector2(-cos24, -sin24),
    //     };
    //     Corners = corners.Clone() as Vector2[];
    //     // Other Corners
    //     var otherLength = Mathf.Sqrt(otherRealSize.x * otherRealSize.x + otherRealSize.y * otherRealSize.y) / 2;
    //     var otherCos13 = Mathf.Cos(otherAngle) * length;
    //     var otherSin13 = Mathf.Sin(otherAngle) * length;
    //     var otherCos24 = Mathf.Cos(180 * Mathf.Deg2Rad - otherLocalAngle + otherObjAngle) * otherLength;
    //     var otherSin24 = Mathf.Sin(180 * Mathf.Deg2Rad - otherLocalAngle + otherObjAngle) * otherLength;
    //     Vector2[] otherCorners = new Vector2[4] {
    //         otherAbsoluteCenter + new Vector2(otherCos13, otherSin13),
    //         otherAbsoluteCenter + new Vector2(otherCos24, otherSin24),
    //         otherAbsoluteCenter + new Vector2(-otherCos13, -otherSin13),
    //         otherAbsoluteCenter + new Vector2(-otherCos24, -otherSin24),
    //     };

    //     bool IsBetween(Vector2 p, Vector2 a, Vector2 b) =>
    //             ((p.x >= a.x && p.x <= b.x) || (p.x <= a.x && p.x >= b.x)) &&
    //             ((p.y >= a.y && p.y <= b.y) || (p.y <= a.y && p.y >= b.y));
    //     for (int i = 0; i < 4; i++)
    //     {
    //         if (IsBetween(corners[i], otherCorners[0], otherCorners[3]))
    //             return true;

    //         if (IsBetween(otherCorners[i], corners[0], corners[3]))

    //             return true;
    //     }

    //     return false;
    // }

    private void OnDrawGizmos()
    {
        // Color[] colors = { Color.red, Color.blue, Color.yellow, Color.cyan };
        // for (int i = 0; i < Corners.Length; i++)
        // {
        //     Gizmos.color = colors[i % colors.Length];
        //     Gizmos.DrawSphere(Corners[i], 0.005f);
        // }
    }
}
[CustomEditor(typeof(CustomRectangleCollider))]
public class CustomRectangleColliderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        CustomRectangleCollider script = (CustomRectangleCollider)target;

        // if (GUILayout.Button("Show Collider"))
        // {
        //     var realScale = HelperMethods.FullScale(script.transform);
        //     var realSize = script.Size * realScale;

        //     var localAngle = Mathf.Atan(realSize.y / realSize.x);
        //     var objAngle = script.transform.rotation.eulerAngles.z * Mathf.Deg2Rad;
        //     var angle = localAngle + objAngle;

        //     var centerDist = Mathf.Sqrt(script.Center.x * script.Center.x * realScale.x * realScale.x + script.Center.y * script.Center.y * realScale.y * realScale.y);
        //     var realCenter = centerDist * new Vector2(Mathf.Cos(objAngle + 90 * Mathf.Deg2Rad),
        //                                               Mathf.Sin(objAngle + 90 * Mathf.Deg2Rad));

        //     var length = Mathf.Sqrt(realSize.x * realSize.x + realSize.y * realSize.y) / 2;
        //     var cos13 = Mathf.Cos(angle) * length;
        //     var sin13 = Mathf.Sin(angle) * length;
        //     var cos24 = Mathf.Cos(180 * Mathf.Deg2Rad - localAngle + objAngle) * length;
        //     var sin24 = Mathf.Sin(180 * Mathf.Deg2Rad - localAngle + objAngle) * length;

        //     var absoluteCenter = (Vector2)script.transform.position + realCenter;
        //     // Debug.Log($"realCenter: {realCenter}, topright : {new Vector2(cos13, sin13)}, bleak : {realSize}");

        //     script.Corners = new Vector2[4] {
        //         absoluteCenter + new Vector2(cos13, sin13),
        //         absoluteCenter + new Vector2(cos24, sin24),
        //         absoluteCenter + new Vector2(-cos13, -sin13),
        //         absoluteCenter + new Vector2(-cos24, -sin24),
        //     };
        // }
        // if (GUILayout.Button("Clear Collider"))
        //     script.Corners = new Vector2[0];
        if (GUILayout.Button("bleak"))
        {
            Debug.Log($"bloo {script.transform.name}");
        }
    }
}