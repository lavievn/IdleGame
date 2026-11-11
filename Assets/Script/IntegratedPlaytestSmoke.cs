#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TuTienCore;
using UnityEngine;
using static Development016Smoke;

internal static class IntegratedPlaytestSmoke
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Read<T>(object owner, string key) => (T)owner.GetType().GetField(key, Fields).GetValue(owner);
    private static void Set(object owner, string key, object value) => owner.GetType().GetField(key, Fields).SetValue(owner, value);

    internal static IEnumerator Run(GameManager game, SaveManager saves, UIManager ui, CombatManager combat,
        HeroController hero, string evidence, Func<int,int,IEnumerator> resize, Func<string,IEnumerator> capture, Action<string> pass)
    {
        Audit(evidence, pass);
        yield return resize(800, 450);
        game.OnNewGameClicked();
        game.OnConfirmResetClicked();
        yield return Ready(game, ui);
        Check(StoryOrigin.IsSafe(game.HeroData) && StoryOrigin.Definition(game.HeroData) == StoryOrigin.HanVu,
            "New game starts in Hàn Vụ");
        Preview(game, 12f, WorldWeather.Clear);
        var bridge = hero.heroRect.GetComponent<CultivationSpriteBridge>();
        Check(bridge != null && bridge.BodyGraphic.sprite.texture.name.StartsWith("sword_", StringComparison.Ordinal),
            "The deployed hero is using PR 12 frames");
        var frame = bridge.BodyGraphic.sprite;
        ui.SetPaused(false);
        yield return Wait(() => bridge.BodyGraphic.sprite != frame, 2f, "Hero animation advances in MainGame");
        yield return capture("integrated-hero-village");
        pass("PR 12 Hero PNG frames render and animate on the deployed Hero in Hàn Vụ thôn.");

        var weatherAudio = Read<WorldWeatherAudio>(game, "weatherAudio");
        var mix = Read<WorldWeatherAudioSettings>(game, "worldWeatherAudioSettings");
        float bedTarget = .33f * mix.environmentVolume * AudioVolumeSettings.Get(GameAudioChannel.Sfx);
        Check(bedTarget > 0f, "SFX master is audible for the integration fixture");
        GameObject host = null;
        yield return Wait(() => (host = game.transform.Find("Map ambience layers")?.gameObject) != null &&
            Find(host, "Meadow_base")?.volume >= bedTarget * .97f, 5, "Village environment loops");
        var villageBed = Find(host, "Meadow_base");
        var oldVolume = villageBed.volume;
        int oldTime = villageBed.timeSamples;
        Preview(game, 22f, WorldWeather.LightRain);
        yield return Wait(() => Find(host, "Rain_05") != null && Find(host, "Night_10") != null &&
            villageBed.volume < oldVolume, 5, "Night and light rain layer gain");
        Check(Find(host, "Meadow_base") == villageBed && villageBed.timeSamples >= oldTime,
            "Weather changes gain without restarting existing loop");
        var vendor = Read<WorldClimateAssetAdapter>(game, "worldClimateAssets") as WorldClimateVendorAdapter;
        yield return Wait(() => vendor != null && vendor.ActiveParticles > 0, 5, "Real Rain Maker emitter");
        yield return capture("integrated-night-rain");
        pass("Night and light rain drive separate SFX layers and real Rain Maker. The village bed keeps its playhead.");

        Preview(game, 12f, WorldWeather.HeavyRain);
        Check(game.TryTravelToWorldMap(StoryOrigin.HanVu.Forest), "Travel from Hàn Vụ to forest via real handler");
        yield return Wait(() => weatherAudio.CurrentMapProfile == "Forest" &&
            Find(host, "Primeval_02") != null && Find(host, "Meadow_base") != null, 10, "Travel crossfade overlap");
        yield return Wait(() => Find(host, "Meadow_base") == null && Find(host, "Rain_07") != null,
            5, "Old environment fades out and heavy rain stays");
        Check(MapBgmController.Instance.GetComponents<AudioSource>().Any(x => x.isPlaying &&
            Resources.LoadAll<AudioClip>("WorldMapMusic").Contains(x.clip)), "Regular BGM still plays from local playlist");
        yield return capture("integrated-forest-heavy-rain");
        pass("Travel crossfades Village → Forest, heavy rain has its own layer, and the separate BGM is still playing.");

        var forest = game.HeroData.worldMapId;
        Set(game, "previewWorldClimate", false);
        Clear(game, combat);
        ui.SetPaused(true);
        var checkpoint = game.HeroData.worldClimate;
        Check(game.TryManualSave(SaveSlot.ManualSave1), "Save combined story and climate checkpoint");
        Check(game.LoadAndContinue(SaveSlot.ManualSave1), "Restore combined story and climate checkpoint");
        yield return Ready(game, ui);
        Check(game.HeroData.worldMapId == forest && StoryOrigin.Definition(game.HeroData) == StoryOrigin.HanVu &&
            game.HeroData.worldClimate.seed == checkpoint.seed && game.HeroData.worldClimate.day == checkpoint.day &&
            Math.Abs(game.HeroData.worldClimate.dayFraction - checkpoint.dayFraction) < .003, "Combined snapshot survives Load");
        pass("Save/Load preserves world location, Hàn Vụ identity and the climate seed/clock.");

        yield return Gate(game, saves, ui, combat, hero, capture, pass);
    }

    private static IEnumerator Gate(GameManager game, SaveManager saves, UIManager ui, CombatManager combat,
        HeroController hero, Func<string,IEnumerator> capture, Action<string> pass)
    {
        game.UnequipGear(EquipmentSlot.Weapon);
        var candidate = UnityEngine.Object.Instantiate(game.HeroData);
        candidate.combatSkills.health = -1;
        var profile = saves.CultivationProfile;
        using (var binding = new CultivationGameplayBinding(candidate, profile, HeavenlyGateV13Fixtures.Session(profile, 180, 7))) { }
        Check(saves.SaveGame(candidate, SaveSlot.ManualSave2), "Owned Gate checkpoint");
        UnityEngine.Object.Destroy(candidate);
        Check(game.LoadAndContinue(SaveSlot.ManualSave2), "Load Gate-ready checkpoint");
        yield return Ready(game, ui);
        var thunder = Read<HeavenlyGateThunderAudio>(combat, "trialThunder");
        int played = thunder.Played;
        long strike = combat.TrialStrikeCount;
        Check(game.RequestTribulation(), "Enter the actual Heavenly Gate");
        yield return Wait(() => combat.TrialStrikeCount > strike, 40, "Actual Gate lightning impact");
        Check(thunder.Played == played && thunder.Pending == 1, "Audio is delayed from impact");
        yield return Wait(() => thunder.Played == played + 1, 3, "One matching thunder shot");
        Check(combat.TrialStrikeCount == strike + 1 && thunder.Pending == 0, "One sound for one real strike");
        yield return capture("integrated-gate-thunder");
        var trialBgm = Read<AudioSource>(MapBgmController.Instance, "trialSource");
        Check(trialBgm != null && trialBgm.isPlaying && trialBgm.clip.name.StartsWith("HeavenlyGateTheme", StringComparison.Ordinal),
            "Gate keeps its BGM playlist");
        Set(combat, "trialThunderDelay", 2.5f);
        yield return Wait(() => thunder.Pending > 0, 6, "Queue another real strike");
        Check(combat.ApplyDamageToRegisteredHero(hero, 1000000, "Owned Gate exit fixture"), "End attempt before delayed sound");
        yield return Wait(() => !game.HasTribulationLiability, 20, "Return from Gate");
        yield return Ready(game, ui);
        Check(thunder.Pending == 0, "Gate exit drops queued thunder");
        Check(game.LoadAndContinue(SaveSlot.ManualSave1), "Load ordinary map after Gate exit");
        yield return Ready(game, ui);
        Check(combat.ApplyDamageToRegisteredHero(hero, 1000000, "Owned map Retry"), "Defeat to exercise Retry");
        game.OnRetryClicked();
        yield return Ready(game, ui);
        yield return new WaitForSecondsRealtime(2.6f);
        Check(thunder.Played == played + 1 && thunder.Pending == 0 && !combat.HeavenlyGateActive &&
            StoryOrigin.Definition(game.HeroData) == StoryOrigin.HanVu, "No stale thunder after exit, Load and Retry");
        pass("One delayed thunder played for the real Gate strike. Ending the next attempt clears pending audio through Load and Retry. Gate BGM stayed separate.");
    }

    private static void Audit(string evidence, Action<string> pass)
    {
        var catalog = WorldEnvironmentAudioCatalog.Load();
        Check(catalog != null && WorldMapCatalog.All.Count > 100, "The real map catalog and ambience data are loaded");
        var rows = new List<string> { "map\tterrain\tprofile\tclips" };
        foreach (var node in WorldMapCatalog.All)
        {
            var profile = catalog.Select(node.Terrain, node.Id, false);
            Check(profile != null && profile.layers.Length > 0, "Map has an ambience profile: " + node.Id);
            var layers = profile.layers.Concat(catalog.Weather).ToArray();
            Check(layers.Length <= 10 && layers.Select(x => x.slot).Distinct().Count() == layers.Length &&
                layers.All(x => x.slot >= 0 && x.slot < 10 && catalog.Clip(x.clip)?.loadType == AudioClipLoadType.Streaming),
                "Map has unique slots and imported streaming clips: " + node.Id);
            rows.Add($"{node.Id}\t{node.TerrainLabel}\t{profile.id}\t{string.Join(",", layers.Select(x => x.clip))}");
        }
        var village = catalog.Select(TerrainType.DamLay, StoryOrigin.HanVu.Village, true);
        Check(village.id == "Village" && village.layers.All(x => catalog.Clip(x.clip) != null), "Safe village profile");
        rows.Add(StoryOrigin.HanVu.Village + " (origin)\tVillage\tVillage\t" +
            string.Join(",", village.layers.Concat(catalog.Weather).Select(x => x.clip)));
        var gate = Resources.Load<AudioClip>("IntegratedAmbience/GateThunder");
        Check(gate != null && gate.length > 3 && gate.length < 7, "Derived one-shot is imported");
        File.WriteAllLines(Path.Combine(evidence, "map-ambience.tsv"), rows);

        var test = new GameObject("Owned 10-layer audio fixture");
        var audio = new WorldWeatherAudio();
        var settings = new WorldWeatherAudioSettings();
        var blend = new WorldClimateFrame(WorldDayPhase.Dusk, WorldWeather.HeavyRain, 19.5f, .8f, .3f, .6f, Color.white, Color.white);
        var wind = new WorldWeatherVisual(null, "TV-16", 3, 0, 0, 0, false, false, false, false);
        audio.Update(test, settings, wind, blend, TerrainType.RungRam, "TV-16", true, true, 0f);
        var sources = test.GetComponentsInChildren<AudioSource>();
        Check(sources.Length == 10 && sources.All(x => x.clip != null && x.loop), "Ten layers at rainy twilight");
        audio.Stop();
        UnityEngine.Object.Destroy(test);
        pass($"{WorldMapCatalog.All.Count} catalog maps plus the Hàn Vụ overlay have complete audio references. Ten streaming layers can play at rainy twilight.");
    }

    private static AudioSource Find(GameObject host, string clip) => host.GetComponents<AudioSource>()
        .FirstOrDefault(x => x.isPlaying && x.clip != null && x.clip.name == clip && x.volume > 0f);

    private static void Preview(GameManager game, float hour, WorldWeather weather)
    {
        Set(game, "previewWorldClimate", true);
        Set(game, "previewWorldHour", hour);
        Set(game, "previewWorldWeather", weather);
    }
}
#endif
