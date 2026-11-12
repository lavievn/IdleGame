using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TuTienCore;
using UnityEngine;

// Tests exercise production data APIs. All numeric loot settings below are
// fixtures; no fixture writes MainGame, player saves, or production config.
static class ItemLootFoundationTests
{
    static int passed, failed;
    static void Check(bool value,string message) {if(!value)throw new Exception(message);}
    static void Run(string name,Action test)
    {
        try {test();passed++;Console.WriteLine("PASS "+name);}
        catch(Exception ex) {failed++;Console.WriteLine("FAIL "+name+": "+ex.Message);}
    }
    static EntityDataSO Hero()
    {
        var h=ScriptableObject.CreateInstance<EntityDataSO>();
        EquipmentInventory.InitializeNewRun(h);EquipmentRuntime.InitializeNewRun(h);
        h.isDirty=false;return h;
    }
    static ProductionLootConfig Config(double material=1,double gear=1)
    {
        return new ProductionLootConfig {
            configured=true,
            material=new MaterialRewardConfig {enabled=true,chance=material,
                materialId=EquipmentRefinement.MaterialId,minQuantity=1,maxQuantity=2},
            equipment=new EquipmentRewardConfig {enabled=true,chance=gear,
                selectionPolicy=EquipmentRewardConfig.SelectionPolicy.Uniform,
                templates=new List<EquipmentRewardConfig.GearTemplate> {
                    new EquipmentRewardConfig.GearTemplate {templateId=EquipmentFoundation.DemoSword,itemLevel=1,mainStat=4},
                    new EquipmentRewardConfig.GearTemplate {templateId=EquipmentFoundation.DemoBow,itemLevel=1,mainStat=4}
                }}
        };
    }
    static CombatLootRewards.Outcome Award(CombatLootRewards service,EntityDataSO h,
        ProductionLootConfig config,int life,out CombatLootRewards.ProductionResult result,out string error)
    {
        var key=service.Register(life,1);
        Check(service.TryClaim(key,h),"new receipt must be claimable");
        return service.CompleteProduction(key,h,config,out result,out error);
    }
    static EquipmentTemplate[] Templates() => (EquipmentTemplate[])typeof(EquipmentFoundation)
        .GetField("templates",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
    static string Items(EntityDataSO h) => NestedEquipmentSaveStub.Encode(h.equipmentItems);

    static void Main(string[] args)
    {
        Run("all current catalog templates create unique valid instances in permitted slots",()=>{
            var ids=new HashSet<string>(StringComparer.Ordinal);var instances=new HashSet<string>();
            var slots=new HashSet<EquipmentSlot>();
            foreach(var t in Templates())
            {
                Check(!string.IsNullOrWhiteSpace(t.id) && ids.Add(t.id) && !string.IsNullOrWhiteSpace(t.name),"stable unique template IDs/names");
                Check(EquipmentFoundation.FindTemplate(t.id)==t,"ordinal lookup returns canonical template");
                for(int n=0;n<2;n++)
                {
                    var item=EquipmentFoundation.CreateCommon(t.id,1,4);
                    Check(EquipmentFoundation.IsValid(item) && instances.Add(item.instanceId),"unique factory instance");
                    Check(EquipmentFoundation.IsSlotAllowed(item,t.slot),"template slot allowed");slots.Add(t.slot);
                }
            }
            Check(slots.Count==7 && EquipmentFoundation.IsSlotAllowed(
                EquipmentFoundation.CreateCommon("base:ring",1,4),EquipmentSlot.RingRight),"all eight positions, shared ring template");
            Check(EquipmentFoundation.CreateCommon("missing:item",1,4)==null && EquipmentCatalog.Find("missing:item")==null,"unknown templates fail lookup/factory");
        });
        Run("material and gear cover all four independent outcomes with one terminal receipt",()=>{
            foreach(bool mat in new[]{false,true})foreach(bool gear in new[]{false,true})
            {
                var h=Hero();int md=0,gd=0;
                var service=new CombatLootRewards(EquipmentFoundation.CreateDemoSword,
                    ()=>{md++;return mat?0:.9;},()=>{gd++;return gear?0:.9;});
                service.BeginEncounter(h);var key=service.Register(1,1);Check(service.TryClaim(key,h),"claim");
                CombatLootRewards.ProductionResult result;string error;
                service.CompleteProduction(key,h,Config(.5,.5),out result,out error);
                Check(result.MaterialOutcome==(mat?CombatLootRewards.Outcome.Awarded:CombatLootRewards.Outcome.NoDrop) &&
                    result.EquipmentOutcome==(gear?CombatLootRewards.Outcome.Awarded:CombatLootRewards.Outcome.NoDrop),"distinct outcomes");
                int draws=md+gd;string items=Items(h);int stock=EquipmentInventory.Count(h,EquipmentRefinement.MaterialId);
                Check(service.CompleteProduction(key,h,Config(),out result,out error)==CombatLootRewards.Outcome.Rejected &&
                    draws==md+gd && items==Items(h) && stock==EquipmentInventory.Count(h,EquipmentRefinement.MaterialId),"duplicate callback neither rolls nor grants");
            }
        });
        Run("seeded stream independence survives changed quantities and disabled peer",()=>{
            Func<bool,bool,bool,Tuple<string,string>> sample=(material,variable,gear)=>{
                var config=Config(.6,.4);config.material.enabled=material;config.equipment.enabled=gear;
                if(!variable)config.material.maxQuantity=1;
                var mr=new System.Random(817);var gr=new System.Random(921);
                var service=new CombatLootRewards(EquipmentFoundation.CreateDemoSword,mr.NextDouble,gr.NextDouble);
                var h=Hero();service.BeginEncounter(h);var mats=new List<string>();var gears=new List<string>();
                for(int i=1;i<=128;i++)
                {
                    CombatLootRewards.ProductionResult result;string error;
                    Award(service,h,config,i,out result,out error);Check(error=="","valid fixture");
                    mats.Add(result.MaterialOutcome+":"+result.Material.quantity);
                    gears.Add(result.EquipmentOutcome+":"+result.Equipment.id);
                }
                return Tuple.Create(string.Join("|",mats),string.Join("|",gears));
            };
            var baseline=sample(true,true,true);
            Check(baseline.Equals(sample(true,true,true)),"repeatable decisions; GUID identity is intentionally unique");
            Check(baseline.Item2==sample(false,true,true).Item2 && baseline.Item2==sample(true,false,true).Item2,"material draws cannot shift gear");
            Check(baseline.Item1==sample(true,true,false).Item1,"gear draws cannot shift material");
        });
        Run("invalid templates fail only their stream; material overflow never partially grants",()=>{
            var h=Hero();var config=Config();config.equipment.templates[0].templateId="missing:gear";
            var service=new CombatLootRewards(EquipmentFoundation.CreateDemoSword,()=>0,()=>0);service.BeginEncounter(h);
            CombatLootRewards.ProductionResult result;string error;
            Award(service,h,config,1,out result,out error);
            Check(result.MaterialOutcome==CombatLootRewards.Outcome.Awarded && result.EquipmentOutcome==CombatLootRewards.Outcome.Failed && h.equipmentItems.Count==0,"bad gear does not erase material");
            EquipmentInventory.Add(h,EquipmentRefinement.MaterialId,EquipmentInventory.MaxQuantity-1);
            int before=EquipmentInventory.Count(h,EquipmentRefinement.MaterialId);
            Award(service,h,Config(),2,out result,out error);
            Check(result.MaterialOutcome==CombatLootRewards.Outcome.Failed && result.EquipmentOutcome==CombatLootRewards.Outcome.Awarded &&
                EquipmentInventory.Count(h,EquipmentRefinement.MaterialId)==before,"full stack rejects material branch without harming gear");
        });
        Run("changing map or invalidating encounter rejects stale receipts without changing owned items",()=>{
            var h=Hero();var service=new CombatLootRewards(EquipmentFoundation.CreateDemoSword,()=>0,()=>0);service.BeginEncounter(h);
            var key=service.Register(1,1);Check(service.TryClaim(key,h),"claim");h.worldMapId="changed";
            CombatLootRewards.ProductionResult result;string error;string before=Items(h);
            Check(service.CompleteProduction(key,h,Config(),out result,out error)==CombatLootRewards.Outcome.Rejected && Items(h)==before,"map context is checked");
            service.BeginEncounter(h);key=service.Register(2,1);Check(service.TryClaim(key,h),"claim fresh encounter");service.Invalidate();
            Check(service.CompleteProduction(key,h,Config(),out result,out error)==CombatLootRewards.Outcome.Rejected && Items(h)==before,"Retry/clear boundary API invalidates old claim");
        });
        Run("loot to inventory equip Forge and in-memory serialization preserves IDs stats stock and history",()=>{
            var h=Hero();var config=Config();config.material.minQuantity=config.material.maxQuantity=14;
            var service=new CombatLootRewards(EquipmentFoundation.CreateDemoSword,()=>0,()=>0);service.BeginEncounter(h);
            CombatLootRewards.ProductionResult result;string error;
            Check(Award(service,h,config,1,out result,out error)==CombatLootRewards.Outcome.Awarded,"real reward APIs");
            string id=result.Equipment.instanceId;var item=EquipmentRuntime.Find(h,id);
            Check(EquipmentRuntime.Equip(h,id,EquipmentSlot.Weapon),"equip received instance");
            Check(EquipmentRefinement.TryApply(h,id,EquipmentRefinement.Operation.NormalUp,out error) &&
                EquipmentRefinement.TryApply(h,id,EquipmentRefinement.Operation.SpecialUp,out error),"Forge data operations");
            Check(EquipmentInventory.Count(h,EquipmentRefinement.MaterialId)==3 && item.forgeHistory.Count==1 && item.mainStats[0].flat==4,"immutable base roll and exact prototype costs");
            string json=JsonUtility.ToJson(h);var copy=ScriptableObject.CreateInstance<EntityDataSO>();
            JsonUtility.FromJsonOverwrite(json,copy);EquipmentInventory.Normalize(copy);EquipmentRuntime.Normalize(copy);
            var restored=EquipmentRuntime.Find(copy,id);
            // Factory items still carry affix v0; load legitimately migrates
            // that metadata. Compare authoritative rolls and Forge history.
            Check(copy.equipmentItems.Count==1 && restored!=null && restored.level==item.level &&
                restored.rarity==item.rarity && restored.refineLevel==item.refineLevel &&
                restored.mainStats[0].flat==4 && restored.mainStats[0].stat==item.mainStats[0].stat &&
                restored.extraStats.Count==0 && restored.specialForgeCount==item.specialForgeCount &&
                restored.naturalRefineLimit==item.naturalRefineLimit &&
                NestedEquipmentSaveStub.Encode(restored.forgeHistory)==NestedEquipmentSaveStub.Encode(item.forgeHistory) &&
                EquipmentInventory.Count(copy,EquipmentRefinement.MaterialId)==3 &&
                EquipmentRuntime.Equipped(copy,EquipmentSlot.Weapon).instanceId==id,"data round trip, not SaveManager gameplay");
            string before=Items(copy);EquipmentRuntime.Normalize(copy);EquipmentInventory.Normalize(copy);
            Check(Items(copy)==before && EquipmentRuntime.Unequip(copy,EquipmentSlot.Weapon) && copy.equipmentItems.Count==1,"repeated reconciliation and unequip neither reroll nor duplicate");
        });
        Run("all weapon families keep physical magical stats separate and leave permanent HP untouched",()=>{
            var h=Hero();int health=h.GetCalculatedHealth(AttackMode.Melee),baseHP=h.baseHealth,growth=h.meleeGrowthBonus;
            int physical=h.GetCalculatedDamage(AttackMode.Melee);
            foreach(string id in new[]{EquipmentFoundation.DemoSword,EquipmentFoundation.DemoBow,EquipmentFoundation.DemoStaff})
            {
                var item=EquipmentFoundation.CreateCommon(id,1,4);Check(EquipmentRuntime.AddInstance(h,item),"add");
                Check(EquipmentRuntime.Equip(h,item.instanceId,EquipmentSlot.Weapon),"free weapon choice");
                bool magical=id==EquipmentFoundation.DemoStaff;
                Check(WeaponBasicAttack.Physical(h)==physical+(magical?0:4) && WeaponBasicAttack.Magical(h)==(magical?4:0),"separate attack channels");
                Check(h.GetCalculatedHealth(WeaponBasicAttack.Resolve(h))==health && h.baseHealth==baseHP && h.meleeGrowthBonus==growth,"no permanent or stance HP change");
                Check(EquipmentRuntime.Unequip(h,EquipmentSlot.Weapon) && h.GetCalculatedHealth(AttackMode.Melee)==health,"unequip restores original HP");
            }
        });
        Run("legacy fields migrate once; missing templates stay opaque without bonuses",()=>{
            var h=ScriptableObject.CreateInstance<EntityDataSO>();
            JsonUtility.FromJsonOverwrite("{\"equipmentItems\":[{\"instanceId\":\"legacy\",\"templateId\":\"sample:iron_sword\",\"level\":1,\"rarity\":0,\"mainStats\":[{\"stat\":1,\"flat\":4}]}]}",h);
            EquipmentRuntime.Normalize(h);var old=EquipmentRuntime.Find(h,"legacy");
            Check(EquipmentFoundation.IsValid(old) && old.mainStats[0].flat==4 && old.forgeSchemaVersion==EquipmentRefinement.SchemaVersion,"missing new fields use legacy migration without reroll");
            var unknown=EquipmentFoundation.CreateDemoSword();unknown.templateId="future:missing";unknown.isEquipped=true;h.equipmentItems.Add(unknown);
            EquipmentRuntime.Normalize(h);Check(h.equipmentItems.Contains(unknown) && !unknown.isEquipped && !EquipmentRuntime.Equip(h,unknown.instanceId,EquipmentSlot.Weapon),"unknown item preserved but cannot equip");
            string before=Items(h);h.isDirty=false;EquipmentRuntime.Normalize(h);
            Check(before==Items(h) && !h.isDirty,"normalization is stable");
        });
        Run("instance ownership is deep copied and duplicate IDs are refused",()=>{
            var h=Hero();var item=EquipmentRefinement.CreateForgeDemoSword();string before=NestedEquipmentSaveStub.Encode(item);
            Check(EquipmentRuntime.AddInstance(h,item) && !EquipmentRuntime.AddInstance(h,item),"one canonical instance per ID");
            var owned=EquipmentRuntime.Find(h,item.instanceId);owned.extraStats[0].flat+=1;owned.mainStats[0].requiredTags.Add("test:owned");
            Check(before==NestedEquipmentSaveStub.Encode(item),"incoming rolls and tag lists are not aliased");
        });
        Run("REGRESSION future Forge schema or checkpoint cannot enter inventory or mutate through Forge",()=>{
            foreach(bool history in new[]{false,true})
            {
                var h=Hero();var item=EquipmentFoundation.CreateDemoSword();
                if(history)item.forgeHistory.Add(new ForgeStageSnapshot {checkpointVersion=EquipmentRefinement.SchemaVersion+1});
                else item.forgeSchemaVersion=EquipmentRefinement.SchemaVersion+1;
                Check(!EquipmentFoundation.IsValid(item) && !EquipmentRuntime.AddInstance(h,item),"future item accepted despite SaveManager rejecting its version");
                h.equipmentItems.Add(item);EquipmentInventory.Add(h,EquipmentRefinement.MaterialId,20);
                string before=Items(h);string error;int stock=EquipmentInventory.Count(h,EquipmentRefinement.MaterialId);
                Check(!EquipmentRefinement.TryApply(h,item.instanceId,EquipmentRefinement.Operation.SpecialUp,out error) &&
                    Items(h)==before && stock==EquipmentInventory.Count(h,EquipmentRefinement.MaterialId),"future Forge state must be read-only");
            }
        });
        Console.WriteLine("RESULT "+passed+" PASS, "+failed+" FAIL (Mono/UnityStub; no SaveManager or save I/O)");
        if(failed>0)Environment.Exit(1);
    }
}
