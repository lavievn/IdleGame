using System;
using System.Collections.Generic;

// Profile A only. Runtime receipts, not a serialized checkpoint or reward ledger.
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
    private EntityDataSO owner;
    private Guid runEpoch, encounter;
    private int mapVersion, mapNumber;
    private string mapId;
    public bool Faulted {get;private set;}
    public int ReceiptCount => lives.Count;

    public CombatLootRewards():this(EquipmentFoundation.CreateDemoSword) {}
    // Injection for isolated failure/re-entry tests; production uses the factory above.
    public CombatLootRewards(Func<EquipmentInstance> create) {factory=create;}
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
}
