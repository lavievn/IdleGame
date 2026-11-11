#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TuTienCore;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;
using static Development016Smoke;

internal static class WorldClimateVendorSmoke
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string WetFixtureMap = "TV-20"; // Lạc Vân Trạch
    private static T Read<T>(object owner, string key) => (T)owner.GetType().GetField(key, Fields).GetValue(owner);
    private static void Set(object owner, string key, object value) => owner.GetType().GetField(key, Fields).SetValue(owner, value);

    private static float Luminance(RenderTexture projection)
    {
        RenderTexture previous = RenderTexture.active;
        var sample = new Texture2D(4, 4, TextureFormat.RGB24, false);
        try
        {
            RenderTexture.active = projection;
            sample.ReadPixels(new Rect(projection.width / 2 - 2, projection.height * .7f, 4, 4), 0, 0);
            var color = sample.GetPixel(2, 2);
            return color.r * .2126f + color.g * .7152f + color.b * .0722f;
        }
        finally
        {
            RenderTexture.active = previous;
            UnityEngine.Object.Destroy(sample);
        }
    }

    internal static IEnumerator Run(GameManager game, UIManager ui, CombatManager combat,
        EnvironmentManager space, Func<int, int, IEnumerator> resize, Func<string, IEnumerator> capture,
        Action<string> pass, bool soak)
    {
        Check(Resources.Load<WorldClimateVendorAssets>(WorldClimateVendorAssets.ResourcePath) != null,
            "Real vendor resources must be included in this build");
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        yield return resize(800, 450);
        game.OnNewGameClicked();
        game.OnConfirmResetClicked();
        yield return Ready(game, ui);
        // Some random birth maps have no heavy rain in their biome table.
        // Keep this owned profile on a real wet map without changing the game catalog.
        game.HeroData.worldMapId = WetFixtureMap;
        WorldMapCatalog.Normalize(game.HeroData);
        Check(game.TryManualSave(SaveSlot.ManualSave1) && game.LoadAndContinue(SaveSlot.ManualSave1),
            "Load the owned wet map fixture");
        yield return Ready(game, ui);
        Check(game.HeroData.worldMapId == WetFixtureMap, "Wet map fixture is active");
        var adapter = Read<WorldClimateAssetAdapter>(game, "worldClimateAssets") as WorldClimateVendorAdapter;
        var ground = Read<GroundPresentation>(space, "groundPresentation");
        Check(adapter != null && ground != null, "MainGame resolved the licensed adapter");
        var skyTiles = Read<List<RectTransform>>(ground, "skyTiles");
        var mountains = Read<List<RectTransform>>(ground, "distantTiles");
        var trees = Read<List<RectTransform>>(ground, "farTiles");
        var floor = Read<List<RectTransform>>(ground, "tiles");
        Clear(game, combat);
        Set(game, "previewWorldClimate", true);
        Set(game, "previewWorldWeather", WorldWeather.Clear);
        float dayLight = 0, nightLight = 0;
        foreach (float hour in new[] { 6.5f, 12f, 18.5f, 22f })
        {
            Set(game, "previewWorldHour", hour);
            yield return new WaitForSecondsRealtime(.14f);
            var image = Read<RawImage>(ground, "projectedSky");
            var projection = image != null ? image.texture as RenderTexture : null;
            Check(projection != null && projection.IsCreated() && image.isActiveAndEnabled && !image.raycastTarget,
                "HDR projection at " + hour);
            Check(image.transform.GetSiblingIndex() < mountains[0].GetSiblingIndex() &&
                mountains[0].gameObject.activeInHierarchy && trees[0].gameObject.activeInHierarchy &&
                floor[0].gameObject.activeInHierarchy && skyTiles.TrueForAll(tile => !tile.gameObject.activeSelf),
                "Only the sky layer is replaced");
            float light = Luminance(projection);
            Check(light > .025f, "Sky shader did not produce a blank image at " + hour);
            if (hour == 12f) dayLight = light;
            if (hour == 22f) nightLight = light;
            yield return capture("vendor-hdr-" + hour.ToString("00.0", System.Globalization.CultureInfo.InvariantCulture));
            pass(FormattableString.Invariant($"HDR phase {hour:F1}h, projection {projection.width}x{projection.height}, sample luma {light:F3}."));
        }
        Check(dayLight > nightLight * 1.55f, "Day and night are distinguishable in rendered output");

        Set(game, "previewWorldHour", 12f);
        Set(game, "previewWorldWeather", WorldWeather.LightRain);
        ui.SetPaused(false);
        yield return new WaitForSecondsRealtime(1.3f);
        int lightDrops = adapter.RainGraphic != null ? adapter.RainGraphic.VisibleDrops : 0;
        Check(lightDrops >= 14 && adapter.RainGraphic.OccupiedColumns == 15, "Light rain visible across viewport");
        yield return capture("vendor-light-rain-800");
        Set(game, "previewWorldWeather", WorldWeather.HeavyRain);
        yield return new WaitForSecondsRealtime(1.3f);
        int heavyDrops = adapter.RainGraphic.VisibleDrops;
        Check(heavyDrops > lightDrops * 1.55f && adapter.ActiveParticles <= WorldClimateVendorAdapter.MaxRainParticles,
            "Heavy rain visibly denser within particle cap");
        pass($"Rain Maker 2D at 800x450: {lightDrops} visible light, {heavyDrops} heavy, {adapter.ActiveParticles} native particles.");
        yield return capture("vendor-heavy-rain-800");

        var battleCanvas = Read<Canvas>(space, "battleCanvas");
        var hud = Read<RectTransform>(ui, "hudCanvas").GetComponent<Canvas>();
        foreach (var size in new[] { new Vector2Int(600, 338), new Vector2Int(1150, 647) })
        {
            float distantBefore = ground.DistantPhase;
            yield return resize(size.x, size.y);
            float skyBefore = Read<float>(ground, "projectedSkyScroll");
            space.PanEnvironment(37);
            float skyPan = (Read<float>(ground, "projectedSkyScroll") - skyBefore) * space.Width;
            Check(Mathf.Abs(skyPan - 37f * ground.farParallaxSpeed * .5f) < .02f,
                "Projected sky moves at half Ground 2 speed");
            yield return new WaitForSecondsRealtime(.85f);
            var drops = adapter.RainGraphic;
            var layer = drops.rectTransform.parent as RectTransform;
            var sky = Read<RawImage>(ground, "projectedSky");
            Check(layer.parent == space.BattleArea && layer.rect.size == sky.rectTransform.rect.size &&
                drops.OccupiedColumns == 15 && !drops.raycastTarget && layer.GetComponent<RectMask2D>() != null &&
                hud.sortingOrder > battleCanvas.sortingOrder && ground.DistantPhase != distantBefore,
                "Rain follows the visual viewport while pan and HUD sorting remain active");
            yield return capture("vendor-heavy-rain-" + size.x);
        }
        pass("Viewport clips and follows the 600/800/1150 battle layouts through pan. Rain draws below interactive HUD.");

        Set(game, "previewWorldWeather", WorldWeather.Clear);
        yield return null;
        yield return null;
        Check(adapter.ActiveParticles == 0 && !adapter.RainGraphic.isActiveAndEnabled, "Dry weather clears emitter and layer");
        Set(game, "worldClimateHdrSky", false);
        yield return null;
        yield return null;
        Check(skyTiles.TrueForAll(tile => tile.gameObject.activeSelf) &&
            !Read<RawImage>(ground, "projectedSky").isActiveAndEnabled, "PNG fallback switch");
        Check(Mathf.Abs(skyTiles[0].sizeDelta.x - space.Width * Mathf.Max(1f, ground.skyScale * .5f)) < .02f,
            "Fallback tiles use half the configured sky size with full viewport coverage");
        Set(game, "worldClimateHdrSky", true);
        Set(game, "previewWorldClimate", false);
        pass("Dry stops and clears Rain Maker completely. The HDR toggle restores the legacy PNGs.");
        // This legacy fixture verifies the optional individual SFX fields.
        var audioSettings = Read<WorldWeatherAudioSettings>(game, "worldWeatherAudioSettings");
        audioSettings.useMapLayers = false;
        yield return WeatherPhase2Smoke.Run(game, ui, combat, space, adapter, resize, capture, pass);
        audioSettings.useMapLayers = true;
        if (soak)
            yield return Soak(game, adapter, resize, capture, pass);
    }

    private static IEnumerator Soak(GameManager game, WorldClimateVendorAdapter adapter,
        Func<int, int, IEnumerator> resize, Func<string, IEnumerator> capture, Action<string> pass)
    {
        yield return resize(800, 450);
        var data = game.HeroData;
        int seed = SeedForWetAndDry(data);
        data.worldClimate = WorldClimateState.AtDawn(seed);
        var start = data.worldClimate;
        double started = Time.realtimeSinceStartupAsDouble, elapsed, active;
        double nextSample = 300, longest = 0;
        int frames = 0, peak = 0, wetFrames = 0, dryFrames = 0, peakImpacts = 0, flashes = 0;
        bool flashed = false;
        var seen = new HashSet<WorldWeather>();
        do
        {
            yield return null;
            frames++;
            elapsed = Time.realtimeSinceStartupAsDouble - started;
            active = (data.worldClimate.day - start.day + data.worldClimate.dayFraction - start.dayFraction) * 780;
            Check(elapsed < 2040, "Too much inactive time during the vendor soak");
            int count = adapter.ActiveParticles;
            peak = Mathf.Max(peak, count);
            int impacts = adapter.ImpactGraphic != null ? adapter.ImpactGraphic.VisibleImpacts : 0;
            peakImpacts = Mathf.Max(peakImpacts, impacts);
            bool flash = adapter.Weather.Flash > 0f;
            if (flash && !flashed) flashes++;
            flashed = flash;
            if (count > 0) wetFrames++; else dryFrames++;
            longest = Math.Max(longest, Time.unscaledDeltaTime * 1000);
            if (active < nextSample) continue;
            var sample = WorldClimate.Sample(data.worldClimate, 780, data.mapTerrain, data.worldMapId, data.mapNumber, false);
            pass(FormattableString.Invariant($"Vendor soak {active:F0}s active, {elapsed:F0}s real, day {data.worldClimate.day}, {sample.Weather}, {frames / elapsed:F1} FPS, current/peak {count}/{peak} particles, {impacts}/{peakImpacts} impacts, {flashes} flashes, Unity allocated {Profiler.GetTotalAllocatedMemoryLong() / 1048576.0:F1} MiB, Mono used {Profiler.GetMonoUsedSizeLong() / 1048576.0:F1} MiB."));
            nextSample += 300;
        } while (active < 1805);
        // Read each actual four-hour forecast slot traversed by the live clock.
        for (int slot = 2; slot <= (int)((data.worldClimate.day + data.worldClimate.dayFraction) * 6); slot++)
            seen.Add(Forecast(data, seed, slot));
        Check(wetFrames > 0 && dryFrames > 0 && peak <= WorldClimateVendorAdapter.MaxRainParticles &&
            peakImpacts > 0 && peakImpacts <= RainImpactCanvasGraphic.Capacity && flashes > 0 &&
            seen.Contains(WorldWeather.LightRain) && seen.Contains(WorldWeather.HeavyRain),
            "The real-time soak exercised both densities and dry cleanup");
        yield return capture("vendor-soak-30min");
        pass(FormattableString.Invariant($"Vendor soak complete: {active:F1}s active, {elapsed:F1}s real, {frames} frames, {frames / elapsed:F2} FPS, longest {longest:F1}ms, peak {peak} particles, {peakImpacts} impacts, {flashes} flashes, wet/dry frames {wetFrames}/{dryFrames}, seed {seed}."));
    }

    private static int SeedForWetAndDry(EntityDataSO data)
    {
        for (int seed = 1; seed <= 1000; seed++)
        {
            var outcomes = new HashSet<WorldWeather>();
            for (int slot = 2; slot <= 14; slot++) outcomes.Add(Forecast(data, seed, slot));
            if (outcomes.Contains(WorldWeather.LightRain) && outcomes.Contains(WorldWeather.HeavyRain) &&
                outcomes.Contains(WorldWeather.Clear)) return seed;
        }
        throw new InvalidOperationException("This owned test map has no seed with both rain levels.");
    }

    private static WorldWeather Forecast(EntityDataSO data, int seed, int slot)
    {
        var state = WorldClimateState.AtDawn(seed);
        state.day = slot / 6;
        state.dayFraction = ((slot % 6) + .5) / 6;
        return WorldClimate.Sample(state, 780, data.mapTerrain, data.worldMapId, data.mapNumber, false).Weather;
    }
}
#endif
