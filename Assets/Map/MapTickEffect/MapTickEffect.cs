using System;
using UnityEngine;

public interface MapTickEffect : ICloneable
{
    MapTickEffectType EffectType { get; }
}