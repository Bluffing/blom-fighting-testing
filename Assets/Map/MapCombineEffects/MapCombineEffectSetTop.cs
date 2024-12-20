public class MapCombineEffectSetTop : MapCombineEffect
{
    public override MapCombineEffectType EffectType => MapCombineEffectType.SetTop;
    public bool combine = false;
    public MapTopElement mapElement;
    public override string ToString()
    {
        return $"{EffectType} {mapElement} at {position} (combine: {combine})";
    }
}