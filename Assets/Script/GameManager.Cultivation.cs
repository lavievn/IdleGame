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
        if(cultivationProfile==null && saveManager!=null && saveManager.CultivationProfile!=null && (saveManager.CultivationProfile.IsProduction || saveManager.HasDevelopmentCultivationLoop))
            cultivationProfile=saveManager.CultivationProfile;
    }
    void InitializeCultivation()
    {
        ResolveProductionCultivation();
        DisposeCultivation();runtimeHeroData.cultivationJson="";runtimeHeroData.tribulationJson="";runtimeHeroData.experienceJson="";
        if(cultivationProfile!=null)cultivation=new CultivationGameplayBinding(runtimeHeroData,cultivationProfile,CultivationGameplayState.New(cultivationProfile),saveManager?.TrialConfiguration);
        AuthorizeCultivationAutoSnapshot();
    }
    bool PrepareCultivation(EntityDataSO candidate,out CultivationGameplayBinding binding)
    {
        ResolveProductionCultivation();binding=null;CultivationSession state;string reason;
        CultivationPlaytestProfile resolved;
        if(!CultivationProductionConfiguration.TryResolve(candidate.cultivationJson,cultivationProfile,out resolved,out reason) || !CultivationGameplayState.TryRestore(candidate.cultivationJson,resolved,out state,out reason))
        {Debug.LogWarning("[Cultivation] Candidate rejected: "+reason);return false;}
        if(state!=null)binding=new CultivationGameplayBinding(candidate,resolved,state,saveManager?.TrialConfiguration);
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
        if(HasTribulationLiability || binding==null || runtimeHeroData==null || !IsGameplayHUDVisible ||
            heroController==null || !heroController.IsDeployed || (combatManager!=null && combatManager.CurrentHeroCombatState.Data!=runtimeHeroData))return;
#if DEVELOPMENT_BUILD && UNITY_STANDALONE_WIN && !UNITY_EDITOR
        long measured=WindowsDevelopmentAcceptanceV10.BeginAdvance();
#endif
        bool paused=Time.timeScale==0f || UIManager.Instance?.BlocksCultivationClock==true;
        var result=binding.Advance(runtimeHeroData,Time.frameCount,Time.deltaTime,paused,CurrentQiSource(binding.Profile));
#if DEVELOPMENT_BUILD && UNITY_STANDALONE_WIN && !UNITY_EDITOR
        WindowsDevelopmentAcceptanceV10.EndAdvance(measured);
#endif
        if(result!=CultivationReason.None && result!=CultivationReason.Paused && result!=CultivationReason.InvalidDelta)
            Debug.LogWarning("[Cultivation] Tick rejected: "+result);
    }

    string CurrentQiSource(CultivationPlaytestProfile profile)
    {
        string mapId=runtimeHeroData.worldMapId;
        if(!profile.HasQiZones)return mapId;
        if(StoryOrigin.IsSafe(runtimeHeroData))return profile.QiSourceAt(mapId,.5);
        var space=environmentManager!=null?environmentManager:EnvironmentManager.Instance;
        if(space==null || space.BattleArea==null || heroController.heroRect==null)return profile.QiSourceAt(mapId,.5);
        Rect area=space.BattleArea.rect;
        if(area.height<=0)return profile.QiSourceAt(mapId,.5);
        float feet=space.Position(heroController.heroRect).y;
        return profile.QiSourceAt(mapId,Mathf.Clamp01((feet-area.yMin)/area.height));
    }
}
