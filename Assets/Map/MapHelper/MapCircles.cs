using System.Collections.Generic;
using UnityEngine;

public static class MapCircles
{
    #region Circle Edge

    private static List<Vector2Int[]> mapCirclesList = new List<Vector2Int[]> {
        new Vector2Int[] { new Vector2Int(0, 0) },
        new Vector2Int[] { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(0, -1) },
    };
    static Vector2Int[] cornerVector4 = new Vector2Int[] {
        new Vector2Int(1, 1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, 1),
        new Vector2Int(-1, -1),
    };
    private static void addEdgeVectors(int radius)
    {
        List<Vector2Int> edge = new List<Vector2Int>();
        for (int x = 0; x < radius; x++)
            for (int y = 0; y < radius; y++)
            {
                int lengthSquared = x * x + y * y;
                int radiusSquared = radius * radius;
                int radiusMin1Squared = (radius - 1) * (radius - 1);

                if (lengthSquared >= radiusSquared || lengthSquared < radiusMin1Squared)
                    continue;

                Vector2Int relativePoint = new Vector2Int(x, y);
                for (int i = 0; i < cornerVector4.Length; i++)
                    edge.Add(cornerVector4[i] * relativePoint);
            }
        mapCirclesList.Add(edge.ToArray());
    }
    public static Vector2Int[] GetEdgeOfCircleVectors(int radius)
    {
        if (mapCirclesList.Count < radius)
            for (int i = mapCirclesList.Count; i <= radius; i++)
                addEdgeVectors(i);
        return mapCirclesList[radius - 1];
    }

    #endregion
}