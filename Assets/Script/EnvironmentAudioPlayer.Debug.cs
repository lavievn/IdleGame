#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

internal sealed partial class EnvironmentAudioPlayer
{
    [Serializable] private sealed class GainOverride { public string id; public float gain; }
    [Serializable] private sealed class MixSnapshot
    {
        public int version = 1;
        public string map, mapFamily, weather;
        public float master = 1f;
        public GainOverride[] clips, families;
    }

    private readonly Dictionary<string, float> debugClipGains = new Dictionary<string, float>(StringComparer.Ordinal);
    private readonly Dictionary<string, float> debugFamilyGains = new Dictionary<string, float>(StringComparer.Ordinal);
    private readonly HashSet<string> muted = new HashSet<string>(StringComparer.Ordinal);
    private string soloClip, soloFamily, debugMap;
    private float debugMaster = 1f, debugWeatherGain, debugRain;
    private WorldWeather debugVisualWeather;
    private bool debugNight;
    private GameObject debugHost;
    private AudioSource audition;
    private int selectedClip, selectedFamily;
    private Vector2 debugScroll;
    private string status;
    private string[] selectableClips;
    private static readonly string[] Families = Enum.GetNames(typeof(AudioFamily));
    private static readonly AudioLayer[] DebugLayers = (AudioLayer[])Enum.GetValues(typeof(AudioLayer));
    internal AudioWeather? PreviewWeather { get; private set; }
    internal bool? PreviewNight { get; private set; }

    private static string MixFile => Path.Combine(Application.persistentDataPath,
        "Diagnostics", "RC4-environment-audio.json");

    private void ObserveAudioFrame(GameObject host, string map, WorldClimateFrame frame, bool night, float weatherGain)
    {
        debugHost = host;
        debugMap = map;
        debugNight = night;
        debugRain = frame.Rain;
        debugVisualWeather = frame.Weather;
        debugWeatherGain = weatherGain;
    }

    private float ClipGain(string id) => debugClipGains.TryGetValue(id, out var gain) ? gain : gains[id];
    private float DebugFamilyGain(AudioFamily family) => debugFamilyGains.TryGetValue(family.ToString(), out var gain)
        ? gain : FamilyGainFor(family);

    private float AuditionGain(EnvironmentAudioSchedule.Entry entry)
    {
        string family = entry.Family.ToString();
        if (muted.Contains(family) || muted.Contains(entry.Name) || soloClip != null && soloClip != entry.Name ||
            soloFamily != null && soloFamily != family) return 0f;
        return ClipGain(entry.Name) * DebugFamilyGain(entry.Family) * debugMaster;
    }

    internal void StopAudioAudition()
    {
        if (audition != null) { audition.Stop(); audition.clip = null; }
        PreviewWeather = null;
        PreviewNight = null;
    }

    private void ResetDebugMix()
    {
        debugClipGains.Clear();
        debugFamilyGains.Clear();
        muted.Clear();
        soloClip = soloFamily = null;
        debugMaster = 1f;
        StopAudioAudition();
    }

    private static GainOverride[] ExportGains(Dictionary<string, float> values)
    {
        var rows = new List<GainOverride>(values.Count);
        foreach (var value in values) rows.Add(new GainOverride { id = value.Key, gain = value.Value });
        rows.Sort((a, b) => StringComparer.Ordinal.Compare(a.id, b.id));
        return rows.ToArray();
    }

    private string ExportDebugMix() => JsonUtility.ToJson(new MixSnapshot {
        map = debugMap, mapFamily = category.ToString(), weather = weather.ToString(), master = debugMaster,
        clips = ExportGains(debugClipGains), families = ExportGains(debugFamilyGains)
    }, true);

    private static bool ValidGain(float gain) => gain >= 0f && gain <= 1f;

    private bool ImportDebugMix(string json)
    {
        var data = JsonUtility.FromJson<MixSnapshot>(json);
        if (data == null || data.version != 1 || !ValidGain(data.master) || data.clips == null || data.families == null)
            return false;
        var importedClips = new Dictionary<string, float>(StringComparer.Ordinal);
        var importedFamilies = new Dictionary<string, float>(StringComparer.Ordinal);
        foreach (var row in data.clips)
            if (row == null || string.IsNullOrEmpty(row.id) || !clips.ContainsKey(row.id) ||
                !ValidGain(row.gain) || !importedClips.TryAdd(row.id, row.gain)) return false;
        foreach (var row in data.families)
            if (row == null || Array.IndexOf(Families, row.id) < 0 ||
                !ValidGain(row.gain) || !importedFamilies.TryAdd(row.id, row.gain)) return false;
        ResetDebugMix();
        debugMaster = data.master;
        foreach (var row in importedClips) debugClipGains.Add(row.Key, row.Value);
        foreach (var row in importedFamilies) debugFamilyGains.Add(row.Key, row.Value);
        return true;
    }

    private void SaveOrLoadMix(bool save)
    {
        try
        {
            if (save)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(MixFile));
                File.WriteAllText(MixFile, ExportDebugMix());
                status = MixFile;
            }
            else status = ImportDebugMix(File.ReadAllText(MixFile)) ? "Loaded " + MixFile : "Invalid mixer JSON";
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
        {
            status = error.Message;
        }
    }

    private void PlayAudition(string clip)
    {
        if (debugHost == null) return;
        if (audition == null)
        {
            audition = debugHost.AddComponent<AudioSource>();
            audition.playOnAwake = false;
        }
        audition.Stop();
        audition.clip = clips[clip];
        audition.volume = ClipGain(clip) * debugMaster;
        audition.Play();
    }

    private void VerifyDebugMix()
    {
        var voice = voices.Find(candidate => candidate.Entry?.Family == AudioFamily.Morning);
        var entry = voice?.Entry ?? throw new InvalidOperationException("Expected a scheduled Morning source.");
        float timer = schedule.SecondsUntil(AudioLayer.Morning, elapsed);
        muted.Add(entry.Name);
        if (schedule.ExplainLayer(AudioLayer.Morning) != null ||
            schedule.ExplainLayer(AudioLayer.Rain) != "excluded by map, time or weather" ||
            AuditionGain(entry) != 0f || !voice.Source.isPlaying ||
            schedule.SecondsUntil(AudioLayer.Morning, elapsed) != timer)
            throw new InvalidOperationException("Mute must keep the voice and its schedule.");
        muted.Clear();
        soloFamily = AudioFamily.Meadow.ToString();
        if (AuditionGain(entry) != 0f) throw new InvalidOperationException("Solo must isolate its family.");
        soloFamily = null;
        debugClipGains[entry.Name] = .23f;
        debugFamilyGains[entry.Family.ToString()] = .5f;
        debugMaster = .8f;
        string snapshot = ExportDebugMix();
        ResetDebugMix();
        if (!ImportDebugMix(snapshot) || Mathf.Abs(AuditionGain(entry) - .092f) > .0001f ||
            ImportDebugMix(snapshot.Replace(entry.Name, "unknown-clip")) ||
            Mathf.Abs(AuditionGain(entry) - .092f) > .0001f)
            throw new InvalidOperationException("Mixer JSON must round-trip and reject invalid IDs without mutation.");
        ResetDebugMix();
    }

    internal void DrawAudioDebug()
    {
        if (selectableClips == null)
        {
            selectableClips = new List<string>(clips.Keys).ToArray();
            Array.Sort(selectableClips, StringComparer.Ordinal);
        }
        GUILayout.Label($"Map {debugMap} · {category} · {(debugNight ? "Night" : "Day")} · {debugVisualWeather} {debugRain:0.00}");
        GUILayout.Label($"Audio {weather} · Weather bus {debugWeatherGain:0.00} · Sources {voices.Count + (audition == null ? 0 : 1)}");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Auto")) { PreviewWeather = null; PreviewNight = null; }
        if (GUILayout.Button("Day")) PreviewNight = false;
        if (GUILayout.Button("Night")) PreviewNight = true;
        if (GUILayout.Button("Dry")) PreviewWeather = AudioWeather.Dry;
        if (GUILayout.Button("Light")) PreviewWeather = AudioWeather.Rain;
        if (GUILayout.Button("Heavy")) PreviewWeather = AudioWeather.HeavyRain;
        if (GUILayout.Button("Storm")) PreviewWeather = AudioWeather.Storm;
        GUILayout.EndHorizontal();
        debugScroll = GUILayout.BeginScrollView(debugScroll);
        debugMaster = DrawGain("Ambience master", debugMaster);
        selectedFamily = Picker(Families, selectedFamily);
        string family = Families[selectedFamily];
        float familyValue = DebugFamilyGain((AudioFamily)selectedFamily);
        float changedFamily = DrawGain(family, familyValue);
        if (changedFamily != familyValue) debugFamilyGains[family] = changedFamily;
        DrawMuteSolo(family, ref soloFamily);
        selectedClip = Picker(selectableClips, selectedClip);
        string clip = selectableClips[selectedClip];
        float oldClipGain = ClipGain(clip);
        float changedClip = DrawGain(clip + ".ogg", oldClipGain);
        if (changedClip != oldClipGain) debugClipGains[clip] = changedClip;
        DrawMuteSolo(clip, ref soloClip);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Audition")) PlayAudition(clip);
        if (GUILayout.Button("Stop clip") && audition != null) { audition.Stop(); audition.clip = null; }
        if (GUILayout.Button("Reset")) ResetDebugMix();
        if (GUILayout.Button("Export JSON")) SaveOrLoadMix(true);
        if (GUILayout.Button("Import JSON")) SaveOrLoadMix(false);
        GUILayout.EndHorizontal();
        if (!string.IsNullOrEmpty(status)) GUILayout.Label(status);
        foreach (AudioLayer layer in DebugLayers)
            GUILayout.Label($"{layer} · {DescribeDebugLayer(layer)}");
        foreach (var voice in voices)
        {
            if (voice.Entry == null) continue;
            var entry = voice.Entry;
            bool eligible = EnvironmentAudioRules.Eligible(entry.Family, entry.Variant, category, weather, debugNight);
            string reason = AuditionGain(entry) == 0f ? "mix silenced" :
                voice.Desired ? "scheduled" : eligible ? "rotating/limit" : "map/time/weather";
            GUILayout.Label($"{entry.Name}.ogg · {entry.Family}/{entry.Variant} · {(voice.Event ? "one-shot" : "loop")} · " +
                $"{(voice.Source.isPlaying ? "playing" : "stopped")} · clip {ClipGain(entry.Name):0.00} · " +
                $"family {DebugFamilyGain(entry.Family):0.00} · mix {rainMix?.Gain(entry.Layer) ?? 1f:0.00} · " +
                $"volume {voice.Source.volume:0.000} · {reason}");
        }
        if (audition != null && audition.clip != null)
            GUILayout.Label($"Audition · {audition.clip.name}.ogg · {(audition.isPlaying ? "playing" : "stopped")} · volume {audition.volume:0.000}");
        GUILayout.EndScrollView();
    }

    private string DescribeDebugLayer(AudioLayer layer)
    {
        int playing = 0, audible = 0;
        foreach (Voice voice in voices)
        {
            if (voice.Entry?.Layer != layer || !voice.Source.isPlaying) continue;
            playing++;
            if (voice.Source.volume > 0f) audible++;
        }
        string reason = schedule.ExplainLayer(layer);
        if (reason != null) return playing == 0 ? reason : $"{reason} · {playing} fading";
        bool eventLayer = layer == AudioLayer.CoastEvent || layer == AudioLayer.Thunder;
        bool timed = eventLayer || layer == AudioLayer.Morning || layer == AudioLayer.Meadow;
        string state = playing > 0 ? $"{playing} playing, {audible} with gain" :
            eventLayer ? "waiting for event" : "waiting for available voice";
        return timed ? $"{state} · next {schedule.SecondsUntil(layer, elapsed):0.0}s" : state;
    }

    private void DrawMuteSolo(string id, ref string solo)
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Toggle(muted.Contains(id), "Mute") != muted.Contains(id))
        { if (!muted.Remove(id)) muted.Add(id); }
        if (GUILayout.Toggle(solo == id, "Solo") != (solo == id)) solo = solo == id ? null : id;
        GUILayout.EndHorizontal();
    }

    private static float DrawGain(string label, float gain)
    {
        GUILayout.Label($"{label}  {gain:0.00}");
        return GUILayout.HorizontalSlider(gain, 0f, 1f);
    }

    private static int Picker(string[] options, int index)
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("◀", GUILayout.Width(45))) index = (index + options.Length - 1) % options.Length;
        GUILayout.Label(options[index] + $"  ({index + 1}/{options.Length})");
        if (GUILayout.Button("▶", GUILayout.Width(45))) index = (index + 1) % options.Length;
        GUILayout.EndHorizontal();
        return index;
    }
}
#endif
