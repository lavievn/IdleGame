using System;
using UnityEngine;

[Serializable]
public sealed class WorldWeatherPresentationSettings
{
    [Range(0f, 1.5f)] public float rainIntensity = 1f;
    [Range(.3f, 2f)] public float rainSpeed = 1f;
    internal const float DefaultDropSize = .60f;
    [Range(.1f, 2f)] public float dropSize = DefaultDropSize;
    [Tooltip("Fixed direction, or a left/right preference when Random Rain Wind is enabled.")]
    [Range(-1f, 1f)] public float windDirection = -.65f;
    [Range(0f, 8f)] public float windStrength = 5f;
    [Range(0f, 5f)] public float gustStrength = 2.5f;
    [Range(0f, 2f)] public float splashIntensity = 1f;
    [Range(0f, .5f)] public float stormHaze = .18f;
    public bool rainWind = true;
    public bool randomRainWind = true;
    public bool groundSplash = true;
    public bool waterRipples = true;
    public bool distantHaze = true;
    public bool lightning = true;
    public bool reducedFlashes;
    [Range(0f, .24f)] public float lightningBrightness = .15f;
    [Range(22f, 120f)] public float lightningInterval = 42f;
    [Tooltip("Optional authored surface strips. X is local to one repeating Ground tile. Y is BattleArea-local.")]
    public WorldWeatherSurface[] surfaces = Array.Empty<WorldWeatherSurface>();
}

[Serializable]
public struct WorldWeatherSurface
{
    public string mapId;
    public Vector2 horizontalRange;
    public float height;
    public bool water;
}

[Serializable]
public sealed class WorldWeatherAudioSettings
{
    public bool useMapLayers = true;
    [Range(0f, 1f)] public float environmentVolume = 1f;
    [Range(0f, 1f)] public float weatherVolume = .5f;
    [Range(.1f, 8f)] public float fadeSeconds = 2f;
    [Min(0)] public int coastWeatherLoopLimit = 2;
    [Min(1f)] public float thunderInterval = 35f;
    [Range(0, 3)] public int morningLoopLimit = 1;
    [Min(1f)] public float morningRotationSeconds = 120f;
    [Range(.1f, 8f)] public float morningFadeSeconds = 2f;
    [Range(.5f, 1f)] public float stormAudioThreshold = .85f;
    [Header("RC3.1 TEST_PROFILE")]
    public EnvironmentRainMix lightRainAudio = new EnvironmentRainMix {
        rainGain = .45f, windGain = .18f, pebbleGain = .45f, thunderGain = .2f,
        coastGain = .65f, pebbleVoices = 1, thunderVoices = 0, thunderSeconds = 100f };
    public EnvironmentRainMix heavyRainAudio = new EnvironmentRainMix {
        rainGain = .8f, windGain = .65f, pebbleGain = .75f, thunderGain = .45f,
        coastGain = .85f, thunderSeconds = 65f };
    public EnvironmentRainMix stormAudio = new EnvironmentRainMix {
        rainGain = 1f, windGain = 1f, pebbleGain = 1f, thunderGain = .9f,
        coastGain = 1f, thunderSeconds = 35f };
    public string[] villageMapIds = Array.Empty<string>();
}
