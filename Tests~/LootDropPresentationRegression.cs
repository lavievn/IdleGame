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
    }
}
