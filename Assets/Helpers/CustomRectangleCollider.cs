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
        Vector2.zero,
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

        _corners[0] = absoluteCenter + new Vector2(cos13, sin13);
        _corners[1] = absoluteCenter + new Vector2(cos24, sin24);
        _corners[2] = absoluteCenter + new Vector2(-cos13, -sin13);
        _corners[3] = absoluteCenter + new Vector2(-cos24, -sin24);

        cornerUpdated = true;
    }

    public bool IsTouching(CustomRectangleCollider other)
    {
        bool IsBetween(Vector2 p, Vector2 a, Vector2 b) =>
                ((p.x >= a.x && p.x <= b.x) || (p.x <= a.x && p.x >= b.x)) &&
                ((p.y >= a.y && p.y <= b.y) || (p.y <= a.y && p.y >= b.y));
        for (int i = 0; i < 4; i++)
        {
            if (IsBetween(Corners[i], other.Corners[0], other.Corners[2]))
                return true;

            if (IsBetween(other.Corners[i], Corners[0], Corners[2]))
                return true;
        }

        return false;
    }

    // private void OnDrawGizmos()
    // {
    //     Color[] colors = { Color.red, Color.blue, Color.yellow, Color.cyan };
    //     for (int i = 0; i < Corners.Length; i++)
    //     {
    //         Gizmos.color = colors[i % colors.Length];
    //         Gizmos.DrawSphere(Corners[i], 0.025f);
    //     }
    // }
}

[CustomEditor(typeof(CustomRectangleCollider))]
public class CustomRectangleColliderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        CustomRectangleCollider script = (CustomRectangleCollider)target;
    }
}