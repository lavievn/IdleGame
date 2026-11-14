using System;
using System.Collections.Generic;

internal enum AudioFamily { Coast, Lodges, Meadow, Morning, Night, Pebble, Primeval, Rain, Thunder, WindRain }
internal enum AudioVariant { Default, Beach, Wave, Wind, Rain, Storm, Cricket, LightRain }
internal enum AudioWeather { Dry, Rain, HeavyRain, Storm }
internal enum AudioMap { Unassigned, Coast, Lodges, Meadow, Forest, Mountain }
internal enum AudioLayer { Coast, CoastEvent, CoastWeather, Lodges, Meadow, MeadowCricket, MeadowWeather,
    Morning, Night, Pebble, Primeval, Rain, Thunder, WindRain }

[Serializable]
public sealed class EnvironmentRainMix
{
    public float rainGain = 1f, windGain = .7f, pebbleGain = .7f, thunderGain = .7f, coastGain = .8f, meadowGain = 1f;
    public int pebbleVoices = 2, windVoices = 1, thunderVoices = 1;
    public float thunderSeconds = 35f, fadeSeconds = 2f;
    public string[] rainClips = Array.Empty<string>(), windClips = Array.Empty<string>(), thunderClips = Array.Empty<string>();

    internal bool Allows(AudioLayer layer, string clip)
    {
        var options = layer == AudioLayer.Rain ? rainClips : layer == AudioLayer.WindRain ? windClips :
            layer == AudioLayer.Thunder ? thunderClips : null;
        return options == null || options.Length == 0 || Array.IndexOf(options, clip) >= 0;
    }

    internal int Limit(AudioLayer layer)
    {
        switch (layer)
        {
            case AudioLayer.Pebble: return Math.Max(0, Math.Min(2, pebbleVoices));
            case AudioLayer.WindRain: return windVoices > 0 ? 1 : 0;
            case AudioLayer.Thunder: return thunderVoices > 0 ? 1 : 0;
            default: return int.MaxValue;
        }
    }

    internal float Gain(AudioLayer layer)
    {
        switch (layer)
        {
            case AudioLayer.Rain: return rainGain;
            case AudioLayer.WindRain: return windGain;
            case AudioLayer.Pebble: return pebbleGain;
            case AudioLayer.Thunder: return thunderGain;
            case AudioLayer.CoastWeather: return coastGain;
            case AudioLayer.MeadowWeather: return meadowGain;
            default: return 1f;
        }
    }
}

internal static class EnvironmentAudioRules
{
    internal static AudioWeather Weather(bool lightRain, bool heavyRain, float rain, float stormThreshold) =>
        heavyRain ? (rain >= stormThreshold ? AudioWeather.Storm : AudioWeather.HeavyRain) :
        lightRain ? AudioWeather.Rain : AudioWeather.Dry;
    internal static bool TryParse(string name, out AudioFamily family, out AudioVariant variant)
    {
        family = default;
        variant = default;
        if (string.IsNullOrEmpty(name)) return false;
        if (name.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase)) name = name.Substring(0, name.Length - 4);
        int split = name.IndexOf('_');
        if (split < 1) return false;
        string prefix = name.Substring(0, split);
        if (!Enum.TryParse(prefix, false, out family) || family.ToString() != prefix) return false;
        string tail = name.Substring(split + 1);
        if (tail.Length >= 2 && IsDigit(tail[0]) && IsDigit(tail[1]) &&
            (tail.Length == 2 || tail[2] == '_'))
        {
            if (tail.Length == 2) return true;
            tail = tail.Substring(3);
        }
        if (family == AudioFamily.Coast)
        {
            switch (tail)
            {
                case "beach": case "beach2": variant = AudioVariant.Beach; return true;
                case "wave": case "big_wave": case "heavy_wave": case "heavy_wave2": case "light_wave":
                case "wave_far": variant = AudioVariant.Wave; return true;
                case "wind": case "wind2": variant = AudioVariant.Wind; return true;
                case "rain": case "heavy_rain": case "medium_rain": variant = AudioVariant.Rain; return true;
                case "storm": case "wave_storm": variant = AudioVariant.Storm; return true;
            }
        }
        if (family == AudioFamily.Meadow)
        {
            switch (tail)
            {
                case "cricket": variant = AudioVariant.Cricket; return true;
                case "light_rain": variant = AudioVariant.LightRain; return true;
                case "rain": variant = AudioVariant.Rain; return true;
            }
        }
        return false;
    }

    private static bool IsDigit(char value) => value >= '0' && value <= '9';

    internal static AudioLayer Layer(AudioFamily family, AudioVariant variant)
    {
        switch (family)
        {
            case AudioFamily.Coast: return variant == AudioVariant.Default ? AudioLayer.Coast :
                variant == AudioVariant.Rain || variant == AudioVariant.Storm ? AudioLayer.CoastWeather : AudioLayer.CoastEvent;
            case AudioFamily.Meadow:
                if (variant == AudioVariant.Cricket) return AudioLayer.MeadowCricket;
                return variant == AudioVariant.Rain || variant == AudioVariant.LightRain ?
                    AudioLayer.MeadowWeather : AudioLayer.Meadow;
            default: return (AudioLayer)Enum.Parse(typeof(AudioLayer), family.ToString());
        }
    }

    internal static int Limit(AudioLayer layer, int coastWeatherLimit, int morningLimit = 1)
    {
        switch (layer)
        {
            case AudioLayer.CoastWeather: return Math.Max(0, Math.Min(2, coastWeatherLimit));
            case AudioLayer.Night: return 4;
            case AudioLayer.Coast: case AudioLayer.Primeval: return 3;
            case AudioLayer.Morning: return Math.Max(0, Math.Min(3, morningLimit));
            case AudioLayer.Pebble: case AudioLayer.Meadow: return 2;
            default: return 1;
        }
    }

    internal static int FamilyLimit(AudioFamily family, int coastWeatherLimit, int morningLimit = 1)
    {
        switch (family)
        {
            case AudioFamily.Coast: return 4 + Limit(AudioLayer.CoastWeather, coastWeatherLimit);
            case AudioFamily.Meadow: return Limit(AudioLayer.Meadow, coastWeatherLimit) +
                Limit(AudioLayer.MeadowCricket, coastWeatherLimit);
            default: return Limit(Layer(family, AudioVariant.Default), coastWeatherLimit, morningLimit);
        }
    }

    internal static bool Eligible(AudioFamily family, AudioVariant variant, AudioMap map, AudioWeather weather,
        bool night)
    {
        switch (family)
        {
            case AudioFamily.Coast: return map == AudioMap.Coast && (variant == AudioVariant.Rain ? weather == AudioWeather.Rain || weather == AudioWeather.HeavyRain :
                variant != AudioVariant.Storm || weather == AudioWeather.Storm);
            case AudioFamily.Lodges: return map == AudioMap.Lodges;
            case AudioFamily.Meadow: return map == AudioMap.Meadow &&
                (variant == AudioVariant.Rain || variant == AudioVariant.LightRain
                    ? weather == AudioWeather.Rain : weather == AudioWeather.Dry && (variant != AudioVariant.Cricket || night));
            case AudioFamily.Morning: return map == AudioMap.Forest && weather == AudioWeather.Dry;
            case AudioFamily.Primeval: return (map == AudioMap.Forest || map == AudioMap.Mountain) &&
                !night && weather == AudioWeather.Dry;
            case AudioFamily.Night: return night && weather == AudioWeather.Dry;
            default: return weather != AudioWeather.Dry;
        }
    }
}

internal sealed class EnvironmentAudioSchedule
{
    internal sealed class Entry
    {
        internal readonly string Name;
        internal readonly AudioFamily Family;
        internal readonly AudioVariant Variant;
        internal readonly AudioLayer Layer;
        internal Entry(string name, AudioFamily family, AudioVariant variant)
        {
            Name = name;
            Family = family;
            Variant = variant;
            Layer = EnvironmentAudioRules.Layer(family, variant);
        }
    }

    private static readonly AudioLayer[] Layers = (AudioLayer[])Enum.GetValues(typeof(AudioLayer));
    private readonly List<Entry>[] pools = new List<Entry>[Layers.Length];
    private readonly List<Entry>[] selections = new List<Entry>[Layers.Length];
    private readonly List<Entry> candidates = new List<Entry>(), previous = new List<Entry>();
    private readonly float[] next = new float[Layers.Length];
    private readonly Random random;
    private AudioMap previousMap;
    private AudioWeather previousWeather;
    private bool previousNight, started;
    private int previousLimit, previousMorningLimit;
    private EnvironmentRainMix previousMix;
    internal readonly List<Entry> Desired = new List<Entry>();
    internal readonly List<Entry> Events = new List<Entry>(2);

    internal EnvironmentAudioSchedule(IEnumerable<string> names, Random random)
    {
        this.random = random;
        foreach (var layer in Layers)
        {
            pools[(int)layer] = new List<Entry>();
            selections[(int)layer] = new List<Entry>();
        }
        var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string name in names)
            if (unique.Add(name) && EnvironmentAudioRules.TryParse(name, out var family, out var variant))
            {
                var entry = new Entry(name, family, variant);
                pools[(int)entry.Layer].Add(entry);
            }
    }

    internal void Reset()
    {
        foreach (var selection in selections) selection.Clear();
        Desired.Clear();
        Events.Clear();
        started = false;
    }

    internal float SecondsUntil(AudioLayer layer, float time) => Math.Max(0f, next[(int)layer] - time);

    internal void Update(AudioMap map, AudioWeather weather, bool night, float time,
        int coastWeatherLimit, float thunderInterval = 35f, int morningLimit = 1,
        float morningSeconds = 120f, EnvironmentRainMix mix = null)
    {
        Events.Clear();
        bool morningEligible = map == AudioMap.Forest && weather == AudioWeather.Dry;
        bool changed = !started || map != previousMap || weather != previousWeather ||
            night != previousNight || coastWeatherLimit != previousLimit || previousMorningLimit != morningLimit ||
            !ReferenceEquals(previousMix, mix);
        if (!started || map != previousMap) next[(int)AudioLayer.CoastEvent] = time + Interval();
        if (!started || weather != previousWeather || !ReferenceEquals(previousMix, mix))
            next[(int)AudioLayer.Thunder] = time + NextThunder(thunderInterval);
        Desired.Clear();
        if (!changed && (map != AudioMap.Meadow || weather != AudioWeather.Dry || time < next[(int)AudioLayer.Meadow]) &&
            (!morningEligible || time < next[(int)AudioLayer.Morning]) &&
            (map != AudioMap.Coast || time < next[(int)AudioLayer.CoastEvent]) &&
            (weather == AudioWeather.Dry || time < next[(int)AudioLayer.Thunder]))
        {
            foreach (var selection in selections) Desired.AddRange(selection);
            return;
        }
        foreach (var layer in Layers)
        {
            int index = (int)layer;
            var selected = selections[index];
            bool rotates = layer == AudioLayer.Meadow || layer == AudioLayer.Morning;
            bool eventLayer = layer == AudioLayer.CoastEvent || layer == AudioLayer.Thunder;
            bool due = (rotates || eventLayer) && time >= next[index];
            if (changed || due)
            {
                candidates.Clear();
                foreach (var entry in pools[index])
                    if (EnvironmentAudioRules.Eligible(entry.Family, entry.Variant, map, weather, night) &&
                        (mix == null || mix.Allows(layer, entry.Name))) candidates.Add(entry);
                if (eventLayer)
                {
                    if (due)
                    {
                        if (candidates.Count > 0 && (mix == null || mix.Limit(layer) > 0))
                            Events.Add(candidates[random.Next(candidates.Count)]);
                        next[index] = time + (layer == AudioLayer.CoastEvent ? Interval() : NextThunder(thunderInterval));
                    }
                    continue;
                }
                int limit = Math.Min(candidates.Count, EnvironmentAudioRules.Limit(layer, coastWeatherLimit, morningLimit));
                if (mix != null) limit = Math.Min(limit, mix.Limit(layer));
                previous.Clear();
                if (rotates && due && selected.Count > 0)
                {
                    previous.AddRange(selected);
                    selected.Clear();
                }
                else selected.RemoveAll(entry => !candidates.Contains(entry));
                if (selected.Count > limit) selected.RemoveRange(limit, selected.Count - limit);
                bool refill = selected.Count < limit;
                candidates.RemoveAll(entry => selected.Contains(entry));
                while (selected.Count < limit)
                {
                    var fresh = candidates.FindAll(entry => !previous.Contains(entry));
                    var choices = fresh.Count > 0 ? fresh : candidates;
                    Entry pick = choices[random.Next(choices.Count)];
                    selected.Add(pick);
                    candidates.Remove(pick);
                }
                if (rotates && (due || refill)) next[index] = time + (layer == AudioLayer.Meadow ? 300f : Math.Max(1f, morningSeconds));
            }
            Desired.AddRange(selected);
        }
        previousMap = map;
        previousWeather = weather;
        previousNight = night;
        previousLimit = coastWeatherLimit;
        previousMorningLimit = morningLimit;
        previousMix = mix;
        started = true;
    }

    private float Interval() => 10f + (float)random.NextDouble() * 5f;
    private float NextThunder(float interval) => Math.Max(1f, interval) * (.75f + .5f * (float)random.NextDouble());
}
