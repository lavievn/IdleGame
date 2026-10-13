using System;
using System.IO;
using UnityEngine;
using TuTienCore;

// Snapshot rollback test: intentionally recreate the reported sequence,
// including the gear, a level increase and resetting combat to full HP.
partial class MotionRegression
{
    static void SaveRollbackRegressionTests()
    {
        Run("0.0.52 manual slot rolls back level, exp, stats, map AND gear",()=>{
            var f=new Field();
            SaveManager save;string dir;
            var gm=MenuManager(f,out save,out dir);
            try
            {
                var hero=HeroAt(1);
                EquipmentInventory.InitializeNewRun(hero);
                Set(gm,"runtimeHeroData",hero);
                Set(gm,"hasDeployed",true);
                gm.preGameUI.SetActive(false);
                f.combat.SetupHeroInfo(hero);
                Check(gm.EquipInventoryItem(EquipmentCatalog.HealthCharm),
                    "wear +100 HP before save");
                int savedMax=f.combat.MaxHeroHP;
                int savedAttack=hero.GetCalculatedDamage(AttackMode.Melee);
                int savedMap=hero.mapNumber;
                int savedPoints=hero.statPoints;
                Check(gm.TryManualSave(SaveSlot.ManualSave1),"snapshot at level 1");
                string json=File.ReadAllText(Path.Combine(dir,"ManualSave1.json"));
                Check(json.Contains("\"currentLevel\":1") ||
                      json.Contains("\"currentLevel\": 1"),
                      "manual snapshot recorded the level, not only gear");

                // Allow many kills/changes after the save, then roll everything back.
                hero.AddExp(5000);
                Check(hero.currentLevel>1,"Hero levelled after the save");
                hero.AllocateDamage();
                hero.CompleteWave();
                Check(gm.UnequipInventoryItem(),"remove charm after saving");
                Check(gm.EquipInventoryItem(EquipmentCatalog.AttackCharm),
                    "change to ATK charm after saving");
                f.combat.CurrentHeroCombatState.CurrentHP=
                    f.combat.MaxHeroHP-40;
                Check(gm.LoadAndContinue(SaveSlot.ManualSave1),
                    "load the exact manually selected slot");
                Check(gm.HeroData.currentLevel==1,
                    "level MUST roll back from levelled run to snapshot level 1");
                Check(gm.HeroData.currentExp==0,"post-save EXP MUST roll back");
                Check(gm.HeroData.statPoints==savedPoints,
                    "post-save stat points MUST roll back");
                Check(gm.HeroData.addedDamage==0,"allocated attack MUST roll back");
                Check(gm.HeroData.mapNumber==savedMap,
                    "post-save map progress MUST roll back");
                Check(gm.HeroData.equippedItemId==EquipmentCatalog.HealthCharm,
                    "saved equipment restored");
                Check(gm.HeroData.GetCalculatedDamage(AttackMode.Melee)==savedAttack,
                    "saved ATK recalculated, not inherited from latest run");
                Check(f.combat.CurrentHeroCombatState.Data==gm.HeroData,
                    "combat state rebinds to loaded Hero, never old instance");
                Check(f.combat.MaxHeroHP==savedMax,
                    "combat maximum HP based on saved level and saved equipment");
                Check(f.combat.CurrentHeroHP==savedMax,
                    "current HP may intentionally refill on checkpoint load");
            }
            finally { Directory.Delete(dir,true); }
        });

        Run("0.0.52 loading old manual slot cannot overwrite its level from newer autosave",()=>{
            var f=new Field();SaveManager save;string dir;
            var gm=MenuManager(f,out save,out dir);
            try
            {
                var old=HeroAt(1);
                var later=HeroAt(10);
                Check(save.SaveGame(old,SaveSlot.ManualSave1),
                    "level 1 manual slot");
                Check(save.SaveGame(later,SaveSlot.AutoSave),
                    "level 10 autosave from same run");
                Check(gm.LoadAndContinue(SaveSlot.ManualSave1),
                    "explicit load slot 1");
                Check(gm.HeroData.currentLevel==1,
                    "explicit load MUST never substitute newest autosave");
                Check(f.combat.CurrentHeroCombatState.Data==gm.HeroData,
                    "combat references manually loaded character");
                Check(f.combat.MaxHeroHP==gm.HeroData.GetCalculatedHealth(
                        f.hero.attackMode),
                    "combat uses manually loaded level and stats");
                var latest=HeroAt(1);
                Check(save.LoadGame(latest,SaveSlot.AutoSave) &&
                      latest.currentLevel==10,
                    "autosave remains a distinct snapshot");
            }
            finally {Directory.Delete(dir,true);}
        });
    }
}
