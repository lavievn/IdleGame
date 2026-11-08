using System;
using TuTienCore;
using UnityEngine;

public partial class SaveManager
{
    public void ConfigureCultivationProductionBeforeAwake(CultivationPlaytestProfile profile)
    {
        if(awakeStarted || !customSaveDirectoryRequested || profile==null || !profile.IsProduction)throw new InvalidOperationException("Explicit production fixture context required before Awake.");
        CultivationProfile=profile;
    }
    void ConfigureDefaultCultivation()
    {
        if(CultivationProfile!=null || !CultivationIntegrationGate.ProductionEnabled)return;
        CultivationPlaytestProfile profile;WorldQiDistribution table;CultivationConfigurationReason reason;string detail;
        if(CultivationProductionConfiguration.TryLoad(out profile,out table,out reason,out detail))CultivationProfile=profile;
        else LastCultivationFailure=reason+": "+detail; // Fail closed: no component initialization or implicit migration.
    }
    public bool NeedsCultivationMigration(SaveSlot slot)
    {
        if(CultivationProfile==null || !CultivationProfile.IsProduction)return false;
        string component,reason;string json=LoadGame(slot.ToString());
        return json!=null && CultivationGameplayState.TryReadHeroJson(json,CultivationProfile,out component,out reason) && string.IsNullOrEmpty(component);
    }
    bool NeedsMigrationReceiptRecovery(SaveSlot slot)
    {
        CultivationMigrationReason reason;
        if(CultivationMigrationTransaction.CompleteInterruptedReceipt(saveDirectory,slot,CultivationProfile,out reason))return false;
        LastCultivationFailure="Migration recovery: "+reason;return true;
    }
    bool ValidateMigrationSnapshot(string json,bool legacy)
    {
        var candidate=ScriptableObject.CreateInstance<EntityDataSO>();
        try {return TryLoadSnapshot(candidate,json,SaveSlot.AutoSave,legacy);}
        finally {ReleaseMigrationCandidate(candidate);}
    }
    public static void ReleaseMigrationCandidate(EntityDataSO candidate)
    {
        if(candidate==null)return;
#if UNITY_EDITOR
        if(!Application.isPlaying){UnityEngine.Object.DestroyImmediate(candidate);return;}
#endif
        UnityEngine.Object.Destroy(candidate);
    }
    public bool TryPrepareCultivationMigration(SaveSlot slot,out CultivationMigrationPlan plan,out EntityDataSO candidate,out CultivationMigrationReason reason)
    {
        candidate=null;
        if(!CultivationMigrationTransaction.TryPrepare(saveDirectory,slot,CultivationProfile,json=>ValidateMigrationSnapshot(json,true),json=>ValidateMigrationSnapshot(json,false),out plan,out reason))return false;
        candidate=ScriptableObject.CreateInstance<EntityDataSO>();
        if(!TryLoadSnapshot(candidate,plan.UpgradedJson,slot,false)){ReleaseMigrationCandidate(candidate);candidate=null;plan=null;reason=CultivationMigrationReason.InvalidSource;return false;}
        return true;
    }
    public bool CommitCultivationMigration(CultivationMigrationPlan plan,bool explicitConsent,out CultivationMigrationReason reason)
    {
        if(plan==null || plan.Directory!=saveDirectory){reason=CultivationMigrationReason.InvalidSource;return false;}
        return CultivationMigrationTransaction.Commit(plan,explicitConsent,json=>ValidateMigrationSnapshot(json,false),out reason,MigrationFault);
    }
    public bool RestoreCultivationBackup(CultivationMigrationPlan plan,string expectedCurrentHash,out CultivationMigrationReason reason)
    {
        if(plan==null || plan.Directory!=saveDirectory){reason=CultivationMigrationReason.RollbackBlocked;return false;}
        return CultivationMigrationTransaction.RestoreOriginal(plan,expectedCurrentHash,out reason);
    }
#if UNITY_EDITOR
    public string FailNextCultivationMigration;
    public long SuccessfulSnapshotWrites {get;private set;}
#endif
    void MigrationFault(string point)
    {
#if UNITY_EDITOR
        if(FailNextCultivationMigration!=point)return;
        if(!customSaveDirectoryRequested)throw new InvalidOperationException("Never fault a player Save directory.");
        FailNextCultivationMigration=null;throw new System.IO.IOException("Owned migration fixture fault: "+point);
#endif
    }
}
