using System;
using UnityEngine;

internal readonly struct WorldWeatherVisual
{
    internal readonly WorldWeatherPresentationSettings Settings;
    internal readonly string MapId;
    internal readonly float Wind, Haze, Flash, BoltPosition;
    internal readonly bool Reset, Gust, Thunder, CloseThunder;

    internal WorldWeatherVisual(WorldWeatherPresentationSettings settings, string mapId, float wind, float haze,
        float flash, float boltPosition, bool reset, bool gust, bool thunder, bool closeThunder)
    {
        Settings = settings;
        MapId = mapId;
        Wind = wind;
        Haze = haze;
        Flash = flash;
        BoltPosition = boltPosition;
        Reset = reset;
        Gust = gust;
        Thunder = thunder;
        CloseThunder = closeThunder;
    }
}

// Derived from the saved climate clock. Crossing a scheduled instant triggers
// one strike. Entry, restart and clock seeks establish a new baseline.
internal sealed class WorldWeatherStorm
{
    private double last = double.NaN;
    private double thunderAt = double.NaN;
    private float remaining, boltPosition;
    private int mapNumber, seed;
    private string mapId;
    private bool gustAbove, closeThunder;

    internal static double StrikeAt(long slot, float interval, int seed, string mapId, int mapNumber) =>
        interval * (slot + .22 + .56 * Unit(slot, seed, mapId, mapNumber));

    internal WorldWeatherVisual Step(WorldWeatherPresentationSettings settings, WorldClimateFrame frame,
        WorldClimateState state, string id, int number, float daySeconds, bool active, bool outdoors)
    {
        double now = (state.day + state.dayFraction) * daySeconds;
        double delta = now - last;
        bool reset = double.IsNaN(last) || delta < 0 || delta > 2 || seed != state.seed || mapId != id || mapNumber != number;
        double previous = last;
        last = now;
        seed = state.seed;
        mapId = id;
        mapNumber = number;
        bool heavy = outdoors && frame.Weather == WorldWeather.HeavyRain;
        float gustWave = Mathf.Sin((float)(now % 17.0) * (2f * Mathf.PI / 17f) + (seed & 7));
        float gustStrength = heavy ? Mathf.Max(0f, gustWave) * settings.gustStrength * frame.Rain : 0f;
        float wind = settings.rainWind && outdoors
            ? frame.Rain * settings.windDirection * settings.windStrength + gustStrength : 0f;
        bool gust = active && !reset && settings.rainWind && !gustAbove && gustStrength > settings.gustStrength * .55f;
        gustAbove = gustStrength > settings.gustStrength * .55f;
        float haze = settings.distantHaze && outdoors ? settings.stormHaze * Mathf.SmoothStep(0f, 1f, frame.Rain) : 0f;
        if (reset || !heavy || !settings.lightning || !active)
        {
            remaining = 0f;
            thunderAt = double.NaN;
        }
        else if (delta > 0)
        {
            float interval = Mathf.Max(22f, settings.lightningInterval);
            long slot = (long)(now / interval);
            double at = StrikeAt(slot, interval, seed, id, number);
            if (previous < at && now >= at)
            {
                remaining = .23f;
                float distance = Unit(slot + 17, seed, id, number);
                closeThunder = distance < .28f;
                boltPosition = Mathf.Lerp(.15f, .85f, Unit(slot + 29, seed, id, number));
                thunderAt = now + Mathf.Lerp(.7f, 4.2f, distance);
            }
            else remaining = Mathf.Max(0f, remaining - (float)delta);
        }
        bool thunder = !double.IsNaN(thunderAt) && now >= thunderAt;
        if (thunder) thunderAt = double.NaN;
        float flash = remaining / .23f * Mathf.Clamp(settings.lightningBrightness, 0f, .24f);
        if (settings.reducedFlashes) flash *= .15f;
        return new WorldWeatherVisual(settings, id, wind, haze, flash, boltPosition, reset, gust, thunder, closeThunder);
    }

    internal void Reset()
    {
        last = double.NaN;
        thunderAt = double.NaN;
        remaining = 0f;
        gustAbove = false;
    }

    private static float Unit(long slot, int seed, string id, int number)
    {
        unchecked
        {
            uint hash = (uint)(slot ^ (slot >> 32)) * 0x9e3779b9u ^ (uint)seed ^ (uint)number;
            if (id != null)
                foreach (char c in id) hash = (hash ^ c) * 16777619u;
            hash ^= hash >> 16;
            hash *= 0x7feb352du;
            hash ^= hash >> 15;
            return (hash & 0x00ffffff) / 16777216f;
        }
    }
}
