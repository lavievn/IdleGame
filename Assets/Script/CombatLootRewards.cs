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
    private sealed class Receipt
    {
        public KillKey Key;
        public bool Claimed, Completed;
    }
    private readonly Dictionary<int,Receipt> lives=new Dictionary<int,Receipt>();
    private readonly Func<EquipmentInstance> factory;
    private readonly Func<double> productionRoll;
    private EntityDataSO owner;
    private Guid runEpoch, encounter;
    private int mapVersion, mapNumber;
    private string mapId;
    public bool Faulted {get;private set;}
    public int ReceiptCount => lives.Count;

    public CombatLootRewards():this(EquipmentFoundation.CreateDemoSword) {}
    // Injection for controlled Profile A tests. Production uses explicit table entries.
    public CombatLootRewards(Func<EquipmentInstance> create):this(create,null) {}
    public CombatLootRewards(Func<EquipmentInstance> create,Func<double> roll)
    {factory=create;var random=new Random();productionRoll=roll??new Func<double>(random.NextDouble);}
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
        out ProductionLootConfig.Reward reward,out string error)
    {
        reward=default(ProductionLootConfig.Reward);error="";
        if(!IsCurrent(key,primary))return Outcome.Rejected;
        var receipt=lives[key.Life];
        if(!receipt.Claimed || receipt.Completed)return Outcome.Rejected;
        receipt.Completed=true; // Same terminal receipt; no second mode, retry or reroll.
        if(Faulted || config==null || !config.configured)return Outcome.NoDrop;
        try
        {
            string reason;
            if(!EquipmentFoundation.TryValidateSaveVersions(primary,out reason) || !config.Validate(out reason))
                throw new InvalidOperationException(reason);
            ProductionLootConfig.Reward selected;
            if(!config.TrySelect(productionRoll(),out selected,out reason))
            {
                if(!string.IsNullOrEmpty(reason))throw new InvalidOperationException(reason);
                return Outcome.NoDrop;
            }
            if(!IsCurrent(key,primary))throw new InvalidOperationException("Encounter changed during reward selection.");
            if(selected.kind==ProductionLootConfig.Kind.CommonEquipment)
            {
                var item=EquipmentFoundation.CreateCommon(selected.id,selected.itemLevel,selected.mainStat);
                if(!EquipmentFoundation.IsValid(item) || !EquipmentRuntime.AddInstance(primary,item))
                    throw new InvalidOperationException("Equipment reward was rejected; no replacement ID generated.");
                selected.instanceId=item.instanceId;
            }
            else if(!EquipmentInventory.Add(primary,selected.id,selected.quantity))
                throw new InvalidOperationException("Material reward was rejected; no partial quantity granted.");
            reward=selected;return Outcome.Awarded;
        }
        catch(Exception ex)
        {
            Faulted=true;
            error="[LOOT PRODUCTION] FAILED run="+key.RunEpoch+" encounter="+key.Encounter+
                " life="+key.Life+" spawn="+key.Spawn+": "+ex.Message;
            return Outcome.Failed;
        }
    }
}
