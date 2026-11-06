using System;
using TuTienCore;

public partial class SaveManager
{
    public CultivationPlaytestProfile CultivationProfile {get;private set;}
    bool migrateCultivationInOwnedPlaytest;
    public string LastCultivationFailure {get;private set;}
    public void ConfigureCultivationPlaytestBeforeAwake(CultivationPlaytestProfile profile,bool initializeLegacy)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if(awakeStarted || !customSaveDirectoryRequested || string.IsNullOrEmpty(saveDirectory) || profile==null)
            throw new InvalidOperationException("Playtest requires an explicit isolated Save directory before Awake.");
        CultivationProfile=profile;migrateCultivationInOwnedPlaytest=initializeLegacy;
#else
        throw new InvalidOperationException("Cultivation playtest is unavailable here.");
#endif
    }
    Guid canonicalCultivationOwner;string canonicalCultivationConfiguration;int canonicalCultivationVersion;
    internal void AuthorizeCultivationAutoSnapshot(Guid owner,CultivationPlaytestProfile profile){canonicalCultivationOwner=owner;canonicalCultivationConfiguration=profile.Id;canonicalCultivationVersion=profile.Version;}
    bool CanReplaceCultivationConfiguration(string previous,string incoming,string slot)
    {
        if(string.IsNullOrEmpty(previous) || string.IsNullOrEmpty(incoming))return true;
        CultivationSession a,b;string reason;if(!CultivationGameplayState.TryRestore(previous,CultivationProfile,out a,out reason) || !CultivationGameplayState.TryRestore(incoming,CultivationProfile,out b,out reason))return false;
        CultivationPlaytestProfile pa,pb;if(!CultivationProductionConfiguration.TryResolve(previous,CultivationProfile,out pa,out reason) || !CultivationProductionConfiguration.TryResolve(incoming,CultivationProfile,out pb,out reason))return false;
        if(pa.Id==pb.Id && pa.Version==pb.Version)return true;
        bool canonicalAuto=slot==SaveSlot.AutoSave.ToString() && b.OwnerId==canonicalCultivationOwner && pb.Id==canonicalCultivationConfiguration && pb.Version==canonicalCultivationVersion;
        if(!canonicalAuto)LastCultivationFailure="Configuration upgrade requires explicit consent; manual slot preserved.";return canonicalAuto;
    }
    bool ValidateCultivationComponent(string component)
    {
        if(CultivationProfile!=null && string.IsNullOrEmpty(component)){LastCultivationFailure="Missing cultivation component on a playtest Hero.";return false;}
        CultivationSession candidate;string reason;
        bool valid=CultivationGameplayState.TryRestore(component,CultivationProfile,out candidate,out reason);
        LastCultivationFailure=reason;return valid;
    }
    bool ValidateCultivationJson(string json,out string component,bool allowLegacy=false)
    {
        string reason;bool valid=CultivationGameplayState.TryReadHeroJson(json,CultivationProfile,out component,out reason);
        if(valid && CultivationProfile!=null && CultivationProfile.IsProduction && string.IsNullOrEmpty(component) && !allowLegacy){valid=false;reason="LegacyMigrationRequired";}
        LastCultivationFailure=reason;return valid;
    }
    string CultivationForLoadedCandidate(string validatedComponent)
    {
        if(!string.IsNullOrEmpty(validatedComponent))return validatedComponent;
        // Explicit owned-playtest policy only. Legacy EXP/Level/items/world are never inputs.
        return migrateCultivationInOwnedPlaytest?CultivationGameplayState.Capture(CultivationGameplayState.New(CultivationProfile),CultivationProfile):"";
    }
#if UNITY_EDITOR
    public enum OwnedWriteFault { None, BeforeWrite, AfterTempWrite, BeforeReplace }
    public OwnedWriteFault FailNextOwnedWrite;
    void CheckOwnedWriteFault(OwnedWriteFault point)
    {
        if(FailNextOwnedWrite!=point)return;
        if(!customSaveDirectoryRequested)throw new InvalidOperationException("Never inject faults into the default player directory.");
        FailNextOwnedWrite=OwnedWriteFault.None;throw new System.IO.IOException("Expected owned fixture fault: "+point);
    }
#endif
}
