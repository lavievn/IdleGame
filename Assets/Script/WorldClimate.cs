using System;
using TuTienCore;
using UnityEngine;

public enum WorldDayPhase { Dawn, Day, Dusk, Night }
public enum WorldWeather { Clear, Cloudy, Fog, LightRain, HeavyRain }

[Serializable]
public struct WorldClimateState
{
    private const int Version = 1;

    public int version;
    public int seed;
    public int day;
    public double dayFraction;

    public static WorldClimateState AtDawn(int seed) => new WorldClimateState
    {
        version = Version,
        seed = seed,
        dayFraction = 5.0 / 24.0
    };

    [Serializable]
    private sealed class Snapshot
    {
        public WorldClimateState worldClimate;
    }

    public static bool TryRead(string json, out WorldClimateState state)
    {
        state = default;
        try
        {
            var snapshot = JsonUtility.FromJson<Snapshot>(json);
            if (snapshot == null) return false;
            state = snapshot.worldClimate;
            if (state.version == 0 && state.seed == 0 && state.day == 0 && state.dayFraction == 0)
            {
                // Stable default for historical saves. A new run supplies its own seed.
                state = AtDawn(780);
                return true;
            }
            return state.version == Version && state.day >= 0 &&
                state.dayFraction >= 0 && state.dayFraction < 1;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}

public readonly struct WorldClimateFrame
{
    public readonly WorldDayPhase Phase;
    public readonly WorldWeather Weather;
    public readonly float Hour, Cloud, Fog, Rain;
    public readonly Color SkyTint, LandTint;

    internal WorldClimateFrame(WorldDayPhase phase, WorldWeather weather, float hour,
        float cloud, float fog, float rain, Color skyTint, Color landTint)
    {
        Phase = phase;
        Weather = weather;
        Hour = hour;
        Cloud = cloud;
        Fog = fog;
        Rain = rain;
        SkyTint = skyTint;
        LandTint = landTint;
    }
}

public static class WorldClimate
{
    public const float DefaultDaySeconds = 780f;
    private const float WeatherBlendSeconds = 8f;
    private const int SlotsPerDay = 6;

    // Columns: clear, cloudy, fog, light rain, heavy rain.
    private static readonly int[] Plains = { 47, 26, 8, 14, 5 };
    private static readonly int[] Forest = { 25, 29, 17, 21, 8 };
    private static readonly int[] Highland = { 38, 31, 15, 12, 4 };
    private static readonly int[] Swamp = { 14, 22, 31, 22, 11 };
    private static readonly int[] Desert = { 79, 19, 1, 1, 0 };
    private static readonly int[] Water = { 28, 26, 16, 20, 10 };
    private static readonly int[] Snow = { 42, 36, 22, 0, 0 };
    private static readonly int[] Steppe = { 56, 26, 5, 11, 2 };

    // Clouds, fog and rain describe a rendering target, not gameplay effects.
    private static readonly Vector3[] WeatherLevels =
    {
        new Vector3(0f, 0f, 0f),
        new Vector3(.75f, 0f, 0f),
        new Vector3(.48f, .85f, 0f),
        new Vector3(.78f, .16f, .35f),
        new Vector3(1f, .4f, 1f)
    };

    private static readonly Color Night = new Color(.30f, .39f, .65f);
    private static readonly Color Dawn = new Color(.91f, .68f, .64f);
    private static readonly Color Dusk = new Color(.91f, .61f, .52f);
    private static readonly Color Overcast = new Color(.72f, .77f, .86f);

    public static float DaySeconds(float configured) =>
        configured >= 1f && !float.IsInfinity(configured) ? configured : DefaultDaySeconds;

    // Returns true at a whole-second boundary so ordinary autosave can capture
    // a quiet map without requesting a separate save or dirtying every frame.
    public static bool Advance(ref WorldClimateState state, float elapsed, float daySeconds)
    {
        if (!(elapsed > 0f) || float.IsInfinity(elapsed)) return false;
        daySeconds = DaySeconds(daySeconds);
        double fraction = state.dayFraction + (double)elapsed / daySeconds;
        double days = Math.Floor(fraction);
        if (days == 0)
        {
            bool crossedSecond = (int)(fraction * daySeconds) != (int)(state.dayFraction * daySeconds);
            state.dayFraction = fraction;
            return crossedSecond;
        }

        state.day = (int)Math.Min(int.MaxValue, state.day + days);
        state.dayFraction = fraction - days;
        return true;
    }

    public static WorldClimateFrame Sample(WorldClimateState state, float daySeconds,
        TerrainType terrain, string mapId, int legacyMap, bool indoor, WorldWeather? weatherOverride = null)
    {
        double slotPosition = state.dayFraction * SlotsPerDay;
        int slot = Math.Min(SlotsPerDay - 1, (int)slotPosition);
        int mapKey = string.IsNullOrEmpty(mapId) ? legacyMap : HashMapId(mapId);
        int[] weights = WeightsFor(terrain, mapId);
        WorldWeather weather = indoor ? WorldWeather.Clear :
            weatherOverride ?? Choose(state.seed, state.day, slot, mapKey, weights);
        Vector3 levels = WeatherLevels[(int)weather];

        if (!indoor && !weatherOverride.HasValue)
        {
            float slotSeconds = DaySeconds(daySeconds) / SlotsPerDay;
            float blendDuration = Mathf.Min(WeatherBlendSeconds, slotSeconds * .5f);
            float blend = Smooth((float)(slotPosition - slot) * slotSeconds / blendDuration);
            if (blend < 1f)
            {
                var previous = slot == 0
                    ? Choose(state.seed, state.day - 1, SlotsPerDay - 1, mapKey, weights)
                    : Choose(state.seed, state.day, slot - 1, mapKey, weights);
                levels = Vector3.Lerp(WeatherLevels[(int)previous], levels, blend);
            }
        }

        float hour = (float)(state.dayFraction * 24.0);
        Color daylight = Daylight(hour, out WorldDayPhase phase);
        Color sky = daylight * Color.Lerp(Color.white, Overcast, levels.x * .64f + levels.y * .24f);
        Color land = Color.Lerp(Color.white, sky, .46f);
        return new WorldClimateFrame(phase, weather, hour, levels.x, levels.y, levels.z, sky, land);
    }

    private static Color Daylight(float hour, out WorldDayPhase phase)
    {
        if (hour < 5f || hour >= 20f)
        {
            phase = WorldDayPhase.Night;
            return Night;
        }
        if (hour < 8f)
        {
            phase = WorldDayPhase.Dawn;
            return hour < 6.5f ? Color.Lerp(Night, Dawn, Smooth((hour - 5f) / 1.5f))
                : Color.Lerp(Dawn, Color.white, Smooth((hour - 6.5f) / 1.5f));
        }
        if (hour < 17f)
        {
            phase = WorldDayPhase.Day;
            return Color.white;
        }
        phase = WorldDayPhase.Dusk;
        return hour < 18.5f ? Color.Lerp(Color.white, Dusk, Smooth((hour - 17f) / 1.5f))
            : Color.Lerp(Dusk, Night, Smooth((hour - 18.5f) / 1.5f));
    }

    private static float Smooth(float value)
    {
        float t = Mathf.Clamp01(value);
        return t * t * (3f - 2f * t);
    }

    private static int[] WeightsFor(TerrainType terrain, string mapId)
    {
        if (mapId == "TL-01" || mapId == "TL-03" || mapId == "TL-04") return Snow;
        if (mapId == "TL-25") return Steppe;
        switch (terrain)
        {
            case TerrainType.RungRam: return Forest;
            case TerrainType.CaoNguyen:
            case TerrainType.DoiNui: return Highland;
            case TerrainType.DamLay: return Swamp;
            case TerrainType.SaMac: return Desert;
            case TerrainType.HoNuoc:
            case TerrainType.Bien: return Water;
            default: return Plains;
        }
    }

    private static int HashMapId(string id)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (char c in id) hash = (hash ^ c) * 16777619;
            return (int)hash;
        }
    }

    private static WorldWeather Choose(int seed, int day, int slot, int mapKey, int[] weights)
    {
        unchecked
        {
            uint hash = (uint)seed ^ (uint)mapKey ^ ((uint)day * 0x9e3779b9u) ^ ((uint)slot * 0x85ebca6bu);
            hash = (hash ^ (hash >> 16)) * 0x7feb352du;
            hash = (hash ^ (hash >> 15)) * 0x846ca68bu;
            int roll = (int)((hash ^ (hash >> 16)) % 100);
            for (int i = 0; i < weights.Length; i++)
            {
                roll -= weights[i];
                if (roll < 0) return (WorldWeather)i;
            }
        }
        return WorldWeather.Clear;
    }
}
