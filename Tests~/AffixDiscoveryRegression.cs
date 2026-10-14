using System;
using System.Collections.Generic;
using UnityEngine;
using TuTienCore;

partial class MotionRegression
{
    private static EquipmentInstance DiscoverySword()
    {
        var item=EquipmentFoundation.CreateDemoSword();
        item.affixSchemaVersion=EquipmentFoundation.AffixSchemaVersion;
        item.itemElement=ElementType.Kim;
        item.potentialRarity=EquipmentRarity.Tien;
        item.rarity=EquipmentRarity.Common; // former celestial relic can look mundane
        item.affixSlotCount=3;
        item.naturalRefineLimit=5;
        item.refineLevel=1;
        item.highestRefineLevel=3;
        item.hasBeenRefined=true;
        item.extraStats.Add(new ItemStatLine {
            stat=CombatStatId.Attack,flat=2f,
            revealed=true,requiredRefineLevel=0
        });
        item.extraStats.Add(new ItemStatLine {
            stat=CombatStatId.Attack,flat=7f,
            revealed=true,requiredRefineLevel=3
        });
        item.extraStats.Add(new ItemStatLine {
            stat=CombatStatId.Attack,flat=11f,
            revealed=false,requiredRefineLevel=4
        });
        return item;
    }

    static void AffixDiscoveryRegressionTests()
    {
        Run("0.0.52 latent affixes have exactly 3 public states and never reroll",()=>{
            var item=DiscoverySword();
            Check(EquipmentFoundation.IsValid(item),"latent item valid");
            Check(EquipmentFoundation.AffixState(item,item.extraStats[0])==
                EquipmentAffixState.Active,"revealed low requirement active");
            Check(EquipmentFoundation.AffixState(item,item.extraStats[1])==
                EquipmentAffixState.KnownLocked,"once-seen line relocked on downgrade");
            Check(EquipmentFoundation.AffixState(item,item.extraStats[2])==
                EquipmentAffixState.Hidden,"never-seen line hidden");
            float rolled=item.extraStats[2].flat;
            item.refineLevel=4;
            item.highestRefineLevel=4;
            Check(EquipmentFoundation.RevealEligibleAffixes(item),
                "only a milestone unveils eligible hidden affix");
            Check(EquipmentFoundation.AffixState(item,item.extraStats[2])==
                EquipmentAffixState.Active,"milestone line works");
            item.refineLevel=1;
            Check(EquipmentFoundation.AffixState(item,item.extraStats[2])==
                EquipmentAffixState.KnownLocked &&
                item.extraStats[2].flat==rolled &&
                item.extraStats[2].revealed &&
                item.highestRefineLevel==4,
                "refinement failure never hides known content or changes roll");
        });

        Run("0.0.52 latent hidden and known lines never count as combat buffs",()=>{
            var hero=HeroAt(1);
            EquipmentInventory.InitializeNewRun(hero);
            EquipmentRuntime.InitializeNewRun(hero);
            int baseAttack=hero.GetCalculatedDamage(AttackMode.Melee);
            var item=DiscoverySword();
            Check(EquipmentRuntime.AddInstance(hero,item),"add latent sword");
            var owned=EquipmentRuntime.Find(hero,item.instanceId);
            Check(owned!=item && owned.extraStats[2]!=item.extraStats[2] &&
                !owned.extraStats[2].revealed,"clone retains hidden state");
            Check(EquipmentRuntime.Equip(hero,item.instanceId,EquipmentSlot.Weapon),
                "equip newly discovered sword");
            Check(hero.GetCalculatedDamage(AttackMode.Melee)==baseAttack+12,
                "only main +10 and first active +2 count");
            EquipmentStatComparison compare=EquipmentStatPreview.Current(hero,AttackMode.Melee);
            Check(compare.Find(CombatStatId.Attack).equippedBonus==12,
                "preview agrees with actual unlock state");
            owned.refineLevel=4;owned.highestRefineLevel=4;
            Check(EquipmentFoundation.RevealEligibleAffixes(owned),"unveil third line");
            Check(EquipmentRuntime.RebuildModifiers(hero),"rebuild after milestone");
            Check(hero.GetCalculatedDamage(AttackMode.Melee)==baseAttack+30,
                "all three affixes now active: main 10 +2 +7 +11");
            owned.refineLevel=1;
            Check(EquipmentRuntime.RebuildModifiers(hero),"rebuild after downgrade");
            Check(hero.GetCalculatedDamage(AttackMode.Melee)==baseAttack+12,
                "downgrade drops bonus without deleting known affix");
            Check(owned.extraStats.Count==3 &&
                owned.extraStats[1].revealed && owned.extraStats[2].revealed,
                "number of rolled lines fixed forever");
        });

        Run("0.0.52 old v2 affixes migrate as known and stay equipped",()=>{
            var hero=HeroAt(1);
            EquipmentInventory.InitializeNewRun(hero);
            EquipmentRuntime.InitializeNewRun(hero);
            var sword=EquipmentFoundation.CreateDemoSword();
            sword.refineLevel=3;
            sword.extraStats.Add(new ItemStatLine {stat=CombatStatId.Attack,flat=4f});
            Check(EquipmentRuntime.AddInstance(hero,sword),"legacy v2 item");
            Check(EquipmentRuntime.Equip(hero,sword.instanceId,EquipmentSlot.Weapon),
                "legacy sword equipped");
            int current=hero.GetCalculatedDamage(AttackMode.Melee);
            var instance=EquipmentRuntime.Find(hero,sword.instanceId);
            instance.extraStats[0].revealed=false; // simulate missing bool in old JSON
            EquipmentRuntime.Normalize(hero);
            Check(instance.affixSchemaVersion==EquipmentFoundation.AffixSchemaVersion
                && instance.affixSlotCount==1 && instance.extraStats[0].revealed,
                "migration preserves existing rolled line as discovered");
            Check(instance.highestRefineLevel==3 && instance.hasBeenRefined &&
                instance.extraStats[0].requiredRefineLevel==0 &&
                hero.GetCalculatedDamage(AttackMode.Melee)==current,
                "migration does not change bonuses or item strength");
        });

        Run("0.0.52 natural potential and dangerous breakthrough are separate",()=>{
            var sword=DiscoverySword();
            sword.refineLevel=8;
            sword.highestRefineLevel=8;
            Check(EquipmentFoundation.IsValid(sword) &&
                sword.naturalRefineLimit==5 &&
                sword.potentialRarity==EquipmentRarity.Tien &&
                sword.rarity==EquipmentRarity.Common,
                "schema can represent risky breakthrough beyond natural cap");
            var normal=new RefinementRuleData {
                method=RefinementMethod.Normal,
                onFailure=RefinementFailureEffect.DestroyItem
            };
            Check(!EquipmentFoundation.IsSafeRefinementRule(normal),
                "normal crafting cannot destroy equipment");
            normal.onFailure=RefinementFailureEffect.LoseRefineLevel;
            Check(EquipmentFoundation.IsSafeRefinementRule(normal),
                "normal crafting may lose minor refinement");
            normal.mayBreakNaturalLimit=true;
            Check(!EquipmentFoundation.IsSafeRefinementRule(normal),
                "ordinary crafting cannot bypass potential");
            var gamble=new RefinementRuleData {
                method=RefinementMethod.Risky,
                mayBreakNaturalLimit=true,
                onFailure=RefinementFailureEffect.DestroyItem
            };
            Check(EquipmentFoundation.IsSafeRefinementRule(gamble),
                "risky path may permit both breakthrough and total loss");
        });

        Run("0.0.52 discovery metadata survives manual save and reload",()=>{
            var f=new Field();SaveManager save;string dir;
            MenuManager(f,out save,out dir);
            try
            {
                var hero=HeroAt(7);
                EquipmentInventory.InitializeNewRun(hero);
                EquipmentRuntime.InitializeNewRun(hero);
                var sword=DiscoverySword();
                Check(EquipmentRuntime.AddInstance(hero,sword),"add latent item");
                Check(EquipmentRuntime.Equip(hero,sword.instanceId,EquipmentSlot.Weapon),
                    "equip latent item");
                Check(save.SaveGame(hero,SaveSlot.ManualSave1),"persist latent item");
                var loaded=HeroAt(1);
                Check(save.LoadGame(loaded,SaveSlot.ManualSave1),"restore latent item");
                var restored=EquipmentRuntime.Equipped(loaded,EquipmentSlot.Weapon);
                Check(restored!=null && restored.affixSlotCount==3 &&
                    restored.itemElement==ElementType.Kim &&
                    restored.potentialRarity==EquipmentRarity.Tien &&
                    restored.rarity==EquipmentRarity.Common &&
                    restored.naturalRefineLimit==5 &&
                    restored.highestRefineLevel==3 && restored.hasBeenRefined,
                    "metadata roundtrips exactly");
                Check(restored.extraStats.Count==3 &&
                    restored.extraStats[0].revealed &&
                    restored.extraStats[1].revealed &&
                    !restored.extraStats[2].revealed &&
                    restored.extraStats[2].requiredRefineLevel==4 &&
                    restored.extraStats[2].flat==11f,
                    "hidden, known and value survive without reroll");
                Check(EquipmentFoundation.AffixState(restored,restored.extraStats[2])
                    ==EquipmentAffixState.Hidden,"save load never reveals hidden line");
            }
            finally { System.IO.Directory.Delete(dir,true); }
        });
    }
}
