#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using TMPro;
using TuTienCore;
using UnityEngine;
using UnityEngine.Video;

// Runs only through the marker-owned MainGame/Windows verification hosts.
internal static class Development015Smoke
{
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;

    static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Fields).GetValue(owner);

    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    static IEnumerator Wait(Func<bool> condition, float seconds, string label)
    {
        float until = Time.realtimeSinceStartup + seconds;
        while (!condition())
        {
            Check(Time.realtimeSinceStartup < until, "Timed out: " + label);
            yield return null;
        }
    }

    static IEnumerator Ready(GameManager game, UIManager ui)
    {
        yield return Wait(() => game.IsGameplayHUDVisible && game.Cultivation != null, 35, "Hero entry");
        ui.SetPaused(true);
        yield return null;
    }

    static void BeginFromCharacter(GameManager game, UIManager ui)
    {
        ui.OpenHeroStats();
        ui.RefreshCharacterProgression();
        Canvas.ForceUpdateCanvases();
        var summary = Field<TextMeshProUGUI>(ui, "progressionSummary");
        summary.ForceMeshUpdate();
        Check(summary.text.Contains("DEV 0.1.5") && summary.text.Contains("Viên mãn") &&
            !summary.isTextOverflowing, "Character shows DEV and the actual bottleneck");
        var button = Field<RectTransform>(ui, "trialButton");
        Vector2 point = RectTransformUtility.WorldToScreenPoint(null, button.TransformPoint(button.rect.center));
        var control = ui.HitControl(point);
        Check(control != null && control.GetRect() == button, "Độ Kiếp hit region");
        ui.HandleMouseClick(point);
        Check(game.TribulationFighting, "Character action enters the DEV trial: " + game.TribulationLockedReason);
    }

    static void GearForOutcome(GameManager game, CombatManager combat, WeaponFamily weapon)
    {
        DevelopmentV13GameplayScenario.Weapon(game, weapon);
        var data = game.HeroData;
        Check(WeaponBasicAttack.Family(WeaponBasicAttack.Equipped(data)) == weapon, "Actual weapon " + weapon);
        var actor = combat.CurrentHeroCombatState.Controller;
        int hp = weapon == WeaponFamily.Unarmed ? 25 : 450;
        data.combatStatModifiers.Add(new CombatStatModifier {
            sourceId = "owned:015:hp", stat = CombatStatId.MaxHealth,
            flat = hp - data.GetCalculatedHealth(WeaponBasicAttack.Mode(weapon))
        });
        if (weapon != WeaponFamily.Unarmed)
        {
            var stat = weapon == WeaponFamily.Staff ? CombatStatId.MagicalAttack : CombatStatId.Attack;
            data.combatStatModifiers.Add(new CombatStatModifier { sourceId = "owned:015:power", stat = stat, flat = 100 });
        }
        combat.RefreshRegisteredHeroStats(actor, true);
        Check(combat.CurrentHeroHP == hp, "Owned HP setup " + weapon + ": " + combat.CurrentHeroHP);
    }

    static void CheckCleanup(CombatManager combat)
    {
        Check(!combat.HeavenlyGateActive && combat.TrialView == null &&
            UnityEngine.Object.FindObjectsByType<HeavenlyTrialView>(FindObjectsInactive.Include).Length == 0 &&
            Array.FindAll(Resources.FindObjectsOfTypeAll<RenderTexture>(), x => x.name == "Heavenly Trial Sky Target").Length == 0,
            "Trial terrain, wall, video and RenderTexture cleaned");
        Check(UnityEngine.Object.FindObjectsByType<HeavenlyLightningVisual>(FindObjectsInactive.Include).Length == 0,
            "Lightning and committed break effect cleaned");
    }

    internal static IEnumerator Run(GameManager game, SaveManager saves, UIManager ui, CombatManager combat,
        string evidence, Func<string, IEnumerator> capture, Action<string> pass, bool windows)
    {
        Check(saves.HasDevelopmentCultivationLoop && saves.TrialConfiguration.IsDevelopment &&
            !CultivationIntegrationGate.HeavenlyTribulationEnabled, "Real DEV config and retail gate closed");
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        UnityEngine.Random.InitState(15015);
        game.OnNewGameClicked();
        game.OnConfirmResetClicked();
        yield return Ready(game, ui);
        Check(game.Cultivation.Current.Foundation.Amount < 1 && game.TribulationLockedReason == "Chưa tới bình cảnh",
            "New starts at Luyện Khí Sơ kỳ and locks the early trial");

        // Prove passive progression from New Game through the real Update.
        ui.SetPaused(false);
        Time.timeScale = 6;
        yield return Wait(() => game.Cultivation.Current.Foundation.Amount >= 2, 10, "passive cultivation");
        ui.SetPaused(true);
        pass("New Game gains cultivation through the live clock before any fixture grants.");
        double beforeMenu = game.Cultivation.ActiveSeconds;
        ui.ToggleSystemMenu();
        ui.SetPaused(false);
        yield return new WaitForSecondsRealtime(.25f);
        Check(game.Cultivation.ActiveSeconds == beforeMenu, "System menu does not advance the cultivation clock");
        ui.SetPaused(true);
        ui.CloseSystemMenu();

        // The one-hour balance is covered by the domain simulation. Seed only
        // an owned checkpoint so native verification can exercise the final
        // transition, Gate, combat and Save without waiting an hour.
        var candidate = UnityEngine.Object.Instantiate(game.HeroData);
        using (var binding = new CultivationGameplayBinding(candidate, saves.CultivationProfile,
            HeavenlyGateV13Fixtures.NearBottleneck(saves.CultivationProfile, remaining: 3))) { }
        Check(saves.SaveGame(candidate, SaveSlot.ManualSave2), "Write owned near-bottleneck fixture");
        UnityEngine.Object.Destroy(candidate);
        Check(game.LoadAndContinue(SaveSlot.ManualSave2), "Load owned near-bottleneck fixture");
        yield return Ready(game, ui);
        ui.SetPaused(false);
        Time.timeScale = 6;
        yield return Wait(() =>
        {
            if (game.Cultivation == null && !game.IsEnteringGameplay) game.OnRetryClicked();
            if (Time.timeScale > 0) Time.timeScale = 6;
            return game.Cultivation != null && game.TribulationLockedReason == "" &&
                game.Cultivation.Current.Reserve.Amount >= 2;
        }, 12, "natural cultivation from near-cap checkpoint");
        ui.SetPaused(true);
        Check(game.TryManualSave(SaveSlot.ManualSave2), "Owned bottleneck checkpoint");
        pass("Live cultivation reaches Luyện Khí Viên mãn from the owned near-cap checkpoint.");
        var families = windows
            ? new[] { WeaponFamily.Sword, WeaponFamily.Unarmed, WeaponFamily.Bow, WeaponFamily.Staff }
            : new[] { WeaponFamily.Sword, WeaponFamily.Unarmed };
        foreach (var weapon in families)
        {
            Check(game.LoadAndContinue(SaveSlot.ManualSave2), "Load bottleneck before " + weapon);
            yield return Ready(game, ui);
            GearForOutcome(game, combat, weapon);
            Check(ExperienceProgress.TryRead(game.HeroData.experienceJson, out var initialBank), "Read initial EXP bank");
            game.HeroData.AddExp(250);
            string banked = game.HeroData.experienceJson;
            Check(ExperienceProgress.TryRead(banked, out var bank) &&
                ExperienceProgress.Amount(bank) == ExperienceProgress.Amount(initialBank) + 250,
                "The live bottleneck banks an EXP award");
            var before = game.Cultivation.Current;
            var cap = game.Cultivation.Capacity;
            double reserve = before.Reserve.Amount;
            double loss = saves.TrialPenalty.Loss(game.Cultivation.Profile.Catalog, before);
            int level = game.HeroData.currentLevel;
            int exp = game.HeroData.currentExp;
            int points = game.HeroData.statPoints;
            string map = game.HeroData.worldMapId;
            long hits = combat.AcceptedHeroHitCount, strikes = combat.TrialStrikeCount;

            BeginFromCharacter(game, ui);
            string activeHash = saves.TrialSnapshotHash(SaveSlot.AutoSave);
            Check(!game.TryManualSave(SaveSlot.ManualSave1) && !game.LoadAndContinue(SaveSlot.ManualSave2) &&
                !game.TryTravelToWorldMap(map) && !game.ReturnToMainMenu() && !game.RequestTribulation() &&
                activeHash == saves.TrialSnapshotHash(SaveSlot.AutoSave), "Active guards retain exact checkpoint");
            if (weapon == WeaponFamily.Sword)
            {
                ui.ClickExitButton();
                Check(game.ShowTribulationNotice && game.TribulationFighting, "In-game exit is blocked");
#if DEVELOPMENT_BUILD && !UNITY_EDITOR
                Application.Quit(91);
                yield return null;
                Check(game.TribulationFighting, "Windows Application.wantsToQuit veto");
#endif
                saves.FailNextTrialWrite = "BeforeReplace";
            }
            var view = combat.TrialView;
#if DEVELOPMENT_BUILD && UNITY_STANDALONE_WIN && !UNITY_EDITOR
            var sky = weapon == WeaponFamily.Bow ? HeavenlyTrialView.SkyVariant.B : HeavenlyTrialView.SkyVariant.A;
            if (weapon == WeaponFamily.Sword || weapon == WeaponFamily.Bow) view.SetProbeSky(sky);
#endif
            yield return Wait(() => view.IsShowingVideo, 7, "Sky video first frame " + weapon);
            var video = view.GetComponent<VideoPlayer>();
            var audio = Field<AudioSource>(MapBgmController.Instance, "trialSource");
            Check(video != null && video.isLooping && audio != null && audio.isPlaying && audio.loop,
                "Active sky and encounter BGM " + weapon);
            var trialTrack = audio.clip;
            yield return Wait(() => !combat.HeavenlyGateApproaching, 26, "real Gate approach " + weapon);
            Check(combat.GateExplorationDistance >= CombatManager.TrialGateApproachDistance && view.GroundScroll > 0,
                "Hero walks to revealed wall on trial terrain");
            if (weapon == WeaponFamily.Sword)
            {
                yield return Wait(() => combat.TrialStrikeCount > strikes, 8, "authoritative lightning");
                yield return capture("015-trial-sword");
                yield return Wait(() => game.CanRetryTribulationCommit, 25, "failed terminal write");
                Check(activeHash == saves.TrialSnapshotHash(SaveSlot.AutoSave) &&
                    game.Cultivation.Current.Foundation.Amount == before.Foundation.Amount &&
                    game.HeroData.experienceJson == banked && game.HeroData.currentLevel == level,
                    "Failed terminal write publishes neither realm nor EXP");
                Check(game.RequestTribulation(), "Retry commits the same terminal candidate");
            }
            else yield return Wait(() => !game.HasTribulationLiability, 25, "automatic result " + weapon);

            bool won = weapon != WeaponFamily.Unarmed;
            var receipt = TribulationSaveTransaction.Read(game.HeroData.tribulationJson);
            var after = game.Cultivation.Current;
            var expectedRealm = won ? "realm:truc-co" : "realm:luyen-khi";
            Check(combat.AcceptedHeroHitCount > hits && receipt.appliedId == receipt.attemptId &&
                receipt.outcome == (won ? "Success" : "Failure") && after.Foundation.ActualRealmId == expectedRealm &&
                after.Foundation.Amount == (won ? 0 : before.Foundation.Amount - loss) &&
                after.Reserve.Amount == reserve && game.HeroData.worldMapId == map &&
                (won ? game.LastTribulationHeroHP > 0 : game.LastTribulationHeroHP == 0) &&
                combat.CurrentHeroHP > 0, "Real terminal HP, preserved snapshot and respawn " + weapon);
            if (won)
            {
                var expectedExp = ExperienceProgress.Amount(bank) + exp;
                int expectedLevel = level;
                while (expectedLevel < 999 && expectedExp >= CombatBalance.RequiredExp(expectedLevel))
                    expectedExp -= CombatBalance.RequiredExp(expectedLevel++);
                Check(ExperienceProgress.TryRead(game.HeroData.experienceJson, out var released) &&
                    ExperienceProgress.Amount(released) == 0 && released.lastReleaseAttempt == receipt.attemptId &&
                    released.lastReleaseRealm == expectedRealm && game.HeroData.currentLevel == expectedLevel &&
                    game.HeroData.currentExp == expectedExp && game.HeroData.statPoints == points + expectedLevel - level &&
                    !game.RetryTribulationCommit(), "Successful commit releases the bank exactly once through Level progression");
                Check(game.Cultivation.Capacity.Absorption > cap.Absorption &&
                    game.Cultivation.Capacity.Refinement > cap.Refinement, "Trúc Cơ Sơ kỳ capacity");
            }
            else
                Check(game.HeroData.experienceJson == banked && game.HeroData.currentLevel == level &&
                    game.HeroData.currentExp == exp && game.HeroData.statPoints == points, "Failure preserves the bank");

            yield return Ready(game, ui);
            yield return Wait(() => UnityEngine.Object.FindObjectsByType<HeavenlyLightningVisual>(FindObjectsInactive.Include).Length == 0,
                5, "visual cleanup");
            yield return Wait(() => Array.TrueForAll(MapBgmController.Instance.GetComponents<AudioSource>(),
                source => !source.isPlaying || source.clip != trialTrack), 8, "trial BGM fade-out");
            CheckCleanup(combat);
            if (weapon == WeaponFamily.Sword)
            {
                string released = game.HeroData.experienceJson;
                int releasedLevel = game.HeroData.currentLevel;
                int releasedExp = game.HeroData.currentExp;
                int releasedPoints = game.HeroData.statPoints;
                Check(game.LoadAndContinue(SaveSlot.AutoSave), "Reload the durable successful terminal");
                yield return Ready(game, ui);
                Check(game.HeroData.experienceJson == released && game.HeroData.currentLevel == releasedLevel &&
                    game.HeroData.currentExp == releasedExp && game.HeroData.statPoints == releasedPoints,
                    "Loading the successful terminal does not release or roll growth again");
                Check(game.TryManualSave(SaveSlot.ManualSave1), "Persist successful terminal for fresh process");
                ui.OpenHeroStats();
                ui.RefreshCharacterProgression();
                Check(Field<TextMeshProUGUI>(ui, "progressionSummary").text.Contains("Trúc Cơ · Sơ kỳ"),
                    "Character shows actual Trúc Cơ Sơ kỳ");
                yield return capture("015-truc-co-character");
                ui.CloseDamageInfo();
                ui.SetPaused(false);
                yield return Wait(() => game.Cultivation.Current.Foundation.Amount > 0, 5, "continuing Trúc Cơ cultivation");
                ui.SetPaused(true);
            }
            pass(weapon + " real basic hit and " + receipt.outcome + ", respawn, preserved Qi/EXP/map, video/VFX/BGM cleanup.");
        }
        File.WriteAllText(Path.Combine(evidence, "dev-015-config.txt"),
            saves.CultivationProfile.Id + "\n" + JsonUtility.ToJson(saves.TrialConfiguration));
        if (!windows) yield break;

        Check(game.LoadAndContinue(SaveSlot.ManualSave2), "Restore owned source for interrupted trial");
        yield return Ready(game, ui);
        Check(game.RequestTribulation(), "Start interruption fixture on same DEV configuration");
        game.TribulationCombatInvalidated(); // Retain Active and let the owned process exit.
        pass("Simulated interruption retained the exact Active checkpoint for a new Windows process.");
    }

    internal static IEnumerator Replay(GameManager game, SaveManager saves, UIManager ui, Action<string> pass)
    {
        Check(saves.TryReadInterruptedDevelopmentTrial(out var source, out _, out var hash, out _),
            "Fresh process reads valid Active");
        Check(!game.LoadAndContinue(SaveSlot.ManualSave2) && !saves.ResetAutoSave(),
            "Active prevents rollback to manual or reset");
        game.OnContinueClicked();
        Check(game.HasPendingConfirmation && hash == saves.TrialSnapshotHash(SaveSlot.AutoSave), "Offer never writes");
        game.OnCancelResetClicked();
        Check(hash == saves.TrialSnapshotHash(SaveSlot.AutoSave), "Cancel preserves Active bytes");

        game.OnContinueClicked();
        saves.FailNextTrialWrite = "BeforeReplace";
        game.OnConfirmResetClicked();
        Check(game.HasPendingConfirmation && game.Cultivation == null &&
            hash == saves.TrialSnapshotHash(SaveSlot.AutoSave), "Failed recovery leaves Active and no Hero");
        game.OnCancelResetClicked();
        game.OnContinueClicked();
        game.OnConfirmResetClicked();
        yield return Ready(game, ui);
        var result = TribulationSaveTransaction.Read(game.HeroData.tribulationJson);
        Check(result.attemptId == source.attemptId && result.appliedId == result.attemptId &&
            result.outcome == "Failure" && result.penalty == saves.TrialPenalty.RequestedLoss &&
            game.Cultivation.Current.Foundation.ActualRealmId == "realm:luyen-khi", "Explicit recovery settles one failure");
        string state = game.HeroData.cultivationJson;
        Check(game.LoadAndContinue(SaveSlot.AutoSave), "Load terminal from same recovery");
        yield return Ready(game, ui);
        Check(state == game.HeroData.cultivationJson, "Failure replay has no second penalty");
        Check(game.LoadAndContinue(SaveSlot.ManualSave1), "Load successful terminal from previous process");
        yield return Ready(game, ui);
        Check(game.Cultivation.Current.Foundation.ActualRealmId == "realm:truc-co" &&
            game.Cultivation.Current.Foundation.Amount < 1, "Success remains Trúc Cơ, no offline gain");
        pass("Fresh Windows process: Cancel and failed write preserve Active; explicit recovery commits once, then failure/success load without reapplying.");
    }
}
#endif
