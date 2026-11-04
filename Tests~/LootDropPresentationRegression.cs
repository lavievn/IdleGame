using System;
using UnityEngine;

partial class MotionRegression
{
    static void WithLootSprite(Action action)
    {
        Resources.Register("Loot/iron_sword", new Sprite());
        try { action(); }
        finally { Resources.Unregister("Loot/iron_sword"); }
    }
    static int VisibleLoot(Field f) { return f.combat.LootDropRoots == null ? 0 : f.combat.LootDropRoots.Count; }
    static void LootDropPresentationTests()
    {
        Run("loot visuals follow accepted reward and captured foot before final victim is pooled", () => WithLootSprite(() => {
            GameManager gm; var f = LootField(true, out gm); var monster = f.Monster(-20);
            LootBattle(f, gm, monster); var victim = f.Enemies[0];
            Vector2 foot = f.space.Position(monster.Rect);
            Lethal(f, victim);
            Check(gm.HeroData.equipmentItems.Count == 1 && VisibleLoot(f) == 1, "one reward, one visual");
            var root = f.combat.LootDropRoots[0];
            Near(root.anchoredPosition.x, foot.x, "death foot X"); Near(root.anchoredPosition.y, foot.y, "death foot Y");
            Check(gm.IsWaitingForExploration && !monster.gameObject.activeSelf, "last kill completes normally");
            DeathCallback(f, victim); Lethal(f, victim);
            Check(VisibleLoot(f) == 1 && gm.HeroData.equipmentItems.Count == 1, "duplicate callback creates neither reward nor visual");
            monster.Rect.anchoredPosition += new Vector2(300, 40);
            Near(root.anchoredPosition.x, foot.x, "pool movement cannot move loot");
        }));
        Run("loot visuals do not appear for disabled rewards or nonlethal callbacks", () => WithLootSprite(() => {
            GameManager gm; var f = LootField(false, out gm); var monster = f.Monster(-20);
            LootBattle(f, gm, monster); var victim = f.Enemies[0]; DeathCallback(f, victim);
            Check(VisibleLoot(f) == 0, "nonlethal callback has no visual");
            Lethal(f, victim); Check(VisibleLoot(f) == 0 && gm.HeroData.equipmentItems.Count == 0, "NoDrop has no visual");
        }));
        Run("loot visuals use environment pan, survive wave cleanup and clear at hard boundaries", () => WithLootSprite(() => {
            GameManager gm; var f = LootField(true, out gm); var monster = f.Monster(-20);
            LootBattle(f, gm, monster); Lethal(f, f.Enemies[0]);
            var root = f.combat.LootDropRoots[0]; float x = root.anchoredPosition.x;
            f.combat.PanEffects(19f); Near(root.anchoredPosition.x, x, "camera-only pan must not apply twice");
            f.space.PanEnvironment(-31f); Near(root.anchoredPosition.x, x - 31f, "actual floor pan");
            f.combat.ClearWaveKeepingMagicVisuals(); Check(VisibleLoot(f) == 1, "wave cleanup retains resting loot");
            f.combat.StartBattle(new System.Collections.Generic.List<GameObject>(), new EntityDataSO());
            Check(VisibleLoot(f) == 1, "new wave retains previous reward graphic");
            f.combat.ForceClearAllMonsters(); Check(VisibleLoot(f) == 0 && !root.gameObject.activeSelf, "hard clear hides immediately");
        }));
        Run("loot missing art never blocks inventory reward or wave completion", () => {
            Resources.Unregister("Loot/iron_sword");
            GameManager gm; var f = LootField(true, out gm); var monster = f.Monster(-20);
            LootBattle(f, gm, monster); Lethal(f, f.Enemies[0]);
            Check(gm.HeroData.equipmentItems.Count == 1 && gm.IsWaitingForExploration && VisibleLoot(f) == 0,
                "presentation failure cannot undo reward or block death");
        });
        Run("loot award failure and repeated death cannot create a visual or retry insertion", () => WithLootSprite(() => {
            GameManager gm; var f = LootField(true, out gm); var monster = f.Monster(-20);
            LootBattle(f, gm, monster); var victim = f.Enemies[0];
            gm.HeroData.equipmentItems = null; gm.HeroData.isDirty = false;
            Lethal(f, victim);
            // An accepted death still credits EXP/wave progress and marks dirty;
            // only the failed item insertion is required to leave inventory alone.
            Check(gm.HeroData.isDirty, "accepted death dirties progress despite loot insertion failure");
            string completed = JsonUtility.ToJson(gm.HeroData, true);
            DeathCallback(f, victim); Lethal(f, victim);
            Check(VisibleLoot(f) == 0 && gm.HeroData.equipmentItems == null,
                "failed award stays terminal with no visual or inventory insertion");
            Check(JsonUtility.ToJson(gm.HeroData, true) == completed, "duplicate death leaves all completed results unchanged");
            Check(gm.IsWaitingForExploration, "failed award still completes final death normally");
        }));
        Run("simultaneous loot graphics retain independent captured feet and never award on animation cleanup", () => WithLootSprite(() => {
            GameManager gm; var f = LootField(true, out gm);
            var a = f.Monster(-20); var b = f.Monster(-200); var c = f.Monster(-380);
            LootBattle(f, gm, a, b, c);
            var victims = new System.Collections.Generic.List<CombatManager.ActiveMonsterInfo>(f.Enemies);
            foreach (var victim in victims) Lethal(f, victim);
            Check(VisibleLoot(f) == 3 && gm.HeroData.equipmentItems.Count == 3, "one graphic for each successful award");
            var roots = f.combat.LootDropRoots;
            Check(roots[0] != roots[1] && roots[1] != roots[2], "separate graphic lifetimes");
            Near(roots[0].anchoredPosition.x, -20, "first captured foot");
            Near(roots[1].anchoredPosition.x, -200, "second captured foot");
            Near(roots[2].anchoredPosition.x, -380, "third captured foot");
            f.combat.GetComponent<LootDropPresentation>().Advance(2f, null);
            Check(VisibleLoot(f) == 0 && gm.HeroData.equipmentItems.Count == 3, "completion grants nothing");
            f.combat.ForceClearAllMonsters();
            Check(gm.HeroData.equipmentItems.Count == 3, "cleanup grants nothing");
        }));
    }
}
