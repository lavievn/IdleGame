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
        yield return resize(600, 338);
        var menu = game.GetComponent<StartMenuUI>();
        menu.GetType().GetMethod("OpenSettings", Fields).Invoke(menu, null);
        yield return null;
        var openingEnv = Read<AudioVolumeBar>(menu, "environmentVolumeBar").Track;
        var openingSfx = Read<AudioVolumeBar>(menu, "sfxVolumeBar").Track;
        Check(openingEnv.gameObject.activeInHierarchy && openingEnv.anchoredPosition.y < openingSfx.anchoredPosition.y &&
            AudioVolumeSettings.Key(GameAudioChannel.Environment) != AudioVolumeSettings.Key(GameAudioChannel.Sfx) &&
            AudioVolumeSettings.Key(GameAudioChannel.Environment) != AudioVolumeSettings.Key(GameAudioChannel.Bgm),
            "Opening has a visible separate environment channel");
        yield return capture("opening-environment-volume");
        menu.GetType().GetMethod("CloseSettings", Fields).Invoke(menu, null);
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
        ui.ToggleMusicVolume();
        var volumePanel = Read<MusicVolumePanelUI>(ui, "musicVolumePanel");
        var gameEnv = Read<AudioVolumeBar>(volumePanel, "environmentBar").Track;
        Check(gameEnv.gameObject.activeInHierarchy &&
            volumePanel.IsVolumeTrack(gameEnv.GetComponent<CustomInteractable>()), "Gameplay has an environment volume drag target");
        yield return capture("gameplay-environment-volume");
        volumePanel.Hide();
        pass("Opening and gameplay expose a separate environmental volume track.");
        var frame = bridge.BodyGraphic.sprite;
        ui.SetPaused(false);
        yield return Wait(() => bridge.BodyGraphic.sprite != frame, 2f, "Hero animation advances in MainGame");
        yield return new WaitForSecondsRealtime(.30f);
        Check(hero.CurrentState == HeroState.Idle && hero.LastMovementSpeed == 0f &&
            hero.MovementDistanceThisFrame == 0f && bridge.CurrentMotion == CultivationSpriteBridge.Motion.Idle,
            "Safe village plays Idle while the Hero is stationary");
        yield return capture("integrated-hero-village");
        pass("PR 12 Hero Idle frames animate in Hàn Vụ without moving the Hero.");

        var weatherAudio = Read<WorldWeatherAudio>(game, "weatherAudio");
        var mix = Read<WorldWeatherAudioSettings>(game, "worldWeatherAudioSettings");
        float bedTarget = .50f * mix.environmentVolume * AudioVolumeSettings.Get(GameAudioChannel.Environment);
        Check(bedTarget > 0f, "Environment master is audible for the integration fixture");
        GameObject host = null;
        yield return Wait(() => (host = game.transform.Find("Map ambience layers")?.gameObject) != null &&
            GroupAtGain(host, "Meadow_", bedTarget), 5, "All Meadow clips play at half gain");
        var villageBed = Find(host, "Meadow_base");
        var oldVolume = villageBed.volume;
        int oldTime = villageBed.timeSamples;
        Preview(game, 22f, WorldWeather.LightRain);
        yield return Wait(() => Find(host, "Rain_05") != null, 5, "Separate light rain layer");
        Check(Find(host, "Meadow_base") == villageBed && Mathf.Abs(villageBed.volume - oldVolume) < .005f &&
            villageBed.timeSamples >= oldTime && GroupAtGain(host, "Meadow_", bedTarget),
            "The same 20 map clips retain their gain and playheads through night and rain");
        var vendor = Read<WorldClimateAssetAdapter>(game, "worldClimateAssets") as WorldClimateVendorAdapter;
        yield return Wait(() => vendor != null && vendor.ActiveParticles > 0, 5, "Real Rain Maker emitter");
        Check(Mathf.Approximately(Read<float>(vendor.RainGraphic, "dropSize"), .60f),
            "Actual rain uses the doubled 0.60 drop scale");
        yield return capture("integrated-night-rain");
        pass($"All 20 Meadow clips play at 0.50 × the Environment bus ({bedTarget:F3}). Night/rain keep the group and playheads. Rain Maker and the weather loop still run.");

        Preview(game, 12f, WorldWeather.HeavyRain);
        Check(game.TryTravelToWorldMap(StoryOrigin.HanVu.Forest), "Travel from Hàn Vụ to forest via real handler");
        yield return Wait(() => weatherAudio.CurrentMapProfile == "Forest" &&
            Find(host, "Primeval_02") != null && Find(host, "Meadow_base") != null, 10, "Travel crossfade overlap");
        yield return Wait(() => !host.GetComponents<AudioSource>().Any(x => x.clip != null &&
            x.clip.name.StartsWith("Meadow_", StringComparison.Ordinal)) && GroupAtGain(host, "Primeval_", bedTarget) &&
            Find(host, "Rain_07") != null, 5, "All Primeval clips play at half gain after crossfade");
        Check(MapBgmController.Instance.GetComponents<AudioSource>().Any(x => x.isPlaying &&
            Resources.LoadAll<AudioClip>("WorldMapMusic").Contains(x.clip)), "Regular BGM still plays from local playlist");
        yield return capture("integrated-forest-heavy-rain");
        pass("Travel crossfades all 20 Meadow clips to all 20 Primeval clips at equal gain. Heavy rain and BGM stay separate.");

        Clear(game, combat);
        var modifier = new CombatStatModifier { sourceId = "owned:movement-probe", stat = CombatStatId.MovementSpeed, flat = 900f };
        game.HeroData.combatStatModifiers.Add(modifier);
        yield return Wait(() => Mathf.Abs(hero.LastMovementSpeed - 100f) < .2f &&
            bridge.CurrentMotion == CultivationSpriteBridge.Motion.Walk, 5f, "Exploration Walk at 100");
        var space = EnvironmentManager.Instance;
        float reach = space.AttackRange(hero.attackMode, true);
        Wave(game, combat, space, hero, new Vector2(-reach - 105f, 0f));
        yield return Wait(() => Mathf.Abs(hero.LastMovementSpeed - 200f) < .3f &&
            bridge.CurrentMotion == CultivationSpriteBridge.Motion.Run, 5f, "Enemy approach Run at 200");
        game.HeroData.combatStatModifiers.Remove(modifier);
        pass("Walk measures 100 and enemy approach Run measures 200 with an owned movement stat modifier present.");

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
        var imported = Resources.LoadAll<AudioClip>("IntegratedAmbience");
        Check(imported.Length == 191, "All 190 original OGG and the dedicated Gate excerpt are imported");
        var rows = new List<string> { "map	terrain	profile	prefix	clips" };
        foreach (var node in WorldMapCatalog.All)
        {
            var profile = catalog.Select(node.Terrain, node.Id, false);
            var clips = catalog.PrefixClips(profile);
            Check(profile != null && clips.Length == 20 && clips.All(x => x.loadType == AudioClipLoadType.Streaming),
                "Map has its complete streaming prefix group: " + node.Id);
            rows.Add($"{node.Id}	{node.TerrainLabel}	{profile.id}	{profile.prefix}	{string.Join(",", clips.Select(x => x.name))}");
        }
        var village = catalog.Select(TerrainType.DamLay, StoryOrigin.HanVu.Village, true);
        var villageClips = catalog.PrefixClips(village);
        Check(village.id == "Village" && villageClips.Length == 20, "Safe village uses all Meadow clips");
        rows.Add(StoryOrigin.HanVu.Village + " (origin)	Village	Village	Meadow	" +
            string.Join(",", villageClips.Select(x => x.name)));
        Check(catalog.Weather.All(x => catalog.Clip(x.clip)?.loadType == AudioClipLoadType.Streaming),
            "Existing weather loops still resolve");
        var gate = Resources.Load<AudioClip>("IntegratedAmbience/GateThunder");
        Check(gate != null && gate.length > 3 && gate.length < 7, "Derived one-shot is imported");
        File.WriteAllLines(Path.Combine(evidence, "map-ambience.tsv"), rows);

        pass($"{WorldMapCatalog.All.Count} maps and the Hàn Vụ overlay resolve a full 20-clip group each. All 190 OGG are packaged.");
    }

    private static bool GroupAtGain(GameObject host, string prefix, float expected)
    {
        var sources = host.GetComponents<AudioSource>().Where(x => x.clip != null &&
            x.clip.name.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
        return sources.Length == 20 && sources.Select(x => x.clip).Distinct().Count() == 20 &&
            sources.All(x => x.isPlaying && !x.isVirtual && x.loop && Mathf.Abs(x.volume - expected) < .005f);
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
