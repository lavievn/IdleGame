#if DEVELOPMENT_BUILD && UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using TuTienCore;
using UnityEngine;
public sealed partial class WindowsDevelopmentAcceptanceV10
{
    IEnumerator QiUpgradeV11()
    {
        Require(saves.CultivationProfile.Version==2,"Packaged V2 default");gm.OnNewGameClicked();gm.OnConfirmResetClicked();yield return Ready();
        WorldMapNode start;Require(WorldMapCatalog.TryGet(gm.HeroData.worldMapId,out start)&&start.IsStart&&gm.Cultivation.Profile.EnvironmentalQiPerSecond(start.Id)==1,"V2 real start");
        CultivationPlaytestProfile p1;WorldQiDistribution qi;CultivationConfigurationReason code;string why;Require(CultivationProductionConfiguration.TryLoadVersion(1,out p1,out qi,out code,out why),"Packaged historical V1");
        var fixture=Instantiate(gm.HeroData);var fb=new CultivationGameplayBinding(fixture,p1,CultivationGameplayState.New(p1));fb.Advance(fixture,0,.037,false,fixture.worldMapId);string component=fixture.cultivationJson;
        Require(saves.SaveGame(fixture,SaveSlot.ManualSave2),"Owned V1 fixture");fb.Dispose();Destroy(fixture);string path=Path.Combine(SavePath,"ManualSave2.json"),original=File.ReadAllText(path);
        Require(gm.LoadAndContinue(SaveSlot.ManualSave2),"V1 Load under V2 binary");yield return Ready();Require(gm.Cultivation.Profile.Version==1&&gm.HeroData.cultivationJson==component,"No silent upgrade");
        var canonical=gm.HeroData;var old=gm.Cultivation;Require(gm.RequestSelectedCultivationConfigurationUpgrade(),"Selected-slot consent");foreach(var size in Sizes)yield return Panel("v11-config-consent",size.x,size.y);
        gm.OnCancelResetClicked();Require(gm.HeroData==canonical&&gm.Cultivation==old&&File.ReadAllText(path)==original,"Cancel unchanged");Require(gm.RequestSelectedCultivationConfigurationUpgrade(),"Repeat consent");gm.OnConfirmResetClicked();yield return Ready();
        Require(old.IsDisposed&&gm.Cultivation.Profile.Version==2&&gm.Cultivation.PendingSeconds==.037,"V2 commit before swap and exact fraction");string[] backups=Directory.GetFiles(SavePath,"ManualSave2.cultivation-*.snapshot.bak");Require(backups.Length==1&&File.ReadAllText(backups[0])==original,"Exact V1 backup");
        var b=gm.Cultivation;Call(gm,"CancelNextWave");combat.ForceClearAllMonsters();gm.activeMonsters.Clear();int level=gm.HeroData.currentLevel;float exp=gm.HeroData.currentExp;ui.SetPaused(false);yield return Until(()=>b.ActiveSeconds>.5,10,"Player sole clock");ui.SetPaused(true);
        Require(Math.Abs(b.Current.Foundation.Amount+b.Current.Reserve.Amount-b.ProcessedSeconds)<1e-8&&gm.HeroData.currentLevel==level&&gm.HeroData.currentExp==exp,"Player Qi accounting and EXP independence");Require(gm.TryManualSave(SaveSlot.ManualSave2),"Save upgraded V2");string saved=gm.HeroData.cultivationJson;Require(gm.LoadAndContinue(SaveSlot.ManualSave2),"Reload V2");yield return Ready();Require(gm.HeroData.cultivationJson==saved,"Exact checkpoint");
        foreach(var size in Sizes){ui.OpenHeroStats();yield return Panel("v11-v2-character",size.x,size.y);ui.CloseDamageInfo();}
        Require(result.uiIssues.Count==0,"V11 UI issues: "+string.Join(";",result.uiIssues));Pass("V11B actual Windows Development Player: V2 default New/start, V1 Load remains V1, selected-slot consent cancel/accept at4 sizes, exact V1 backup/fraction, one Update clock/accounting, no EXP grant, V2 Save/Load. Method-driven fixture, native input NOT RUN.");
    }
}
#endif
