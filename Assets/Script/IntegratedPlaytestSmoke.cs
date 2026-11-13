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
        Audit(pass);
        yield return resize(600, 338);
        var menu = game.GetComponent<StartMenuUI>();
        yield return Wait(() => Read<bool>(menu, "openingMenuReady") && Read<CanvasGroup>(menu, "rootFade").alpha > .95f, 30f, "Opening settings are visible after title reveal");
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
        var family = Read<EnvironmentAudioPlayer>(weatherAudio, "familyPlayer");
        Check(family != null, "Verified asset manifest owns environment audio");
        yield return Wait(() => family.ActiveLoops(AudioLayer.Meadow, bedTarget) == 2 &&
            family.ActiveLoops(AudioLayer.MeadowCricket, bedTarget) == 1, 6, "Village has two Meadow beds and one cricket");
        Preview(game, 22f, WorldWeather.LightRain);
        yield return Wait(() => family.ActiveLoops(AudioLayer.Meadow) == 0 &&
            family.ActiveLoops(AudioLayer.MeadowCricket) == 0 && family.ActiveLoops(AudioLayer.MeadowWeather) == 1 &&
            family.ActiveLoops(AudioLayer.Rain) == 1 && family.ActiveLoops(AudioLayer.Night) == 0, 8,
            "Wet Meadow is exclusive while global Rain remains independent");
        var vendor = Read<WorldClimateAssetAdapter>(game, "worldClimateAssets") as WorldClimateVendorAdapter;
        yield return Wait(() => vendor != null && vendor.ActiveParticles > 0, 5, "Real Rain Maker emitter");
        Check(Mathf.Approximately(Read<float>(vendor.RainGraphic, "dropSize"), .60f),
            "Actual rain uses the doubled 0.60 drop scale");
        yield return capture("integrated-night-rain");
        pass("Wet Meadow fades out its dry loops while Rain Maker and global Rain remain active.");

        Preview(game, 12f, WorldWeather.HeavyRain);
        Check(game.TryTravelToWorldMap(StoryOrigin.HanVu.Forest), "Travel from Hàn Vụ to forest via real handler");
        yield return Wait(() => weatherAudio.CurrentMapProfile == "Forest" &&
            family.ActiveLoops(AudioLayer.MeadowWeather) == 0 && family.ActiveLoops(AudioLayer.Morning) == 3 &&
            family.ActiveLoops(AudioLayer.Primeval) == 0 && family.ActiveLoops(AudioLayer.Rain) == 1, 10,
            "Heavy rain forest plays Morning and global Rain while Primeval rests");
        Check(MapBgmController.Instance.GetComponents<AudioSource>().Any(x => x.isPlaying &&
            Resources.LoadAll<AudioClip>("WorldMapMusic").Contains(x.clip)), "Regular BGM still plays from local playlist");
        yield return capture("integrated-forest-heavy-rain");
        pass("Travel clears the village layers. Wet forest uses Morning and keeps global Rain separate from BGM.");
        yield return Ready(game, ui);
        Clear(game, combat);
        ui.SetPaused(false);
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
        int level = game.HeroData.currentLevel;
        int currentExp = game.HeroData.currentExp;
        Check(ExperienceProgress.TryRead(game.HeroData.experienceJson, out var before), "Read reserve before award");
        game.HeroData.AddExp(250);
        Check(ExperienceProgress.TryRead(game.HeroData.experienceJson, out var bank) &&
            ExperienceProgress.Amount(bank) == ExperienceProgress.Amount(before) + 250 &&
            game.HeroData.currentLevel == level && game.HeroData.currentExp == currentExp,
            "Runtime Hero banks EXP at the DEV Gate bottleneck");
        string banked = game.HeroData.experienceJson;
        Check(game.TryManualSave(SaveSlot.ManualSave2) && game.LoadAndContinue(SaveSlot.ManualSave2),
            "Save and Load the banked EXP together with story and climate");
        yield return Ready(game, ui);
        Check(game.HeroData.experienceJson == banked && StoryOrigin.Definition(game.HeroData) == StoryOrigin.HanVu &&
            game.HeroData.worldClimate.version == 1, "Load preserves EXP, origin and climate");
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
        Check(game.HeroData.experienceJson == banked, "Failed trial keeps the reserve");
        pass("Gate-ready Hero banks EXP, keeps it across Save/Load and a failed trial, and preserves origin and climate.");
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

    private static void Audit(Action<string> pass)
    {
        Check(EnvironmentAudioPlayer.Load() != null && WorldMapCatalog.All.Count > 100,
            "The real map catalog and verified audio manifest are loaded");
        var imported = Resources.LoadAll<AudioClip>("IntegratedAmbience");
        Check(imported.Length == 191 && imported.All(x => x.loadType == AudioClipLoadType.Streaming),
            "All original OGG and the dedicated Gate excerpt remain imported and streaming");
        var gate = Resources.Load<AudioClip>("IntegratedAmbience/GateThunder");
        Check(gate != null && gate.length > 3 && gate.length < 7, "Derived Gate one-shot is imported");
        pass($"{WorldMapCatalog.All.Count} maps are available with a verified environment manifest and the existing Gate clip.");
    }
    private static void Preview(GameManager game, float hour, WorldWeather weather)
    {
        Set(game, "previewWorldClimate", true);
        Set(game, "previewWorldHour", hour);
        Set(game, "previewWorldWeather", weather);
    }
}
#endif
