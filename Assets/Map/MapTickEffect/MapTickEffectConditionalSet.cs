using System;
using UnityEngine;

public class MapTickEffectConditionalSet : MapTickEffectSet
{
    public override MapTickEffectType EffectType => MapTickEffectType.ConditionalSet;
    public Func<MapBotElement, bool> ifCondition;
    public MapTickEffect mapTickEffectIfTrue { set; private get; } = null;
    public MapTickEffect MapTickEffectIfTrue(int x, int y)
    {
        if (mapTickEffectIfTrue == null) return null;
        var tickEffect = (MapTickEffect)mapTickEffectIfTrue.Clone();
        return MapTickEffectFactory.CloneWithPosition(tickEffect, new Vector2Int(x, y));
    }

    public MapTickEffect mapTickEffectIfFalse { set; private get; } = null;
    public MapTickEffect MapTickEffectIfFalse(int x, int y)
    {
        if (mapTickEffectIfFalse == null) return null;
        var tickEffect = (MapTickEffect)mapTickEffectIfFalse.Clone();
        return MapTickEffectFactory.CloneWithPosition(tickEffect, new Vector2Int(x, y));
    }

    public override string ToString()
    {
        return $"{EffectType} {mapElement} at {position} (combine: {combine})";
    }
    public new object Clone()
    {
        return new MapTickEffectConditionalSet
        {
            mapElement = mapElement,
            position = position,
            combine = combine,
            mapTickEffectIfTrue = mapTickEffectIfTrue,
            mapTickEffectIfFalse = mapTickEffectIfFalse,
            ifCondition = ifCondition
        };
    }
}