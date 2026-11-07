using System;
using TuTienCore;
using UnityEngine;

public partial class GameManager
{
    CultivationPlaytestProfile cultivationProfile;
    CultivationGameplayBinding cultivation;
    public CultivationGameplayBinding Cultivation {get{return cultivation!=null && !cultivation.IsDisposed?cultivation:null;}}
    // Explicit isolated Editor/Development context, configured before owner Awake.
    public void ConfigureCultivationPlaytest(CultivationPlaytestProfile profile)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        var saves=saveManager!=null?saveManager:GetComponent<SaveManager>();
        if(runtimeHeroData!=null || profile==null || saves==null || saves.CultivationProfile!=profile)
            throw new InvalidOperationException("Configure matching isolated Save context before Hero creation.");
        cultivationProfile=profile;
#else
        throw new InvalidOperationException("Cultivation playtest requires Editor/Development context.");
#endif
    }
    void ResolveProductionCultivation()
    {
        if(cultivationProfile==null && saveManager!=null && saveManager.CultivationProfile!=null && saveManager.CultivationProfile.IsProduction)cultivationProfile=saveManager.CultivationProfile;
    }
    void InitializeCultivation()
    {
        ResolveProductionCultivation();
        DisposeCultivation();runtimeHeroData.cultivationJson="";runtimeHeroData.tribulationJson="";
        if(cultivationProfile!=null)cultivation=new CultivationGameplayBinding(runtimeHeroData,cultivationProfile,CultivationGameplayState.New(cultivationProfile));
        AuthorizeCultivationAutoSnapshot();
    }
    bool PrepareCultivation(EntityDataSO candidate,out CultivationGameplayBinding binding)
    {
        ResolveProductionCultivation();binding=null;CultivationSession state;string reason;
        CultivationPlaytestProfile resolved;
        if(!CultivationProductionConfiguration.TryResolve(candidate.cultivationJson,cultivationProfile,out resolved,out reason) || !CultivationGameplayState.TryRestore(candidate.cultivationJson,resolved,out state,out reason))
        {Debug.LogWarning("[Cultivation] Candidate rejected: "+reason);return false;}
        if(state!=null)binding=new CultivationGameplayBinding(candidate,resolved,state);
        return true;
    }
    void ReplaceCultivation(CultivationGameplayBinding prepared)
    {DisposeCultivation();cultivation=prepared;AuthorizeCultivationAutoSnapshot();}
    void AuthorizeCultivationAutoSnapshot(){if(saveManager!=null && Cultivation!=null)saveManager.AuthorizeCultivationAutoSnapshot(Cultivation.OwnerId,Cultivation.Profile);}

    void DisposeCultivation(){if(cultivation!=null)cultivation.Dispose();cultivation=null;}
    void EnsureCultivationBinding()
    {
        if(cultivationProfile==null || runtimeHeroData==null || Cultivation!=null)return;
        CultivationGameplayBinding prepared;if(PrepareCultivation(runtimeHeroData,out prepared))ReplaceCultivation(prepared);
    }
    void TickCultivation()
    {
        var binding=Cultivation;
        if(HasTribulationLiability || binding==null || runtimeHeroData==null || !hasDeployed || enteringGameplay ||
            heroController==null || !heroController.IsDeployed || (combatManager!=null && combatManager.CurrentHeroCombatState.Data!=runtimeHeroData))return;
#if DEVELOPMENT_BUILD && UNITY_STANDALONE_WIN && !UNITY_EDITOR
        long measured=WindowsDevelopmentAcceptanceV10.BeginAdvance();
#endif
        var result=binding.Advance(runtimeHeroData,Time.frameCount,Time.deltaTime,Time.timeScale==0f,runtimeHeroData.worldMapId);
#if DEVELOPMENT_BUILD && UNITY_STANDALONE_WIN && !UNITY_EDITOR
        WindowsDevelopmentAcceptanceV10.EndAdvance(measured);
#endif
        if(result!=CultivationReason.None && result!=CultivationReason.Paused && result!=CultivationReason.InvalidDelta)
            Debug.LogWarning("[Cultivation] Tick rejected: "+result);
    }
}
