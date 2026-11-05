using System;
using UnityEngine;

partial class MotionRegression
{
    // Explicit fixture data only, never a production balance default.
    static ProductionLootConfig RewardTable(double gear=.4,double material=.3)
    {
        var c=new ProductionLootConfig {configured=true};
        if(gear>0)c.entries.Add(new ProductionLootConfig.Rule {kind=ProductionLootConfig.Kind.CommonEquipment,
            id=EquipmentFoundation.DemoSword,probability=gear,quantity=1,itemLevel=1,mainStat=7});
        if(material>0)c.entries.Add(new ProductionLootConfig.Rule {kind=ProductionLootConfig.Kind.ForgeMaterial,
            id=EquipmentRefinement.MaterialId,probability=material,quantity=14});
        return c;
    }
    static EntityDataSO RewardHero()
    {
        var h=HeroAt(2);EquipmentInventory.InitializeNewRun(h);EquipmentRuntime.InitializeNewRun(h);h.isDirty=false;return h;
    }
    static CombatLootRewards.KillKey RewardClaim(CombatLootRewards service,EntityDataSO h,int life=1)
    {var k=service.Register(life,1);Check(service.TryClaim(k,h),"claim accepted life");return k;}
    static void ProductionRewardTests(string only=null)
    {
        int matched=0;Action<string,Action> run=(name,test)=>{if(only==null || name.Contains(only)){matched++;Run(name,test);}};
        run("production: unconfigured stays NoDrop, terminal across dev and production, no roll",()=>{
            var h=RewardHero();int rolls=0;var s=new CombatLootRewards(EquipmentFoundation.CreateDemoSword,()=>{rolls++;return 0;});
            s.BeginEncounter(h);var k=RewardClaim(s,h);ProductionLootConfig.Reward r;string error;
            Check(s.CompleteProduction(k,h,new ProductionLootConfig(),out r,out error)==CombatLootRewards.Outcome.NoDrop,"no implicit reward");
            Check(rolls==0 && !h.isDirty && h.equipmentItems.Count==0,"no mutation or roll");
            string id;Check(s.Complete(k,h,true,out id,out error)==CombatLootRewards.Outcome.Rejected,"no second reward mode");
        });
        foreach(double roll in new[]{0d,.4d,.7d,.999d})
        run("production: exclusive configured outcome and remainder / "+roll,()=>{
            var h=RewardHero();var s=new CombatLootRewards(EquipmentFoundation.CreateDemoSword,()=>roll);
            s.BeginEncounter(h);var k=RewardClaim(s,h);ProductionLootConfig.Reward r;string error;
            var outcome=s.CompleteProduction(k,h,RewardTable(),out r,out error);
            Check(outcome==(roll<.7?CombatLootRewards.Outcome.Awarded:CombatLootRewards.Outcome.NoDrop),"expected bucket");
            Check(h.equipmentItems.Count==(roll<.4?1:0),"real gear count");
            Check(EquipmentInventory.Count(h,EquipmentRefinement.MaterialId)==(roll>=.4 && roll<.7?14:0),"real material count");
            Check(!s.TryClaim(k,h) && s.CompleteProduction(k,h,RewardTable(),out r,out error)==CombatLootRewards.Outcome.Rejected,"no duplicate or reroll");
        });
        foreach(string invalid in new[]{"unknown gear","unknown material","probability","quantity","level","stat","sum"})
        run("production: invalid configuration fails closed / "+invalid,()=>{
            var h=RewardHero();var c=RewardTable();
            if(invalid=="unknown gear")c.entries[0].id="missing:gear";
            if(invalid=="unknown material")c.entries[1].id="missing:material";
            if(invalid=="probability")c.entries[0].probability=double.NaN;
            if(invalid=="quantity")c.entries[1].quantity=0;
            if(invalid=="level")c.entries[0].itemLevel=0;
            if(invalid=="stat")c.entries[0].mainStat=float.PositiveInfinity;
            if(invalid=="sum")c.entries[1].probability=.8;
            int rolls=0;var s=new CombatLootRewards(EquipmentFoundation.CreateDemoSword,()=>{rolls++;return 0;});
            s.BeginEncounter(h);var k=RewardClaim(s,h);ProductionLootConfig.Reward r;string error;
            Check(s.CompleteProduction(k,h,c,out r,out error)==CombatLootRewards.Outcome.Failed && error.Length>0,"reject bad table");
            Check(rolls==0 && h.equipmentItems.Count==0 && EquipmentInventory.Count(h,EquipmentRefinement.MaterialId)==0 && !h.isDirty,"no grant or mutation");
        });
        run("production: overflow, future save and stale context cannot award",()=>{
            var h=RewardHero();EquipmentInventory.Add(h,EquipmentRefinement.MaterialId,EquipmentInventory.MaxQuantity);h.isDirty=false;
            var s=new CombatLootRewards(EquipmentFoundation.CreateDemoSword,()=>0);s.BeginEncounter(h);
            ProductionLootConfig.Reward r;string error;
            Check(s.CompleteProduction(RewardClaim(s,h),h,RewardTable(0,1),out r,out error)==CombatLootRewards.Outcome.Failed,"overflow rejected");
            Check(!h.isDirty && EquipmentInventory.Count(h,EquipmentRefinement.MaterialId)==EquipmentInventory.MaxQuantity,"no clipping or partial credit");
            s=new CombatLootRewards();s.BeginEncounter(h);var k=RewardClaim(s,h);s.Invalidate();
            Check(s.CompleteProduction(k,h,RewardTable(1,0),out r,out error)==CombatLootRewards.Outcome.Rejected,"cancelled receipt");
            h.inventoryVersion=EquipmentInventory.SchemaVersion+1;s=new CombatLootRewards();s.BeginEncounter(h);
            Check(s.CompleteProduction(RewardClaim(s,h),h,RewardTable(1,0),out r,out error)==CombatLootRewards.Outcome.Failed,"future schema rejected before grant");
            Check(h.equipmentItems.Count==0,"no future-schema item");
        });
        run("production: reentry invalidation and invalid roll cannot grant",()=>{
            var h=RewardHero();CombatLootRewards s=null;s=new CombatLootRewards(EquipmentFoundation.CreateDemoSword,()=>{s.Invalidate();return 0;});
            s.BeginEncounter(h);ProductionLootConfig.Reward r;string error;
            Check(s.CompleteProduction(RewardClaim(s,h),h,RewardTable(1,0),out r,out error)==CombatLootRewards.Outcome.Failed,"context rechecked after selection");
            Check(h.equipmentItems.Count==0 && !h.isDirty,"no stale grant");
            s=new CombatLootRewards(EquipmentFoundation.CreateDemoSword,()=>1);s.BeginEncounter(h);
            Check(s.CompleteProduction(RewardClaim(s,h),h,RewardTable(1,0),out r,out error)==CombatLootRewards.Outcome.Failed,"invalid [0,1) roll rejected");
        });
        run("production: accepted kill without test flag grants once and preserves sword VFX",()=>WithLootSprite(()=>{
            UnityEngine.Random.InitState(19);GameManager gm;var f=LootField(false,out gm);Set(gm,"productionLoot",RewardTable(1,0));
            var monster=f.Monster(-20);LootBattle(f,gm,monster);var victim=f.Enemies[0];
            string beforeRng,afterRng;UnityEngine.Random.InitState(19);Lethal(f,victim);beforeRng=UnityEngine.Random.Range(0f,1f).ToString("R");
            Check(!gm.ControlledLootTestEnabled && gm.HeroData.equipmentItems.Count==1 && VisibleLoot(f)==1,"production route and real cosmetic");
            var item=gm.HeroData.equipmentItems[0];Check(item.mainStats[0].flat==7 && !item.isEquipped,"uses explicit config, not demo factory values");
            DeathCallback(f,victim);Lethal(f,victim);Check(gm.HeroData.equipmentItems.Count==1 && VisibleLoot(f)==1,"duplicate death rejected");
            UnityEngine.Random.InitState(19);GameManager off;var same=LootField(false,out off);var other=same.Monster(-20);LootBattle(same,off,other);
            UnityEngine.Random.InitState(19);Lethal(same,same.Enemies[0]);afterRng=UnityEngine.Random.Range(0f,1f).ToString("R");
            Check(beforeRng==afterRng && gm.HeroData.currentExp==off.HeroData.currentExp,"combat RNG and EXP unchanged: "+beforeRng+" / "+afterRng+"; EXP "+gm.HeroData.currentExp+" / "+off.HeroData.currentExp);
        }));
        run("production: material kill, equipment and real Forge cost/stats without dev grants",()=>{
            UnityEngine.Random.InitState(19);GameManager gm;var f=LootField(false,out gm);Set(gm,"productionLoot",RewardTable(1,0));
            var target=f.Monster(-20);LootBattle(f,gm,target);Lethal(f,f.Enemies[0]);var item=gm.HeroData.equipmentItems[0];
            Check(gm.EquipGear(item.instanceId,EquipmentSlot.Weapon),"equip actual awarded item");
            string message;Check(!gm.ApplyForgeTest(item.instanceId,EquipmentRefinement.Operation.NormalUp,out message),"no free forge");
            int damage=WeaponBasicAttack.DamageSource(gm.HeroData);long gold=gm.HeroData.gold;
            Set(gm,"productionLoot",RewardTable(0,1));target=f.Monster(-20);LootBattle(f,gm,target);Lethal(f,f.Enemies[0]);
            Check(EquipmentInventory.Count(gm.HeroData,EquipmentRefinement.MaterialId)==14,"actual material kill");
            Check(gm.ApplyForgeTest(item.instanceId,EquipmentRefinement.Operation.NormalUp,out message),"normal consumes real stock");
            Check(EquipmentInventory.Count(gm.HeroData,EquipmentRefinement.MaterialId)==13,"normal cost 1");
            Check(gm.ApplyForgeTest(item.instanceId,EquipmentRefinement.Operation.SpecialUp,out message),"special consumes real stock");
            Check(EquipmentInventory.Count(gm.HeroData,EquipmentRefinement.MaterialId)==3 && gm.HeroData.gold==gold,"special cost 10, no invented gold fee");
            Check(WeaponBasicAttack.DamageSource(gm.HeroData)>damage && item.forgeHistory.Count>0,"equipped stats and history updated");
        });
        run("production: dev stock APIs follow build guard",()=>{
            GameManager gm;var f=LootField(false,out gm);int before=EquipmentInventory.Count(gm.HeroData,EquipmentRefinement.MaterialId);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Check(GameManager.DevelopmentItemGrantsEnabled && gm.GrantForgeMaterials(),"dev tool retained");
            Check(EquipmentInventory.Count(gm.HeroData,EquipmentRefinement.MaterialId)==before+100,"explicit dev quantity");
#else
            Check(!GameManager.DevelopmentItemGrantsEnabled && !gm.GrantForgeMaterials() && !gm.GrantForgeTestSword() && !gm.GrantDemoSword() && !gm.GrantInventorySamples(),"release APIs deny dev grants");
            Check(EquipmentInventory.Count(gm.HeroData,EquipmentRefinement.MaterialId)==before && gm.HeroData.equipmentItems.Count==0,"release progression cannot use dev stock");
#endif
        });
        Check(matched>0,"production test selector matched a case" );
    }
}
