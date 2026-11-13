using System;
using System.Collections.Generic;
using TuTienCore;
using UnityEngine;

internal sealed class EnvironmentAudioPlayer
{
    [Serializable] private sealed class Manifest { public bool complete; public Asset[] assets; public string[] lodgesMapIds; }
    [Serializable] private sealed class Asset { public string clip; public string resource; public float gain; }

    private sealed class Voice
    {
        internal AudioSource Source;
        internal EnvironmentAudioSchedule.Entry Entry;
        internal bool Event, Desired;
        internal float Weight, Target;
    }

    private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
    private readonly Dictionary<string, float> gains = new Dictionary<string, float>(StringComparer.Ordinal);
    private readonly HashSet<AudioClip> seen = new HashSet<AudioClip>();
    private readonly HashSet<string> lodges;
    private readonly List<Voice> voices = new List<Voice>();
    private readonly EnvironmentAudioSchedule schedule;
    private string previousMap;
    private int previousMapNumber;
    private float elapsed;

    private EnvironmentAudioPlayer(Manifest manifest)
    {
        lodges = new HashSet<string>(manifest.lodgesMapIds ?? Array.Empty<string>(), StringComparer.Ordinal);
        foreach (Asset asset in manifest.assets)
        {
            if (asset == null || string.IsNullOrEmpty(asset.clip) || clips.ContainsKey(asset.clip) ||
                !EnvironmentAudioRules.TryParse(asset.clip, out _, out _)) continue;
            var clip = Resources.Load<AudioClip>("IntegratedAmbience/" + asset.resource);
            if (clip == null || !seen.Add(clip)) continue;
            clips.Add(asset.clip, clip);
            gains.Add(asset.clip, Mathf.Clamp01(asset.gain));
        }
        schedule = new EnvironmentAudioSchedule(clips.Keys, new System.Random());
    }

    internal static EnvironmentAudioPlayer Load()
    {
        var asset = Resources.Load<TextAsset>("EnvironmentAudioManifest");
        if (asset == null) return null;
        Manifest manifest;
        try { manifest = JsonUtility.FromJson<Manifest>(asset.text); }
        catch (ArgumentException) { return null; }
        if (manifest == null || !manifest.complete || manifest.assets == null || manifest.assets.Length == 0) return null;
        var player = new EnvironmentAudioPlayer(manifest);
        return player.clips.Count == manifest.assets.Length ? player : null;
    }

    internal static AudioMap Map(TerrainType terrain, string profile, bool assignedLodges)
    {
        if (assignedLodges) return AudioMap.Lodges;
        if (profile == "Village" || profile == "Plains" || profile == "Steppe") return AudioMap.Meadow;
        if (profile == "Snow") return AudioMap.Unassigned;
        switch (terrain)
        {
            case TerrainType.Bien: return AudioMap.Coast;
            case TerrainType.RungRam: case TerrainType.DoiNui: case TerrainType.CaoNguyen: return AudioMap.ForestMountain;
            default: return AudioMap.Unassigned;
        }
    }

    internal void Update(GameObject host, bool active, bool running, TerrainType terrain, string map, int mapNumber,
        string profile, WorldClimateFrame frame, float environment, float weatherGain, float delta, float fadeSeconds,
        int coastWeatherLimit, float thunderInterval)
    {
        if (!active) { Stop(); return; }
        var category = Map(terrain, profile, lodges.Contains(map ?? ""));
        if (previousMap != map || previousMapNumber != mapNumber)
        {
            foreach (Voice voice in voices) if (voice.Event) Clear(voice);
            previousMap = map;
            previousMapNumber = mapNumber;
            schedule.Reset();
        }
        if (running) elapsed += delta;
        var weather = frame.Weather == WorldWeather.HeavyRain ? AudioWeather.Storm :
            frame.Weather == WorldWeather.LightRain ? AudioWeather.Rain : AudioWeather.Dry;
        schedule.Update(category, weather, frame.Phase == WorldDayPhase.Night, elapsed, coastWeatherLimit, thunderInterval);
        var coastCrossfade = CoastCrossfadeVoice(coastWeatherLimit);
        foreach (Voice voice in voices)
        {
            voice.Desired = voice.Event ? voice.Source.isPlaying &&
                EnvironmentAudioRules.Eligible(voice.Entry.Family, voice.Entry.Variant, category, weather, false) :
                voice.Entry != null && schedule.Desired.Contains(voice.Entry);
            if (voice.Entry == null) continue;
            voice.Target = voice.Desired ? 1f : 0f;
            float speed = voice == coastCrossfade ? .5f : 1f;
            voice.Weight = Mathf.MoveTowards(voice.Weight, voice.Target, speed * delta / Mathf.Max(.1f, fadeSeconds));
            voice.Source.volume = voice.Weight * Gain(voice.Entry, environment, weatherGain);
            if (!voice.Desired && (voice.Weight <= 0f || voice.Event)) Clear(voice);
        }
        foreach (var entry in schedule.Desired)
            if (!voices.Exists(voice => voice.Entry == entry) && CanStart(entry, coastWeatherLimit)) Play(host, entry, false);
        if (running)
            foreach (var entry in schedule.Events)
                if (!voices.Exists(voice => voice.Entry == entry) && CanStart(entry, coastWeatherLimit)) Play(host, entry, true);
    }

    private Voice CoastCrossfadeVoice(int limit)
    {
        if (limit < 2 || !schedule.Desired.Exists(entry => entry.Layer == AudioLayer.CoastWeather)) return null;
        int occupied = 0;
        Voice outgoing = null;
        foreach (Voice voice in voices)
        {
            if (voice.Entry?.Layer != AudioLayer.CoastWeather) continue;
            occupied++;
            if (schedule.Desired.Contains(voice.Entry)) return null;
            if (outgoing == null || voice.Weight > outgoing.Weight) outgoing = voice;
        }
        // Keep one outgoing loop audible until another voice frees a slot for the incoming variant.
        return occupied >= limit ? outgoing : null;
    }

    private bool CanStart(EnvironmentAudioSchedule.Entry entry, int coastWeatherLimit)
    {
        int family = 0, layer = 0, totalLayer = 0;
        int limit = EnvironmentAudioRules.Limit(entry.Layer, coastWeatherLimit);
        foreach (Voice voice in voices)
        {
            if (voice.Entry == null || voice.Entry.Family != entry.Family) continue;
            if (voice.Entry.Layer == entry.Layer) totalLayer++;
            if (entry.Family == AudioFamily.Meadow && (entry.Layer == AudioLayer.MeadowWeather) !=
                (voice.Entry.Layer == AudioLayer.MeadowWeather)) return false;
            // A bounded overlap provides crossfade for rotating loops.
            bool transition = !voice.Desired && (entry.Layer == AudioLayer.Meadow || entry.Layer == AudioLayer.Morning);
            if (transition && entry.Layer == voice.Entry.Layer) continue;
            family++;
            if (voice.Entry.Layer == entry.Layer) layer++;
        }
        return family < EnvironmentAudioRules.FamilyLimit(entry.Family, coastWeatherLimit) &&
            layer < limit && totalLayer < limit * 2;
    }

    private float Gain(EnvironmentAudioSchedule.Entry entry, float environment, float weather) => gains[entry.Name] *
        (entry.Layer == AudioLayer.Rain || entry.Layer == AudioLayer.WindRain || entry.Layer == AudioLayer.Thunder ||
         entry.Layer == AudioLayer.Pebble || entry.Layer == AudioLayer.CoastWeather || entry.Layer == AudioLayer.MeadowWeather ? weather : environment);

    private void Play(GameObject host, EnvironmentAudioSchedule.Entry entry, bool oneShot)
    {
        Voice voice = voices.Find(item => item.Entry == null);
        if (voice == null)
        {
            var source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            voice = new Voice { Source = source };
            voices.Add(voice);
        }
        voice.Entry = entry;
        voice.Event = oneShot;
        voice.Desired = true;
        voice.Weight = oneShot ? 1f : 0f;
        voice.Source.clip = clips[entry.Name];
        voice.Source.loop = !oneShot;
        voice.Source.volume = 0f;
        voice.Source.Play();
    }

    private static void Clear(Voice voice)
    {
        voice.Source.Stop();
        voice.Source.clip = null;
        voice.Source.volume = 0f;
        voice.Entry = null;
        voice.Event = false;
        voice.Desired = false;
    }

    internal void Stop()
    {
        foreach (Voice voice in voices) Clear(voice);
        schedule.Reset();
        elapsed = 0f;
        previousMap = null;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    internal int ActiveLoops(AudioLayer layer, float gain = -1f) => voices.FindAll(v =>
        v.Entry?.Layer == layer && v.Source.isPlaying && v.Source.loop &&
        (gain < 0f || Mathf.Abs(v.Source.volume - gain) < .005f)).Count;

    internal static void VerifyTransitions()
    {
        var host = new GameObject("EnvironmentAudioSmoke");
        try
        {
            var verified = Load();
            if (verified == null || verified.clips.Count == 0)
                throw new InvalidOperationException("Verified audio manifest failed to resolve");
            var player = verified;
            var settings = new WorldWeatherAudioSettings();
            void Step(WorldWeather weather, string map = "coast", int visit = 1, float dt = 1f)
            {
                var frame = new WorldClimateFrame(WorldDayPhase.Day, weather, 12f, 0f, 0f, 0f, Color.white, Color.white);
                player.Update(host, true, true, map == "coast" ? TerrainType.Bien : TerrainType.HoNuoc, map, visit,
                    null, frame, 1f, 1f, dt, 2f, settings.coastWeatherLoopLimit, settings.thunderInterval);
            }
            bool Has(string clip) => player.voices.Exists(v => v.Entry?.Name == clip && v.Source.loop);
            bool WetGlobal() => player.voices.Exists(v => v.Entry?.Family == AudioFamily.Rain && v.Source.loop);
            bool RainLoops() => Has("Coast_heavy_rain") && Has("Coast_medium_rain");
            int CoastWetCount() => player.voices.FindAll(v => v.Entry?.Layer == AudioLayer.CoastWeather).Count;
            void Check(bool condition, string message)
            {
                if (!condition) throw new InvalidOperationException(message);
            }
            Step(WorldWeather.LightRain);
            Step(WorldWeather.LightRain);
            Check(RainLoops() && WetGlobal(), "Both Coast Rain loops and global Rain start independently");
            Step(WorldWeather.HeavyRain, dt: .25f);
            Check(RainLoops() && CoastWetCount() == 2, "Rain first frees a slot for Storm");
            for (int i = 0; i < 8 && !Has("Coast_wave_storm"); i++) Step(WorldWeather.HeavyRain, dt: .25f);
            Check(Has("Coast_wave_storm") && CoastWetCount() == 2, "Rain crossfades with Storm within the physical limit");
            Step(WorldWeather.HeavyRain, dt: 2f);
            Check(!Has("Coast_heavy_rain") && !Has("Coast_medium_rain") && Has("Coast_wave_storm"),
                "Coast Rain retires after fade");
            Step(WorldWeather.HeavyRain);
            Step(WorldWeather.LightRain, dt: .25f);
            Check(!RainLoops() && Has("Coast_wave_storm") && CoastWetCount() == 2, "Storm crossfades with Rain within the physical limit");
            Step(WorldWeather.Clear, dt: 2f);
            Check(player.voices.TrueForAll(voice => voice.Entry == null || voice.Entry.Layer == AudioLayer.Coast),
                "Clear retires all wet voices");
            int pool = host.GetComponents<AudioSource>().Length;
            for (int visit = 2; visit <= 4; visit++)
            {
                Step(WorldWeather.LightRain, visit: visit);
                for (int i = 0; i < 8; i++) Step(WorldWeather.LightRain, visit: visit);
                Step(WorldWeather.LightRain, "lake", visit, 2f);
                Check(!Has("Coast_heavy_rain") && !Has("Coast_medium_rain") && WetGlobal(),
                    "Travel clears Coast and keeps global Rain");
                player.Stop();
                Check(host.GetComponents<AudioSource>().Length == pool, "Repeated updates and stop/reset reuse the same sources");
            }
        }
        finally { UnityEngine.Object.Destroy(host); }
    }
#endif
}
