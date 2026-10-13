using System;
using System.IO;
using UnityEngine;
using TuTienCore;

partial class MotionRegression
{
    private static int EquipmentModifierCount(EntityDataSO hero)
    {
        int count=0;
        if (hero.combatStatModifiers != null)
            foreach(var m in hero.combatStatModifiers)
                if (m != null && m.sourceId == EquipmentInventory.ModifierSource) count++;
        return count;
    }

    static void InventoryEquipmentRegressionTests()
    {
        Run("0.0.52 new Hero receives exactly two samples; grants are once only",()=>{
            var hero=HeroAt(1);
            EquipmentInventory.InitializeNewRun(hero);
            Check(EquipmentInventory.Count(hero,EquipmentCatalog.HealthCharm)==1 &&
                  EquipmentInventory.Count(hero,EquipmentCatalog.AttackCharm)==1,
                  "both starter items in inventory");
            Check(hero.testItemsGranted &&
                  !EquipmentInventory.GrantSamples(hero),"cannot farm sample grants");
            Check(string.IsNullOrEmpty(hero.equippedItemId),
                  "starts with an empty equipment slot");
            Check(EquipmentModifierCount(hero)==0,"empty slot grants no stat");
        });

        Run("0.0.52 equip, swap, unequip never accumulates item modifiers",()=>{
            var hero=HeroAt(1);
            int health=hero.GetCalculatedHealth(AttackMode.Melee);
            int attack=hero.GetCalculatedDamage(AttackMode.Melee);
            EquipmentInventory.InitializeNewRun(hero);
            hero.AddCombatModifier(new CombatStatModifier {
                sourceId="test_bow", stat=CombatStatId.Attack,flat=3,
                requiredTags=new System.Collections.Generic.List<string>{"stance:bow"}
            });
            Check(EquipmentInventory.Equip(hero,EquipmentCatalog.HealthCharm),"equip health charm");
            Check(hero.GetCalculatedHealth(AttackMode.Melee)==health+100,
                  "health charm gives exactly +100");
            Check(EquipmentInventory.Count(hero,EquipmentCatalog.HealthCharm)==0,
                  "equipped item is not duplicated in bag");
            Check(EquipmentModifierCount(hero)==1,"one equipment modifier");
            Check(EquipmentInventory.Equip(hero,EquipmentCatalog.AttackCharm),"swap into attack charm");
            Check(hero.GetCalculatedHealth(AttackMode.Melee)==health,
                  "removing health charm restores base health");
            Check(hero.GetCalculatedDamage(AttackMode.Melee)==attack+20,
                  "attack charm gives exactly +20");
            Check(EquipmentInventory.Count(hero,EquipmentCatalog.HealthCharm)==1 &&
                  EquipmentInventory.Count(hero,EquipmentCatalog.AttackCharm)==0,
                  "swapping puts old charm back into shared bag");
            Check(hero.GetCalculatedDamage(AttackMode.RangedPhysical)==attack+23,
                  "other conditional modifiers remain untouched");
            Check(EquipmentModifierCount(hero)==1,
                  "swapping never stacks equipment modifiers");
            Check(EquipmentInventory.Unequip(hero),"unequip current charm");
            Check(hero.GetCalculatedDamage(AttackMode.Melee)==attack,
                  "unequipping restores baseline ATK");
            Check(EquipmentModifierCount(hero)==0,"no lingering item modifier");
            Check(EquipmentInventory.Count(hero,EquipmentCatalog.AttackCharm)==1,
                  "unequipped item returned to bag");
            Check(!EquipmentInventory.Unequip(hero),"double unequip is harmless");
            Check(!EquipmentInventory.Equip(hero,"invalid:item"),"invalid items rejected");
        });

        Run("0.0.52 GameManager equip recalculates HP with its existing ratio rule",()=>{
            var f=new Field();
            SaveManager saves;string dir;
            var gm=MenuManager(f,out saves,out dir);
            try
            {
                var hero=HeroAt(1);
                EquipmentInventory.InitializeNewRun(hero);
                Set(gm,"runtimeHeroData",hero);
                gm.preGameUI.SetActive(false);
                Set(gm,"hasDeployed",true);
                f.combat.SetupHeroInfo(hero);
                var state=f.combat.CurrentHeroCombatState;
                int originalMax=state.MaxHP;
                state.CurrentHP=originalMax/2;
                Check(gm.EquipInventoryItem(EquipmentCatalog.HealthCharm),
                    "equip through game manager");
                Check(state.MaxHP==originalMax+100,
                    "combat state refreshed after gear mutation");
                Check(state.CurrentHP==UnityEngine.Mathf.RoundToInt((originalMax/2f/originalMax)*(originalMax+100)),
                    "gear changes preserve current HP percentage");
                Check(gm.UnequipInventoryItem(),"unequip through game manager");
                Check(state.MaxHP==originalMax,"max HP returns to baseline");
                Check(state.CurrentHP<=state.MaxHP,"remaining HP cannot exceed max");
            }
            finally { Directory.Delete(dir,true); }
        });

        Run("0.0.52 SaveManager restores bag and item modifier once, including manual slot",()=>{
            var f=new Field();
            SaveManager save;string dir;
            MenuManager(f,out save,out dir);
            try
            {
                var hero=HeroAt(8);
                EquipmentInventory.InitializeNewRun(hero);
                Check(EquipmentInventory.Equip(hero,EquipmentCatalog.AttackCharm),"equipped");
                int expected=hero.GetCalculatedDamage(AttackMode.RangedPhysical);
                Check(save.SaveGame(hero,SaveSlot.ManualSave1),"manual save saved bag and equipment");
                var loaded=HeroAt(1);
                loaded.equippedItemId=EquipmentCatalog.HealthCharm; // template contamination
                loaded.inventory.Add(new InventoryStack {
                    itemId=EquipmentCatalog.HealthCharm,quantity=88
                });
                Check(save.LoadGame(loaded,SaveSlot.ManualSave1),"read manual slot");
                Check(loaded.equippedItemId==EquipmentCatalog.AttackCharm,
                    "saved equipment overrode template");
                Check(EquipmentInventory.Count(loaded,EquipmentCatalog.HealthCharm)==1,
                    "bag restored from save, not template");
                Check(EquipmentInventory.Count(loaded,EquipmentCatalog.AttackCharm)==0,
                    "worn gear not duplicated in inventory");
                Check(loaded.testItemsGranted,"one-time grant flag persisted");
                Check(loaded.GetCalculatedDamage(AttackMode.RangedPhysical)==expected,
                    "equipment ATTACK bonus immediately active on load");
                Check(EquipmentModifierCount(loaded)==1,"only one derived item modifier");
                EquipmentInventory.Normalize(loaded);
                Check(EquipmentModifierCount(loaded)==1 &&
                      loaded.GetCalculatedDamage(AttackMode.RangedPhysical)==expected,
                    "normalizing save twice never duplicates gear bonus");
            }
            finally { Directory.Delete(dir,true); }
        });

        Run("0.0.52 old save safely migrates to empty bag without template items",()=>{
            var f=new Field();
            SaveManager save;string dir;
            MenuManager(f,out save,out dir);
            try
            {
                File.WriteAllText(Path.Combine(dir,"AutoSave.json"),
                    "{\"currentLevel\":10,\"currentExp\":0}");
                var template=HeroAt(1);
                EquipmentInventory.InitializeNewRun(template);
                EquipmentInventory.Equip(template,EquipmentCatalog.HealthCharm);
                Check(save.LoadGame(template,SaveSlot.AutoSave),"0.0.51 save loads");
                Check(template.currentLevel==10,"progress preserved");
                Check(template.inventory.Count==0 &&
                      string.IsNullOrEmpty(template.equippedItemId),
                    "old save doesn't inherit equipment from template");
                Check(EquipmentModifierCount(template)==0,
                    "old save has no phantom equipped modifiers");
                Check(!template.testItemsGranted,"old run can claim samples once");
                Check(EquipmentInventory.GrantSamples(template) &&
                      !EquipmentInventory.GrantSamples(template),
                    "old run can claim samples exactly once");
            }
            finally { Directory.Delete(dir,true); }
        });
    }
}
