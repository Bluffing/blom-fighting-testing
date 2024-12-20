using UnityEngine;

public class MapTickEffectWave : MapTickEffect
{
    public MapTickEffectType EffectType => MapTickEffectType.Wave;
    public MapTickEffect mapTickEffect;
    public Vector2Int center = new Vector2Int(0, 0);
    public uint tickDelay = 1;
    public int radius = 0;
    public bool propagating = false;
    public MapBotElement propagatingElem = MapBotElement.Neutral;
    public MapBotElement[] passThrough;
    public bool bounce = false;
    public int hasDoneSomething = 0;
    public int id = Random.Range(0, 1000000);

    public override string ToString()
    {
        return $"{EffectType} ({mapTickEffect}) from center {center} (radius: {radius})";
    }
    public object Clone()
    {
        return new MapTickEffectWave
        {
            mapTickEffect = mapTickEffect,
            center = center,
            tickDelay = tickDelay,
            radius = radius,
            passThrough = passThrough,
            bounce = bounce,
            hasDoneSomething = hasDoneSomething
        };
    }
}