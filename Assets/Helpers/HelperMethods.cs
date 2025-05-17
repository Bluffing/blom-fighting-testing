
using UnityEngine;

public static class HelperMethods
{
    public static Vector2 FullScale(Transform t)
    {
        Vector2 scale = new Vector2(1, 1);
        while (t != null)
        {
            scale *= t.localScale;
            t = t.parent;
        }
        return scale;
    }
}