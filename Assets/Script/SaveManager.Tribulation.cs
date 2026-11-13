using System;
using System.IO;
using UnityEngine;
using TuTienCore;
public partial class SaveManager
{
    public HeavenlyGateConfiguration TrialConfiguration {get;private set;}public HeavenlyGatePenaltyPolicy TrialPenalty {get;private set;}
    public bool HasDevelopmentCultivationLoop => TrialConfiguration?.IsDevelopment == true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    FileStream developmentProfileLock;

#if UNITY_5_3_OR_NEWER
    void ConfigureDefaultDevelopmentLoop()
    {
        if (!CultivationIntegrationGate.DevelopmentCultivationLoopEnabled ||
            customSaveDirectoryRequested || gameObject.scene.name != "MainGame" ||
            (Application.platform != RuntimePlatform.WindowsEditor &&
             Application.platform != RuntimePlatform.WindowsPlayer))
            return;

        try
        {
            var fingerprint = DevelopmentCultivation015.Load(out var profile, out var gate, out var penalty);
            ConfigureSaveDirectoryBeforeAwake(DevelopmentCultivation015.PlayerSaveDirectory(fingerprint));
            Directory.CreateDirectory(saveDirectory);
            string lockPath = Path.Combine(saveDirectory, ".development-015.lock");
            if (File.Exists(lockPath) && (File.GetAttributes(lockPath) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Development save lock must not be a link.");
            developmentProfileLock = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            ConfigureDevelopmentLoop(profile, gate, penalty);
            Debug.Log("[DEV 0.1.5] Saves: " + saveDirectory);
        }
        catch
        {
            // Never let a second instance or a bad configuration enter gameplay on this profile.
            gameObject.SetActive(false);
            developmentProfileLock?.Dispose();
            throw;
        }
    }

    // The existing marker-owned test launchers select their path first.
    internal void ConfigureOwnedDevelopmentLoopBeforeAwake()
    {
        DevelopmentCultivation015.Load(out var profile, out var gate, out var penalty);
        ConfigureDevelopmentLoop(profile, gate, penalty);
    }

    void ConfigureDevelopmentLoop(CultivationPlaytestProfile profile, HeavenlyGateConfiguration gate,
        HeavenlyGatePenaltyPolicy penalty)
    {
        ConfigureCultivationPlaytestBeforeAwake(profile, false);
        TrialConfiguration = gate.Copy();
        TrialPenalty = penalty;
    }
#endif
#endif

    internal bool HasInterruptedDevelopmentTrial()
    {
        return HasDevelopmentCultivationLoop && ExistingTrialBlocks(Path.Combine(saveDirectory, "AutoSave.json"));
    }

    internal bool TryReadInterruptedDevelopmentTrial(out TribulationSaveReceipt receipt, out string json,
        out string hash, out string reason)
    {
        receipt = null;
        json = hash = null;
        reason = "Không tìm thấy checkpoint Active hợp lệ";
        if (!HasDevelopmentCultivationLoop) return false;
        try
        {
            string path = Path.Combine(saveDirectory, "AutoSave.json");
            if (!File.Exists(path)) return false;
            var bytes = File.ReadAllBytes(path);
            json = CultivationMigrationTransaction.Text(bytes);
            if (!ReadTrialRoot(json, out var trialComponent, out reason) || string.IsNullOrEmpty(trialComponent) ||
                !CultivationGameplayState.TryReadHeroJson(json, CultivationProfile, out var cultivation, out reason) ||
                !TribulationSaveTransaction.Validate(trialComponent, cultivation, CultivationProfile,
                    TrialConfiguration, TrialPenalty, out reason))
                return false;

            var active = TribulationSaveTransaction.Read(trialComponent);
            if (active.outcome != "Active") return false;
            receipt = active;
            hash = CultivationMigrationTransaction.Hash(bytes);
            return true;
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
        {
            reason = e.Message;
            return false;
        }
    }

    internal bool TryLoadInterruptedTrialCandidate(EntityDataSO candidate, string json)
        => HasDevelopmentCultivationLoop && TryLoadSnapshot(candidate, json, SaveSlot.AutoSave, false, allowActiveTrial: true);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public string FailNextTrialWrite;
#endif
    public void ConfigureTribulationFixtureBeforeAwake(HeavenlyGateConfiguration config,HeavenlyGatePenaltyPolicy penalty)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if(awakeStarted||!customSaveDirectoryRequested||CultivationProfile==null||CultivationProfile.IsProduction||config==null||!config.IsFixture||!config.IsValid||config.cultivationConfiguration!=CultivationProfile.Id||penalty==null||config.penaltyPolicyId!=penalty.Id)throw new InvalidOperationException("Trial requires isolated pre-Awake fixture profile and explicit policy");
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
        reason=null;if(TrialConfiguration==null||!customSaveDirectoryRequested||candidate==null||!ExperienceProgress.TryRead(candidate.experienceJson,out _)||string.IsNullOrEmpty(candidate.tribulationJson)||candidate.combatSkills==null||!candidate.combatSkills.IsValid()||!ValidateTrial(candidate,out reason)||!ValidateCultivationComponent(candidate.cultivationJson)||!EquipmentFoundation.TryValidateSaveVersions(candidate,out reason))return false;
        if(!MasteryFoundation.IsValid(candidate.mastery)){reason="Invalid Mastery snapshot";return false;}
        Action<string> fault=point=>{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(developmentProfileLock==null&&FailNextTrialWrite==point){FailNextTrialWrite=null;throw new IOException("Owned trial fault "+point);}
#endif
        };
        bool ok=TribulationSaveTransaction.Commit(saveDirectory,SaveSlot.AutoSave,expectedHash,JsonUtility.ToJson(candidate,true),TribulationSaveTransaction.Read(candidate.tribulationJson).sourceSnapshotHash,fault,out reason);
        if(ok)candidate.isDirty=false;return ok;
    }
}
