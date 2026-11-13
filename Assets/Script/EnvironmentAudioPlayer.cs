using System;
using System.Collections.Generic;
using TuTienCore;
using UnityEngine;

internal sealed class EnvironmentAudioPlayer
{
    [Serializable] private sealed class Manifest { public bool complete; public Asset[] assets; public string[] lodgesMapIds; }
    [Serializable] private sealed class Asset { public string clip; public float gain; }

    private sealed class Voice
    {
        internal AudioSource Source;
        internal EnvironmentAudioSchedule.Entry Entry;
        internal bool Event, Desired;
        internal float Weight, Target;
    }

    private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
    private readonly Dictionary<string, float> gains = new Dictionary<string, float>(StringComparer.Ordinal);
    private readonly HashSet<string> lodges;
    private readonly List<Voice> voices = new List<Voice>();
    private readonly EnvironmentAudioSchedule schedule;
    private string previousMap;
    private int previousMapNumber;
    private float elapsed;

    private EnvironmentAudioPlayer(Manifest manifest, System.Random random)
    {
        lodges = new HashSet<string>(manifest.lodgesMapIds ?? Array.Empty<string>(), StringComparer.Ordinal);
        foreach (Asset asset in manifest.assets)
        {
            if (asset == null || string.IsNullOrEmpty(asset.clip) || clips.ContainsKey(asset.clip) ||
                !EnvironmentAudioRules.TryParse(asset.clip, out _, out _)) continue;
            var clip = Resources.Load<AudioClip>("IntegratedAmbience/" + asset.clip);
            if (clip == null || !string.Equals(clip.name, asset.clip, StringComparison.Ordinal)) continue;
            clips.Add(asset.clip, clip);
            gains.Add(asset.clip, Mathf.Clamp01(asset.gain));
        }
        schedule = new EnvironmentAudioSchedule(clips.Keys, random);
    }

    internal static EnvironmentAudioPlayer Load() => Load(new System.Random());

    private static EnvironmentAudioPlayer Load(System.Random random)
    {
        var asset = Resources.Load<TextAsset>("EnvironmentAudioManifest");
        if (asset == null) return null;
        Manifest manifest;
        try { manifest = JsonUtility.FromJson<Manifest>(asset.text); }
        catch (ArgumentException) { return null; }
        if (manifest == null || !manifest.complete || manifest.assets == null || manifest.assets.Length == 0) return null;
        var player = new EnvironmentAudioPlayer(manifest, random);
        return player.clips.Count == manifest.assets.Length ? player : null;
    }

    internal static AudioMap Map(TerrainType terrain, string map, bool village, bool assignedLodges)
    {
        if (assignedLodges) return AudioMap.Lodges;
        if (village || map == "TL-25") return AudioMap.Meadow;
        if (map == "TL-01" || map == "TL-03" || map == "TL-04") return AudioMap.Unassigned;
        switch (terrain)
        {
            case TerrainType.Bien: return AudioMap.Coast;
            case TerrainType.RungRam: return AudioMap.Forest;
            case TerrainType.DoiNui: case TerrainType.CaoNguyen: return AudioMap.Mountain;
            case TerrainType.DongBang: return AudioMap.Meadow;
            default: return AudioMap.Unassigned;
        }
    }

    internal void Update(GameObject host, bool active, bool running, TerrainType terrain, string map, int mapNumber,
        bool village, WorldClimateFrame frame, float environment, float weatherGain, float delta, float fadeSeconds,
        int coastWeatherLimit, float thunderInterval)
    {
        if (!active) { Stop(); return; }
        var category = Map(terrain, map, village, lodges.Contains(map ?? ""));
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
        schedule.Update(category, weather, frame.Phase == WorldDayPhase.Night, frame.Hour, elapsed, coastWeatherLimit, thunderInterval);
        var coastCrossfade = CoastCrossfadeVoice(coastWeatherLimit);
        foreach (Voice voice in voices)
        {
            if (voice.Entry?.Family == AudioFamily.Morning && !EnvironmentAudioRules.IsMorningHour(frame.Hour))
            {
                Clear(voice);
                continue;
            }
            voice.Desired = voice.Event ? voice.Source.isPlaying &&
                EnvironmentAudioRules.Eligible(voice.Entry.Family, voice.Entry.Variant, category, weather, false, false) :
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
        int family = 0, layer = 0;
        int limit = EnvironmentAudioRules.Limit(entry.Layer, coastWeatherLimit);
        foreach (Voice voice in voices)
        {
            if (voice.Entry == null || voice.Entry.Family != entry.Family) continue;
            family++;
            if (voice.Entry.Layer == entry.Layer) layer++;
        }
        return family < EnvironmentAudioRules.FamilyLimit(entry.Family, coastWeatherLimit) &&
            layer < limit;
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
            var verified = Load(new System.Random(205));
            if (verified == null || verified.clips.Count == 0)
                throw new InvalidOperationException("Verified audio manifest failed to resolve");
            var player = verified;
            var settings = new WorldWeatherAudioSettings();
            void Step(WorldWeather weather, string map = "coast", int visit = 1, float dt = 1f,
                WorldDayPhase phase = WorldDayPhase.Day, float hour = 12f)
            {
                var frame = new WorldClimateFrame(phase, weather, hour, 0f, 0f, 0f, Color.white, Color.white);
                TerrainType terrain = map == "coast" ? TerrainType.Bien :
                    WorldMapCatalog.TryGet(map, out var node) ? node.Terrain : TerrainType.HoNuoc;
                player.Update(host, true, true, terrain, map, visit,
                    false, frame, 1f, 1f, dt, 2f, settings.coastWeatherLoopLimit, settings.thunderInterval);
            }
            bool Has(string clip) => player.voices.Exists(v => v.Entry?.Name == clip && v.Source.loop);
            bool WetGlobal() => player.voices.Exists(v => v.Entry?.Family == AudioFamily.Rain && v.Source.loop);
            bool RainLoops() => Has("Coast_heavy_rain") && Has("Coast_medium_rain");
            int CoastWetCount() => player.voices.FindAll(v => v.Entry?.Layer == AudioLayer.CoastWeather).Count;
            void Check(bool condition, string message)
            {
                if (!condition) throw new InvalidOperationException(message);
            }
            Step(WorldWeather.Clear);
            Check(player.ActiveLoops(AudioLayer.Coast) == 3, "Clear day has three real Coast sources");
            Step(WorldWeather.Clear, phase: WorldDayPhase.Night);
            Check(player.ActiveLoops(AudioLayer.Coast) == 3 && player.ActiveLoops(AudioLayer.Night) == 4,
                "Clear night overlays four sources while retaining Coast");
            Step(WorldWeather.LightRain, phase: WorldDayPhase.Night);
            Step(WorldWeather.LightRain, dt: 2f, phase: WorldDayPhase.Night);
            Check(RainLoops() && WetGlobal(), "Both Coast Rain loops and global Rain start independently");
            Check(player.ActiveLoops(AudioLayer.Night) == 0 && player.ActiveLoops(AudioLayer.Pebble) == 2 &&
                player.ActiveLoops(AudioLayer.WindRain) == 1, "Rain retires Night and activates wet overlay sources");
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
            Step(WorldWeather.LightRain, "lake", 2, .25f);
            Check(WetGlobal() && player.ActiveLoops(AudioLayer.CoastWeather) > 0,
                "Travel preserves the global loop while the previous map is fading");
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

            void Meadow(WorldWeather weather, float dt = 1f) => Step(weather, "TL-25", dt: dt);
            Voice MeadowVoice() => player.voices.Find(v => v.Entry?.Family == AudioFamily.Meadow);
            Meadow(WorldWeather.Clear);
            Meadow(WorldWeather.Clear, 2f);
            var initial = MeadowVoice()?.Entry;
            Check(initial != null && player.ActiveLoops(AudioLayer.Meadow, .30f) == 1, "One dry Meadow source at reduced gain");
            Meadow(WorldWeather.Clear, 301f);
            Check(MeadowVoice()?.Entry != initial && player.ActiveLoops(AudioLayer.Meadow) == 1, "Meadow rotation stops the old source first");
            Meadow(WorldWeather.Clear, 2f);
            Meadow(WorldWeather.LightRain, .25f);
            Check(player.ActiveLoops(AudioLayer.Meadow) == 1 && player.ActiveLoops(AudioLayer.MeadowWeather) == 0 && WetGlobal(),
                "Meadow fades its dry clip before starting a wet clip while global Rain starts independently");
            Meadow(WorldWeather.LightRain, 2f);
            Meadow(WorldWeather.LightRain, 2f);
            Check(player.ActiveLoops(AudioLayer.Meadow) == 0 && player.ActiveLoops(AudioLayer.MeadowWeather, .30f) == 1 && WetGlobal(),
                "One wet Meadow source at reduced gain");
            Meadow(WorldWeather.Clear, .25f);
            Check(player.ActiveLoops(AudioLayer.Meadow) == 0 && player.ActiveLoops(AudioLayer.MeadowWeather) == 1,
                "Dry Meadow waits for the wet source to finish fading");

            player.Stop();
            Step(WorldWeather.Clear, "TV-16", phase: WorldDayPhase.Dawn, hour: 5f);
            Step(WorldWeather.Clear, "TV-16", dt: 2f);
            var firstMorning = player.voices.Find(v => v.Entry?.Family == AudioFamily.Morning)?.Entry;
            Check(firstMorning != null && player.ActiveLoops(AudioLayer.Morning) == 1, "One Morning source at dawn through noon");
            Step(WorldWeather.Clear, "TV-16", dt: 118f);
            Check(!Has(firstMorning.Name) && player.ActiveLoops(AudioLayer.Morning) == 1, "Morning rotation uses one source");
            Step(WorldWeather.HeavyRain, "TV-16", dt: .25f, hour: 13f);
            Check(player.ActiveLoops(AudioLayer.Morning) == 0, "Morning stops at 13:00");
            Step(WorldWeather.HeavyRain, "TV-16", phase: WorldDayPhase.Night, hour: 22f);
            Check(player.ActiveLoops(AudioLayer.Morning) == 0 && WetGlobal(), "Night storm has no Morning source");
            Step(WorldWeather.Clear, "TV-16", phase: WorldDayPhase.Dawn, hour: 6.5f);
            Step(WorldWeather.Clear, "TV-16", dt: 2f, phase: WorldDayPhase.Dawn, hour: 6.5f);
            Step(WorldWeather.LightRain, "TV-16", dt: 2f, phase: WorldDayPhase.Dawn, hour: 6.5f);
            Check(player.ActiveLoops(AudioLayer.Morning) == 0 && WetGlobal(), "Forest Morning fades out in light rain");
            Step(WorldWeather.HeavyRain, "TV-16", phase: WorldDayPhase.Dawn, hour: 6.5f);
            Check(player.ActiveLoops(AudioLayer.Morning) == 0 && WetGlobal(), "Forest storm at dawn has only wet overlays");
            Step(WorldWeather.Clear, "TV-21", dt: 2f, phase: WorldDayPhase.Dawn, hour: 6.5f);
            Check(player.ActiveLoops(AudioLayer.Morning) == 0 && player.ActiveLoops(AudioLayer.Primeval) == 3,
                "Huyền Ảnh Cốc dry dawn uses Primeval without Morning");
            Step(WorldWeather.HeavyRain, "TV-21", dt: 2f, phase: WorldDayPhase.Dawn, hour: 6.5f);
            Check(player.ActiveLoops(AudioLayer.Morning) == 0 && player.ActiveLoops(AudioLayer.Primeval) == 0 && WetGlobal(),
                "Huyền Ảnh Cốc storm at dawn keeps global Rain");
        }
        finally { UnityEngine.Object.Destroy(host); }
    }
#endif
}
