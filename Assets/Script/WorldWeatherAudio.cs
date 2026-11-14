using System;
using TuTienCore;
using UnityEngine;

internal sealed class WorldWeatherAudio
{
    private EnvironmentAudioPlayer familyPlayer;
    private bool resolved;

    internal void Update(GameObject host, WorldWeatherAudioSettings settings, WorldClimateFrame frame,
        TerrainType terrain, string map, bool active, bool safeVillage, int mapNumber, bool running)
    {
        if (!active || !settings.useMapLayers)
        {
            Stop();
            return;
        }
        if (!resolved)
        {
            familyPlayer = EnvironmentAudioPlayer.Load();
            resolved = true;
            if (familyPlayer == null) Debug.LogWarning("Environment audio manifest is unavailable. Ambience is disabled.");
        }
        float environment = AudioVolumeSettings.Get(GameAudioChannel.Environment);
        bool village = safeVillage || (settings.villageMapIds != null && Array.IndexOf(settings.villageMapIds, map) >= 0);
        if (familyPlayer == null) return;
        familyPlayer.Update(host, true, running, terrain, map, mapNumber, village, frame,
            Mathf.Clamp01(settings.environmentVolume) * environment, Mathf.Clamp01(settings.weatherVolume) * environment,
            Time.unscaledDeltaTime, settings
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            , familyPlayer.PreviewWeather, familyPlayer.PreviewNight
#endif
        );
    }

    internal void Stop() => familyPlayer?.Stop();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    internal EnvironmentAudioPlayer DebugPlayer => familyPlayer;
#endif
}
