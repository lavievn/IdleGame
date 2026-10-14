using System;
using System.Collections.Generic;
using UnityEngine;
using TuTienCore;

partial class MotionRegression
{
    static void EquipmentStatPreviewRegressionTests()
    {
        Run("0.0.52 preview common sword leaves Hero and save untouched",()=>{
            var hero=HeroAt(1);
            EquipmentInventory.InitializeNewRun(hero);
            EquipmentRuntime.InitializeNewRun(hero);
            var sword=EquipmentFoundation.CreateDemoSword();
            Check(EquipmentRuntime.AddInstance(hero,sword),"sword in bag");
            bool dirty=hero.isDirty;
            var originalMods=hero.combatStatModifiers;
            int originalCount=originalMods.Count;
            EquipmentStatComparison compare;
            Check(EquipmentStatPreview.TryEquip(hero,sword.instanceId,
                EquipmentSlot.Weapon,AttackMode.Melee,out compare),"valid candidate");
            var line=compare.Find(CombatStatId.Attack);
            Check(line!=null && line.current==hero.GetCalculatedDamage(AttackMode.Melee)
                && line.preview==line.current+10f &&
                line.withoutEquipment==line.current,
                "preview shows current attack + 10");
            Check(!EquipmentRuntime.Find(hero,sword.instanceId).isEquipped &&
                hero.combatStatModifiers==originalMods &&
                hero.combatStatModifiers.Count==originalCount &&
                hero.isDirty==dirty,"comparison is pure and cannot dirty save");
            Check(EquipmentRuntime.Equip(hero,sword.instanceId,EquipmentSlot.Weapon),
                "equip sword for real");
            Check(hero.GetCalculatedDamage(AttackMode.Melee)==(int)line.preview,
                "preview result equals real equipped attack");
            Check(EquipmentStatPreview.Current(hero,AttackMode.Melee)
                .Find(CombatStatId.Attack).equippedBonus==10f,
                "character sheet has exact base + equipment bonus");
        });

        Run("0.0.52 compare replacing sword and moving rings accounts for removed bonuses",()=>{
            var hero=HeroAt(1);
            EquipmentInventory.InitializeNewRun(hero);
            EquipmentRuntime.InitializeNewRun(hero);
            var weak=EquipmentFoundation.CreateCommon(EquipmentFoundation.DemoSword,1,10f);
            var strong=EquipmentFoundation.CreateCommon(EquipmentFoundation.DemoSword,1,17f);
            var ring1=EquipmentFoundation.CreateCommon("base:ring",1,3f);
            var ring2=EquipmentFoundation.CreateCommon("base:ring",1,5f);
            foreach(var item in new[]{weak,strong,ring1,ring2})
                Check(EquipmentRuntime.AddInstance(hero,item),"add distinct item");
            Check(EquipmentRuntime.Equip(hero,weak.instanceId,EquipmentSlot.Weapon),
                "weak sword worn");
            EquipmentStatComparison preview;
            Check(EquipmentStatPreview.TryEquip(hero,strong.instanceId,
                EquipmentSlot.Weapon,AttackMode.Melee,out preview),"preview swap");
            var atk=preview.Find(CombatStatId.Attack);
            Near(atk.difference,7f,"replacing +10 with +17 is only +7");
            Check(EquipmentRuntime.Equipped(hero,EquipmentSlot.Weapon).instanceId ==
                weak.instanceId,"no implicit swap during preview");
            Check(EquipmentRuntime.Equip(hero,strong.instanceId,EquipmentSlot.Weapon),
                "actual swap");
            Near(preview.Find(CombatStatId.Attack).preview,
                hero.GetCalculatedDamage(AttackMode.Melee),"preview matches actual swap");
            Check(EquipmentRuntime.Equip(hero,ring1.instanceId,EquipmentSlot.RingLeft) &&
                EquipmentRuntime.Equip(hero,ring2.instanceId,EquipmentSlot.RingRight),
                "both rings worn");
            int before=hero.GetCalculatedDamage(AttackMode.Melee);
            Check(EquipmentStatPreview.TryEquip(hero,ring1.instanceId,
                EquipmentSlot.RingRight,AttackMode.Melee,out preview),"move left ring to right");
            Near(preview.Find(CombatStatId.Attack).difference,-5f,
                "moving ring to occupied slot removes old right ring bonus");
            Check(hero.GetCalculatedDamage(AttackMode.Melee)==before,
                "read-only ring move");
            Check(EquipmentRuntime.Equip(hero,ring1.instanceId,EquipmentSlot.RingRight),
                "actual ring movement");
            Check(EquipmentRuntime.Equipped(hero,EquipmentSlot.RingLeft)==null &&
                EquipmentRuntime.Equipped(hero,EquipmentSlot.RingRight).instanceId==ring1.instanceId,
                "ring occupies right only");
            Near(preview.Find(CombatStatId.Attack).preview,
                hero.GetCalculatedDamage(AttackMode.Melee),"ring preview equals real result");
        });

        Run("0.0.52 preview supports stance restrictions and elemental affixes",()=>{
            var hero=HeroAt(1);
            EquipmentRuntime.InitializeNewRun(hero);
            var sword=EquipmentFoundation.CreateDemoSword();
            sword.rarity=EquipmentRarity.Thien;
            sword.extraStats.Add(new ItemStatLine {
                stat=CombatStatId.ElementDamagePercent,
                element=ElementType.Hoa,flat=.08f,
                requiredTags=new List<string>{"stance:melee"}
            });
            Check(EquipmentRuntime.AddInstance(hero,sword),"elemental sword");
            EquipmentStatComparison melee,bow;
            Check(EquipmentStatPreview.TryEquip(hero,sword.instanceId,
                EquipmentSlot.Weapon,AttackMode.Melee,out melee),"melee preview");
            Check(EquipmentStatPreview.TryEquip(hero,sword.instanceId,
                EquipmentSlot.Weapon,AttackMode.RangedPhysical,out bow),"bow preview");
            Near(melee.Find(CombatStatId.Attack).difference,10f,
                "melee sword buff applies");
            Near(bow.Find(CombatStatId.Attack).difference,0f,
                "bow stance does not gain melee attack");
            Near(melee.Find(CombatStatId.ElementDamagePercent,ElementType.Hoa)
                .difference,.08f,"Fire bonus applies to melee stance");
            Near(bow.Find(CombatStatId.ElementDamagePercent,ElementType.Hoa)
                .difference,0f,"Fire affix blocked outside stance");
            Near(melee.Find(CombatStatId.ElementDamagePercent,ElementType.Thuy)
                .difference,0f,"Fire does not apply to Water");
        });

        Run("0.0.52 legacy charm compare and unequip match actual combat and HP",()=>{
            var hero=HeroAt(1);
            EquipmentInventory.InitializeNewRun(hero);
            EquipmentRuntime.InitializeNewRun(hero);
            EquipmentStatComparison preview;
            Check(EquipmentStatPreview.TryEquipLegacy(hero,
                EquipmentCatalog.HealthCharm,AttackMode.Melee,out preview),
                "preview old HP charm");
            Near(preview.Find(CombatStatId.MaxHealth).difference,100f,
                "HP preview +100");
            Check(EquipmentInventory.Equip(hero,EquipmentCatalog.HealthCharm),"wear old HP charm");
            Near(preview.Find(CombatStatId.MaxHealth).preview,
                hero.GetCalculatedHealth(AttackMode.Melee),"HP matches real equip");
            Check(EquipmentStatPreview.TryEquipLegacy(hero,
                EquipmentCatalog.AttackCharm,AttackMode.Melee,out preview),
                "preview replacing HP with attack charm");
            Near(preview.Find(CombatStatId.MaxHealth).difference,-100f,
                "HP loses old charm bonus");
            Near(preview.Find(CombatStatId.Attack).difference,20f,
                "attack gains new charm bonus");
            Check(EquipmentInventory.Equip(hero,EquipmentCatalog.AttackCharm),
                "actually replace legacy charm");
            Near(preview.Find(CombatStatId.MaxHealth).preview,
                hero.GetCalculatedHealth(AttackMode.Melee),"HP matches replacement");
            Near(preview.Find(CombatStatId.Attack).preview,
                hero.GetCalculatedDamage(AttackMode.Melee),"ATK matches replacement");
            Check(EquipmentStatPreview.TryUnequip(hero,EquipmentSlot.Artifact,
                AttackMode.Melee,out preview),"preview unequip legacy");
            Near(preview.Find(CombatStatId.Attack).difference,-20f,
                "unequipping charm removes 20 ATK");
            Check(EquipmentInventory.Unequip(hero),"real unequip");
            Near(preview.Find(CombatStatId.Attack).preview,
                hero.GetCalculatedDamage(AttackMode.Melee),"unequip prediction equals real");
        });

        Run("0.0.52 preview rejects invalid slots and artifacts occupied by legacy",()=>{
            var hero=HeroAt(1);
            EquipmentInventory.InitializeNewRun(hero);
            EquipmentRuntime.InitializeNewRun(hero);
            var artifact=EquipmentFoundation.CreateCommon("base:artifact",1,35f);
            Check(EquipmentRuntime.AddInstance(hero,artifact),"artifact available");
            EquipmentStatComparison preview;
            Check(!EquipmentStatPreview.TryEquip(hero,artifact.instanceId,
                EquipmentSlot.Head,AttackMode.Melee,out preview),
                "artifact cannot pretend to occupy head slot");
            Check(EquipmentInventory.Equip(hero,EquipmentCatalog.HealthCharm),
                "legacy artifact worn");
            Check(!EquipmentStatPreview.TryEquip(hero,artifact.instanceId,
                EquipmentSlot.Artifact,AttackMode.Melee,out preview),
                "preview cannot bypass actual legacy artifact collision");
            Check(EquipmentInventory.Unequip(hero),"clear legacy");
            Check(EquipmentRuntime.Equip(hero,artifact.instanceId,EquipmentSlot.Artifact),
                "new artifact worn");
            Check(!EquipmentStatPreview.TryEquipLegacy(hero,
                EquipmentCatalog.AttackCharm,AttackMode.Melee,out preview),
                "preview cannot equip legacy over modern artifact");
            Check(EquipmentStatPreview.TryUnequip(hero,EquipmentSlot.Artifact,
                AttackMode.Melee,out preview),"preview unequip modern");
            Near(preview.Find(CombatStatId.MaxHealth).difference,-35f,
                "unequip modern artifact subtracts exact HP");
            Check(EquipmentRuntime.Unequip(hero,EquipmentSlot.Artifact),
                "actual modern unequip");
            Near(preview.Find(CombatStatId.MaxHealth).preview,
                hero.GetCalculatedHealth(AttackMode.Melee),
                "modern artifact preview matches real unequip");
        });
    }
}
