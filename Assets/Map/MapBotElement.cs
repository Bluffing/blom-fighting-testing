using System.Collections.Generic;
using UnityEngine;

using ToEffectDictionary = System.Collections.Generic.Dictionary<MapBotElement, MapCombineEffect[]>;
using CombineFromDictionary = System.Collections.Generic.Dictionary<MapBotElement, System.Collections.Generic.Dictionary<MapBotElement, MapCombineEffect[]>>;
using System.Linq;
using System;

public enum MapBotElement
{
    Neutral,
    Error, // pretty much just for debug

    // Temp
    Warm,
    Burn,
    Cool,
    Freeze,

    // Fire
    Fire,

    // Water
    Water,
    UnconductiveWater,
    PoisonWater,

    // Ice
    Ice,
    PoisonIce,

    // Poison
    Poison,
    LitPoison, // poison on fire
}

public class CombineElement
{
    public MapBotElement start;
    public MapBotElement add;
}

public static class MapElementFunctions
{
    #region Combine Effect Dictionary

    static MapCombineEffect[] EffectCenterAndSides(MapBotElement center, bool combineCenter, uint centerDelay, MapBotElement side, bool combineSide, uint sideDelay)
    {
        return new MapCombineEffect[] {
            new MapCombineEffectSetBot { mapElement = center, combine = combineCenter, tickDelay = centerDelay, position = new Vector2Int(0, 0), },

            // new MapElementEffectSetBot { mapElement = side, combine = combineSide, tickDelay = sideDelay, position = new Vector2Int(1, 1), },
            // new MapElementEffectSetBot { mapElement = side, combine = combineSide, tickDelay = sideDelay, position = new Vector2Int(1, -1), },
            new MapCombineEffectSetBot { mapElement = side, combine = combineSide, tickDelay = sideDelay, position = new Vector2Int(1, 0), },

            // new MapElementEffectSetBot { mapElement = side, combine = combineSide, tickDelay = sideDelay, position = new Vector2Int(-1, 1), },
            // new MapElementEffectSetBot { mapElement = side, combine = combineSide, tickDelay = sideDelay, position = new Vector2Int(-1, -1), },
            new MapCombineEffectSetBot { mapElement = side, combine = combineSide, tickDelay = sideDelay, position = new Vector2Int(-1, 0), },

            new MapCombineEffectSetBot { mapElement = side, combine = combineSide, tickDelay = sideDelay, position = new Vector2Int(0, 1), },
            new MapCombineEffectSetBot { mapElement = side, combine = combineSide, tickDelay = sideDelay, position = new Vector2Int(0, -1), },
        };
    }
    static MapCombineEffect[] SteamWaterAndWarmSides(MapTopElement centerTop, uint sideDelay)
    {
        return new MapCombineEffect[] {
            new MapCombineEffectSetBot { mapElement = MapBotElement.Neutral },
            new MapCombineEffectSetTop { mapElement = centerTop, combine = true, tickDelay = 1 },

            new MapCombineEffectSetBot { mapElement = MapBotElement.Warm, combine = true, tickDelay = sideDelay, position = new Vector2Int(1, 1), },
            new MapCombineEffectSetBot { mapElement = MapBotElement.Warm, combine = true, tickDelay = sideDelay, position = new Vector2Int(1, -1), },
            new MapCombineEffectSetBot { mapElement = MapBotElement.Warm, combine = true, tickDelay = sideDelay, position = new Vector2Int(1, 0), },

            new MapCombineEffectSetBot { mapElement = MapBotElement.Warm, combine = true, tickDelay = sideDelay, position = new Vector2Int(-1, 1), },
            new MapCombineEffectSetBot { mapElement = MapBotElement.Warm, combine = true, tickDelay = sideDelay, position = new Vector2Int(-1, -1), },
            new MapCombineEffectSetBot { mapElement = MapBotElement.Warm, combine = true, tickDelay = sideDelay, position = new Vector2Int(-1, 0), },

            new MapCombineEffectSetBot { mapElement = MapBotElement.Warm, combine = true, tickDelay = sideDelay, position = new Vector2Int(0, 1), },
            new MapCombineEffectSetBot { mapElement = MapBotElement.Warm, combine = true, tickDelay = sideDelay, position = new Vector2Int(0, -1), },
        };
    }
    static MapCombineEffect[] EffectSetBot(MapBotElement elem, bool combine = false, uint tickDelay = 0)
    {
        return new MapCombineEffect[] { new MapCombineEffectSetBot { mapElement = elem, combine = combine, tickDelay = tickDelay } };
    }
    static MapCombineEffect[] EffectSetTop(MapTopElement elem, bool combine = false, uint tickDelay = 0)
    {
        return new MapCombineEffect[] { new MapCombineEffectSetTop { mapElement = elem, combine = combine, tickDelay = tickDelay } };
    }

    static ToEffectDictionary Classics = new ToEffectDictionary()
    {
        { MapBotElement.Warm, new MapCombineEffect[0] },
        { MapBotElement.Burn, EffectCenterAndSides(MapBotElement.Burn, false, 0, MapBotElement.Warm, true, 1) },
        { MapBotElement.Cool, new MapCombineEffect[0] },
        { MapBotElement.Freeze, EffectCenterAndSides(MapBotElement.Freeze, false, 0, MapBotElement.Cool, true, 1) },

        { MapBotElement.Fire, EffectCenterAndSides(MapBotElement.Fire, false, 0, MapBotElement.Burn, true, 1) },
        { MapBotElement.PoisonWater, EffectCenterAndSides(MapBotElement.PoisonWater, false, 0, MapBotElement.Poison, true, 1) },
        { MapBotElement.Ice, EffectCenterAndSides(MapBotElement.Ice, false, 0, MapBotElement.Cool, true, 1) },
        { MapBotElement.PoisonIce, EffectCenterAndSides(MapBotElement.PoisonIce, false, 0, MapBotElement.Cool, true, 1) },
        { MapBotElement.LitPoison, EffectCenterAndSides(MapBotElement.LitPoison, false, 0, MapBotElement.Burn, true, 1) },
        { MapBotElement.Poison, new MapCombineEffect[0] },
    };


    // checked
    static readonly ToEffectDictionary NeutralToEffectDictionary = new ToEffectDictionary()
    {
        // Neutral <- Neutral
        // Neutral <- Burn
        // Neutral <- Freeze
        // Neutral <- Fire
        // Neutral <- Water
        // Neutral <- UnconductiveWater
        // Neutral <- PoisonWater
        // Neutral <- Ice
        // Neutral <- PoisonIce
        // Neutral <- Poison
        // Neutral <- LitPoison

        // Neutral <- Warm
        { MapBotElement.Warm, EffectSetBot(MapBotElement.Warm) },

        // Neutral <- Cool
        { MapBotElement.Cool, EffectSetBot(MapBotElement.Cool) },
    };

    // checked
    static readonly ToEffectDictionary WarmToEffectDictionary = new ToEffectDictionary()
    {
        // Warm <- Neutral
        // Warm <- Burn
        // Warm <- Warm
        // Warm <- Freeze
        // Warm <- Fire
        // Warm <- Water
        // Warm <- UnconductiveWater
        // Warm <- PoisonWater
        // Warm <- Poison

        // Warm <- Cool
        { MapBotElement.Cool, EffectSetBot(MapBotElement.Neutral) },

        // Warm <- Ice
        { MapBotElement.Ice, EffectCenterAndSides(MapBotElement.Water, false, 0, MapBotElement.Cool, true, 1) },

        // Warm <- PoisonIce
        { MapBotElement.PoisonIce, EffectCenterAndSides(MapBotElement.PoisonWater, false, 0, MapBotElement.Cool, true, 1) },

        // Warm <- LitPoison
        { MapBotElement.LitPoison, EffectCenterAndSides(MapBotElement.LitPoison, false, 0, MapBotElement.Burn, true, 1) },
    };

    // checked
    static readonly ToEffectDictionary BurnToEffectDictionary = new ToEffectDictionary()
    {
        // Burn <- Neutral
        // Burn <- Warm
        // Burn <- Burn
        // Burn <- Cool
        // Burn <- Fire
        // Burn <- PoisonWater
        // Burn <- PoisonIce
        // Burn <- Poison
        // Burn <- LitPoison

        // Burn <- Freeze
        { MapBotElement.Freeze, EffectSetBot(MapBotElement.Neutral, false) },

        // Burn <- Water
        { MapBotElement.Water, SteamWaterAndWarmSides(MapTopElement.Steam, 1) },

        // Burn <- UnconductiveWater
        { MapBotElement.UnconductiveWater, SteamWaterAndWarmSides(MapTopElement.Steam, 1) },

        // Burn <- Ice
        { MapBotElement.Ice, SteamWaterAndWarmSides(MapTopElement.Steam, 1) },
    };

    // checked
    static readonly ToEffectDictionary CoolToEffectDictionary = new ToEffectDictionary()
    {
        // Cool <- Neutral
        // Cool <- Burn
        // Cool <- Freeze
        // Cool <- Cool
        // Cool <- Water
        // Cool <- UnconductiveWater
        // Cool <- PoisonWater
        // Cool <- Ice
        // Cool <- PoisonIce
        // Cool <- Poison
        // Cool <- LitPoison

        // Cool <- Warm
        { MapBotElement.Warm, EffectSetBot(MapBotElement.Neutral) },

        // // Cool <- Fire
        // { MapBotElement.Fire, EffectSetBot(MapBotElement.Neutral) },
    };

    // checked
    static readonly ToEffectDictionary FreezeToEffectDictionary = new ToEffectDictionary()
    {
        // Freeze <- Neutral
        // Freeze <- Cool
        // Freeze <- Freeze
        // Freeze <- PoisonWater
        // Freeze <- Ice
        // Freeze <- PoisonIce
        // Freeze <- Poison

        // Freeze <- Warm
        { MapBotElement.Warm, EffectSetBot(MapBotElement.Freeze) },

        // Freeze <- Burn
        { MapBotElement.Burn, EffectSetBot(MapBotElement.Neutral) },

        // Freeze <- Fire
        { MapBotElement.Fire, EffectSetBot(MapBotElement.Neutral) },

        // Freeze <- Water
        { MapBotElement.Water, EffectCenterAndSides(MapBotElement.Ice, false, 0, MapBotElement.Cool, true, 1) },

        // Freeze <- UnconductiveWater
        { MapBotElement.UnconductiveWater, EffectCenterAndSides(MapBotElement.Ice, false, 0, MapBotElement.Cool, true, 1) },

        // Freeze <- PoisonWater
        { MapBotElement.PoisonWater, EffectCenterAndSides(MapBotElement.PoisonIce, false, 0, MapBotElement.Cool, true, 1) },

        // Freeze <- LitPoison
        { MapBotElement.LitPoison, EffectSetBot(MapBotElement.Poison) },
    };

    // checked
    static readonly ToEffectDictionary FireToEffectDictionary = new ToEffectDictionary()
    {
        // Fire <- Neutral
        // Fire <- Warm
        // Fire <- Cool
        // Fire <- Fire
        // Fire <- LitPoison
        // Fire <- Poison

        // Fire <- Burn
        { MapBotElement.Burn, EffectCenterAndSides(MapBotElement.Fire, false, 0, MapBotElement.Warm, true, 1) }, // classic burn with fire center

        // // Fire <- Cool
        // { MapBotElement.Cool, EffectSetBot(MapBotElement.Neutral) },

        // Fire <- Freeze
        { MapBotElement.Freeze, EffectSetBot(MapBotElement.Neutral) },

        // Fire <- Water
        { MapBotElement.Water, SteamWaterAndWarmSides(MapTopElement.Steam, 1) },

        // Fire <- UnconductiveWater
        { MapBotElement.UnconductiveWater, SteamWaterAndWarmSides(MapTopElement.Steam, 1) },

        // Fire <- PoisonWater
        { MapBotElement.PoisonWater, EffectCenterAndSides(MapBotElement.LitPoison, false, 0, MapBotElement.Burn, true, 1) },

        // Fire <- Ice
        { MapBotElement.Ice, EffectCenterAndSides(MapBotElement.Water, false, 0, MapBotElement.Cool, true, 1) },

        // Fire <- PoisonIce
        { MapBotElement.PoisonIce, EffectCenterAndSides(MapBotElement.LitPoison, false, 0, MapBotElement.Cool, true, 1) },
    };

    // checked
    static readonly ToEffectDictionary WaterToEffectDictionary = new ToEffectDictionary()
    {
        // Water <- Neutral
        // Water <- Warm
        // Water <- Cool
        // Water <- Water
        // Water <- UnconductiveWater
        // Water <- PoisonWater
        // Water <- Ice
        // Water <- PoisonIce
        // Water <- LitPoison

        // Water <- Burn
        { MapBotElement.Burn, SteamWaterAndWarmSides(MapTopElement.Steam, 1) },

        // Water <- Freeze
        { MapBotElement.Freeze, EffectCenterAndSides(MapBotElement.Ice, false, 0, MapBotElement.Cool, true, 1) },

        // Water <- Fire
        { MapBotElement.Fire, SteamWaterAndWarmSides(MapTopElement.Steam, 1) },

        // Water <- Poison
        { MapBotElement.Poison, EffectCenterAndSides(MapBotElement.PoisonWater, false, 0, MapBotElement.Poison, true, 1) },
    };

    // checked
    static readonly ToEffectDictionary UnconductiveWaterToEffectDictionary = new ToEffectDictionary()
    {
        // UnconductiveWater <- Neutral
        // UnconductiveWater <- Warm
        // UnconductiveWater <- Cool
        // UnconductiveWater <- Water
        // UnconductiveWater <- UnconductiveWater
        // UnconductiveWater <- PoisonWater
        // UnconductiveWater <- Ice
        // UnconductiveWater <- PoisonIce
        // UnconductiveWater <- LitPoison

        // UnconductiveWater <- Burn
        { MapBotElement.Burn, EffectSetTop(MapTopElement.Steam) },

        // UnconductiveWater <- Freeze
        { MapBotElement.Freeze, EffectCenterAndSides(MapBotElement.Ice, false, 0, MapBotElement.Cool, true, 1) },

        // UnconductiveWater <- Fire
        { MapBotElement.Fire, EffectSetTop(MapTopElement.Steam) },

        // UnconductiveWater <- Poison
        { MapBotElement.Poison, EffectCenterAndSides(MapBotElement.PoisonWater, false, 0, MapBotElement.Poison, true, 1) },
    };

    // checked
    static readonly ToEffectDictionary PoisonWaterToEffectDictionary = new ToEffectDictionary()
    {
        // PoisonWater <- Neutral
        // PoisonWater <- Warm
        // PoisonWater <- Cool
        // PoisonWater <- UnconductiveWater
        // PoisonWater <- PoisonWater
        // PoisonWater <- Ice
        // PoisonWater <- PoisonIce
        // PoisonWater <- Poison
        // PoisonWater <- LitPoison

        // PoisonWater <- Burn
        { MapBotElement.Burn, EffectCenterAndSides(MapBotElement.LitPoison, false, 0, MapBotElement.Burn, true, 1) },

        // PoisonWater <- Freeze
        { MapBotElement.Freeze, EffectCenterAndSides(MapBotElement.PoisonIce, false, 0, MapBotElement.Cool, true, 1) },

        // PoisonWater <- Fire
        { MapBotElement.Fire, EffectCenterAndSides(MapBotElement.LitPoison, false, 0, MapBotElement.Burn, true, 1) },

        // PoisonWater <- Water
        // { MapBotElement.Water, new MapElementEffect[0] },
        // { MapBotElement.Water,
        //     EffectToSide(MapBotElement.Water, 1)
        //     .Append(new CombineElementEffectSetBot { mapElement = MapBotElement.PoisonWater }).ToArray()
        // },
    };

    // checked
    static readonly ToEffectDictionary IceToEffectDictionary = new ToEffectDictionary()
    {
        // Ice <- Neutral
        // Ice <- Cool
        // Ice <- Ice
        // Ice <- LitPoison

        // Ice <- Warm
        { MapBotElement.Warm, EffectSetBot(MapBotElement.Water) },

        // Ice <- Burn
        { MapBotElement.Burn, SteamWaterAndWarmSides(MapTopElement.Steam, 1) },

        // Ice <- Freeze
        { MapBotElement.Freeze, new MapCombineEffect[0] },

        // Ice <- Fire
        { MapBotElement.Fire, SteamWaterAndWarmSides(MapTopElement.Steam, 1) },

        // Ice <- Water
        { MapBotElement.Water, new MapCombineEffect[0] },

        // Ice <- UnconductiveWater
        { MapBotElement.UnconductiveWater, new MapCombineEffect[0] },

        // Ice <- PoisonWater
        { MapBotElement.PoisonWater, EffectSetBot(MapBotElement.PoisonIce) },
        // { MapBotElement.PoisonWater, EffectCenterAndSides(MapBotElement.PoisonIce, false, 0, MapBotElement.Poison, true, 1) },

        // Ice <- Poison
        { MapBotElement.Poison, EffectSetBot(MapBotElement.PoisonIce) },
        // { MapBotElement.Poison, EffectCenterAndSides(MapBotElement.PoisonIce, false, 0, MapBotElement.Poison, true, 1) },

        // Ice <- PoisonIce
        { MapBotElement.PoisonIce, EffectSetBot(MapBotElement.PoisonIce) },
        // { MapBotElement.PoisonIce, EffectCenterAndSides(MapBotElement.PoisonIce, false, 0, MapBotElement.Poison, true, 1) },
    };

    // checked
    static readonly ToEffectDictionary PoisonIceToEffectDictionary = new ToEffectDictionary()
    {
        // PoisonIce <- Neutral
        // PoisonIce <- Cool
        // PoisonIce <- Warm
        // PoisonIce <- Water
        // PoisonIce <- UnconductiveWater
        // PoisonIce <- PoisonWater
        // PoisonIce <- Ice
        // PoisonIce <- PoisonIce
        // PoisonIce <- Poison

        // PoisonIce <- Freeze
        { MapBotElement.Freeze, new MapCombineEffect[0] },

        // PoisonIce <- Burn
        { MapBotElement.Burn, EffectSetBot(MapBotElement.LitPoison) },

        // PoisonIce <- Fire
        { MapBotElement.Fire, EffectSetBot(MapBotElement.LitPoison) },
    };

    // checked
    static readonly ToEffectDictionary PoisonToEffectDictionary = new ToEffectDictionary()
    {
        // Poison <- Neutral
        // Poison <- Warm
        // Poison <- Burn
        // Poison <- Cool
        // Poison <- Freeze
        // Poison <- Fire
        // Poison <- Water
        // Poison <- UnconductiveWater
        // Poison <- PoisonWater
        // Poison <- Ice
        // Poison <- PoisonIce
        // Poison <- Poison
        // Poison <- LitPoison
    };

    // checked
    static readonly ToEffectDictionary LitPoisonToEffectDictionary = new ToEffectDictionary()
    {
        // LitPoison <- Neutral
        // LitPoison <- Warm
        // LitPoison <- Burn
        // LitPoison <- Cool
        // LitPoison <- Fire
        // LitPoison <- PoisonWater
        // LitPoison <- Ice
        // LitPoison <- PoisonIce
        // LitPoison <- Poison
        // LitPoison <- LitPoison

        // LitPoison <- Freeze
        { MapBotElement.Freeze, EffectSetBot(MapBotElement.Neutral) },

        // LitPoison <- Water
        { MapBotElement.Water, EffectSetBot(MapBotElement.PoisonWater) },

        // LitPoison <- UnconductiveWater
        { MapBotElement.UnconductiveWater, EffectSetBot(MapBotElement.PoisonWater) },
    };

    static readonly ToEffectDictionary BleakToEffectDictionary = new ToEffectDictionary()
    {
        // Bleak <- Neutral
        // Bleak <- Warm
        // Bleak <- Burn
        // Bleak <- Cool
        // Bleak <- Freeze
        // Bleak <- Fire
        // Bleak <- Water
        // Bleak <- UnconductiveWater
        // Bleak <- PoisonWater
        // Bleak <- Ice
        // Bleak <- PoisonIce
        // Bleak <- Poison
        // Bleak <- LitPoison
    };

    static readonly CombineFromDictionary CombineElementEffectsDictionary = new CombineFromDictionary() {
        { MapBotElement.Neutral,            NeutralToEffectDictionary           },
        { MapBotElement.Warm,               WarmToEffectDictionary              },
        { MapBotElement.Burn,               BurnToEffectDictionary              },
        { MapBotElement.Cool,               CoolToEffectDictionary              },
        { MapBotElement.Freeze,             FreezeToEffectDictionary            },
        { MapBotElement.Fire,               FireToEffectDictionary              },
        { MapBotElement.Water,              WaterToEffectDictionary             },
        { MapBotElement.UnconductiveWater,  UnconductiveWaterToEffectDictionary },
        { MapBotElement.PoisonWater,        PoisonWaterToEffectDictionary       },
        { MapBotElement.Ice,                IceToEffectDictionary               },
        { MapBotElement.PoisonIce,          PoisonIceToEffectDictionary         },
        { MapBotElement.Poison,             PoisonToEffectDictionary            },
        { MapBotElement.LitPoison,          LitPoisonToEffectDictionary         },

    };

    #endregion

    #region Combine

    public static MapCombineEffect[] CombineElements(MapBotElement current, MapBotElement next)
    {
        if (current == next) return new MapCombineEffect[0];
        if (!CombineElementEffectsDictionary.ContainsKey(current)) return new MapCombineEffect[0]; // wtf

        if (!CombineElementEffectsDictionary[current].ContainsKey(next))
            if (Classics.ContainsKey(next))
            {
                Debug.Log($"classic");
                return Classics[next];
            }
            else
            {
                // Debug.Log($"no effect");
                // return new MapElementEffect[0];
                return new MapCombineEffect[] { new MapCombineEffectSetBot { mapElement = next } };
            }

        // Debug.Log($"combine");
        return CombineElementEffectsDictionary[current][next];
    }

    #endregion

    #region Color

    readonly static Dictionary<string, Color> BotElementColors = new Dictionary<string, Color>()
    {
        { "errorgreen",     new Color(0.678f, 0.973f, 0.078f) },
        { "lightblue",      new Color(0.486f, 0.961f, 1f) },
        { "transparent",    new Color(0, 0, 0, 0) },
        { "fire",           new Color(1, 1, 1, 1) },
        { "water",          new Color(1, 1, 1, 1) },
        { "ice",            new Color(1, 1, 1, 1) },
        { "steam",          new Color(1, 1, 1, 1) },
        { "poisonsteam",    new Color(1, 1, 1, 1) },
        { "poison",         new Color(1, 1, 1, 1) },
    };

    // private static Dictionary<MapBotElement, Color> MapElemColorsDict = new Dictionary<MapBotElement, Color>()
    // {
    //     { MapBotElement.Neutral, transparent },
    //     { MapBotElement.Error, errorgreen },

    //     { MapBotElement.Warm, transparent },
    //     { MapBotElement.Burn, transparent },
    //     { MapBotElement.Cool, transparent },
    //     { MapBotElement.Freeze, transparent },

    //     { MapBotElement.Fire, Color.red },

    //     { MapBotElement.Water, water },
    //     { MapBotElement.UnconductiveWater, water },

    //     { MapBotElement.Ice, ice },
    //     { MapBotElement.PoisonIce, ice },

    //     { MapBotElement.Poison, poison },

    // };

    // Debugging
    private static Dictionary<MapBotElement, Color> MapElemColorsDict = new Dictionary<MapBotElement, Color>()
    {
        { MapBotElement.Neutral, new Color(0, 0, 0, 0) },
        { MapBotElement.Error, new Color(0.678f, 0.973f, 0.078f) },

        { MapBotElement.Warm, Color.magenta },
        { MapBotElement.Burn, Color.cyan },
        { MapBotElement.Cool, Color.blue },
        { MapBotElement.Freeze, Color.yellow },

        { MapBotElement.Fire, Color.red },

        { MapBotElement.Water, Color.cyan },
        { MapBotElement.UnconductiveWater, Color.blue },
        { MapBotElement.PoisonWater, Color.magenta },

        { MapBotElement.Ice, Color.green },
        { MapBotElement.PoisonIce, Color.blue },

        { MapBotElement.Poison, Color.black },
        { MapBotElement.LitPoison, Color.yellow },
    };
    public static Color GetElementColor(MapBotElement elem)
    {
        if (MapElemColorsDict.ContainsKey(elem))
            return MapElemColorsDict[elem];
        else
            return new Color(0.678f, 0.973f, 0.078f); // error
    }

    #endregion
}