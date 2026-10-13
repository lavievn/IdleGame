using System;
using System.Collections.Generic;
using UnityEngine;
using TuTienCore;

partial class MotionRegression
{
    private static int GearOwnedModifierCount(EntityDataSO hero)
    {
        int result=0;
        if(hero.combatStatModifiers!=null) foreach(var mod in hero.combatStatModifiers)
            if(mod!=null && mod.sourceId!=null &&
                mod.sourceId.StartsWith(EquipmentFoundation.SourcePrefix,
                    StringComparison.Ordinal)) result++;
        return result;
    }

    static void MultiSlotEquipmentRegressionTests()
    {
        Run("0.0.52 exact rarity order and top-tier chance units",()=>{
            Check((int)EquipmentRarity.Common==0 &&
                (int)EquipmentRarity.Hoang==1 &&
                (int)EquipmentRarity.Huyen==2 &&
                (int)EquipmentRarity.Dia==3 &&
                (int)EquipmentRarity.Thien==4 &&
                (int)EquipmentRarity.Tien==5,"common hoang huyen dia thien tien");
            Near((float)EquipmentFoundation.ThienDropProbability,.00005f,
                "0.005 percent must be probability 0.00005");
            Near((float)EquipmentFoundation.TienDropProbability,.00001f,
                "0.001 percent must be probability 0.00001");
        });

        Run("0.0.52 extra affix slots are caps, not forced affix count",()=>{
            Check(EquipmentFoundation.MaximumExtraLines(1)==1,"level 1 max one extra");
            Check(EquipmentFoundation.MaximumExtraLines(11)==2,"level 11 max two");
            Check(EquipmentFoundation.MaximumExtraLines(200)==6,"caps at six");
            var sword=EquipmentFoundation.CreateDemoSword();
            sword.rarity=EquipmentRarity.Thien;
            Check(sword.extraStats.Count==0 && EquipmentFoundation.IsValid(sword),
                "high-rarity sword can spawn with zero extras");
            sword.extraStats.Add(new ItemStatLine {
                stat=CombatStatId.ElementDamagePercent,flat=.05f,element=ElementType.Hoa
            });
            Check(EquipmentFoundation.IsValid(sword),"one fire bonus is allowed at lv1");
            sword.extraStats.Add(new ItemStatLine {stat=CombatStatId.Armor,flat=3});
            Check(!EquipmentFoundation.IsValid(sword),"extra beyond cap rejected");
        });

        Run("0.0.52 slots, distinct rings, sword stance and idempotent effects",()=>{
            var hero=HeroAt(1);
            EquipmentInventory.InitializeNewRun(hero);
            EquipmentRuntime.InitializeNewRun(hero);
            int baseline=hero.GetCalculatedDamage(AttackMode.Melee);
            var sword=EquipmentFoundation.CreateDemoSword();
            Check(EquipmentRuntime.AddInstance(hero,sword),"sword added to bag");
            Check(!EquipmentRuntime.AddInstance(hero,sword),"same id cannot be duplicated");
            Check(!EquipmentRuntime.Equip(hero,sword.instanceId,EquipmentSlot.Head),
                "sword cannot occupy helmet slot");
            Check(EquipmentRuntime.Equip(hero,sword.instanceId,EquipmentSlot.Weapon),
                "sword equipped");
            Check(hero.GetCalculatedDamage(AttackMode.Melee)==baseline+10,
                "weapon bonus applies exactly once");
            Check(hero.GetCalculatedDamage(AttackMode.RangedPhysical)==baseline,
                "melee sword does not buff ranged stance");
            EquipmentRuntime.Normalize(hero);
            EquipmentRuntime.Normalize(hero);
            Check(hero.GetCalculatedDamage(AttackMode.Melee)==baseline+10 &&
                GearOwnedModifierCount(hero)==1,"normalization cannot double count sword");
            var r1=EquipmentFoundation.CreateCommon("base:ring",1,3f);
            var r2=EquipmentFoundation.CreateCommon("base:ring",1,5f);
            Check(r1.instanceId!=r2.instanceId,"identical templates have unique item ids");
            Check(EquipmentRuntime.AddInstance(hero,r1) &&
                  EquipmentRuntime.AddInstance(hero,r2),"two rings in bag");
            Check(EquipmentRuntime.Equip(hero,r1.instanceId,EquipmentSlot.RingLeft),
                "first ring in left slot");
            Check(EquipmentRuntime.Equip(hero,r2.instanceId,EquipmentSlot.RingRight),
                "second ring in right slot");
            Check(hero.GetCalculatedDamage(AttackMode.Melee)==baseline+18,
                "sword and two rings sum exactly once");
            Check(GearOwnedModifierCount(hero)==3,"one modifier per worn main stat");
            Check(EquipmentRuntime.Unequip(hero,EquipmentSlot.RingLeft) &&
                EquipmentRuntime.Find(hero,r1.instanceId)!=null &&
                !EquipmentRuntime.Find(hero,r1.instanceId).isEquipped,
                "ring goes back into bag without duplication");
            Check(hero.GetCalculatedDamage(AttackMode.Melee)==baseline+15,
                "ring modifier removed only from left slot");
            Check(EquipmentRuntime.Unequip(hero,EquipmentSlot.Weapon),
                "remove sword");
            Check(hero.GetCalculatedDamage(AttackMode.Melee)==baseline+5,
                "other ring remains active");
        });

        Run("0.0.52 all eight slots can be independently occupied and removed",()=>{
            var hero=HeroAt(1);
            EquipmentInventory.InitializeNewRun(hero);
            EquipmentRuntime.InitializeNewRun(hero);
            string[] templates={"sample:iron_sword","base:head","base:chest",
                "base:boots","base:artifact","base:necklace","base:ring","base:ring"};
            EquipmentSlot[] slots={EquipmentSlot.Weapon,EquipmentSlot.Head,
                EquipmentSlot.Chest,EquipmentSlot.Boots,EquipmentSlot.Artifact,
                EquipmentSlot.Necklace,EquipmentSlot.RingLeft,EquipmentSlot.RingRight};
            for(int i=0;i<slots.Length;i++)
            {
                var piece=EquipmentFoundation.CreateCommon(templates[i],1,i+1);
                Check(piece!=null && EquipmentRuntime.AddInstance(hero,piece),
                    "slot item created");
                Check(EquipmentRuntime.Equip(hero,piece.instanceId,slots[i]),
                    "slot accepts its own template");
            }
            Check(GearOwnedModifierCount(hero)==8,
                "each worn slot contributes once");
            foreach(var slot in slots)
            {
                Check(EquipmentRuntime.Equipped(hero,slot)!=null,
                    "occupied slot visible");
                Check(EquipmentRuntime.Unequip(hero,slot),
                    "slot can be cleared without losing the item");
            }
            Check(GearOwnedModifierCount(hero)==0 &&
                hero.equipmentItems.Count==8,
                "eight pieces remain in bag with zero active gear effects");
        });

        Run("0.0.52 crafting material stacks are not destroyed by v1 bag migration",()=>{
            var hero=HeroAt(1);
            EquipmentInventory.InitializeNewRun(hero);
            Check(EquipmentInventory.Add(hero,"ore:iron",3),
                "stackable unknown material accepted");
            EquipmentInventory.Normalize(hero);
            Check(EquipmentInventory.Count(hero,"ore:iron")==3,
                "material is not deleted for missing trinket catalog entry");
        });

        Run("0.0.52 rolled stats survive normalize without re-roll",()=>{
            var hero=HeroAt(1);
            EquipmentRuntime.InitializeNewRun(hero);
            var sword=EquipmentFoundation.CreateDemoSword();
            sword.rarity=EquipmentRarity.Thien;
            sword.mainStats[0].flat=18f;
            sword.extraStats.Add(new ItemStatLine {
                stat=CombatStatId.ElementDamagePercent,
                flat=.05f,element=ElementType.Hoa
            });
            Check(EquipmentRuntime.AddInstance(hero,sword),"rolled instance added");
            Check(EquipmentRuntime.Equip(hero,sword.instanceId,EquipmentSlot.Weapon),
                "rolled sword equipped");
            var on=EquipmentRuntime.Equipped(hero,EquipmentSlot.Weapon);
            Check(on.rarity==EquipmentRarity.Thien &&
                  on.mainStats[0].flat==18f &&
                  on.extraStats.Count==1,"rolled rarity and affixes retained");
            Check(GearOwnedModifierCount(hero)==2,"main and extra modifiers active");
            float fire=CombatStats.Get(hero,CombatStatId.ElementDamagePercent,0f,
                ElementType.Hoa,new[]{"stance:melee"});
            Near(fire,.05f,"fire bonus active");
            EquipmentRuntime.Normalize(hero);
            Check(GearOwnedModifierCount(hero)==2 &&
                  on.extraStats[0].flat==.05f,"re-normalize cannot reroll affixes");
            Check(!EquipmentRuntime.GrantDemoSword(hero) || hero.demoSwordGranted,
                "test reward can only be granted once");
        });

        Run("0.0.52 legacy talisman and modern artifact never double-occupy",()=>{
            var hero=HeroAt(1);
            EquipmentInventory.InitializeNewRun(hero);
            EquipmentRuntime.InitializeNewRun(hero);
            var artifact=EquipmentFoundation.CreateCommon("base:artifact",1,30f);
            Check(EquipmentRuntime.AddInstance(hero,artifact),"artifact in bag");
            Check(EquipmentInventory.Equip(hero,EquipmentCatalog.HealthCharm),
                "legacy HP charm works");
            Check(!EquipmentRuntime.Equip(hero,artifact.instanceId,EquipmentSlot.Artifact),
                "new Artifact can't silently stack with legacy HP charm");
            Check(EquipmentInventory.Unequip(hero),"legacy charm removed");
            Check(EquipmentRuntime.Equip(hero,artifact.instanceId,EquipmentSlot.Artifact),
                "modern artifact equipped");
            Check(!EquipmentInventory.Equip(hero,EquipmentCatalog.HealthCharm),
                "legacy charm blocked while new Artifact is worn");
        });

        Run("0.0.52 manual snapshot restores unique gear, rarity, affixes and professions",()=>{
            var f=new Field();SaveManager save;string dir;
            MenuManager(f,out save,out dir);
            try
            {
                var hero=HeroAt(7);
                EquipmentInventory.InitializeNewRun(hero);
                EquipmentRuntime.InitializeNewRun(hero);
                var sword=EquipmentFoundation.CreateDemoSword();
                sword.level=11;
                sword.rarity=EquipmentRarity.Thien;
                sword.refineLevel=3;
                sword.mainStats[0].flat=18f;
                sword.extraStats.Add(new ItemStatLine {
                    stat=CombatStatId.ElementDamagePercent,
                    flat=.05f,element=ElementType.Hoa
                });
                Check(EquipmentRuntime.AddInstance(hero,sword),"unique sword added");
                Check(EquipmentRuntime.Equip(hero,sword.instanceId,EquipmentSlot.Weapon),
                    "unique sword equipped");
                hero.gold=837L;
                hero.professionProgress.Add(new ProfessionProgress {
                    profession=SupportProfession.Forging,level=4,experience=99
                });
                hero.learnedRecipes.Add(new LearnedProfessionRecipe {
                    recipeId="forge:iron",learnedRank=2
                });
                hero.learnedSupportSkills.Add(new LearnedSupportSkill {
                    skillId="support:armor",level=3
                });
                int savedAttack=hero.GetCalculatedDamage(AttackMode.Melee);
                Check(save.SaveGame(hero,SaveSlot.ManualSave1),"saved snapshot");
                hero.currentLevel=19;hero.gold=0; // mutate active character
                EquipmentRuntime.Unequip(hero,EquipmentSlot.Weapon);
                var loaded=HeroAt(1);
                loaded.equipmentItems=new List<EquipmentInstance> {
                    EquipmentFoundation.CreateDemoSword()
                };
                Check(save.LoadGame(loaded,SaveSlot.ManualSave1),"loaded snapshot");
                var restored=EquipmentRuntime.Equipped(loaded,EquipmentSlot.Weapon);
                Check(loaded.currentLevel==7 && restored!=null,
                    "level and equipped weapon rolled back together");
                Check(restored.instanceId==sword.instanceId &&
                      restored.level==11 &&
                      restored.rarity==EquipmentRarity.Thien &&
                      restored.refineLevel==3,"same unique item and refinement retained");
                Check(restored.mainStats.Count==1 && restored.mainStats[0].flat==18f &&
                      restored.extraStats.Count==1 &&
                      restored.extraStats[0].element==ElementType.Hoa,
                    "main and optional elemental rolls retained");
                Check(loaded.GetCalculatedDamage(AttackMode.Melee)==savedAttack,
                    "loaded weapon effect not based on current future progression");
                Check(GearOwnedModifierCount(loaded)==2,
                    "rederived one modifier per actual line, without duplicate");
                Check(loaded.gold==837L &&
                      loaded.professionProgress.Count==1 &&
                      loaded.professionProgress[0].level==4 &&
                      loaded.learnedRecipes[0].learnedRank==2 &&
                      loaded.learnedSupportSkills[0].level==3,
                    "gold, forging, recipes and buffs survived the save");
                EquipmentRuntime.Normalize(loaded);
                Check(GearOwnedModifierCount(loaded)==2 &&
                      loaded.GetCalculatedDamage(AttackMode.Melee)==savedAttack,
                    "repeat migration cannot increase power");
            }
            finally {System.IO.Directory.Delete(dir,true);}
        });

        Run("0.0.52 professions and refinement are pure save schemas",()=>{
            var hero=HeroAt(1);
            EquipmentRuntime.InitializeNewRun(hero);
            hero.gold=135L;
            hero.professionProgress.Add(new ProfessionProgress{
                profession=SupportProfession.Forging,level=3,experience=27
            });
            hero.learnedRecipes.Add(new LearnedProfessionRecipe{
                recipeId="forge:iron",learnedRank=1
            });
            hero.learnedSupportSkills.Add(new LearnedSupportSkill{
                skillId="buff:armor",level=1
            });
            var recipe=new CraftingRecipeData {
                recipeId="forge:iron",profession=SupportProfession.Forging,
                requiredProfessionLevel=3,goldCost=15,
                materials=new List<CraftingMaterialCost>{
                    new CraftingMaterialCost{itemId="ore:iron",quantity=2}
                }
            };
            var rule=new RefinementRuleData {
                targetRefineLevel=2,successWeightPerMillion=500000,
                onFailure=RefinementFailureEffect.NoChange
            };
            Check(hero.gold==135 && hero.professionProgress.Count==1 &&
                hero.learnedRecipes.Count==1 && hero.learnedSupportSkills.Count==1 &&
                recipe.materials[0].quantity==2 &&
                rule.successWeightPerMillion==500000,
                "data can express gold, jobs, books, buffs and recipes without executing crafting");
        });
    }
}
