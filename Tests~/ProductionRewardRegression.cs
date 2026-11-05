using System;
using System.Collections.Generic;
using UnityEngine;

partial class MotionRegression
{
    // All gear numbers here are test data, never MainGame balance defaults.
    static ProductionLootConfig RewardTable(double gear=.08,double material=.6)
    {
        var c=new ProductionLootConfig {configured=true};
        c.material=new MaterialRewardConfig {enabled=material>0,chance=material,materialId=EquipmentRefinement.MaterialId,minQuantity=1,maxQuantity=2};
        c.equipment=new EquipmentRewardConfig {enabled=gear>0,chance=gear,selectionPolicy=EquipmentRewardConfig.SelectionPolicy.Uniform};
        c.equipment.templates.Add(new EquipmentRewardConfig.GearTemplate {templateId=EquipmentFoundation.DemoSword,itemLevel=1,mainStat=7});
        return c;
    }
    static EntityDataSO RewardHero()
    {var h=HeroAt(2);EquipmentInventory.InitializeNewRun(h);EquipmentRuntime.InitializeNewRun(h);h.isDirty=false;return h;}
    static CombatLootRewards.KillKey RewardClaim(CombatLootRewards service,EntityDataSO h,int life=1)
    {var k=service.Register(life,1);Check(service.TryClaim(k,h),"claim accepted life");return k;}
    static CombatLootRewards RewardService(Func<double> material,Func<double> gear,
        Func<ProductionLootConfig.Reward,EquipmentInstance> factory=null)
    {return new CombatLootRewards(EquipmentFoundation.CreateDemoSword,material,gear,factory);}
    static void ProductionRewardTests(string only=null)
    {
        int matched=0;Action<string,Action> run=(name,test)=>{if(only==null || name.Contains(only)){matched++;Run(name,test);}};
        run("v01 unconfigured and both disabled are terminal without RNG",()=>{
            foreach(bool configured in new[]{false,true}) {
                int draws=0;var s=RewardService(()=>{draws++;return 0;},()=>{draws++;return 0;});var h=RewardHero();s.BeginEncounter(h);
                var k=RewardClaim(s,h);CombatLootRewards.ProductionResult r;string error,id;
                Check(s.CompleteProduction(k,h,new ProductionLootConfig {configured=configured},out r,out error)==CombatLootRewards.Outcome.NoDrop,"NoDrop");
                Check(draws==0 && !h.isDirty && h.equipmentItems.Count==0,"no defaults or draws");
                Check(s.Complete(k,h,true,out id,out error)==CombatLootRewards.Outcome.Rejected,"same receipt cannot award test mode afterwards");
            }
        });
        foreach(bool mat in new[]{false,true})foreach(bool gear in new[]{false,true})
        run("v01 independent outcomes material="+mat+" gear="+gear,()=>{
            var h=RewardHero();int mr=0,gr=0;var s=RewardService(()=>{mr++;return mat?0:.9;},()=>{gr++;return gear?0:.9;});s.BeginEncounter(h);
            var k=RewardClaim(s,h);CombatLootRewards.ProductionResult r;string error;
            Check(s.CompleteProduction(k,h,RewardTable(),out r,out error)==(mat||gear?CombatLootRewards.Outcome.Awarded:CombatLootRewards.Outcome.NoDrop),"aggregate outcome");
            Check(r.MaterialOutcome==(mat?CombatLootRewards.Outcome.Awarded:CombatLootRewards.Outcome.NoDrop) &&
                r.EquipmentOutcome==(gear?CombatLootRewards.Outcome.Awarded:CombatLootRewards.Outcome.NoDrop),"separate outcomes");
            Check(EquipmentInventory.Count(h,EquipmentRefinement.MaterialId)==(mat?1:0) && h.equipmentItems.Count==(gear?1:0),"real inventory");
            int before=mr+gr;Check(s.CompleteProduction(k,h,RewardTable(),out r,out error)==CombatLootRewards.Outcome.Rejected && before==mr+gr,"duplicate does not reroll");
        });
        foreach(string branch in new[]{"material","equipment"})foreach(string bad in new[]{"id","chance","bounds"})
        run("v01 invalid "+branch+" "+bad+" preserves other stream",()=>{
            var c=RewardTable(1,1);
            if(branch=="material") {
                if(bad=="id")c.material.materialId="missing:material";
                if(bad=="chance")c.material.chance=double.NaN;
                if(bad=="bounds")c.material.maxQuantity=0;
            } else {
                if(bad=="id")c.equipment.templates[0].templateId="missing:gear";
                if(bad=="chance")c.equipment.chance=1.01;
                if(bad=="bounds")c.equipment.templates[0].itemLevel=0;
            }
            var h=RewardHero();var s=RewardService(()=>0,()=>0);s.BeginEncounter(h);var k=RewardClaim(s,h);CombatLootRewards.ProductionResult r;string error;
            Check(s.CompleteProduction(k,h,c,out r,out error)==CombatLootRewards.Outcome.Awarded && error.Contains("["+branch+"]"),"partial success with specific error");
            Check(r.MaterialOutcome==(branch=="material"?CombatLootRewards.Outcome.Failed:CombatLootRewards.Outcome.Awarded),"material isolated");
            Check(r.EquipmentOutcome==(branch=="equipment"?CombatLootRewards.Outcome.Failed:CombatLootRewards.Outcome.Awarded),"gear isolated");
            Check(EquipmentInventory.Count(h,EquipmentRefinement.MaterialId)==(branch=="material"?0:1) && h.equipmentItems.Count==(branch=="equipment"?0:1),"no rollback or fabricated replacement");
            Check(!s.Faulted && s.CompleteProduction(k,h,c,out r,out error)==CombatLootRewards.Outcome.Rejected,"no global fault or callback retry");
        });
        foreach(string bad in new[]{"range","quantity cap","policy","empty templates","duplicate template","main stat"})
        run("v01 additional validation / "+bad,()=>{
            var c=RewardTable(1,1);bool material=bad=="range"||bad=="quantity cap";
            if(bad=="range")c.material.minQuantity=3;
            if(bad=="quantity cap")c.material.maxQuantity=EquipmentInventory.MaxQuantity+1;
            if(bad=="policy")c.equipment.selectionPolicy=EquipmentRewardConfig.SelectionPolicy.Unconfigured;
            if(bad=="empty templates")c.equipment.templates.Clear();
            if(bad=="duplicate template")c.equipment.templates.Add(c.equipment.templates[0]);
            if(bad=="main stat")c.equipment.templates[0].mainStat=float.PositiveInfinity;
            var h=RewardHero();var s=RewardService(()=>0,()=>0);s.BeginEncounter(h);CombatLootRewards.ProductionResult r;string error;
            s.CompleteProduction(RewardClaim(s,h),h,c,out r,out error);
            Check((material?r.MaterialOutcome:r.EquipmentOutcome)==CombatLootRewards.Outcome.Failed,"invalid branch rejected");
            Check((material?r.EquipmentOutcome:r.MaterialOutcome)==CombatLootRewards.Outcome.Awarded,"valid branch still granted");
        });
        foreach(bool disableMaterial in new[]{false,true})
        run("v01 disabled group never draws or validates / material="+disableMaterial,()=>{
            var c=RewardTable(1,1);int md=0,gd=0;
            if(disableMaterial){c.material.enabled=false;c.material.chance=double.NaN;}else{c.equipment.enabled=false;c.equipment.templates.Clear();}
            var h=RewardHero();var s=RewardService(()=>{md++;return 0;},()=>{gd++;return 0;});s.BeginEncounter(h);CombatLootRewards.ProductionResult r;string error;
            Check(s.CompleteProduction(RewardClaim(s,h),h,c,out r,out error)==CombatLootRewards.Outcome.Awarded && error=="","enabled stream works");
            Check(disableMaterial?md==0 && gd==1:gd==0 && md==2,"disabled stream consumes no draws");
        });
        run("v01 inclusive material range and exact inventory limit",()=>{
            foreach(int quantity in new[]{1,2}) {
                int draw=0;var c=RewardTable(0,1);var h=RewardHero();EquipmentInventory.Add(h,EquipmentRefinement.MaterialId,9998);
                var s=RewardService(()=>draw++==0?0:quantity==1?0:.999999,()=>0);s.BeginEncounter(h);CombatLootRewards.ProductionResult r;string error;
                var outcome=s.CompleteProduction(RewardClaim(s,h),h,c,out r,out error);
                Check(outcome==(quantity==1?CombatLootRewards.Outcome.Awarded:CombatLootRewards.Outcome.Failed),"no partial clipping");
                Check(EquipmentInventory.Count(h,EquipmentRefinement.MaterialId)==(quantity==1?9999:9998),"exact limit and atomic quantity");
            }
        });
        run("v01 material overflow still commits decided equipment",()=>{
            var h=RewardHero();EquipmentInventory.Add(h,EquipmentRefinement.MaterialId,9999);var c=RewardTable(1,1);
            var s=RewardService(()=>0,()=>0);s.BeginEncounter(h);var k=RewardClaim(s,h);CombatLootRewards.ProductionResult r;string error;
            Check(s.CompleteProduction(k,h,c,out r,out error)==CombatLootRewards.Outcome.Awarded && r.MaterialOutcome==CombatLootRewards.Outcome.Failed,"partial failure");
            Check(h.equipmentItems.Count==1 && EquipmentInventory.Count(h,EquipmentRefinement.MaterialId)==9999,"gear retained, material not clipped");
            s.CompleteProduction(k,h,c,out r,out error);Check(h.equipmentItems.Count==1,"no duplicate gear");
        });
        foreach(bool duplicateId in new[]{false,true})
        run("v01 equipment factory failure retains material / duplicate="+duplicateId,()=>{
            var h=RewardHero();var existing=EquipmentFoundation.CreateCommon(EquipmentFoundation.DemoSword,1,7);EquipmentRuntime.AddInstance(h,existing);
            int calls=0;var s=RewardService(()=>0,()=>0,r=>{calls++;if(!duplicateId)throw new InvalidOperationException("fixture failure");return existing;});
            s.BeginEncounter(h);var k=RewardClaim(s,h);var c=RewardTable(1,1);CombatLootRewards.ProductionResult result;string error;
            Check(s.CompleteProduction(k,h,c,out result,out error)==CombatLootRewards.Outcome.Awarded && result.EquipmentOutcome==CombatLootRewards.Outcome.Failed,"failure recorded");
            Check(EquipmentInventory.Count(h,EquipmentRefinement.MaterialId)==1 && h.equipmentItems.Count==1 && calls==1,"no rollback, replacement or reroll");
            Check(s.CompleteProduction(k,h,c,out result,out error)==CombatLootRewards.Outcome.Rejected && calls==1,"terminal callback");
            s.CompleteProduction(RewardClaim(s,h,2),h,c,out result,out error);
            Check(EquipmentInventory.Count(h,EquipmentRefinement.MaterialId)==2 && calls==2,"later independent material stream still works");
        });
        run("v01 stale encounter pooling token and future save reject both",()=>{
            var h=RewardHero();var s=RewardService(()=>0,()=>0);s.BeginEncounter(h);var k=RewardClaim(s,h);s.Invalidate();
            CombatLootRewards.ProductionResult r;string error;
            Check(s.CompleteProduction(k,h,RewardTable(1,1),out r,out error)==CombatLootRewards.Outcome.Rejected,"stale encounter");
            s.BeginEncounter(h);var fresh=s.Register(1,2);Check(!s.TryClaim(k,h) && s.TryClaim(fresh,h),"old pooled spawn token rejected");
            h.inventoryVersion=EquipmentInventory.SchemaVersion+1;
            Check(s.CompleteProduction(fresh,h,RewardTable(1,1),out r,out error)==CombatLootRewards.Outcome.Failed && h.equipmentItems.Count==0,"future schema fail closed");
        });
        run("v01 invalidation during equipment creation keeps already committed material",()=>{
            var h=RewardHero();CombatLootRewards s=null;s=RewardService(()=>0,()=>0,r=>{s.Invalidate();return EquipmentFoundation.CreateCommon(r.id,r.itemLevel,r.mainStat);});
            s.BeginEncounter(h);CombatLootRewards.ProductionResult result;string error;
            s.CompleteProduction(RewardClaim(s,h),h,RewardTable(1,1),out result,out error);
            Check(result.MaterialOutcome==CombatLootRewards.Outcome.Awarded && result.EquipmentOutcome==CombatLootRewards.Outcome.Failed,"no unsafe rollback after context change");
            Check(EquipmentInventory.Count(h,EquipmentRefinement.MaterialId)==1 && h.equipmentItems.Count==0,"only pre-invalidation commit retained");
        });
        run("v01 seeded outcomes repeat and streams remain independent",()=>{
            Func<bool,bool,bool,Tuple<string,string>> sample=(materialEnabled,variableQuantity,equipmentEnabled)=>{
                var c=RewardTable(.08,.6);c.material.enabled=materialEnabled;if(!variableQuantity)c.material.maxQuantity=1;
                c.equipment.enabled=equipmentEnabled;
                c.equipment.templates.Add(new EquipmentRewardConfig.GearTemplate {templateId=EquipmentFoundation.DemoBow,itemLevel=2,mainStat=11});
                var mr=new System.Random(817);var gr=new System.Random(921);var s=RewardService(mr.NextDouble,gr.NextDouble);var h=RewardHero();s.BeginEncounter(h);
                var rows=new List<string>();var mats=new List<string>();for(int i=1;i<=200;i++) {CombatLootRewards.ProductionResult r;string error;s.CompleteProduction(RewardClaim(s,h,i),h,c,out r,out error);rows.Add(r.EquipmentOutcome+":"+r.Equipment.id+":"+r.Equipment.itemLevel+":"+r.Equipment.mainStat);mats.Add(r.MaterialOutcome+":"+r.Material.quantity);}
                return Tuple.Create(string.Join("|",rows),string.Join("|",mats));
            };
            var baseline=sample(true,true,true);var repeat=sample(true,true,true);
            Check(baseline.Equals(repeat),"same seed/config repeats outcomes (GUID identities stay unique)");
            Check(baseline.Item1==sample(false,true,true).Item1 && baseline.Item1==sample(true,false,true).Item1,"material draws never shift gear results");
            Check(baseline.Item2==sample(true,true,false).Item2,"gear draws never shift material results");
        });
        run("v01 enabled zero chance and invalid RNG are local",()=>{
            var c=RewardTable();c.material.chance=c.equipment.chance=0;int draws=0;
            var s=RewardService(()=>{draws++;return 1;},()=>{draws++;return 1;});var h=RewardHero();s.BeginEncounter(h);CombatLootRewards.ProductionResult r;string error;
            Check(s.CompleteProduction(RewardClaim(s,h),h,c,out r,out error)==CombatLootRewards.Outcome.NoDrop && draws==0,"zero chance never grants or draws");
            s=RewardService(()=>1,()=>0);s.BeginEncounter(h);
            Check(s.CompleteProduction(RewardClaim(s,h),h,RewardTable(1,1),out r,out error)==CombatLootRewards.Outcome.Awarded && r.MaterialOutcome==CombatLootRewards.Outcome.Failed && r.EquipmentOutcome==CombatLootRewards.Outcome.Awarded,"bad material RNG cannot suppress equipment");
        });
        run("v01 accepted death grants both once with only gear VFX and unchanged combat RNG",()=>WithLootSprite(()=>{
            UnityEngine.Random.InitState(19);GameManager gm;var f=LootField(false,out gm);Set(gm,"productionLoot",RewardTable(1,1));
            var target=f.Monster(-20);LootBattle(f,gm,target);var victim=f.Enemies[0];UnityEngine.Random.InitState(19);Lethal(f,victim);float after=UnityEngine.Random.Range(0f,1f);
            Check(!gm.ControlledLootTestEnabled && gm.HeroData.equipmentItems.Count==1 && EquipmentInventory.Count(gm.HeroData,EquipmentRefinement.MaterialId)>=1 && VisibleLoot(f)==1,"both rewards, one sword visual");
            DeathCallback(f,victim);Lethal(f,victim);Check(gm.HeroData.equipmentItems.Count==1 && VisibleLoot(f)==1,"duplicate callbacks");
            UnityEngine.Random.InitState(19);GameManager other;var off=LootField(false,out other);target=off.Monster(-20);LootBattle(off,other,target);UnityEngine.Random.InitState(19);Lethal(off,off.Enemies[0]);
            Check(after==UnityEngine.Random.Range(0f,1f) && gm.HeroData.currentExp==other.HeroData.currentExp,"combat RNG/EXP unchanged");
        }));
        run("v01 material-only death does not create sword VFX",()=>WithLootSprite(()=>{
            GameManager gm;var f=LootField(false,out gm);Set(gm,"productionLoot",RewardTable(0,1));var m=f.Monster(-20);LootBattle(f,gm,m);Lethal(f,f.Enemies[0]);
            Check(EquipmentInventory.Count(gm.HeroData,EquipmentRefinement.MaterialId)>=1 && gm.HeroData.equipmentItems.Count==0 && VisibleLoot(f)==0,"material feedback is not a fake sword");
        }));
        run("v01 both streams feed real Equipment and Forge",()=>{
            GameManager gm;var f=LootField(false,out gm);var c=RewardTable(1,1);c.material.minQuantity=c.material.maxQuantity=14;Set(gm,"productionLoot",c);
            var m=f.Monster(-20);LootBattle(f,gm,m);Lethal(f,f.Enemies[0]);var item=gm.HeroData.equipmentItems[0];
            Check(gm.EquipGear(item.instanceId,EquipmentSlot.Weapon),"equip real awarded gear");int before=WeaponBasicAttack.DamageSource(gm.HeroData);string error;
            Check(gm.ApplyForgeTest(item.instanceId,EquipmentRefinement.Operation.NormalUp,out error) && EquipmentInventory.Count(gm.HeroData,EquipmentRefinement.MaterialId)==13,"normal cost");
            Check(gm.ApplyForgeTest(item.instanceId,EquipmentRefinement.Operation.SpecialUp,out error) && EquipmentInventory.Count(gm.HeroData,EquipmentRefinement.MaterialId)==3,"special cost");
            Check(WeaponBasicAttack.DamageSource(gm.HeroData)>before && item.forgeHistory.Count>0,"stats/history");
        });
        Check(matched>0,"production test selector matched a case");
    }
}
