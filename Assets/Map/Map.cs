using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using System.IO;

public class Map : MonoBehaviour
{
    public MapBotElement[,] map;
    public MapBotElement[,] prevTickMap;
    public int width;
    public int height;
    public bool debugUpdateMap = false;
    public bool debugUpdateVerbose = false;

    Material objMaterial;
    Texture2D mapTexture;
    bool updatedMap = false;

    public uint tickPerSecond = 30;
    float singleTickTime;
    public uint tick = 0;
    float lastTick = 0;
    Dictionary<uint, List<MapTickEffect>> DelayedTickEffectMap = new Dictionary<uint, List<MapTickEffect>>();


    void Start()
    {
        singleTickTime = 1f / tickPerSecond;

        objMaterial = GetComponent<Renderer>().material;
        mapTexture = (Texture2D)objMaterial.GetTexture("_MainTex");
        if (mapTexture is null)
            CreateBlankTexture();

        map = new MapBotElement[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                map[x, y] = MapBotElement.Neutral;
        prevTickMap = map.Clone() as MapBotElement[,];

        FullBuildTexture();
    }

    #region TICK

    void Update()
    {
        if (debugUpdateMap)
        {
            DebugUpdate();
            return;
        }

        lastTick += Time.deltaTime;
        uint nextTick = (uint)Math.Round(lastTick * tickPerSecond);
        lastTick -= nextTick * singleTickTime;
        nextTick += tick; // nextTick is the tick we want to reach

        while (tick != nextTick) // this will 100% blow up one day
        {
            tick++;
            if (!DelayedTickEffectMap.ContainsKey(tick))
                continue;

            var mapCopy = map.Clone() as MapBotElement[,];
            // Debug.Log($"tick ({tick}) count: {DelayedTickEffectMap[tick].Count}");
            Debug.Log($"tick ({tick}) count: {DelayedTickEffectMap[tick].Count}\nbounce count: {waveBounceCounter}, waveOp: {waveOp}, complexWaveOp: {complexWaveOp}\nratio: {(double)complexWaveOp / (waveOp + 1) * 100}%\ndelta: {Time.deltaTime}");
            // string path = Application.persistentDataPath + "/test.txt";
            // Debug.Log($"path: {path}");
            // StreamWriter writer = new StreamWriter(path, true);
            using (StreamWriter writer = File.CreateText("Assets/Map/testos.txt"))
            {
                writer.WriteLine(bleak);
            }
            // writer.WriteLine("\n\nBLEAK\n\n");
            // writer.Close();

            for (int i = 0; i < DelayedTickEffectMap[tick].Count; i++)
                ApplyTickEffect(DelayedTickEffectMap[tick][i]);
            DelayedTickEffectMap.Remove(tick);

            prevTickMap = mapCopy;
            // Debug.Log($"tick ({tick}) done");
        }

        if (updatedMap)
        {
            AfterMapUpdate();
            updatedMap = false;
        }
    }

    void DebugUpdate()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            if (DelayedTickEffectMap.Count == 0)
            {
                Debug.Log("no delayed effects");
                return;
            }
            else
                Debug.Log("finding next tick");

            while (!DelayedTickEffectMap.ContainsKey(tick))
                tick++;

            Debug.Log($"next tick : {tick}");
            Debug.Log($"count: {DelayedTickEffectMap[tick].Count}");

            for (int i = 0; i < DelayedTickEffectMap[tick].Count; i++)
            {
                // Debug.Log(DelayedTickEffectMap[tick][i]);
                ApplyTickEffect(DelayedTickEffectMap[tick][i]);
            }
            DelayedTickEffectMap.Remove(tick);

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    prevTickMap[x, y] = map[x, y];
        }

        if (updatedMap)
        {
            AfterMapUpdate();
            updatedMap = false;
        }
    }

    void AfterMapUpdate()
    {
        mapTexture.Apply();

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                prevTickMap[x, y] = map[x, y];
    }

    #endregion

    #region BASE

    public void CreateBlankTexture()
    {
        objMaterial = GetComponent<Renderer>().sharedMaterial;
        var newMapTexture = new Texture2D(width, height);

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                newMapTexture.SetPixel(x, y, MapElementFunctions.GetElementColor(MapBotElement.Error));

        // Debug.Log($"new texture: {width}x{height}");
        newMapTexture.filterMode = FilterMode.Point;
        newMapTexture.wrapMode = TextureWrapMode.Clamp;
        newMapTexture.Apply();
        objMaterial.SetTexture("_MainTex", newMapTexture);
        mapTexture = newMapTexture;
    }

    #endregion

    #region ADDING ELEMENTS

    static Vector2Int[] sidesVector8 = new Vector2Int[] {
        new Vector2Int(1, 1),
        new Vector2Int(1, 0),
        new Vector2Int(1, -1),

        new Vector2Int(-1, 1),
        new Vector2Int(-1, 0),
        new Vector2Int(-1, -1),

        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
    };
    static Vector2Int[] cornerVector4 = new Vector2Int[] {
        new Vector2Int(1, 1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, 1),
        new Vector2Int(-1, -1),
    };
    bool XYIsInMap(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;
    bool XYIsInMap(Vector2Int pos) => pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    Vector2Int WorldToLocal(Vector2 mouseClick)
    {
        // mouseclick - local position + half of the width of the map (from (0,0) to (scale.x, scale.y))
        Vector2 relatifClick = mouseClick - (Vector2)transform.position + (Vector2)transform.localScale * 5f;

        return new Vector2Int(
            (int)(relatifClick.x / (transform.localScale.x * 10) * width),
            (int)(relatifClick.y / (transform.localScale.y * 10) * height)
        );
    }
    void SetElementToXY(MapBotElement element, int x, int y, bool combine = true)
    {
        if (!XYIsInMap(x, y)) return;

        if (combine)
            ApplyCombiningEffects(MapElementFunctions.CombineElements(map[x, y], element), x, y);
        else
            map[x, y] = element;

        mapTexture.SetPixel(x, y, MapElementFunctions.GetElementColor(map[x, y]));
        updatedMap = true;
    }
    void AddDelayedEffect(uint tickDelay, MapTickEffect tickEffect)
    {
        uint tickKey = tick + tickDelay;
        if (!DelayedTickEffectMap.ContainsKey(tickKey))
            DelayedTickEffectMap.Add(tickKey, new List<MapTickEffect>() { tickEffect });
        else
            DelayedTickEffectMap[tickKey].Add(tickEffect);
    }

    public void AddRectangleFromWorld(MapBotElement element, Vector2 mouseClick, int rectangleWidth, int rectangleHeight)
    {
        var localCenter = WorldToLocal(mouseClick);
        AddRectangle(element, localCenter, rectangleWidth, rectangleHeight);
    }
    public void AddRectangle(MapBotElement element, Vector2Int start, int rectangleWidth, int rectangleHeight, bool combine = true)
    {
        // Debug.Log("adding rectangle");
        int xClamped(int v) => Mathf.Clamp(v, 0, width);
        int yClamped(int v) => Mathf.Clamp(v, 0, height);

        int xStart = xClamped(start.x - rectangleWidth / 2);
        int xEnd = xClamped(start.x + (int)Mathf.Ceil(rectangleWidth / 2f));
        int yStart = yClamped(start.y - rectangleHeight / 2);
        int yEnd = yClamped(start.y + (int)Mathf.Ceil(rectangleHeight / 2f));

        // Debug.Log($"xStart: {xStart}, xEnd: {xEnd}, yStart: {yStart}, yEnd: {yEnd}");

        for (int x = xStart; x <= xEnd; x++)
            for (int y = yStart; y <= yEnd; y++)
                SetElementToXY(element, x, y, combine);
    }

    public void AddCircleFromWorld(MapBotElement element, Vector2 mouseClick, int radius, bool combine = true)
    {
        var localCenter = WorldToLocal(mouseClick);
        AddCircle(element, localCenter, radius, combine);
    }
    public void AddCircle(MapBotElement element, Vector2Int center, int radius, bool combine = true)
    {
        for (int x = 0; x < radius; x++)
            for (int y = 0; y < radius; y++)
                if (x * x + y * y < radius * radius)
                {
                    SetElementToXY(element, center.x + x, center.y + y, combine);
                    SetElementToXY(element, center.x + x, center.y - y, combine);
                    SetElementToXY(element, center.x - x, center.y + y, combine);
                    SetElementToXY(element, center.x - x, center.y - y, combine);
                }
    }

    #region old
    // public bool AddCircleEdge(MapTickEffect tickEffect, Vector2Int center, int radius, bool propagating = false, MapBotElement propagatingElem = MapBotElement.Neutral, MapBotElement[] passThrough = null)
    // {
    //     MapTickEffect newEffect(Vector2Int pos)
    //     {
    //         switch (tickEffect.EffectType)
    //         {
    //             case MapTickEffectType.Set:
    //                 var setEffect = (MapTickEffectSet)tickEffect;
    //                 var copySet = (MapTickEffectSet)setEffect.Clone();
    //                 copySet.position = pos;
    //                 return copySet;
    //             case MapTickEffectType.ConditionalSet:
    //                 var condSetEffect = (MapTickEffectConditionalSet)tickEffect;
    //                 var copyCondSet = (MapTickEffectConditionalSet)condSetEffect.Clone();
    //                 copyCondSet.position = pos;
    //                 return copyCondSet;
    //             default:
    //                 return null;
    //         }
    //     }
    //     bool checkPropagating(Vector2Int pos)
    //     {
    //         if (!propagating)
    //             return true;

    //         for (int i = 0; i < sidesVector8.Length; i++)
    //         {
    //             var s = pos + sidesVector8[i];
    //             if (XYIsInMap(s) && prevTickMap[s.x, s.y] == propagatingElem)
    //                 return true;
    //         }

    //         return false;
    //     }
    //     bool checkCollision(Vector2Int pos)
    //     {
    //         if (passThrough is null)
    //             return false;

    //         int xDistance = Mathf.Abs(pos.x - center.x);
    //         int yDistance = Mathf.Abs(pos.y - center.y);
    //         if (xDistance <= 1 && yDistance <= 1)
    //             return false;

    //         bool collide(int x, int y) => !XYIsInMap(x, y) || Array.IndexOf(passThrough, prevTickMap[x, y]) == -1;

    //         if (xDistance >= yDistance)
    //         {
    //             // larger x diff, calculate y
    //             float a = (float)(pos.y - center.y) / (pos.x - center.x);
    //             int diff = pos.x - center.x;
    //             int increment = diff > 0 ? 1 : -1;
    //             bool endLambda(int x) => diff > 0 ? x < diff : x > diff;

    //             for (int x = 0; endLambda(x); x += increment)
    //             {
    //                 int y = Mathf.RoundToInt(a * x + center.y);
    //                 int realX = x + center.x;

    //                 if (collide(realX, y))
    //                     return true;
    //             }
    //         }
    //         else
    //         {
    //             // larger y diff, calculate x
    //             float a = (float)(pos.x - center.x) / (pos.y - center.y);
    //             int diff = pos.y - center.y;
    //             int increment = diff > 0 ? 1 : -1;
    //             bool endLambda(int y) => diff > 0 ? y < diff : y > diff;

    //             for (int y = 0; endLambda(y); y += increment)
    //             {
    //                 int x = Mathf.RoundToInt(y * a + center.x);
    //                 int realY = y + center.y;
    //                 if (collide(x, realY))
    //                     return true;
    //             }
    //         }
    //         return false;
    //     }

    //     var effect = new List<MapTickEffect>();

    //     for (int x = 0; x < radius; x++)
    //         for (int y = 0; y < radius; y++)
    //         {
    //             int lengthSquared = x * x + y * y;
    //             int radiusSquared = radius * radius;
    //             int radiusMin1Squared = (radius - 1) * (radius - 1);

    //             if (lengthSquared >= radiusSquared || lengthSquared < radiusMin1Squared)
    //                 continue;

    //             Vector2Int relativePoint = new Vector2Int(x, y);
    //             for (int i = 0; i < cornerVector4.Length; i++)
    //             {
    //                 var localPoint = center + cornerVector4[i] * relativePoint;
    //                 if (checkCollision(localPoint))
    //                     continue;

    //                 if (XYIsInMap(localPoint) && checkPropagating(localPoint))
    //                     effect.Add(newEffect(localPoint));
    //             }
    //         }

    //     ApplyTickEffect(effect.ToArray());
    //     return effect.Count > 0;
    // }
    #endregion

    string bleak = "";
    public bool AddCircleEdge(MapTickEffectWave waveEffect)
    {
        MapTickEffect newEffect(Vector2Int pos)
        {
            switch (waveEffect.mapTickEffect.EffectType)
            {
                case MapTickEffectType.Set:
                    var setEffect = (MapTickEffectSet)waveEffect.mapTickEffect;
                    var copySet = (MapTickEffectSet)setEffect.Clone();
                    copySet.position = pos;
                    return copySet;
                case MapTickEffectType.ConditionalSet:
                    var condSetEffect = (MapTickEffectConditionalSet)waveEffect.mapTickEffect;
                    var copyCondSet = (MapTickEffectConditionalSet)condSetEffect.Clone();
                    copyCondSet.position = pos;
                    return copyCondSet;
                default:
                    return null;
            }
        }
        bool canPropagating(Vector2Int pos)
        {
            if (!waveEffect.propagating)
                return true;
            if (XYIsInMap(pos) && map[pos.x, pos.y] == waveEffect.propagatingElem)
                return false;

            for (int i = 0; i < sidesVector8.Length; i++)
            {
                var s = pos + sidesVector8[i];
                if (XYIsInMap(s) && prevTickMap[s.x, s.y] == waveEffect.propagatingElem)
                    return true;
            }

            return false;
        }
        bool collides(Vector2Int pos)
        {
            if (waveEffect.passThrough is null)
                return false;

            int xDistance = Mathf.Abs(pos.x - waveEffect.center.x);
            int yDistance = Mathf.Abs(pos.y - waveEffect.center.y);
            if (xDistance <= 1 && yDistance <= 1)
                return false;

            bool collide(int x, int y) => !XYIsInMap(x, y) || Array.IndexOf(waveEffect.passThrough, map[x, y]) == -1;

            if (xDistance >= yDistance)
            {
                // larger x diff, calculate y
                float a = (float)(pos.y - waveEffect.center.y) / (pos.x - waveEffect.center.x);
                int diff = pos.x - waveEffect.center.x;
                int increment = diff > 0 ? 1 : -1;
                bool endLambda(int x) => diff > 0 ? x < diff : x > diff;

                for (int x = 0; endLambda(x); x += increment)
                {
                    int y = Mathf.RoundToInt(a * x + waveEffect.center.y);
                    int realX = x + waveEffect.center.x;

                    if (collide(realX, y))
                        return true;
                }
            }
            else
            {
                // larger y diff, calculate x
                float a = (float)(pos.x - waveEffect.center.x) / (pos.y - waveEffect.center.y);
                int diff = pos.y - waveEffect.center.y;
                int increment = diff > 0 ? 1 : -1;
                bool endLambda(int y) => diff > 0 ? y < diff : y > diff;

                for (int y = 0; endLambda(y); y += increment)
                {
                    int x = Mathf.RoundToInt(y * a + waveEffect.center.x);
                    int realY = y + waveEffect.center.y;
                    if (collide(x, realY))
                        return true;
                }
            }
            return false;
        }
        bool nextStepCollides(Vector2Int pos)
        {
            if (waveEffect.passThrough is null)
                return false;

            // if it's already not a passThrough, dont collide
            if (Array.IndexOf(waveEffect.passThrough, map[pos.x, pos.y]) == -1)
                return false;

            int xDistance = Mathf.Abs(pos.x - waveEffect.center.x);
            int yDistance = Mathf.Abs(pos.y - waveEffect.center.y);

            if (xDistance <= 1 && yDistance <= 1)
                return false;

            Vector2Int nextStep;
            if (xDistance > yDistance)
            {
                float a = (float)(pos.y - waveEffect.center.y) / (pos.x - waveEffect.center.x);
                int step = pos.x > waveEffect.center.x ? 1 : -1;
                int x = pos.x + step;
                int y = Mathf.RoundToInt((pos.x - waveEffect.center.x + step) * a + waveEffect.center.y);
                nextStep = new Vector2Int(x, y);
            }
            else
            {
                float a = (float)(pos.x - waveEffect.center.x) / (pos.y - waveEffect.center.y);
                int step = pos.y > waveEffect.center.y ? 1 : -1;
                int y = pos.y + step;
                int x = Mathf.RoundToInt((pos.y - waveEffect.center.y + step) * a + waveEffect.center.x);
                nextStep = new Vector2Int(x, y);
            }

            // bool collide(int x, int y) => !XYIsInMap(x, y) || Array.IndexOf(waveEffect.passThrough, map[x, y]) == -1;
            // return collide(nextStep.x, nextStep.y);

            // if next is not passThrough, collide
            if (!XYIsInMap(nextStep))
            {
                // Debug.Log($"not in map");
                return true;
            }
            else if (Array.IndexOf(waveEffect.passThrough, map[nextStep.x, nextStep.y]) == -1)
                return true;
            return false;
            return !XYIsInMap(nextStep) || Array.IndexOf(waveEffect.passThrough, map[nextStep.x, nextStep.y]) == -1;
        }

        // Debug.Log($"radius: {waveEffect.radius}");
        if (waveEffect.radius == 1)
        {
            ApplyTickEffect(newEffect(waveEffect.center));
            return true;
        }
        var effect = new List<MapTickEffect>();

        string s = "new Vector2Int[] {";

        for (int x = 0; x < waveEffect.radius; x++)
            for (int y = 0; y < waveEffect.radius; y++)
            {
                waveOp++;

                int lengthSquared = x * x + y * y;
                int radiusSquared = waveEffect.radius * waveEffect.radius;
                int radiusMin1Squared = (waveEffect.radius - 1) * (waveEffect.radius - 1);

                if (lengthSquared >= radiusSquared || lengthSquared < radiusMin1Squared)
                    continue;

                complexWaveOp++;

                Vector2Int relativePoint = new Vector2Int(x, y);
                for (int i = 0; i < cornerVector4.Length; i++)
                {
                    var localPoint = waveEffect.center + cornerVector4[i] * relativePoint;

                    s += $"new Vector2Int({(cornerVector4[i] * relativePoint).x}, {(cornerVector4[i] * relativePoint).y}), ";

                    if (collides(localPoint))
                        continue;

                    if (nextStepCollides(localPoint) && waveEffect.bounce && waveEffect.hasDoneSomething > 0)
                    {
                        var wave = (MapTickEffectWave)waveEffect.Clone();
                        wave.center = localPoint;
                        wave.radius = 1;
                        wave.hasDoneSomething = 0;
                        wave.bounce = false;

                        waveBounceCounter++;
                        // Debug.Log($"tick ({tick}) start bounce at {localPoint} id: {wave.id}");
                        // Debug.Log($"from {waveEffect.id} {waveEffect.center} with {waveEffect.hasDoneSomething} effects");
                        StartWave(wave);
                        continue;
                    }

                    if (XYIsInMap(localPoint) && canPropagating(localPoint))
                    {
                        effect.Add(newEffect(localPoint));
                        continue;
                    }
                }
            }

        waveEffect.hasDoneSomething += effect.Count;
        // waveEffect.hasDoneSomething = true;
        Debug.Log($"radius: {waveEffect.radius}");
        s += $"}}, // {waveEffect.radius}\n";
        bleak += s;
        // Debug.Log(s);

        ApplyTickEffect(effect.ToArray());
        return effect.Count > 0;
    }
    int waveBounceCounter = 0;
    ulong waveOp = 0;
    ulong complexWaveOp = 0;


    public void StartWaveFromWorld(MapTickEffectWave waveEffect, Vector2 mouseClick)
    {
        var localCenter = WorldToLocal(mouseClick);
        waveEffect.center = localCenter;
        StartWave(waveEffect);
    }
    public void StartWave(MapTickEffectWave waveEffect)
    {
        // AddCircleEdge(waveEffect.mapTickEffect, waveEffect.center, 1);
        AddCircleEdge(waveEffect);
        waveEffect.radius++;
        AddDelayedEffect(waveEffect.tickDelay, waveEffect);
    }

    #endregion

    #region APPLYING EFFECTS

    void ApplyCombiningEffects(MapCombineEffect[] effects, int x, int y)
    {
        for (int i = 0; i < effects.Length; i++)
        {
            var effect = effects[i];
            if (!XYIsInMap(effect.position.x + x, effect.position.y + y)) continue;

            switch (effect.EffectType)
            {
                case MapCombineEffectType.SetBot:
                    var setBotEffect = (MapCombineEffectSetBot)effect;
                    var pos = new Vector2Int(x + setBotEffect.position.x, y + setBotEffect.position.y);
                    // Debug.Log($"applying effect {setBotEffect} at {pos}");

                    if (setBotEffect.tickDelay == 0)
                    {
                        if (!setBotEffect.combine)
                        {
                            map[pos.x, pos.y] = setBotEffect.mapElement;
                            updatedMap = true;
                            continue;
                        }

                        var current = map[pos.x, pos.y];
                        var combineEffects = MapElementFunctions.CombineElements(current, setBotEffect.mapElement);
                        ApplyCombiningEffects(combineEffects, pos.x, pos.y);
                        continue;
                    }

                    var tickEffect = new MapTickEffectSet
                    {
                        mapElement = setBotEffect.mapElement,
                        position = pos,
                        combine = setBotEffect.combine,
                    };
                    // Debug.Log($"adding delayed effect at {tickKey}, position : {setEffect.position + new Vector2Int(x, y)}");

                    AddDelayedEffect(setBotEffect.tickDelay, tickEffect);

                    continue;

                case MapCombineEffectType.SetTop:
                    continue;
                    throw new NotImplementedException();

                case MapCombineEffectType.WaveSet:
                    throw new NotImplementedException();
                    // var waveSetEffect = (MapCombineEffectWaveStartBot)effect;
                    // StartWave(waveSetEffect.mapTickEffect, new Vector2Int(x, y), true, waveSetEffect.)

                    // AddCircleEdge(waveSetEffect.mapTickEffect, new Vector2Int(x, y), waveSetEffect.startingRadius);
                    // AddDelayedEffect(waveSetEffect.tickDelay, new MapTickEffectWave()
                    // {
                    //     mapTickEffect = waveSetEffect.mapTickEffect,
                    //     center = new Vector2Int(x, y),
                    //     radius = waveSetEffect.startingRadius + 1,
                    // });

                    // DelayedTickEffectMap.Add(tick + waveSetEffect.tickDelay, new List<MapCellEffect>() { waveSetEffect });
                    continue;

                default:
                    continue;
            }
        }
    }

    void ApplyTickEffect(MapTickEffect effect)
    {
        if (effect is null) return;

        switch (effect.EffectType)
        {
            case MapTickEffectType.Set:
                var setEffect = (MapTickEffectSet)effect;
                SetElementToXY(setEffect.mapElement, setEffect.position.x, setEffect.position.y, setEffect.combine);
                break;
            case MapTickEffectType.ConditionalSet:
                var condSetEffect = (MapTickEffectConditionalSet)effect;

                Vector2Int posCond = condSetEffect.position;
                if (!XYIsInMap(posCond.x, posCond.y))
                    break;

                if (condSetEffect.ifCondition(map[posCond.x, posCond.y]))
                    ApplyTickEffect(condSetEffect.MapTickEffectIfTrue(posCond.x, posCond.y));
                else
                    ApplyTickEffect(condSetEffect.MapTickEffectIfFalse(posCond.x, posCond.y));

                // SetElementToXY(condSetEffect.mapElement, posCond.x, posCond.y, condSetEffect.combine);
                break;
            case MapTickEffectType.Wave:
                var waveEffect = (MapTickEffectWave)effect;
                // var circleEdgeRes = AddCircleEdge(waveEffect.mapTickEffect, waveEffect.center, waveEffect.radius++, waveEffect.propagating, waveEffect.propagatingElem, waveEffect.passThrough);
                var circleEdgeRes = AddCircleEdge(waveEffect);
                if (circleEdgeRes)
                {
                    // foreach (var v2i in waveEffect.collisions)
                    //     SetElementToXY(MapBotElement.Error, v2i.x, v2i.y, false);
                    waveEffect.radius++;
                    AddDelayedEffect(waveEffect.tickDelay, waveEffect);
                }
                // else
                //     Debug.Log("wave end");
                break;
            default:
                break;
        }
    }
    void ApplyTickEffect(MapTickEffect[] effects)
    {
        for (int i = 0; i < effects.Length; i++)
            ApplyTickEffect(effects[i]);
    }

    #endregion

    #region DEBUG

    public void WhatsHere(Vector2 mouseClick)
    {
        var local = WorldToLocal(mouseClick);
        Debug.Log($"local: {local}, element: {map[local.x, local.y]}");
    }

    void FullBuildTexture()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                mapTexture.SetPixel(x, y,
                    MapElementFunctions.GetElementColor(map[x, y]));
        mapTexture.Apply();
    }

    public void TestShaderTextureOffline()
    {
        map = new MapBotElement[width, height];
        objMaterial = GetComponent<Renderer>().sharedMaterial;

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (x == width / 2 - 1 || x == width / 2 ||
                    y == height / 2 - 1 || y == height / 2)
                    map[x, y] = MapBotElement.Fire;
                else if ((x + y) % 2 == 0)
                {
                    map[x, y] = MapBotElement.Ice;
                    if (x < 4 && y < 4)
                        Debug.Log($"ice: {x}, {y}");
                }
                else
                {
                    map[x, y] = MapBotElement.Error;
                    if (x < 4 && y < 4)
                        Debug.Log($"Error: {x}, {y}");
                }

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                mapTexture.SetPixel(x, y,
                    MapElementFunctions.GetElementColor(map[x, y]));
        mapTexture.Apply();
        // objMaterial.SetTexture("_MainTex", newMapTexture);
    }

    #endregion
}

public class MapCell
{
    public MapBotElement bot;
    public MapTopElement top;
}

[CustomEditor(typeof(Map))]
public class ProceduralBricksEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        Map script = (Map)target;

        if (GUILayout.Button("create and update texture"))
        {
            script.CreateBlankTexture();
        }
        if (GUILayout.Button("test texture offline"))
        {
            script.TestShaderTextureOffline();
        }
        if (GUILayout.Button("testos rectange"))
        {
            script.AddRectangle(MapBotElement.Water, new Vector2Int(10, 10), 10, 10);
        }
        if (GUILayout.Button("testos circle"))
        {
            script.AddCircle(MapBotElement.Water, new Vector2Int(30, 30), 10);
        }
    }
}