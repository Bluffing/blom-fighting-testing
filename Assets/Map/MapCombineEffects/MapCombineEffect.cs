using UnityEngine;

public abstract class MapCombineEffect
{
    public abstract MapCombineEffectType EffectType { get; }
    public Vector2Int position = new Vector2Int(0, 0);
    public uint tickDelay = 0;
}
