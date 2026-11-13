using System;
using System.Collections.Generic;
using TuTienCore;
using UnityEngine;

// Reuses sources for map ambience and weather on the environmental volume channel.
internal sealed class WorldWeatherAudio
{
    private sealed class Loop
    {
        private readonly AudioSource[] channels = new AudioSource[2];
        private int current = -1;

        internal void Update(GameObject host, AudioClip clip, float gain, float step)
        {
            int next = -1;
            if (clip != null && gain > 0f)
            {
                for (int i = 0; i < channels.Length; i++)
                    if (channels[i] != null && channels[i].clip == clip) next = i;
                if (next < 0)
                {
                    next = 0;
                    if (current >= 0) next = 1 - current;
                    else if (channels[0] != null && (channels[1] == null || channels[0].volume > channels[1].volume))
                        next = 1;
                    AudioSource source = channels[next] ??= Create(host, true);
                    source.Stop();
                    source.volume = 0f;
                    source.clip = clip;
                    source.Play();
                }
            }
            current = next;
            for (int i = 0; i < channels.Length; i++)
            {
                AudioSource source = channels[i];
                if (source == null) continue;
                source.volume = Mathf.MoveTowards(source.volume, i == current ? gain : 0f, step);
                if (i != current && source.volume == 0f && source.clip != null)
                {
                    source.Stop();
                    source.clip = null;
                }
            }
        }

        internal void Stop()
        {
            current = -1;
            foreach (var source in channels)
            {
                if (source == null) continue;
                source.Stop();
                source.clip = null;
                source.volume = 0f;
            }
        }
    }

    private readonly Loop biome = new Loop(), wildlife = new Loop(), rain = new Loop(), wind = new Loop();
    private readonly List<Loop> mapLoops = new List<Loop>();
    private Loop[] weatherLoops = Array.Empty<Loop>();
    private WorldEnvironmentAudioCatalog catalog;
    private EnvironmentAudioPlayer familyPlayer;
    private WorldEnvironmentAudioCatalog.Profile mapProfile;
    private GameObject layerHost;
    private bool resolved;
    internal string CurrentMapProfile => mapProfile?.id;
    private AudioSource oneShots;
    private float nextSplashAt;
    internal int ThunderPlayed { get; private set; }

    internal void Update(GameObject host, WorldWeatherAudioSettings settings, WorldWeatherVisual visual,
        WorldClimateFrame frame, TerrainType terrain, string id, bool active, bool outdoors, float splash, bool safeVillage = false,
        int mapNumber = 0, bool running = true)
    {
        float environment = AudioVolumeSettings.Get(GameAudioChannel.Environment);
        float envGain = Mathf.Clamp01(settings.environmentVolume) * environment;
        float weatherGain = Mathf.Clamp01(settings.weatherVolume) * environment;
        float step = Time.unscaledDeltaTime / Mathf.Max(.1f, settings.fadeSeconds);
        bool night = frame.Phase == WorldDayPhase.Night;
        bool village = !string.IsNullOrEmpty(id) && settings.villageMapIds != null && Array.IndexOf(settings.villageMapIds, id) >= 0;
        float rainGain = outdoors && active ? frame.Rain : 0f;
        float windGain = outdoors && active ? Mathf.Clamp01(Mathf.Abs(visual.Wind) * .24f) : 0f;
        AudioClip wildlifeClip = night && settings.nightAmbience != null ? settings.nightAmbience : settings.birdsInsects;
        if (settings.useMapLayers && !resolved)
        {
            catalog = WorldEnvironmentAudioCatalog.Load();
            familyPlayer = EnvironmentAudioPlayer.Load();
            if (catalog != null)
            {
                weatherLoops = new Loop[catalog.Weather.Length];
                for (int i = 0; i < weatherLoops.Length; i++) weatherLoops[i] = new Loop();
            }
            resolved = true;
        }
        bool useFamilies = settings.useMapLayers && familyPlayer != null;
        bool layered = settings.useMapLayers && (catalog != null || useFamilies);
        var profile = catalog?.Select(terrain, id, safeVillage || village);
        familyPlayer?.Update(host, useFamilies && active, running, terrain, id, mapNumber, profile?.id, frame,
            envGain, weatherGain, Time.unscaledDeltaTime, settings.fadeSeconds, settings.coastWeatherLoopLimit, settings.thunderInterval);
        UpdateLayers(host, !useFamilies && layered && active, terrain, id, safeVillage || village, outdoors, frame, visual.Wind,
            envGain, weatherGain, step);
        if (useFamilies && active) mapProfile = profile;
        biome.Update(host, !layered && active ? Ambience(settings, terrain, safeVillage || village) : null, envGain, step);
        wildlife.Update(host, !layered && active && outdoors ? wildlifeClip : null, envGain, step);
        rain.Update(host, layered ? null : rainGain > .55f ? settings.heavyRainLoop : settings.lightRainLoop, rainGain * weatherGain, step);
        wind.Update(host, layered ? null : settings.windLoop, windGain * weatherGain, step);
        if (oneShots != null) oneShots.volume = weatherGain;
        if (!active || !outdoors || weatherGain <= 0f) return;
        if (visual.Thunder)
        {
            AudioClip clip = visual.CloseThunder ? settings.closeThunder ?? settings.distantThunder :
                settings.distantThunder ?? settings.closeThunder;
            if (Play(host, clip, weatherGain, 1f)) ThunderPlayed++;
        }
        if (visual.Gust) Play(host, settings.strongWindGust, weatherGain, Mathf.Max(.35f, windGain));
        if (splash > 0f && settings.rainSplash != null && Time.unscaledTime >= nextSplashAt)
        {
            nextSplashAt = Time.unscaledTime + .28f;
            Play(host, settings.rainSplash, weatherGain, Mathf.Clamp01(splash * .45f));
        }
    }

    private void UpdateLayers(GameObject host, bool active, TerrainType terrain, string id, bool village,
        bool outdoors, WorldClimateFrame frame, float windSpeed, float envGain, float weatherGain, float step)
    {
        mapProfile = active ? catalog.Select(terrain, id, village) : null;
        var clips = mapProfile != null ? catalog.PrefixClips(mapProfile) : Array.Empty<AudioClip>();
        if (mapProfile != null && layerHost == null)
        {
            layerHost = new GameObject("Map ambience layers");
            layerHost.transform.SetParent(host.transform, false);
        }
        while (mapLoops.Count < clips.Length) mapLoops.Add(new Loop());
        float gain = WorldEnvironmentAudioCatalog.PrefixClipGain * envGain;
        for (int i = 0; i < mapLoops.Count; i++)
            mapLoops[i].Update(layerHost, i < clips.Length ? clips[i] : null, gain, step);

        if (catalog == null) return;
        for (int i = 0; i < catalog.Weather.Length; i++)
        {
            var layer = catalog.Weather[i];
            float volume = active && outdoors ? WorldEnvironmentAudioCatalog.LayerGain(layer, frame, windSpeed) * weatherGain : 0f;
            weatherLoops[i].Update(layerHost, volume > 0f ? catalog.Clip(layer.clip) : null, volume, step);
        }
    }

    private static AudioClip Ambience(WorldWeatherAudioSettings settings, TerrainType terrain, bool village)
    {
        if (village) return settings.villageAmbience;
        switch (terrain)
        {
            case TerrainType.RungRam: return settings.forestAmbience;
            case TerrainType.CaoNguyen:
            case TerrainType.DoiNui: return settings.mountainWind;
            case TerrainType.DamLay:
            case TerrainType.HoNuoc:
            case TerrainType.Bien: return settings.waterStream;
            default: return null;
        }
    }

    private bool Play(GameObject host, AudioClip clip, float gain, float strength)
    {
        if (clip == null) return false;
        if (oneShots == null) oneShots = Create(host, false);
        oneShots.volume = gain;
        oneShots.PlayOneShot(clip, strength);
        return true;
    }

    private static AudioSource Create(GameObject host, bool looping)
    {
        var source = host.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 0f;
        source.loop = looping;
        return source;
    }

    internal void Stop()
    {
        familyPlayer?.Stop();
        foreach (var loop in mapLoops) loop.Stop();
        foreach (var loop in weatherLoops) loop.Stop();
        biome.Stop();
        wildlife.Stop();
        rain.Stop();
        wind.Stop();
        if (oneShots != null) oneShots.Stop();
    }
}
