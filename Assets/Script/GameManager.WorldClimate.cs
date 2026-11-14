using System;
using UnityEngine;

public partial class GameManager
{
    [Header("Dynamic environment experiment")]
    [SerializeField, Min(1f)] private float worldDaySeconds = WorldClimate.DefaultDaySeconds;
    [SerializeField] private bool worldClimateLighting = true;
    [SerializeField] private bool worldClimateHdrSky = true;
    [SerializeField] private bool worldClimateRain2D = true;
    [SerializeField] private WorldClimateAssetAdapter worldClimateAssets;
    [SerializeField] private string[] worldClimateIndoorMapIds = Array.Empty<string>();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [SerializeField] private bool previewWorldClimate;
    [SerializeField, Range(0f, 23.999f)] private float previewWorldHour = 12f;
    [SerializeField] private WorldWeather previewWorldWeather;
    private static readonly float[] ClimateTestHours = { 6.5f, 12f, 18.5f, 22f };
    internal int ClimateTestHour { get; private set; }
    internal int ClimateTestWeather { get; private set; }
    internal EnvironmentAudioPlayer AudioDebugPlayer => weatherAudio.DebugPlayer;
    internal void CycleClimateTestHour() => ClimateTestHour = (ClimateTestHour + 1) % (ClimateTestHours.Length + 1);
    internal void CycleClimateTestWeather() => ClimateTestWeather = (ClimateTestWeather + 1) % ((int)WorldWeather.HeavyRain + 2);
#endif

    [Header("Rain, wind and storm visuals")]
    [SerializeField] private WorldWeatherPresentationSettings worldWeatherPresentation = new WorldWeatherPresentationSettings();
    [Header("Environmental SFX")]
    [SerializeField] private WorldWeatherAudioSettings worldWeatherAudioSettings = new WorldWeatherAudioSettings();
    private readonly WorldWeatherAudio weatherAudio = new WorldWeatherAudio();
    private readonly WorldWeatherStorm weatherStorm = new WorldWeatherStorm();
    private EntityDataSO weatherOwner;
    private bool skipClimateFrame;
    private bool vendorAssetsResolved;

    private void OnApplicationFocus(bool focused)
    {
        // The first delta after focus returns can include time between frames
        // spent outside the application, especially with runInBackground off.
        skipClimateFrame = true;
    }

    private void OnApplicationPause(bool paused) => skipClimateFrame = true;

    private void TickWorldClimate()
    {
        bool skipFrame = skipClimateFrame;
        skipClimateFrame = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (preGameUI != null && preGameUI.activeInHierarchy)
        {
            ClimateTestHour = 0;
            ClimateTestWeather = 0;
        }
#endif
        if (runtimeHeroData == null || !hasDeployed || HasTribulationLiability)
        {
            weatherStorm.Reset();
            if (!IsEnteringGameplay || HasTribulationLiability)
                weatherAudio.Stop();
            if (environmentManager != null) environmentManager.ClearWorldClimate();
            return;
        }

        bool running = IsGameplayHUDVisible && !HasPendingConfirmation &&
            Time.timeScale > 0f && (Application.isFocused || Application.isBatchMode);
        if (running && !skipFrame &&
            WorldClimate.Advance(ref runtimeHeroData.worldClimate, Time.unscaledDeltaTime, worldDaySeconds))
            runtimeHeroData.isDirty = true;

        if (environmentManager == null) return;
        if (!vendorAssetsResolved && (worldClimateHdrSky || worldClimateRain2D))
        {
            vendorAssetsResolved = true;
            if (worldClimateAssets == null) worldClimateAssets = WorldClimateVendorAdapter.TryInstall(gameObject);
        }
        var data = runtimeHeroData;
        var state = data.worldClimate;
        WorldWeather? weatherOverride = null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (previewWorldClimate)
        {
            state.dayFraction = Mathf.Clamp(previewWorldHour, 0f, 23.999f) / 24.0;
            if ((uint)previewWorldWeather <= (uint)WorldWeather.HeavyRain)
                weatherOverride = previewWorldWeather;
        }
        if (ClimateTestHour > 0)
            state.dayFraction = ClimateTestHours[ClimateTestHour - 1] / 24.0;
        if (ClimateTestWeather > 0)
            weatherOverride = (WorldWeather)(ClimateTestWeather - 1);
#endif
        bool indoor = !string.IsNullOrEmpty(data.worldMapId) &&
            Array.IndexOf(worldClimateIndoorMapIds, data.worldMapId) >= 0;
        var frame = WorldClimate.Sample(state, worldDaySeconds, data.mapTerrain,
            data.worldMapId, data.mapNumber, indoor, weatherOverride);
        if (weatherOwner != data)
        {
            weatherOwner = data;
            weatherStorm.Reset();
        }
        var visual = weatherStorm.Step(worldWeatherPresentation, frame, data.worldClimate, data.worldMapId,
            data.mapNumber, worldDaySeconds, running, !indoor);
        environmentManager.ApplyWorldClimate(frame, worldClimateLighting,
            worldClimateHdrSky && !indoor, worldClimateRain2D && !indoor, worldClimateAssets, visual);
        weatherAudio.Update(gameObject, worldWeatherAudioSettings, frame, data.mapTerrain, data.worldMapId,
            IsGameplayHUDVisible || IsEnteringGameplay, TuTienCore.StoryOrigin.IsSafe(data),
            data.mapNumber, running && !skipFrame);
    }
}
