public class MapCombineEffectSetBot : MapCombineEffect
{
    public override MapCombineEffectType EffectType => MapCombineEffectType.SetBot;
    public bool combine = false;
    public MapBotElement mapElement;
    public override string ToString()
    {
        return $"{EffectType} {mapElement} at {position} (combine: {combine})";
    }
}
