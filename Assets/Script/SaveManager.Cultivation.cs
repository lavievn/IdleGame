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
    bool ValidateCultivationComponent(string component)
    {
        if(CultivationProfile!=null && string.IsNullOrEmpty(component)){LastCultivationFailure="Missing cultivation component on a playtest Hero.";return false;}
        CultivationSession candidate;string reason;
        bool valid=CultivationGameplayState.TryRestore(component,CultivationProfile,out candidate,out reason);
        LastCultivationFailure=reason;return valid;
    }
    bool ValidateCultivationJson(string json,out string component)
    {
        string reason;bool valid=CultivationGameplayState.TryReadHeroJson(json,CultivationProfile,out component,out reason);
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
