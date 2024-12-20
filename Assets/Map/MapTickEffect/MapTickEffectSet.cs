
using UnityEngine;

public class MapTickEffectSet : MapTickEffect
{
    public virtual MapTickEffectType EffectType => MapTickEffectType.Set;
    public MapBotElement mapElement;
    public Vector2Int position = new Vector2Int(0, 0);
    public bool combine = false;

    public override string ToString()
    {
        return $"{EffectType} {mapElement} at {position} (combine: {combine})";
    }
    public object Clone()
    {
        return new MapTickEffectSet
        {
            mapElement = mapElement,
            position = position,
            combine = combine
        };
    }
}