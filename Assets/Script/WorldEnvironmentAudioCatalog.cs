using System;
using System.Collections.Generic;
using TuTienCore;
using UnityEngine;

internal sealed class WorldEnvironmentAudioCatalog
{
    internal const int SlotCount = 10;
    private const string ClipRoot = "IntegratedAmbience/";

    [Serializable]
    internal sealed class Layer
    {
        public int slot;
        public string clip, response;
        public float gain;
    }

    [Serializable]
    internal sealed class Profile
    {
        public string id;
        public Layer[] layers;
    }

    [Serializable]
    private sealed class Document
    {
        public Profile[] profiles;
        public Layer[] weather;
    }

    private readonly Dictionary<string, Profile> profiles = new Dictionary<string, Profile>(StringComparer.Ordinal);
    private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
    internal Layer[] Weather { get; }
    internal static WorldEnvironmentAudioCatalog Load()
    {
        var json = Resources.Load<TextAsset>("WorldEnvironmentAudioCatalog");
        return json != null ? new WorldEnvironmentAudioCatalog(JsonUtility.FromJson<Document>(json.text)) : null;
    }

    private WorldEnvironmentAudioCatalog(Document data)
    {
        foreach (var profile in data.profiles) profiles.Add(profile.id, profile);
        Weather = data.weather;
    }

    internal Profile Select(TerrainType terrain, string mapId, bool village)
    {
        string id;
        if (village) id = "Village";
        else if (mapId == "TL-01" || mapId == "TL-03" || mapId == "TL-04") id = "Snow";
        else if (mapId == "TL-25") id = "Steppe";
        else
        {
            switch (terrain)
            {
                case TerrainType.CaoNguyen:
                case TerrainType.DoiNui: id = "Highland"; break;
                case TerrainType.DamLay: id = "Swamp"; break;
                case TerrainType.RungRam: id = "Forest"; break;
                case TerrainType.SaMac: id = "Desert"; break;
                case TerrainType.HoNuoc: id = "Lake"; break;
                case TerrainType.Bien: id = "Coast"; break;
                default: id = "Plains"; break;
            }
        }
        return profiles.TryGetValue(id, out var profile) ? profile : null;
    }

    internal AudioClip Clip(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (!clips.TryGetValue(name, out var clip))
        {
            clip = Resources.Load<AudioClip>(ClipRoot + name);
            clips.Add(name, clip);
        }
        return clip;
    }

    internal static float LayerGain(Layer layer, WorldClimateFrame frame, float wind)
    {
        float night = 1f - Mathf.SmoothStep(0f, 1f, (frame.Hour - 5f) / 2f)
            + Mathf.SmoothStep(0f, 1f, (frame.Hour - 18f) / 3f);
        float heavy = Mathf.SmoothStep(0f, 1f, (frame.Rain - .30f) / .70f);
        switch (layer.response)
        {
            case "day": return layer.gain * (1f - night) * (1f - .85f * frame.Rain) *
                (1f - .12f * frame.Cloud - .18f * frame.Fog);
            case "night": return layer.gain * night * (1f - .75f * frame.Rain);
            case "lightRain": return layer.gain * Mathf.Clamp01(frame.Rain / .35f) * (1f - heavy);
            case "heavyRain": return layer.gain * heavy;
            case "wind": return layer.gain * Mathf.Clamp01(Mathf.Abs(wind) / 5f) * frame.Rain;
            default: return layer.gain * (1f - .30f * frame.Rain);
        }
    }
}
