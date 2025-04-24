using System.Collections.Generic;
using UnityEngine;

public class RoomInfo : MonoBehaviour
{
    public Vector2Int position;
    public Vector2Int[] SpaceUsed;
    public List<GameObject> Neighbours = new();
    public List<GameObject> Connected = new();
}
