using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using TuTienCore;

partial class MotionRegression
{
    static int PrototypeDamage(EntityDataSO attacker,EntityDataSO defender,AttackMode mode,float roll=1)
    {
        var trace=SynergyMath.EvaluateBasicDamage(attacker,defender,mode,roll);
        return CombatDamage.ResolveBasicSnapshot(trace.elementDamage,trace.basicBoost,defender,mode,new CombatResistanceSettings());
    }
    // Read the actual authored scene, not a parallel set of prototype defaults.
    static ProductionLootConfig MainGameGearConfig()
    {
        string scene=File.ReadAllText("Assets/Scenes/MainGame.unity");
        var block=Regex.Match(scene,@"(?s)  productionLoot:\s*(.*?)  activeMonsters:");
        Check(block.Success && scene.Contains("  controlledLootTest: 0"),"authored production config, controlled A off");
        string text=block.Groups[1].Value;
        Check(Regex.IsMatch(text,@"configured: 1") && Regex.IsMatch(text,@"(?s)material:\s+enabled: 1\s+chance: 0\.6\s+materialId: material:refinement\s+minQuantity: 1\s+maxQuantity: 2"),"material unchanged");
        Check(Regex.IsMatch(text,@"(?s)equipment:\s+enabled: 1\s+chance: 0\.08\s+selectionPolicy: 1"),"equipment enabled 8% Uniform");
        var c=new ProductionLootConfig {configured=true,material=new MaterialRewardConfig {enabled=true,chance=.60,materialId=EquipmentRefinement.MaterialId,minQuantity=1,maxQuantity=2}};
        c.equipment.enabled=true;c.equipment.chance=.08;c.equipment.selectionPolicy=EquipmentRewardConfig.SelectionPolicy.Uniform;
        foreach(Match m in Regex.Matches(text,@"templateId: (\S+)\s+itemLevel: (\d+)\s+mainStat: ([\d.]+)"))
            c.equipment.templates.Add(new EquipmentRewardConfig.GearTemplate {templateId=m.Groups[1].Value,itemLevel=int.Parse(m.Groups[2].Value),mainStat=float.Parse(m.Groups[3].Value,CultureInfo.InvariantCulture)});
        Check(c.equipment.templates.Count==2,"exactly two production templates");return c;
    }
    static void GearDropPrototypeTests()
    {
        Run("gear prototype authored catalog and probability boundaries",()=>{
            var c=MainGameGearConfig();
            for(int i=0;i<2;i++) {
                var t=c.equipment.templates[i];Check(t.templateId==(i==0?EquipmentFoundation.DemoSword:EquipmentFoundation.DemoBow) && t.itemLevel==1 && t.mainStat==4,"explicit level 1 Attack4");
                var item=EquipmentFoundation.CreateCommon(t.templateId,t.itemLevel,t.mainStat);
                Check(EquipmentFoundation.IsValid(item) && item.rarity==EquipmentRarity.Common && item.extraStats.Count==0,"valid Common, no affixes");
                int draw=0;var r=c.RollEquipment(()=>draw++==0?.079999:(i==0?0:.999999));
                Check(r.id==t.templateId && r.quantity==1,"uniform selection endpoints");
            }
            Check(c.RollEquipment(()=>.08).kind==ProductionLootConfig.Kind.Unconfigured,"8% boundary excludes no-drop");
            Check(c.RollMaterial(()=>.60).kind==ProductionLootConfig.Kind.Unconfigured,"60% material boundary");
        });
        foreach(var template in MainGameGearConfig().equipment.templates)
        Run("gear prototype death/equip/replace/forge "+template.templateId,()=>{
            GameManager gm;var f=WeaponField(out gm);Set(gm,"controlledLootTest",false);
            var c=MainGameGearConfig();c.material.chance=c.equipment.chance=1;
            c.equipment.templates=new List<EquipmentRewardConfig.GearTemplate>{template};Set(gm,"productionLoot",c);
            var monster=f.Monster(-20);LootBattle(f,gm,monster);var old=f.Enemies[0];Lethal(f,old);DeathCallback(f,old);
            var h=gm.HeroData;Check(h.equipmentItems.Count==1,"accepted death, duplicate denied");
            string id=h.equipmentItems[0].instanceId;
            Check(EquipmentInventory.Count(h,EquipmentRefinement.MaterialId)>=1 && EquipmentInventory.Count(h,EquipmentRefinement.MaterialId)<=2,"independent material awarded");
            var defender=HeroAt(1);defender.spiritRoots=new List<ElementType>{ElementType.Vo};defender.NormalizeRoots();
            int before=WeaponBasicAttack.DamageSource(h),damageBefore=PrototypeDamage(h,defender,AttackMode.Melee);
            Check(gm.EquipGear(id,EquipmentSlot.Weapon),"actual GameManager equip handler");
            var mode=WeaponBasicAttack.Resolve(h);int after=WeaponBasicAttack.DamageSource(h);
            int damageAfter=PrototypeDamage(h,defender,mode);
            int lowRollAfter=PrototypeDamage(h,defender,mode,.85f);
            Check(after==before+4 && f.hero.attackMode==mode && WeaponBasicAttack.Magical(h)==0,"physical stat/attack profile refreshed");
            Check(damageAfter>PrototypeDamage(defender,defender,mode),"real damage evaluator uses increment in the same mode");
            Check(gm.UnequipGear(EquipmentSlot.Weapon) && WeaponBasicAttack.DamageSource(h)==before,"unequip removes exact modifier");
            Check(gm.EquipGear(id,EquipmentSlot.Weapon),"re-equip");
            // Gather additional real reward materials, not the development grant.
            var service=RewardService(()=>0,()=>.99);service.BeginEncounter(h);c.equipment.enabled=false;
            for(int i=1;EquipmentInventory.Count(h,EquipmentRefinement.MaterialId)<11;i++) {
                CombatLootRewards.ProductionResult r;string error;service.CompleteProduction(RewardClaim(service,h,i),h,c,out r,out error);Check(error=="","resource income");
            }
            int stock=EquipmentInventory.Count(h,EquipmentRefinement.MaterialId);string message;
            Check(gm.ApplyForgeTest(id,EquipmentRefinement.Operation.NormalUp,out message),message);
            var gear=EquipmentRuntime.Find(h,id);Near(EquipmentRefinement.PowerMultiplier(gear),1.02f,"normal +2% of original stat");
            Check(gm.ApplyForgeTest(id,EquipmentRefinement.Operation.SpecialUp,out message),message);
            Near(EquipmentRefinement.PowerMultiplier(gear),1.16f,"special +14% of original stat");
            Check(EquipmentInventory.Count(h,EquipmentRefinement.MaterialId)==stock-11 && gear.forgeHistory.Count==1 && gear.specialForgeCount==1,"costs/history");
            Check(WeaponBasicAttack.DamageSource(h)==before+5,"4.64 rounds to 5 after normal+special");
            int forged=PrototypeDamage(h,defender,mode);
            int lowRollForged=PrototypeDamage(h,defender,mode,.85f);
            Check(lowRollForged>lowRollAfter,"Forge changes real damage even when the maximum Bow roll rounds to the same integer");
            Console.WriteLine("METRIC "+template.templateId+" source="+before+"->"+after+"->"+WeaponBasicAttack.DamageSource(h)+" damage="+damageBefore+"("+AttackMode.Melee+")->"+damageAfter+"("+mode+")->"+forged+" stock="+stock+"->"+EquipmentInventory.Count(h,EquipmentRefinement.MaterialId));
            Console.WriteLine("METRIC "+template.templateId+" roll0.85 damage="+lowRollAfter+"->"+lowRollForged+" after Normal+Special");
            var replacement=EquipmentFoundation.CreateCommon(template.templateId,1,4);Check(EquipmentRuntime.AddInstance(h,replacement) && gm.EquipGear(replacement.instanceId,EquipmentSlot.Weapon),"replace via real handler");
            Check(!gear.isEquipped && WeaponBasicAttack.DamageSource(h)==after && h.equipmentItems.Count==2 && replacement.instanceId!=id,"no stacking/duplication on replacement");
        });
        Run("gear prototype retains starter charm and Staff contract",()=>{
            GameManager gm;var f=WeaponField(out gm);var h=gm.HeroData;
            EquipmentInventory.Add(h,EquipmentCatalog.AttackCharm,1);Check(gm.EquipInventoryItem(EquipmentCatalog.AttackCharm),"starter charm handler");
            var t=MainGameGearConfig().equipment.templates[0];var gear=EquipmentFoundation.CreateCommon(t.templateId,t.itemLevel,t.mainStat);EquipmentRuntime.AddInstance(h,gear);
            Check(gm.EquipGear(gear.instanceId,EquipmentSlot.Weapon) && WeaponBasicAttack.Physical(h)==34 && h.equippedItemId==EquipmentCatalog.AttackCharm,"charm20 + base10 + sword4, independent slots");
            // Not in the production table: assert existing channel/mode, not staff balance.
            var staff=EquipmentFoundation.CreateCommon(EquipmentFoundation.DemoStaff,1,4);EquipmentRuntime.AddInstance(h,staff);
            Check(gm.EquipGear(staff.instanceId,EquipmentSlot.Weapon) && f.hero.attackMode==AttackMode.RangedMagic && WeaponBasicAttack.Magical(h)==4 && WeaponBasicAttack.Physical(h)==30,"Staff magic does not consume physical Attack/charm or stale sword bonus");
        });
        Run("gear prototype lightweight seeded 100/500/1000 roll simulation",()=>{
            var c=MainGameGearConfig();var mr=new System.Random(1105);var er=new System.Random(20261105);
            int material=0,quantity=0,gear=0,both=0,none=0;var counts=new Dictionary<string,int>();
            foreach(var t in c.equipment.templates)counts[t.templateId]=0;
            var verifyMr=new System.Random(1105);var verifyEr=new System.Random(20261105);
            for(int i=1;i<=1000;i++) {
                var m=c.RollMaterial(mr.NextDouble);var g=c.RollEquipment(er.NextDouble);
                var repeatM=c.RollMaterial(verifyMr.NextDouble);var repeatG=c.RollEquipment(verifyEr.NextDouble);
                Check(m.kind==repeatM.kind && m.quantity==repeatM.quantity && g.kind==repeatG.kind && g.id==repeatG.id,"seed replay");
                bool mat=m.kind!=ProductionLootConfig.Kind.Unconfigured,eq=g.kind!=ProductionLootConfig.Kind.Unconfigured;
                if(mat){material++;quantity+=m.quantity;Check(m.quantity>=1 && m.quantity<=2,"material range");}
                if(eq){gear++;counts[g.id]++;Check(g.quantity==1,"exactly one gear");}
                if(mat&&eq)both++;if(!mat&&!eq)none++;
                if(i==100 || i==500 || i==1000)Console.WriteLine("SIM kills="+i+" materialHits="+material+" materialTotal="+quantity+" gear="+gear+" sword="+counts[EquipmentFoundation.DemoSword]+" bow="+counts[EquipmentFoundation.DemoBow]+" both="+both+" none="+none);
            }
            Check(material>500 && material<700 && gear>35 && gear<125 && both>15 && both<85,"broad deterministic sanity bounds, not gameplay balance");
            Check(counts[EquipmentFoundation.DemoSword]>0 && counts[EquipmentFoundation.DemoBow]>0 && none==1000-material-gear+both,"coverage/accounting");
        });
    }
}
