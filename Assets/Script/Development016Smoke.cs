#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using TuTienCore;
using UnityEngine;

// Only the marker-owned Editor and Windows launchers invoke this scenario.
internal static class Development016Smoke
{
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static T Get<T>(object owner, string name) => (T)owner.GetType().GetField(name, Fields).GetValue(owner);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Fields).SetValue(owner, value);
    static void Call(object owner, string name, params object[] args) => owner.GetType().GetMethod(name, Fields).Invoke(owner, args);
    internal static void Check(bool good, string label) { if (!good) throw new InvalidOperationException(label); }

    internal static IEnumerator Wait(Func<bool> condition, float seconds, string label)
    {
        float until = Time.realtimeSinceStartup + seconds;
        while (!condition())
        {
            Check(Time.realtimeSinceStartup < until, "Timeout: " + label);
            yield return null;
        }
    }

    internal static IEnumerator Ready(GameManager game, UIManager ui)
    {
        yield return Wait(() => game.IsGameplayHUDVisible, 30, "MainGame entry");
        ui.SetPaused(true);
        yield return null;
    }

    internal static void Click(UIManager ui, string name)
    {
        var rect = Array.Find(Get<RectTransform>(ui, "hudCanvas").GetComponentsInChildren<RectTransform>(true),
            t => t.name == name && t.gameObject.activeInHierarchy);
        Check(rect != null, "Visible control " + name);
        Canvas.ForceUpdateCanvases();
        Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        var hit = ui.HitControl(point);
        Check(hit != null && hit.GetRect() == rect, "Hit region " + name);
        ui.HandleMouseClick(point);
    }

    internal static void Equip(GameManager game, string template)
    {
        var item = game.HeroData.equipmentItems.Find(x => x.templateId == template);
        Check(item != null && game.EquipGear(item.instanceId, EquipmentSlot.Weapon), "Equip " + template);
    }

    internal static void Clear(GameManager game, CombatManager combat)
    {
        Call(game, "CancelNextWave");
        combat.ForceClearAllMonsters();
        game.activeMonsters.Clear();
    }

    const int TestHP = 5000;
    internal static List<CombatManager.ActiveMonsterInfo> Wave(GameManager game, CombatManager combat,
        EnvironmentManager space, HeroController hero, params Vector2[] positions)
    {
        Clear(game, combat);
        Set(game, "retryWaveSize", positions.Length);
        Call(game, "CallNextWave");
        var enemies = new List<CombatManager.ActiveMonsterInfo>(Get<List<CombatManager.ActiveMonsterInfo>>(combat, "activeMonsters"));
        Check(enemies.Count == positions.Length, "Fixture uses real wave monsters");
        var origin = space.Position(hero.heroRect);
        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            enemy.currentHP = enemy.maxHP = TestHP;
            enemy.controller.moveSpeed = 0;
            enemy.attackTimer = 1000;
            enemy.controller.ReceiveAlert(hero, true);
            space.SetPosition(enemy.controller.Rect, origin + positions[i]);
        }
        combat.CurrentHeroCombatState.AttackTimer = 0;
        return enemies;
    }

    static void AddSkill(UIManager ui)
    {
        Click(ui, "WeaponSkillsButton");
        Click(ui, "SkillAdd");
        Click(ui, "SkillClose");
    }

    static string Snapshot(CombatSkillProgress progress) => JsonUtility.ToJson(progress);

    internal static IEnumerator Run(GameManager game, SaveManager saves, UIManager ui, CombatManager combat,
        HeroController hero, EnvironmentManager space, Func<string, IEnumerator> capture, Action<string> pass)
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        Check(saves.HasDevelopmentCultivationLoop, "Same Development cultivation profile");
        game.OnNewGameClicked();
        game.OnConfirmResetClicked();
        yield return Ready(game, ui);
        var actor = combat.CurrentHeroCombatState;
        var progress = game.HeroData.combatSkills;
        Check(actor.CurrentMana == 100 && actor.MaxMana == 100 && progress.queue.Count == 0, "New Mana and empty queue");
        Click(ui, "WeaponSkillsButton");
        Click(ui, "SkillGrantDev");
        Click(ui, "SkillGrantDev");
        Click(ui, "SkillClose");
        Check(game.HeroData.equipmentItems.FindAll(x => x.templateId.StartsWith("sample:skill-")).Count == 3 &&
            progress.queue.Count == 0, "Three explicit, idempotent grants without queuing");
        var common = EquipmentFoundation.CreateDemoStaff();
        Check(EquipmentRuntime.AddInstance(game.HeroData, common), "Owned Common Staff");
        Equip(game, common.templateId);
        progress.learned.Add(WeaponSkillRegistry.Fireball);
        Check(!WeaponSkillRegistry.Find(WeaponSkillRegistry.Fireball).IsAvailable(game.HeroData,
            WeaponBasicAttack.Equipped(game.HeroData)), "Learned data cannot enable equipment-only Fireball");
        progress.learned.Clear();
        Check(common.extraStats.Count == 0, "Common item main stats only");

        var enemies = Wave(game, combat, space, hero, new Vector2(-35, 0), new Vector2(-42, 0), new Vector2(-49, 0));
        long hits = combat.AcceptedHeroHitCount, skills = combat.WeaponSkillReleases;
        ui.SetPaused(false);
        yield return Wait(() => combat.PendingProjectileCount > 0, 5, "Staff basic release");
        var shot = Get<IList>(combat, "projectiles")[0];
        var locked = Get<CombatManager.ActiveMonsterInfo>(shot, "monster");
        space.SetPosition(locked.controller.Rect, space.Position(locked.controller.Rect) - new Vector2(45,0));
        yield return Wait(() => combat.AcceptedHeroHitCount > hits, 5, "Staff basic");
        ui.SetPaused(true);
        Check(locked.currentHP < TestHP && enemies.FindAll(x => x.currentHP < TestHP).Count == 1 && combat.AcceptedHeroHitCount == hits+1 &&
            combat.WeaponSkillReleases == skills && actor.CurrentMana == 100, "Common Staff single-target without Mana");
        pass("Common Staff basic is one accepted magical hit; no extra effect, learned leak or Mana spend.");

        Equip(game, EquipmentFoundation.SkillSampleIds[0]);
        enemies = Wave(game, combat, space, hero, new Vector2(-30,0), new Vector2(-100,30),
            new Vector2(150,0), new Vector2(-180,55), new Vector2(-260,0));
        hits = combat.AcceptedHeroHitCount;
        ui.SetPaused(false);
        yield return Wait(() => combat.AcceptedHeroHitCount > hits, 5, "unqueued Sword basic");
        ui.SetPaused(true);
        Check(combat.WeaponSkillReleases == skills && combat.AcceptedHeroHitCount == hits+1, "Equipment alone does not schedule skill");
        AddSkill(ui);
        enemies = Wave(game, combat, space, hero, new Vector2(-30,0), new Vector2(-100,30),
            new Vector2(150,0), new Vector2(-180,55), new Vector2(-260,0));
        hits = combat.AcceptedHeroHitCount;
        ui.SetPaused(false);
        yield return Wait(() => combat.WeaponSkillReleases > skills, 5, "Sword cast");
        ui.SetPaused(true);
        Check(combat.LastWeaponSkill == WeaponSkillRegistry.Thrust && combat.AcceptedHeroHitCount == hits+2 &&
            enemies[0].currentHP < TestHP && enemies[1].currentHP < TestHP &&
            enemies[2].currentHP == TestHP && enemies[3].currentHP == TestHP && enemies[4].currentHP == TestHP,
            $"Thrust hits each target in forward 240×84 exactly once: hits={combat.AcceptedHeroHitCount-hits}, HP=" +
            string.Join(",", enemies.ConvertAll(e=>e.currentHP)));
        float cooldown = progress.Remaining(WeaponSkillRegistry.Thrust);
        Check(cooldown > 4.9f && actor.CurrentMana == 100 && game.UnequipGear(EquipmentSlot.Weapon), "Sword cooldown, zero cost");
        Click(ui, "WeaponSkillsButton");
        Click(ui, "SkillQueueSlot0");
        Check(Get<TextMeshProUGUI>(ui, "skillRequirements").text.Contains("Không khả dụng"), "Unequipped queued skill is marked unavailable");
        Click(ui, "SkillClose");
        Equip(game, EquipmentFoundation.SkillSampleIds[0]);
        Check(progress.queue.Count == 1 && progress.Remaining(WeaponSkillRegistry.Thrust) == cooldown, "Same cooldown across re-equip");
        pass("Sword rectangle includes two and excludes behind, outside width and outside height; unqueued basic and cooldown through swap.");

        Equip(game, EquipmentFoundation.SkillSampleIds[1]);
        AddSkill(ui);
        enemies = Wave(game, combat, space, hero, new Vector2(-125,0), new Vector2(-180,0), new Vector2(80,0));
        hits = combat.AcceptedHeroHitCount;
        long arrows = combat.VolleyProjectiles;
        ui.SetPaused(false);
        yield return Wait(() => combat.VolleyProjectiles > arrows, 5, "first volley arrow");
        foreach (var enemy in enemies.GetRange(0,2))
        {
            var position = space.Position(enemy.controller.Rect);
            space.SetPosition(enemy.controller.Rect, position - new Vector2(45,0));
        }
        yield return Wait(() => combat.AcceptedHeroHitCount >= hits+3, 5, "three locked arrow impacts");
        ui.SetPaused(true);
        Check(combat.LastWeaponSkill == WeaponSkillRegistry.Volley && combat.VolleyProjectiles == arrows+3 &&
            combat.AcceptedHeroHitCount == hits+3 && enemies[2].currentHP == TestHP, "Exactly three aimed front shots");
        pass("Bow sequence fires three arrows; moving the eligible targets beyond old collision radius still hits, rear target untouched.");

        Equip(game, EquipmentFoundation.SkillSampleIds[2]);
        AddSkill(ui);
        enemies = Wave(game, combat, space, hero, new Vector2(-45,0), new Vector2(-90,0), new Vector2(-450,0));
        hits = combat.AcceptedHeroHitCount;
        skills = combat.WeaponSkillReleases;
        ui.SetPaused(false);
        yield return Wait(() => combat.WeaponSkillReleases > skills, 5, "Fireball release");
        Check(combat.LastWeaponSkill == WeaponSkillRegistry.Fireball && Math.Abs(actor.CurrentMana-95) < .05f, "Fireball charged five Mana once");
        actor.AttackTimer = 20;
        yield return Wait(() => combat.AcceptedHeroHitCount >= hits+2, 5, "legacy Fireball impact");
        ui.SetPaused(true);
        Check(combat.AcceptedHeroHitCount == hits+2 && enemies[0].currentHP < TestHP && enemies[1].currentHP < TestHP &&
            enemies[2].currentHP == TestHP, "Fireball AoE uses the legacy radius");
        Click(ui, "WeaponSkillsButton");
        Click(ui, "SkillQueueSlot2");
        Click(ui, "SkillUp");
        Check(progress.queue[1] == WeaponSkillRegistry.Fireball && progress.queue[2] == WeaponSkillRegistry.Volley,
            "Move selected skill earlier");
        yield return capture("016-weapon-skills");
        Click(ui, "SkillClose");
        progress.mana = 0;
        progress.cooldowns.Clear(); // Isolate resource gating from cooldown in this owned test only.
        enemies = Wave(game, combat, space, hero, new Vector2(-40,0), new Vector2(-60,0));
        hits = combat.AcceptedHeroHitCount;
        skills = combat.WeaponSkillReleases;
        ui.SetPaused(false);
        yield return Wait(() => combat.AcceptedHeroHitCount > hits, 5, "basic fallback when Mana is insufficient");
        ui.SetPaused(true);
        Check(combat.WeaponSkillReleases == skills && combat.AcceptedHeroHitCount == hits+1, "No stalling without Mana");
        pass("Fireball uses 5 Mana with legacy AoE; insufficient Mana falls back to single-target basic. Skill UI reorder works.");

        Clear(game, combat);
        actor.CurrentHP = actor.MaxHP - 100;
        progress.mana = 20;
        float began = Time.time;
        int hpBefore = actor.CurrentHP;
        ui.SetPaused(false);
        yield return Wait(() => Time.time - began >= 1.1f, 5, "exploration regeneration");
        ui.SetPaused(true);
        Check(actor.CurrentHP-hpBefore >= 9 && actor.CurrentHP-hpBefore <= 12 &&
            Math.Abs(progress.mana-20-2*(Time.time-began)) < .15f, "Exploration rates 10 HP/s and 2 Mana/s");
        var regenOpponent=Wave(game, combat, space, hero, new Vector2(-45,0))[0];
        regenOpponent.controller.attackMode=regenOpponent.cycleMode=AttackMode.Melee;
        ui.SetPaused(false);
        yield return Wait(() => combat.CastingWeaponSkill == WeaponSkillRegistry.Fireball, 5, "actual combat windup");
        hpBefore = actor.CurrentHP;
        yield return new WaitForSeconds(.5f);
        ui.SetPaused(true);
        Check(actor.CurrentHP == hpBefore, "No natural HP regeneration in combat");
        progress.cooldowns.Add(new CombatSkillCooldown { id = WeaponSkillRegistry.Fireball, seconds = 3.5f });
        string snapshot = Snapshot(progress);
        yield return new WaitForSecondsRealtime(.25f);
        Check(Snapshot(progress) == snapshot, "Pause cannot advance cooldown or resources");
        ui.ToggleSystemMenu();
        ui.SetPaused(false);
        yield return new WaitForSeconds(.25f);
        ui.SetPaused(true);
        ui.CloseSystemMenu();
        Check(Snapshot(progress) == snapshot, "System menu time does not advance resources");
        Check(game.TryManualSave(SaveSlot.ManualSave1) && game.LoadAndContinue(SaveSlot.ManualSave1) &&
            Snapshot(game.HeroData.combatSkills) == snapshot, "Save and immediate Load retain ordered queue, cursor, Mana, HP and cooldown");
        yield return Ready(game, ui);
        Check(CultivationSaveJson.TryObject(JsonUtility.ToJson(game.HeroData), out var fields, out _) &&
            fields.Remove("combatSkills"), "Prepare a pre-0.1.6 owned snapshot");
        var legacyJson = new System.Text.StringBuilder("{");
        foreach (var field in fields)
        {
            if (legacyJson.Length > 1) legacyJson.Append(',');
            legacyJson.Append('"').Append(field.Key).Append("\":").Append(field.Value);
        }
        File.WriteAllText(Path.Combine(saves.SaveDirectory, "ManualSave2.json"), legacyJson.Append('}').ToString());
        var oldHero = UnityEngine.Object.Instantiate(game.HeroData);
        Check(saves.LoadGame(oldHero, SaveSlot.ManualSave2) && oldHero.combatSkills.mana == 100 &&
            oldHero.combatSkills.health == -1 && oldHero.combatSkills.queue.Count == 0,
            "Real legacy load initializes absent component without inheriting runtime data");
        UnityEngine.Object.Destroy(oldHero);
        progress = game.HeroData.combatSkills;
        float mana = progress.mana;
        Call(combat, "ApplyIncomingDamage", 1000000, "Owned Retry fixture", null, null);
        Check(actor.CurrentHP == 0 && progress.mana == mana, "Death does not regenerate");
        Check(game.LoadAndContinue(SaveSlot.AutoSave) && actor.CurrentHP == 0 && !game.IsGameplayHUDVisible,
            "Loading a dead Hero does not resurrect it");
        game.OnRetryClicked();
        Check(actor.CurrentHP == actor.MaxHP && actor.CurrentMana == mana, "Explicit Retry revives HP without refilling Mana");
        yield return Ready(game, ui);
        pass("Canonical regeneration, pause, Save/Load and explicit Retry preserve the combat resources contract.");

        Equip(game, EquipmentFoundation.SkillSampleIds[0]);
        var candidate = UnityEngine.Object.Instantiate(game.HeroData);
        candidate.combatStatModifiers.Add(new CombatStatModifier { sourceId = "owned:016:gate",
            stat = CombatStatId.Attack, flat = 100 });
        var profile = saves.CultivationProfile;
        using (var binding = new CultivationGameplayBinding(candidate, profile,
            HeavenlyGateV13Fixtures.Session(profile, 180, 7))) { }
        Check(saves.SaveGame(candidate, SaveSlot.ManualSave2), "Owned gate source snapshot");
        UnityEngine.Object.Destroy(candidate);
        Check(game.LoadAndContinue(SaveSlot.ManualSave2), "Load gate source");
        yield return Ready(game, ui);
        Check(game.RequestTribulation(), "Enter real Gate");
        yield return Wait(() => !game.HasTribulationLiability, 45, "complete real gate with skills");
        Check(game.Cultivation.Current.Foundation.ActualRealmId == "realm:truc-co" &&
            game.Cultivation.Current.Reserve.Amount == 7 && game.HeroData.combatSkills.queue.Count == 3,
            "Trial victory preserves cultivation and skills");
        pass("Real Heavenly Gate settles and redeploys at Trúc Cơ with reserve and ordered skills intact.");
    }

    internal static void Replay(GameManager game, SaveManager saves, Action<string> pass)
    {
        var json = File.ReadAllText(Path.Combine(saves.SaveDirectory, "ManualSave1.json"));
        Check(CombatSkillProgress.TryRead(json, out var saved) && saved.queue.Count == 3 &&
            saved.mana < 100 && saved.health > 0 && saved.cooldowns.Count > 0,
            "Fresh process has the previous owned checkpoint");
        Check(game.LoadAndContinue(SaveSlot.ManualSave1) && Snapshot(game.HeroData.combatSkills) == Snapshot(saved),
            "Restart restores the exact queue, cursor, Mana, HP and cooldown without elapsed-time credit");
        pass("Fresh Windows process loads exact non-full Mana/HP, cooldown and queue cursor.");
    }
}
#endif
