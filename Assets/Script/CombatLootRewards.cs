using System;
using System.Collections.Generic;

// Shared production/Profile A runtime receipts, never a serialized reward ledger.
public sealed class CombatLootRewards
{
    public struct KillKey
    {
        public readonly Guid RunEpoch, Encounter;
        public readonly int Life, Spawn;
        public KillKey(Guid run,Guid encounter,int life,int spawn)
        {RunEpoch=run;Encounter=encounter;Life=life;Spawn=spawn;}
    }
    public enum Outcome { Rejected, NoDrop, Awarded, Failed }
    public struct ProductionResult
    {
        public Outcome MaterialOutcome,EquipmentOutcome;
        public ProductionLootConfig.Reward Material,Equipment;
        public string MaterialError,EquipmentError;
    }
    private sealed class Receipt
    {
        public KillKey Key;
        public bool Claimed, Completed;
    }
    private readonly Dictionary<int,Receipt> lives=new Dictionary<int,Receipt>();
    private readonly Func<EquipmentInstance> factory;
    private readonly Func<double> materialRoll,equipmentRoll;
    private readonly Func<ProductionLootConfig.Reward,EquipmentInstance> equipmentFactory;
    private EntityDataSO owner;
    private Guid runEpoch, encounter;
    private int mapVersion, mapNumber;
    private string mapId;
    public bool Faulted {get;private set;}
    public int ReceiptCount => lives.Count;

    public CombatLootRewards():this(EquipmentFoundation.CreateDemoSword) {}
    // Injection for controlled Profile A tests. Production uses explicit table entries.
    public CombatLootRewards(Func<EquipmentInstance> create):this(create,null,null,null) {}
    public CombatLootRewards(Func<EquipmentInstance> create,Func<double> material,Func<double> equipment,
        Func<ProductionLootConfig.Reward,EquipmentInstance> makeEquipment=null)
    {
        factory=create;
        // Distinct seeds and states: changing quantity draws in one stream must
        // never shift the other. Never use the combat UnityEngine.Random stream.
        var mr=new Random(Guid.NewGuid().GetHashCode());var er=new Random(Guid.NewGuid().GetHashCode());
        materialRoll=material??new Func<double>(mr.NextDouble);equipmentRoll=equipment??new Func<double>(er.NextDouble);
        equipmentFactory=makeEquipment??(r=>EquipmentFoundation.CreateCommon(r.id,r.itemLevel,r.mainStat));
    }
    public void BeginEncounter(EntityDataSO primary)
    {
        Invalidate();
        if(primary==null)return;
        if(!ReferenceEquals(owner,primary)) {owner=primary;runEpoch=Guid.NewGuid();}
        mapVersion=primary.worldMapVersion;mapNumber=primary.mapNumber;mapId=primary.worldMapId;
        encounter=Guid.NewGuid();
    }
    public void Invalidate() {encounter=Guid.Empty;lives.Clear();}
    public KillKey Register(int life,int spawn)
    {
        if(encounter==Guid.Empty || lives.ContainsKey(life))return default(KillKey);
        var key=new KillKey(runEpoch,encounter,life,spawn);
        lives.Add(life,new Receipt {Key=key});return key;
    }
    public bool MatchesOwner(EntityDataSO primary)
    {
        return primary!=null && ReferenceEquals(owner,primary) &&
            primary.worldMapVersion==mapVersion && primary.worldMapId==mapId && primary.mapNumber==mapNumber;
    }
    public bool IsCurrent(KillKey key,EntityDataSO primary)
    {
        Receipt receipt;
        return MatchesOwner(primary) && encounter!=Guid.Empty &&
            key.RunEpoch==runEpoch && key.Encounter==encounter && lives.TryGetValue(key.Life,out receipt) &&
            receipt.Key.Spawn==key.Spawn;
    }
    public bool TryClaim(KillKey key,EntityDataSO primary)
    {
        if(!IsCurrent(key,primary))return false;
        var receipt=lives[key.Life];
        if(receipt.Claimed)return false;
        receipt.Claimed=true;return true;
    }
    public Outcome Complete(KillKey key,EntityDataSO primary,bool enabled,
        out string itemId,out string error)
    {
        itemId="";error="";
        if(!IsCurrent(key,primary))return Outcome.Rejected;
        var receipt=lives[key.Life];
        if(!receipt.Claimed || receipt.Completed)return Outcome.Rejected;
        // Terminal BEFORE calling a factory/API: no re-entry or reroll on failure.
        receipt.Completed=true;
        if(!enabled || Faulted)return Outcome.NoDrop;
        try
        {
            string reason;
            if(!EquipmentFoundation.TryValidateSaveVersions(primary,out reason))
                throw new InvalidOperationException(reason);
            var item=factory();
            if(!IsCurrent(key,primary))throw new InvalidOperationException("Encounter changed during reward creation.");
            if(item==null || !EquipmentFoundation.IsValid(item) ||
                !EquipmentFoundation.SupportsItemVersions(item) ||
                item.templateId!=EquipmentFoundation.DemoSword || item.level!=1 ||
                item.rarity!=EquipmentRarity.Common || item.refineLevel!=0 ||
                item.extraStats.Count!=0 || item.isEquipped ||
                !EquipmentRuntime.AddInstance(primary,item))
                throw new InvalidOperationException("CreateDemoSword/AddInstance rejected; no replacement ID will be generated.");
            itemId=item.instanceId;return Outcome.Awarded;
        }
        catch(Exception ex)
        {
            Faulted=true;
            error="[LOOT TEST A] FAILED run="+key.RunEpoch+" encounter="+key.Encounter+
                " life="+key.Life+" spawn="+key.Spawn+": "+ex.Message+
                " Awarding disabled for this CombatManager; fix the data and restart Play Mode.";
            return Outcome.Failed;
        }
    }

    public Outcome CompleteProduction(KillKey key,EntityDataSO primary,ProductionLootConfig config,
        out ProductionResult result,out string error)
    {
        result=default(ProductionResult);error="";
        if(!IsCurrent(key,primary))return Outcome.Rejected;
        var receipt=lives[key.Life];
        if(!receipt.Claimed || receipt.Completed)return Outcome.Rejected;
        receipt.Completed=true; // Same terminal receipt; no second mode, retry or reroll.
        result.MaterialOutcome=result.EquipmentOutcome=Outcome.NoDrop;
        if(Faulted || config==null || !config.configured)return Outcome.NoDrop;
        string reason;
        if(!EquipmentFoundation.TryValidateSaveVersions(primary,out reason))
        {
            if(config.material!=null && config.material.enabled) {result.MaterialOutcome=Outcome.Failed;result.MaterialError=reason;}
            if(config.equipment!=null && config.equipment.enabled) {result.EquipmentOutcome=Outcome.Failed;result.EquipmentError=reason;}
            return FinishProduction(key,ref result,out error);
        }
        // Decide BOTH streams before committing either. Failed planning is local
        // to its stream; the other decision is not skipped or rolled again.
        ProductionLootConfig.Reward material=default(ProductionLootConfig.Reward),equipment=default(ProductionLootConfig.Reward);
        try {material=config.RollMaterial(materialRoll);}
        catch(Exception ex) {result.MaterialOutcome=Outcome.Failed;result.MaterialError=ex.Message;}
        try {equipment=config.RollEquipment(equipmentRoll);}
        catch(Exception ex) {result.EquipmentOutcome=Outcome.Failed;result.EquipmentError=ex.Message;}
        if(material.kind!=ProductionLootConfig.Kind.Unconfigured)
        {
            try {
                if(!IsCurrent(key,primary))throw new InvalidOperationException("Stale encounter before material grant.");
                if(!EquipmentInventory.Add(primary,material.id,material.quantity))throw new InvalidOperationException("Material inventory cannot accept the decided quantity.");
                result.Material=material;result.MaterialOutcome=Outcome.Awarded;
            } catch(Exception ex) {result.MaterialOutcome=Outcome.Failed;result.MaterialError=ex.Message;}
        }
        if(equipment.kind!=ProductionLootConfig.Kind.Unconfigured)
        {
            try {
                if(!IsCurrent(key,primary))throw new InvalidOperationException("Stale encounter before equipment creation.");
                var item=equipmentFactory(equipment);
                if(!IsCurrent(key,primary) || !EquipmentFoundation.TryValidateSaveVersions(primary,out reason))
                    throw new InvalidOperationException("Encounter or save data changed during equipment creation.");
                var template=EquipmentFoundation.FindTemplate(equipment.id);
                if(!EquipmentFoundation.IsValid(item) || !EquipmentFoundation.SupportsItemVersions(item) ||
                    item.templateId!=equipment.id || item.level!=equipment.itemLevel || item.rarity!=EquipmentRarity.Common ||
                    item.refineLevel!=0 || item.isEquipped || item.extraStats.Count!=0 || item.mainStats.Count!=1 ||
                    item.mainStats[0].stat!=template.mainStat || item.mainStats[0].flat!=equipment.mainStat ||
                    item.mainStats[0].percent!=0 || !EquipmentRuntime.AddInstance(primary,item))
                    throw new InvalidOperationException("Decided equipment rejected; no replacement ID or reroll.");
                equipment.instanceId=item.instanceId;result.Equipment=equipment;result.EquipmentOutcome=Outcome.Awarded;
            } catch(Exception ex) {result.EquipmentOutcome=Outcome.Failed;result.EquipmentError=ex.Message;}
        }
        return FinishProduction(key,ref result,out error);
    }
    private static Outcome FinishProduction(KillKey key,ref ProductionResult result,out string error)
    {
        string context=" run="+key.RunEpoch+" encounter="+key.Encounter+" life="+key.Life+" spawn="+key.Spawn;
        error="";
        if(result.MaterialOutcome==Outcome.Failed)error="[LOOT PRODUCTION][material]"+context+": "+result.MaterialError;
        if(result.EquipmentOutcome==Outcome.Failed)error+=(error.Length>0?"\n":"")+"[LOOT PRODUCTION][equipment]"+context+": "+result.EquipmentError;
        if(result.MaterialOutcome==Outcome.Awarded || result.EquipmentOutcome==Outcome.Awarded)return Outcome.Awarded;
        if(result.MaterialOutcome==Outcome.Failed || result.EquipmentOutcome==Outcome.Failed)return Outcome.Failed;
        return Outcome.NoDrop;
    }
}
