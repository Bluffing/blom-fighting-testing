using UnityEngine;

public class MapCombineEffectWaveStartBot : MapCombineEffect
{
    public override MapCombineEffectType EffectType => MapCombineEffectType.WaveSet;
    public Vector2Int waveCenter;
    public MapTickEffect mapTickEffect;
    public bool combine;
    public int startingRadius;
}