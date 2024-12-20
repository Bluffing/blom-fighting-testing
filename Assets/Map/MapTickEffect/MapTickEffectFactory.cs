using System;
using UnityEngine;

public class MapTickEffectFactory
{
    public static MapTickEffect CloneWithPosition(MapTickEffect mapTickEffect, Vector2Int position)
    {
        var tickEffect = (MapTickEffect)mapTickEffect.Clone();
        switch (mapTickEffect.EffectType)
        {
            case MapTickEffectType.Set:
                var setTickEffect = (MapTickEffectSet)tickEffect;
                setTickEffect.position = position;
                return setTickEffect;
            case MapTickEffectType.ConditionalSet:
                var condTickEffect = (MapTickEffectConditionalSet)tickEffect;
                condTickEffect.position = position;
                return condTickEffect;
            case MapTickEffectType.Wave:
                var waveTickEffect = (MapTickEffectWave)tickEffect;
                waveTickEffect.center = position;
                return waveTickEffect;
            default:
                throw new NotImplementedException();
        }
    }

}