#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using TuTienCore;
using UnityEngine;
using static Development016Smoke;

internal static class WeatherPhase2Smoke
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Read<T>(object owner, string name) => (T)owner.GetType().GetField(name, Fields).GetValue(owner);
    private static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Fields).SetValue(owner, value);

    internal static IEnumerator Run(GameManager game, UIManager ui, CombatManager combat, EnvironmentManager space,
        WorldClimateVendorAdapter adapter, Func<int, int, IEnumerator> resize, Func<string, IEnumerator> capture, Action<string> pass)
    {
        yield return resize(800, 450);
        var settings = Read<WorldWeatherPresentationSettings>(game, "worldWeatherPresentation");
        var audio = Read<WorldWeatherAudioSettings>(game, "worldWeatherAudioSettings");
        var audioRuntime = Read<WorldWeatherAudio>(game, "weatherAudio");
        var data = game.HeroData;
        int initialSources = game.GetComponents<AudioSource>().Length;
        var audioOptions = audio.fadeSeconds;
        Set(game, "previewWorldClimate", true);
        Set(game, "previewWorldHour", 12f);
        Set(game, "previewWorldWeather", WorldWeather.LightRain);
        ui.SetPaused(false);
        SettleWind(data);
        yield return new WaitForSecondsRealtime(1.5f);
        var impacts = adapter.ImpactGraphic;
        Check(impacts != null && impacts.VisibleImpacts > 0 && impacts.VisibleRipples == 0, "Light rain impacts a dry floor");
        float lightStrength = impacts.PeakStrength;
        int light = impacts.VisibleImpacts;
        float lightWind = adapter.RainGraphic.AverageWind;
        yield return capture("phase2-light-splash");

        Set(game, "previewWorldWeather", WorldWeather.HeavyRain);
        SettleWind(data);
        yield return new WaitForSecondsRealtime(1.5f);
        float heavyWind = adapter.RainGraphic.AverageWind;
        Check(Mathf.Abs(lightWind) > 1f && Mathf.Abs(heavyWind) > 2f &&
            Mathf.Approximately(Read<float>(adapter.RainGraphic, "dropSize"), .60f),
            "Native particles fall diagonally with a 60 percent rendered drop scale");
        VerifyRainWind(data);
        pass(FormattableString.Invariant($"Rain wind: native horizontal velocity {lightWind:F2}/{heavyWind:F2} for light/heavy rain, seeded left/right changes with smooth boundaries, drop scale 0.60."));
        Check(impacts.VisibleImpacts > light && impacts.PeakStrength > lightStrength &&
            impacts.VisibleImpacts <= RainImpactCanvasGraphic.Capacity && adapter.Weather.Haze > 0f,
            "Heavy rain produces denser, stronger splashes and distant haze");
        float heavyStrength = impacts.PeakStrength;
        pass(FormattableString.Invariant($"Rain impact: light {light} visible at strength {lightStrength:F2}, heavy {impacts.VisibleImpacts} at {heavyStrength:F2}; wind samples {lightWind:F2}/{heavyWind:F2}."));
        yield return capture("phase2-heavy-splash");

        var rect = space.BattleArea.rect;
        var liveDepth = impacts.ActiveDepthRange();
        Check(liveDepth.x >= rect.yMin && liveDepth.y <= rect.yMax &&
            liveDepth.x < rect.yMin + .22f * rect.height && liveDepth.y > rect.yMax - .22f * rect.height,
            "Live dry splashes reach both the far and near bands inside BattleArea");
        float min = float.PositiveInfinity, max = float.NegativeInfinity;
        for (uint seed = 1; seed <= 256; seed++)
        {
            float depth = impacts.GroundAt(rect.center.x, seed, out bool water);
            Check(!water && depth >= rect.yMin && depth <= rect.yMax, "Random depths stay inside the dry BattleArea");
            min = Mathf.Min(min, depth);
            max = Mathf.Max(max, depth);
        }
        Check(max - min > rect.height * .95f, "Random drop targets cover the full battle depth");
        pass(FormattableString.Invariant($"Random dry impact depths: {min:F1}..{max:F1}, bounds {rect.yMin:F1}..{rect.yMax:F1}; live {liveDepth.x:F1}..{liveDepth.y:F1}."));
        float floor = rect.center.y;
        settings.surfaces = new[] { new WorldWeatherSurface
        {
            mapId = data.worldMapId, horizontalRange = new Vector2(0f, rect.width * .3f), height = floor + 9f, water = true
        } };
        yield return new WaitForSecondsRealtime(1f);
        bool foundWater = impacts.TryGetImpactPosition(true, out var before);
        Check(impacts.VisibleRipples > 0 && impacts.VisibleRipples < impacts.VisibleImpacts &&
            foundWater && Mathf.Approximately(before.y, floor + 9f),
            "Ripples occur only on the authored raised water strip");
        space.PanEnvironment(31f);
        Check(impacts.TryGetImpactPosition(true, out var after) &&
            Mathf.Abs(after.x - before.x - 31f) < .02f && after.y == before.y, "The same splash tracks Ground pan");
        foreach (var size in new[] { new Vector2Int(600, 338), new Vector2Int(1150, 647) })
        {
            yield return resize(size.x, size.y);
            yield return new WaitForSecondsRealtime(.8f);
            Check(impacts.VisibleImpacts > 0 && impacts.VisibleRipples > 0 && !impacts.raycastTarget,
                "Impacts and water remain visible and non-interactive after resize");
            yield return capture("phase2-water-" + size.x);
        }
        settings.surfaces = Array.Empty<WorldWeatherSurface>();
        yield return resize(800, 450);
        pass("Raised water strip alone emits ripples. Splash position follows Ground pan and survives 600/800/1150 layouts.");

        VerifyStormSchedule(settings, data);
        JumpBeforeStrike(data, settings);
        // The shader projection is rate limited. Capture after the flash has
        // reached the actual render target, not just the scheduler state.
        yield return Wait(() => Read<Vector2>(adapter, "lastFlash").x > 0f, 3f, "A rendered heavy-rain sky flash");
        yield return capture("phase2-lightning");
        yield return new WaitForSecondsRealtime(4.4f);
        Check(audioRuntime.ThunderPlayed == 0 && game.GetComponents<AudioSource>().Length == initialSources,
            "All null placeholders create no AudioSource and play no thunder");
        pass("Seeded lightning triggers in heavy rain. Indoor/reload suppression and reduced flash checked. All null clips remain silent.");

        var lightLoop = AudioClip.Create("Owned light loop", 44100, 1, 44100, false);
        var heavyLoop = AudioClip.Create("Owned heavy loop", 44100, 1, 44100, false);
        var thunder = AudioClip.Create("Owned thunder", 44100, 1, 44100, false);
        audio.lightRainLoop = lightLoop;
        audio.heavyRainLoop = heavyLoop;
        audio.distantThunder = audio.closeThunder = thunder;
        audio.fadeSeconds = .35f;
        Set(game, "previewWorldWeather", WorldWeather.LightRain);
        yield return Wait(() => Volume(game, lightLoop) > .08f, 2f, "Light rain audio fade-in");
        Set(game, "previewWorldWeather", WorldWeather.HeavyRain);
        yield return Wait(() => Volume(game, heavyLoop) > .02f && Volume(game, lightLoop) > 0f,
            2f, "Light-to-heavy overlap");
        yield return Wait(() => Volume(game, lightLoop) == 0f && Volume(game, heavyLoop) > .1f, 2f, "Heavy fade completes");
        int thunderBefore = audioRuntime.ThunderPlayed;
        JumpBeforeStrike(data, settings);
        yield return Wait(() => adapter.Weather.Flash > 0f, 3f, "A second scheduled sky flash");
        Check(audioRuntime.ThunderPlayed == thunderBefore, "Thunder has not played at lightning instant");
        yield return Wait(() => audioRuntime.ThunderPlayed == thunderBefore + 1, 5f, "One delayed thunder playback");
        int poolCount = game.GetComponents<AudioSource>().Length;
        Check(poolCount == initialSources + 3, "Rain crossfade reuses two channels plus one thunder source");

        Set(game, "previewWorldClimate", false);
        double transition = MoveBeforeDrySlot(data);
        yield return new WaitForSecondsRealtime(.7f);
        Check(Seconds(data) > transition && adapter.ActiveParticles > 0, "Heavy-to-clear retains rain during the existing blend");
        yield return Wait(() => adapter.ActiveParticles == 0 && impacts.VisibleImpacts == 0 &&
            !adapter.RainGraphic.isActiveAndEnabled, 10f, "Natural heavy-to-clear blend drains all rain effects");
        Check(Volume(game, heavyLoop) == 0f, "Weather loop fades out with precipitation");
        pass("One delayed thunder playback using a silent fixture clip. Loop crossfade reuses its sources, then fades out as the real 8-second HeavyRain-to-Clear blend stops rain.");

        yield return MapAudioAndLifecycle(game, ui, combat, adapter, impacts, data, poolCount, pass);
        audio.lightRainLoop = audio.heavyRainLoop = audio.distantThunder = audio.closeThunder = null;
        audio.fadeSeconds = audioOptions;
        UnityEngine.Object.Destroy(lightLoop);
        UnityEngine.Object.Destroy(heavyLoop);
        UnityEngine.Object.Destroy(thunder);
        ui.SetPaused(false);
    }

    private static IEnumerator MapAudioAndLifecycle(GameManager game, UIManager ui, CombatManager combat,
        WorldClimateVendorAdapter adapter, RainImpactCanvasGraphic impacts, EntityDataSO data, int poolCount, Action<string> pass)
    {
        var playlist = Resources.LoadAll<AudioClip>("WorldMapMusic");
        if (playlist.Length > 0)
            Check(playlist.Length == 12 && playlist.All(track => track.loadType == AudioClipLoadType.Streaming),
                "Twelve user-provided map tracks imported as streaming");
        var bgm = MapBgmController.Instance;
        var channels = bgm.GetComponents<AudioSource>();
        Check(channels.Length == 2, "The existing BGM sources are reused");
        var trialTrack = (AudioClip)typeof(MapBgmController).GetMethod("ChooseTrialMusic", Fields).Invoke(bgm, null);
        Check(trialTrack != null && trialTrack.name.StartsWith("HeavenlyGateTheme", StringComparison.Ordinal) &&
            !playlist.Contains(trialTrack), "Thiên Quan remains on its own playlist");
        yield return Wait(() => channels.Any(source => source.isPlaying), 4f, "Map BGM started");
        var previous = channels.Where(source => source.isPlaying).OrderByDescending(source => source.volume).First().clip;
        if (playlist.Length > 0) Check(playlist.Contains(previous), "Ordinary map uses the local playlist");
        Set(game, "previewWorldClimate", true);
        Set(game, "previewWorldWeather", WorldWeather.HeavyRain);
        yield return Wait(() => impacts.VisibleImpacts > 0, 4f, "Rain before map travel");
        ui.SetPaused(true);
        var checkpoint = data.worldClimate;
        Check(game.TryManualSave(SaveSlot.ManualSave2), "Owned save before travel");
        Check(WorldMapCatalog.TryGet(data.worldMapId, out var node), "Active owned map exists");
        data.worldMapCleared.Add(node.Id);
        string destination = node.Links.First(id => WorldMapCatalog.TravelBlockReason(data, id) == null);
        int generation = impacts.Generation;
        Check(game.TryTravelToWorldMap(destination), "Travel from a rainy map");
        yield return Ready(game, ui);
        Check(impacts.Generation > generation, "Travel cleared old map impacts");
        if (playlist.Length > 0)
        {
            yield return Wait(() => channels.Any(source => source.isPlaying && source.clip != previous &&
                playlist.Contains(source.clip) && source.volume > 0f), 3f, "A new playlist track starts for the destination");
            yield return Wait(() => channels.Count(source => source.isPlaying) == 1, 4f, "Map crossfade releases its outgoing source");
        }
        Set(game, "previewWorldClimate", false);
        Check(game.LoadAndContinue(SaveSlot.ManualSave2), "Owned save loads after travel");
        yield return Ready(game, ui);
        var restored = game.HeroData;
        Check(restored.worldMapId == node.Id && restored.worldClimate.seed == checkpoint.seed &&
            restored.worldClimate.day == checkpoint.day && Math.Abs(restored.worldClimate.dayFraction - checkpoint.dayFraction) < .002,
            "Load preserves clock, seed and map");
        game.OnHeroDied();
        game.OnRetryClicked();
        yield return Ready(game, ui);
        Clear(game, combat);
        Check(game.HeroData.worldClimate.seed == checkpoint.seed && game.HeroData.worldClimate.day == checkpoint.day &&
            Math.Abs(game.HeroData.worldClimate.dayFraction - checkpoint.dayFraction) < .004 &&
            adapter.Weather.Flash == 0f && game.GetComponents<AudioSource>().Length == poolCount,
            "Retry keeps clock and does not duplicate sources or replay a strike");
        pass(playlist.Length > 0
            ? "Travel clears splashes, picks a different streaming BGM, and fades the old track. Save/Load/Retry preserve climate and source counts."
            : "Travel clears splashes. Save/Load/Retry preserve climate and source counts; local playlist was absent.");
    }

    private static void SettleWind(EntityDataSO data)
    {
        float period = WorldWeatherStorm.RainWindPeriod;
        SetSeconds(data, Math.Ceiling(Seconds(data) / period) * period + period * .5);
    }

    private static void VerifyRainWind(EntityDataSO data)
    {
        var settings = new WorldWeatherPresentationSettings { gustStrength = 0f };
        var weather = WorldClimate.Sample(data.worldClimate, 780f, data.mapTerrain, data.worldMapId,
            data.mapNumber, false, WorldWeather.LightRain);
        var storm = new WorldWeatherStorm();
        var state = data.worldClimate;
        float WindAt(double time)
        {
            state.day = (int)(time / 780.0);
            state.dayFraction = time / 780.0 - state.day;
            return storm.Step(settings, weather, state, data.worldMapId, data.mapNumber, 780f, true, true).Wind;
        }
        bool left = false, right = false;
        float period = WorldWeatherStorm.RainWindPeriod;
        for (int slot = 1; slot <= 32; slot++)
        {
            double boundary = slot * period;
            float sample = WindAt(boundary + 7);
            left |= sample < -1f;
            right |= sample > 1f;
            Check(Mathf.Abs(WindAt(boundary - .01) - WindAt(boundary + .01)) < .03f,
                "Random wind is continuous across a new wind interval");
        }
        float remembered = WindAt(147);
        WindAt(203);
        Check(left && right && Mathf.Approximately(remembered, WindAt(147)), "Wind uses both directions and is repeatable");
        settings.randomRainWind = false;
        settings.windDirection = 1f;
        float fixedWind = WindAt(147);
        Check(fixedWind > 1f && Mathf.Approximately(fixedWind, WindAt(203)), "Fixed wind direction stays available");
        settings.windStrength = 0f;
        Check(WindAt(147) == 0f, "Zero wind strength means vertical rain");
    }

    private static void VerifyStormSchedule(WorldWeatherPresentationSettings settings, EntityDataSO data)
    {
        const int slot = 4;
        var sample = data.worldClimate;
        float interval = settings.lightningInterval;
        double strike = WorldWeatherStorm.StrikeAt(slot, interval, sample.seed, data.worldMapId, data.mapNumber);
        var frame = WorldClimate.Sample(sample, 780f, data.mapTerrain, data.worldMapId, data.mapNumber, false, WorldWeather.HeavyRain);
        var clock = new WorldWeatherStorm();
        WorldWeatherVisual Step(double seconds, bool outdoors)
        {
            sample.day = (int)(seconds / 780.0);
            sample.dayFraction = seconds / 780.0 - sample.day;
            return clock.Step(settings, frame, sample, data.worldMapId, data.mapNumber, 780f, true, outdoors);
        }
        Check(Step(strike - .2, true).Flash == 0f && Step(strike + .02, true).Flash > 0f, "A strike crosses one seeded time");
        clock.Reset();
        Check(Step(strike + .02, true).Flash == 0f, "Reload after a strike never replays it");
        clock.Reset();
        Step(strike - .2, false);
        Check(Step(strike + .02, false).Flash == 0f, "No indoor lightning");
        clock.Reset();
        settings.reducedFlashes = true;
        Step(strike - .2, true);
        Check(Step(strike + .02, true).Flash <= settings.lightningBrightness * .16f, "Reduced-flash setting");
        settings.reducedFlashes = false;
        clock.Reset();
        float min = float.PositiveInfinity, max = float.NegativeInfinity;
        for (int t = 0; t <= 18; t += 2)
        {
            float wind = Step(strike + t, true).Wind;
            min = Mathf.Min(min, wind);
            max = Mathf.Max(max, wind);
        }
        Check(max - min > .05f, "Seeded gust changes the rain direction with time");
    }

    private static float Volume(GameManager game, AudioClip clip) =>
        game.GetComponents<AudioSource>().Where(source => source.clip == clip && source.isPlaying).Sum(source => source.volume);

    private static double Seconds(EntityDataSO data) => (data.worldClimate.day + data.worldClimate.dayFraction) * 780;

    private static void SetSeconds(EntityDataSO data, double seconds)
    {
        data.worldClimate.day = (int)(seconds / 780.0);
        data.worldClimate.dayFraction = seconds / 780.0 - data.worldClimate.day;
    }

    private static void JumpBeforeStrike(EntityDataSO data, WorldWeatherPresentationSettings settings)
    {
        long firstSlot = (long)(Seconds(data) / settings.lightningInterval) + 1;
        var frame = WorldClimate.Sample(data.worldClimate, 780, data.mapTerrain,
            data.worldMapId, data.mapNumber, false, WorldWeather.HeavyRain);
        for (long slot = firstSlot; slot < firstSlot + 24; slot++)
        {
            double strike = WorldWeatherStorm.StrikeAt(slot, settings.lightningInterval,
                data.worldClimate.seed, data.worldMapId, data.mapNumber);
            var forecast = new WorldWeatherStorm();
            SetSeconds(data, strike - .25);
            forecast.Step(settings, frame, data.worldClimate, data.worldMapId, data.mapNumber, 780, true, true);
            SetSeconds(data, strike + .02);
            var visual = forecast.Step(settings, frame, data.worldClimate, data.worldMapId, data.mapNumber, 780, true, true);
            if (visual.BoltPosition < .20f || visual.BoltPosition > .39f) continue;
            SetSeconds(data, strike - .25);
            return;
        }
        throw new InvalidOperationException("No owned fixture strike outside the HUD.");
    }

    private static double MoveBeforeDrySlot(EntityDataSO data)
    {
        for (int seed = 1; seed <= 1000; seed++)
        {
            data.worldClimate.seed = seed;
            for (int slot = 2; slot <= 12; slot++)
            {
                double boundary = slot * 130;
                SetSeconds(data, boundary - 60);
                var before = WorldClimate.Sample(data.worldClimate, 780, data.mapTerrain, data.worldMapId, data.mapNumber, false);
                SetSeconds(data, boundary + 60);
                var after = WorldClimate.Sample(data.worldClimate, 780, data.mapTerrain, data.worldMapId, data.mapNumber, false);
                if (before.Weather != WorldWeather.HeavyRain || after.Weather != WorldWeather.Clear) continue;
                SetSeconds(data, boundary - .2);
                return boundary;
            }
        }
        throw new InvalidOperationException("No heavy-to-clear interval on the owned wet map.");
    }
}
#endif
