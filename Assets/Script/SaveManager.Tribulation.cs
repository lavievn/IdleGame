using System;
using System.IO;
using UnityEngine;
using TuTienCore;
public partial class SaveManager
{
    public HeavenlyGateConfiguration TrialConfiguration {get;private set;}public HeavenlyGatePenaltyPolicy TrialPenalty {get;private set;}
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public string FailNextTrialWrite;
#endif
    public void ConfigureTribulationFixtureBeforeAwake(HeavenlyGateConfiguration config,HeavenlyGatePenaltyPolicy penalty)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if(awakeStarted||!customSaveDirectoryRequested||CultivationProfile==null||CultivationProfile.IsProduction||config==null||!config.IsValid||config.cultivationConfiguration!=CultivationProfile.Id||penalty==null||config.penaltyPolicyId!=penalty.Id)throw new InvalidOperationException("Trial requires isolated pre-Awake fixture profile and explicit policy");
        TrialConfiguration=config.Copy();TrialPenalty=penalty;
#else
        throw new InvalidOperationException("Production trial configuration is unapproved");
#endif
    }
    bool ExistingTrialBlocks(string path)
    {
        if(!File.Exists(path))return false;string component,why;
        try{string raw=File.ReadAllText(path);if(!ReadTrialRoot(raw,out component,out why))return raw.IndexOf("tribulationJson",StringComparison.Ordinal)>=0;if(string.IsNullOrEmpty(component))return false;var receipt=TribulationSaveTransaction.Read(component);string cultivation; if(!CultivationGameplayState.TryReadHeroJson(raw,CultivationProfile,out cultivation,out why)||!TribulationSaveTransaction.Validate(component,cultivation,CultivationProfile,TrialConfiguration,TrialPenalty,out why))return true;return receipt.outcome=="Active";}
        catch(Exception e) when(e is ArgumentException||e is IOException||e is UnauthorizedAccessException){return true;}
    }
    bool ValidateTrial(EntityDataSO hero,out string reason)=>TribulationSaveTransaction.Validate(hero.tribulationJson,hero.cultivationJson,CultivationProfile,TrialConfiguration,TrialPenalty,out reason);
    bool ReadTrialRoot(string json,out string component,out string reason)
    {
        component="";System.Collections.Generic.Dictionary<string,string> fields;
        if(!CultivationSaveJson.TryObject(json,out fields,out reason))return false;
        string token;if(fields.TryGetValue("tribulationJson",out token)&&!CultivationSaveJson.TryString(token,out component)){reason="Invalid trial component boundary";return false;}
        return true;
    }
    internal bool TryReadTrialCheckpoint(string expectedJson,out string hash)
    {
        hash=null;try{var bytes=File.ReadAllBytes(Path.Combine(saveDirectory,SaveSlot.AutoSave+".json"));if(CultivationMigrationTransaction.Text(bytes)!=expectedJson)return false;hash=CultivationMigrationTransaction.Hash(bytes);return true;}
        catch(Exception e) when(e is IOException||e is UnauthorizedAccessException){return false;}
    }
    public string TrialSnapshotHash(SaveSlot slot){string path=Path.Combine(saveDirectory,slot+".json");return File.Exists(path)?CultivationMigrationTransaction.Hash(File.ReadAllBytes(path)):null;}
    public bool CommitTrialCandidate(EntityDataSO candidate,string expectedHash,out string reason)
    {
        reason=null;if(TrialConfiguration==null||!customSaveDirectoryRequested||candidate==null||string.IsNullOrEmpty(candidate.tribulationJson)||!ValidateTrial(candidate,out reason)||!ValidateCultivationComponent(candidate.cultivationJson)||!EquipmentFoundation.TryValidateSaveVersions(candidate,out reason))return false;
        Action<string> fault=point=>{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(FailNextTrialWrite==point){FailNextTrialWrite=null;throw new IOException("Owned trial fault "+point);}
#endif
        };
        bool ok=TribulationSaveTransaction.Commit(saveDirectory,SaveSlot.AutoSave,expectedHash,JsonUtility.ToJson(candidate,true),TribulationSaveTransaction.Read(candidate.tribulationJson).sourceSnapshotHash,fault,out reason);
        if(ok)candidate.isDirty=false;return ok;
    }
}
