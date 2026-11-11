using System;
using UnityEngine;

[Serializable]
public sealed class WorldWeatherPresentationSettings
{
    [Range(0f, 1.5f)] public float rainIntensity = 1f;
    [Range(.3f, 2f)] public float rainSpeed = 1f;
    [Range(.3f, 2f)] public float dropSize = 1f;
    [Range(-1f, 1f)] public float windDirection = -.65f;
    [Range(0f, 8f)] public float windStrength = 3.5f;
    [Range(0f, 5f)] public float gustStrength = 2.5f;
    [Range(0f, 2f)] public float splashIntensity = 1f;
    [Range(0f, .5f)] public float stormHaze = .18f;
    public bool rainWind = true;
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
    [Range(0f, 1f)] public float environmentVolume = .6f;
    [Range(0f, 1f)] public float weatherVolume = .5f;
    [Range(.1f, 8f)] public float fadeSeconds = 2f;
    public string[] villageMapIds = Array.Empty<string>();
    public AudioClip villageAmbience, forestAmbience, mountainWind, waterStream, birdsInsects, nightAmbience;
    public AudioClip lightRainLoop, heavyRainLoop, windLoop, strongWindGust, rainSplash, distantThunder, closeThunder;
}
