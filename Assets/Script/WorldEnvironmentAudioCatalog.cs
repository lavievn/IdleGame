using System;
using System.Collections.Generic;
using TuTienCore;
using UnityEngine;

internal sealed class WorldEnvironmentAudioCatalog
{
    internal const float PrefixClipGain = .5f;
    private const string ClipRoot = "IntegratedAmbience/";

    [Serializable]
    internal sealed class Layer
    {
        public string clip, response;
        public float gain;
    }

    [Serializable]
    internal sealed class Profile
    {
        public string id, prefix;
    }

    [Serializable]
    private sealed class Document
    {
        public Profile[] profiles;
        public Layer[] weather;
    }

    private readonly Dictionary<string, Profile> profiles = new Dictionary<string, Profile>(StringComparer.Ordinal);
    private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
    private readonly Dictionary<string, AudioClip[]> groups = new Dictionary<string, AudioClip[]>(StringComparer.Ordinal);
    private AudioClip[] importedClips;
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

    internal AudioClip[] PrefixClips(Profile profile)
    {
        if (profile == null) return Array.Empty<AudioClip>();
        if (!groups.TryGetValue(profile.prefix, out var group))
        {
            importedClips ??= Resources.LoadAll<AudioClip>(ClipRoot.TrimEnd('/'));
            string prefix = profile.prefix + "_";
            group = Array.FindAll(importedClips, clip => clip.name.StartsWith(prefix, StringComparison.Ordinal));
            Array.Sort(group, (a, b) => StringComparer.Ordinal.Compare(a.name, b.name));
            groups.Add(profile.prefix, group);
        }
        return group;
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
        float heavy = Mathf.SmoothStep(0f, 1f, (frame.Rain - .30f) / .70f);
        switch (layer.response)
        {
            case "lightRain": return layer.gain * Mathf.Clamp01(frame.Rain / .35f) * (1f - heavy);
            case "heavyRain": return layer.gain * heavy;
            case "wind": return layer.gain * Mathf.Clamp01(Mathf.Abs(wind) / 5f) * frame.Rain;
            default: return 0f;
        }
    }
}
