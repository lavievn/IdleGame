#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TuTienCore;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static Development016Smoke;

internal static class WorldClimateSmoke
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Read<T>(object owner, string key) => (T)owner.GetType().GetField(key, Fields).GetValue(owner);
    private static void Set(object owner, string key, object value) => owner.GetType().GetField(key, Fields).SetValue(owner, value);
    private static bool Near(double a, double b, double tolerance = 1e-7) => Math.Abs(a - b) <= tolerance;
    private static WorldClimateFrame ReadFrame(EntityDataSO data) =>
        WorldClimate.Sample(data.worldClimate, 780, data.mapTerrain, data.worldMapId, data.mapNumber, false);

    private static bool SameState(WorldClimateState a, WorldClimateState b) =>
        a.version == b.version && a.seed == b.seed && a.day == b.day && Near(a.dayFraction, b.dayFraction);

    private static void ClockAndClimate(Action<string> pass)
    {
        var state = WorldClimateState.AtDawn(391);
        Check(WorldClimate.DefaultDaySeconds == 780 && WorldClimate.Sample(state, 780, TerrainType.RungRam,
            "forest", 0, false).Phase == WorldDayPhase.Dawn, "New clock starts at dawn");
        Check(!WorldClimate.Advance(ref state, 0, 780), "Zero elapsed");
        WorldClimate.Advance(ref state, 780, 780);
        Check(state.day == 1 && Near(state.dayFraction, 5.0 / 24), "Exactly 13 minutes per day");
        WorldClimate.Advance(ref state, 780, 780);
        Check(state.day == 2 && Near(state.dayFraction, 5.0 / 24), "Two exact cycles");
        WorldClimate.Advance(ref state, 32.5f, 780);
        Check(Near(state.dayFraction, 6.0 / 24), "32.5 seconds per game hour");
        WorldClimate.Advance(ref state, 60, 60);
        Check(state.day == 3 && Near(state.dayFraction, 6.0 / 24), "Day length setting");
        WorldClimateFrame At(double hour)
        {
            var at = state;
            at.dayFraction = hour / 24;
            return WorldClimate.Sample(at, 780, TerrainType.RungRam, "forest", 0, false, WorldWeather.Clear);
        }
        var hours = new[] { 5.0, 8.0, 17.0, 20.0 };
        for (int i = 0; i < hours.Length; ++i)
        {
            var before = At(hours[i] - .002);
            var after = At(hours[i] + .002);
            Check(after.Phase == (WorldDayPhase)i &&
                Near(before.SkyTint.r, after.SkyTint.r, .0001), "Continuous phase boundary " + hours[i]);
        }
        Check(At(2).SkyTint.b > At(2).SkyTint.r && At(6.5).SkyTint.r > At(6.5).SkyTint.b &&
            At(12).LandTint == Color.white, "Distinct night, dawn and midday colors");
        pass("Clock 0, 13 and 26 simulated minutes, custom day length, 32.5 seconds/hour and smooth phase boundaries.");

        var outcomes = new HashSet<WorldWeather>();
        bool foundDifferentMap = false, foundBlend = false;
        for (int day = 0; day < 100; ++day)
        {
            state.day = day;
            state.dayFraction = 4.0 / 24 + 9.0 / 780;
            var forest = WorldClimate.Sample(state, 780, TerrainType.RungRam, "forest", 0, false);
            outcomes.Add(forest.Weather);
            Check(WorldClimate.Sample(state, 780, TerrainType.RungRam, "forest", 0, false).Weather == forest.Weather,
                "Deterministic same map/day/slot");
            var swamp = WorldClimate.Sample(state, 780, TerrainType.DamLay, "swamp", 0, false);
            foundDifferentMap |= swamp.Weather != forest.Weather;
            Check(WorldClimate.Sample(state, 780, TerrainType.DoiNui, "TL-01", 0, false).Rain == 0,
                "No rain in cold biome");
            Check(WorldClimate.Sample(state, 780, TerrainType.DamLay, "cave", 0, true, WorldWeather.HeavyRain).Rain == 0,
                "Indoor suppresses rain, including override");
            state.dayFraction = 4.0 / 24 - .00001;
            var previous = WorldClimate.Sample(state, 780, TerrainType.RungRam, "forest", 0, false);
            if (Near(previous.Rain, forest.Rain)) continue;
            state.dayFraction = 4.0 / 24 + 4.0 / 780;
            var middle = WorldClimate.Sample(state, 780, TerrainType.RungRam, "forest", 0, false);
            Check(Near(middle.Rain, (previous.Rain + forest.Rain) * .5, .0001), "Smooth half-way rain intensity");
            foundBlend = true;
        }
        Check(outcomes.Count == 5 && foundDifferentMap && foundBlend, "All five weather states and stable biome variation");
        pass("Deterministic biome weather, all five outcomes, 8s rain blend and dry indoor/cold profiles.");
    }

    private static IEnumerator SaveTravelRetry(GameManager game, SaveManager saves, UIManager ui, Action<string> pass)
    {
        var data = game.HeroData;
        var original = data.worldClimate;
        yield return new WaitForSecondsRealtime(.25f);
        Check(SameState(original, data.worldClimate), "Paused frames do not advance the clock");
        if (Application.isFocused || Application.isBatchMode)
        {
            ui.SetPaused(false);
            Time.timeScale = .4f;
            double start = Time.realtimeSinceStartupAsDouble;
            yield return new WaitForSecondsRealtime(.65f);
            double elapsed = Time.realtimeSinceStartupAsDouble - start;
            ui.SetPaused(true);
            double clockElapsed = (data.worldClimate.dayFraction - original.dayFraction) * 780;
            Check(clockElapsed >= elapsed - .16 && clockElapsed <= elapsed + .16, "Active runtime uses unscaled seconds");
            pass("In-scene active clock uses unscaled elapsed time and pauses.");
        }
        else pass("Pause confirmed. Live unscaled timing assertion skipped because the Windows window was unfocused.");

        data.worldClimate.day = 2;
        data.worldClimate.dayFraction = 7.999 / 24;
        var checkpoint = data.worldClimate;
        var forecast = ReadFrame(data);
        Check(game.TryManualSave(SaveSlot.ManualSave1), "Save just before phase/slot boundary");
        string json = saves.LoadGame(SaveSlot.ManualSave1.ToString());
        data.worldClimate.day = 8;
        Check(game.LoadAndContinue(SaveSlot.ManualSave1), "Load into gameplay");
        data = game.HeroData;
        var restored = ReadFrame(data);
        Check(SameState(checkpoint, data.worldClimate) && restored.Weather == forecast.Weather &&
            Near(restored.Rain, forecast.Rain) && restored.SkyTint == forecast.SkyTint, "Clock and in-flight blend restored");
        yield return Ready(game, ui);

        string legacy = Regex.Replace(json, "\"worldClimate\"\\s*:\\s*\\{[^}]*\\}\\s*,", "");
        Check(legacy != json && !legacy.Contains("\"worldClimate\""), "Historical fixture omits the climate field");
        string slot2 = Path.Combine(saves.SaveDirectory, "ManualSave2.json");
        File.WriteAllText(slot2, legacy);
        var candidate = ScriptableObject.CreateInstance<EntityDataSO>();
        candidate.worldClimate = WorldClimateState.AtDawn(955);
        Check(saves.LoadGame(candidate, SaveSlot.ManualSave2) &&
            SameState(candidate.worldClimate, WorldClimateState.AtDawn(780)), "Legacy save defaults independent of template");
        var marker = new Regex("(\"worldClimate\"\\s*:\\s*\\{\\s*\"version\"\\s*:\\s*)1");
        string future = marker.Replace(json, "${1}99", 1);
        Check(future != json, "Future fixture version replaced");
        File.WriteAllText(slot2, future);
        Check(!saves.LoadGame(candidate, SaveSlot.ManualSave2) &&
            SameState(candidate.worldClimate, WorldClimateState.AtDawn(780)) &&
            !saves.SaveGame(data, SaveSlot.ManualSave2) && File.ReadAllText(slot2) == future,
            "Future component fails before mutation and cannot be overwritten");
        UnityEngine.Object.Destroy(candidate);
        pass("Real SaveManager round trip at 07:59, deterministic in-flight blend, legacy default and future-version protection.");

        Check(WorldMapCatalog.TryGet(data.worldMapId, out var node), "Current map catalog");
        data.worldMapCleared.Add(node.Id); // Owned fixture unlocks ordinary adjacent travel.
        string destination = node.Links.First(id => WorldMapCatalog.TravelBlockReason(data, id) == null);
        checkpoint = data.worldClimate;
        Check(game.TryTravelToWorldMap(destination) && data.worldMapId == destination &&
            SameState(checkpoint, data.worldClimate), "Travel does not reset world clock");
        yield return Ready(game, ui);
        checkpoint = data.worldClimate;
        forecast = ReadFrame(data);
        game.OnHeroDied();
        game.OnRetryClicked();
        Check(SameState(checkpoint, data.worldClimate) && ReadFrame(data).Weather == forecast.Weather,
            "Retry keeps day, seed and weather");
        yield return Ready(game, ui);
        pass("Manual map travel and Retry keep world time and same-map weather.");
    }

    internal static IEnumerator Run(GameManager game, SaveManager saves, UIManager ui, EnvironmentManager space,
        Func<int, int, IEnumerator> resize, Func<string, IEnumerator> capture, Action<string> pass)
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        ClockAndClimate(pass);
        game.OnNewGameClicked();
        game.OnConfirmResetClicked();
        yield return Ready(game, ui);
        yield return SaveTravelRetry(game, saves, ui, pass);

        var ground = Read<GroundPresentation>(space, "groundPresentation");
        var canvas = Read<Canvas>(space, "battleCanvas");
        var camera = Camera.main;
        var cameraData = camera != null ? camera.GetComponent<UniversalAdditionalCameraData>() : null;
        string pipeline = GraphicsSettings.currentRenderPipeline?.GetType().Name ?? "Built-in";
        string renderer = cameraData?.scriptableRenderer.GetType().Name ?? "No URP camera renderer";
        Check(ground != null && canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay,
            "Stable Battle Canvas is overlay");
        pass("Renderer " + pipeline + " / " + renderer + ", " + SystemInfo.graphicsDeviceType + " / " +
            SystemInfo.graphicsDeviceName + "; ScreenSpaceOverlay Battle Canvas.");

        var skyTiles = Read<List<RectTransform>>(ground, "skyTiles");
        var mountains = Read<List<RectTransform>>(ground, "distantTiles");
        var trees = Read<List<RectTransform>>(ground, "farTiles");
        var terrain = Read<List<RectTransform>>(ground, "tiles");
        Check(skyTiles.Count == 3 && mountains.Count == 3 && trees.Count == 3 && terrain.Count == 3, "All art layers installed");
        Set(game, "previewWorldClimate", true);
        Set(game, "previewWorldWeather", WorldWeather.Clear);
        yield return resize(800, 450);
        foreach (int hour in new[] { 6, 12, 19, 22 })
        {
            Set(game, "previewWorldHour", (float)hour);
            yield return null;
            Check(skyTiles.All(sky => sky.gameObject.activeInHierarchy && sky.GetComponent<Image>().sprite != null),
                "Legacy PNG fallback at " + hour);
            Check(terrain[0].GetComponent<Image>().color == Read<Color>(ground, "worldLandTint"), "Land tint applied");
            yield return capture("climate-legacy-" + hour.ToString("00"));
        }
        Set(game, "worldClimateLighting", false);
        yield return null;
        Check(skyTiles.All(t => t.GetComponent<Image>().color == Color.white), "Lighting can be disabled");
        Set(game, "worldClimateLighting", true);
        Set(game, "previewWorldHour", 12f);
        Set(game, "previewWorldWeather", WorldWeather.HeavyRain);
        var adapter = game.gameObject.AddComponent<WorldClimateFixtureAdapter>();
        Set(game, "worldClimateAssets", adapter);
        float startingPhase = ground.DistantPhase;
        space.PanEnvironment(37);
        Check(!Near(startingPhase, ground.DistantPhase), "Existing parallax pan still moves mountains");

        foreach (var size in new[] { new Vector2Int(600, 338), new Vector2Int(800, 450), new Vector2Int(1150, 647) })
        {
            yield return resize(size.x, size.y);
            var projected = Read<RawImage>(ground, "projectedSky");
            Check(projected != null && projected.isActiveAndEnabled && !projected.raycastTarget &&
                projected.transform.GetSiblingIndex() < mountains[0].GetSiblingIndex() &&
                skyTiles.All(sky => !sky.gameObject.activeSelf) && trees[0].gameObject.activeInHierarchy &&
                terrain[0].gameObject.activeInHierarchy, "Exactly one sky behind the active foreground");
            Check(Near(projected.rectTransform.rect.width, space.BattleArea.rect.width, .01) &&
                Near(projected.rectTransform.rect.height, adapter.LastViewport.height, .01) &&
                Near(adapter.Intensity, 1) && adapter.LastParent == space.BattleArea, "Projection and rain viewport follow resize");
        }
        adapter.Available = false;
        Set(game, "previewWorldWeather", WorldWeather.Clear);
        yield return null;
        Check(skyTiles.All(sky => sky.gameObject.activeSelf) && !Read<RawImage>(ground, "projectedSky").isActiveAndEnabled &&
            adapter.Stops == 1, "Missing sky output switches back and clear stops the rain adapter");
        Set(game, "worldClimateAssets", null);
        Set(game, "previewWorldClimate", false);
        UnityEngine.Object.Destroy(adapter);
        yield return capture("climate-fallback-1150");
        pass("Legacy fallback and tint, projection sorting and viewport, parallax pan, adapter stop at clear, 600/800/1150 (synthetic adapter contract only).");
    }

    internal static IEnumerator Soak(GameManager game, UIManager ui, CombatManager combat,
        Func<int, int, IEnumerator> resize, Func<string, IEnumerator> capture, Action<string> pass)
    {
        yield return resize(800, 450);
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;
        game.OnNewGameClicked();
        game.OnConfirmResetClicked();
        yield return Ready(game, ui);
        Clear(game, combat); // No enemy can end the owned exploration soak.
        var data = game.HeroData;
        var initial = data.worldClimate;
        ui.SetPaused(false);
        double started = Time.realtimeSinceStartupAsDouble;
        double nextSample = 300;
        long frames = 0;
        double maxMs = 0;
        while (Time.realtimeSinceStartupAsDouble - started < 1805)
        {
            yield return null;
            frames++;
            maxMs = Math.Max(maxMs, Time.unscaledDeltaTime * 1000);
            double elapsed = Time.realtimeSinceStartupAsDouble - started;
            if (elapsed < nextSample) continue;
            pass($"Soak {elapsed:F0}s, day {data.worldClimate.day}, phase {ReadFrame(data).Phase}, " +
                $"managed {GC.GetTotalMemory(false) / 1048576.0:F1} MiB, {frames / elapsed:F1} frames/s, max {maxMs:F1} ms.");
            nextSample += 300;
        }
        double runtime = Time.realtimeSinceStartupAsDouble - started;
        double clock = (data.worldClimate.day - initial.day + data.worldClimate.dayFraction - initial.dayFraction) * 780;
        Check(runtime >= 1805 && Math.Abs(clock - runtime) < 1 && data.worldClimate.day >= 2, "Active real-time soak");
        yield return capture("climate-soak-30min");
        pass($"Active fallback exploration soak {runtime:F1}s, clock {clock:F1}s, {frames} frames.");
    }

    internal static IEnumerator Reopen(GameManager game, SaveManager saves, UIManager ui, Action<string> pass)
    {
        string json = saves.LoadGame(SaveSlot.ManualSave1.ToString());
        Check(WorldClimateState.TryRead(json, out var checkpoint) &&
            checkpoint.day == 2 && Near(checkpoint.dayFraction, 7.999 / 24), "Owned checkpoint for restart");
        Check(game.LoadAndContinue(SaveSlot.ManualSave1) && SameState(checkpoint, game.HeroData.worldClimate),
            "Fresh Windows process loaded the same clock and seed");
        var frame = ReadFrame(game.HeroData);
        Check(frame.Phase == WorldDayPhase.Dawn, "Reload preserves side of the phase boundary");
        yield return Ready(game, ui);
        pass("Fresh Windows Player process/scene restored the owned 07:59 checkpoint with no offline elapsed time.");
    }
}

// Synthetic test double for display ordering and the adapter lifecycle. It
// never renders or claims Rain Maker or BG Studio output.
internal sealed class WorldClimateFixtureAdapter : WorldClimateAssetAdapter
{
    public bool Available = true;
    public Rect LastViewport;
    public RectTransform LastParent;
    public float Intensity;
    public int Stops;

    public override bool TryGetProjectedSky(WorldClimateFrame frame, Rect viewport, out Texture texture)
    {
        texture = Available ? Texture2D.whiteTexture : null;
        return Available;
    }

    public override void SetRain2D(float intensity, RectTransform battleArea, Rect viewport)
    {
        LastViewport = viewport;
        LastParent = battleArea;
        Intensity = intensity;
    }

    public override void StopRain2D()
    {
        Intensity = 0;
        Stops++;
    }
}
#endif
