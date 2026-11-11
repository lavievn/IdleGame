using System;
using TuTienCore;
using UnityEngine;

// Uses the same master SFX setting as combat. Only the fixed, reusable
// sources for configured clips are created.
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
    private readonly Loop[] layers = new Loop[WorldEnvironmentAudioCatalog.SlotCount];
    private readonly WorldEnvironmentAudioCatalog.Layer[] desired = new WorldEnvironmentAudioCatalog.Layer[WorldEnvironmentAudioCatalog.SlotCount];
    private WorldEnvironmentAudioCatalog catalog;
    private WorldEnvironmentAudioCatalog.Profile mapProfile;
    private GameObject layerHost;
    private bool resolved;
    internal string CurrentMapProfile => mapProfile?.id;
    private AudioSource oneShots;
    private float nextSplashAt;
    internal int ThunderPlayed { get; private set; }

    internal void Update(GameObject host, WorldWeatherAudioSettings settings, WorldWeatherVisual visual,
        WorldClimateFrame frame, TerrainType terrain, string id, bool active, bool outdoors, float splash, bool safeVillage = false)
    {
        float sfx = AudioVolumeSettings.Get(GameAudioChannel.Sfx);
        float envGain = Mathf.Clamp01(settings.environmentVolume) * sfx;
        float weatherGain = Mathf.Clamp01(settings.weatherVolume) * sfx;
        float step = Time.unscaledDeltaTime / Mathf.Max(.1f, settings.fadeSeconds);
        bool night = frame.Phase == WorldDayPhase.Night;
        bool village = !string.IsNullOrEmpty(id) && settings.villageMapIds != null && Array.IndexOf(settings.villageMapIds, id) >= 0;
        float rainGain = outdoors && active ? frame.Rain : 0f;
        float windGain = outdoors && active ? Mathf.Clamp01(Mathf.Abs(visual.Wind) * .24f) : 0f;
        AudioClip wildlifeClip = night && settings.nightAmbience != null ? settings.nightAmbience : settings.birdsInsects;
        if (settings.useMapLayers && !resolved)
        {
            catalog = WorldEnvironmentAudioCatalog.Load();
            resolved = true;
        }
        bool layered = settings.useMapLayers && catalog != null;
        UpdateLayers(host, layered && active, terrain, id, safeVillage || village, outdoors, frame, visual.Wind,
            envGain, weatherGain, step);
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
        Array.Clear(desired, 0, desired.Length);
        if (mapProfile != null)
        {
            if (layerHost == null)
            {
                layerHost = new GameObject("Map ambience layers");
                layerHost.transform.SetParent(host.transform, false);
            }
            foreach (var layer in mapProfile.layers) desired[layer.slot] = layer;
            if (outdoors)
                foreach (var layer in catalog.Weather) desired[layer.slot] = layer;
        }
        for (int i = 0; i < layers.Length; i++)
        {
            var layer = desired[i];
            float volume = layer != null ? WorldEnvironmentAudioCatalog.LayerGain(layer, frame, windSpeed)
                * (i < 7 ? envGain : weatherGain) : 0f;
            if (volume > 0f)
            {
                layers[i] ??= new Loop();
                layers[i].Update(layerHost, catalog.Clip(layer.clip), volume, step);
            }
            else layers[i]?.Update(layerHost, null, 0f, step);
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
        foreach (var layer in layers) layer?.Stop();
        biome.Stop();
        wildlife.Stop();
        rain.Stop();
        wind.Stop();
        if (oneShots != null) oneShots.Stop();
    }
}
