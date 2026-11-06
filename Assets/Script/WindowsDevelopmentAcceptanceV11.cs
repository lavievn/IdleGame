#if DEVELOPMENT_BUILD && UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using TuTienCore;
using UnityEngine;
public sealed partial class WindowsDevelopmentAcceptanceV10
{
    IEnumerator CapacityV11()
    {
        Require(saves.CultivationProfile.Version==3,"Packaged default capacity config");gm.OnNewGameClicked();gm.OnConfirmResetClicked();yield return Ready();var b=gm.Cultivation;
        Require(b.Capacity.Absorption==1.2&&b.Capacity.Refinement==1,"New base caps");
        CultivationPlaytestProfile p2;WorldQiDistribution qi;CultivationConfigurationReason code;string why;Require(CultivationProductionConfiguration.TryLoadVersion(2,out p2,out qi,out code,out why),"Packaged V2 historical config");
        var fixture=Instantiate(gm.HeroData);string hot=null;foreach(var n in WorldMapCatalog.All)if(!n.IsBoss&&p2.EnvironmentalQiPerSecond(n.Id)==5){hot=n.Id;break;}Require(hot!=null,"Actual hotspot");fixture.worldMapId=hot;WorldMapCatalog.Normalize(fixture);
        var state=new CultivationSession(Guid.NewGuid(),Guid.NewGuid(),p2.Catalog,p2.Rules,new CultivationSnapshot(new CultivationFoundation(p2.StartingRealm,899.95),new UnrefinedQiReserve(100),new SpiritualEnergy(17),new BloodEssenceState(0,0),new FoundationInjuryState(0)));
        var fb=new CultivationGameplayBinding(fixture,p2,state);fb.Advance(fixture,0,.037,false,hot);Require(saves.SaveGame(fixture,SaveSlot.ManualSave2),"Owned V2 stage fixture");fb.Dispose();Destroy(fixture);string path=Path.Combine(SavePath,"ManualSave2.json"),source=File.ReadAllText(path);
        Require(gm.LoadAndContinue(SaveSlot.ManualSave2),"Load V2 as V2");yield return Ready();Require(gm.Cultivation.Profile.Version==2&&gm.Cultivation.Profile.Rules.CapacityCatalog==null,"V2 retains historical throughput until consent");
        b=gm.Cultivation;Require(gm.RequestSelectedCultivationConfigurationUpgrade(),"V2->V3 consent");gm.OnCancelResetClicked();Require(gm.Cultivation==b&&File.ReadAllText(path)==source,"Cancel original");Require(gm.RequestSelectedCultivationConfigurationUpgrade(),"Accept capacity consent");gm.OnConfirmResetClicked();yield return Ready();
        Require(b.IsDisposed&&gm.Cultivation.Profile.Version==3&&gm.Cultivation.PendingSeconds==.037&&gm.Cultivation.Capacity.Absorption==1.2,"Accepted exact state/fraction, new capacity derived");b=gm.Cultivation;double initial=b.Current.Foundation.Amount+b.Current.Reserve.Amount;Call(gm,"CancelNextWave");combat.ForceClearAllMonsters();gm.activeMonsters.Clear();int level=gm.HeroData.currentLevel;float exp=gm.HeroData.currentExp;
        ui.SetPaused(false);yield return Until(()=>b.ActiveSeconds>.4,10,"Capacity stage change");ui.SetPaused(true);
        Require(b.Current.Foundation.Amount>900&&Math.Abs(b.Capacity.Absorption-1.35)<1e-12&&b.Capacity.Refinement==1.08&&b.LastStepFlow.EnvironmentalSupply==5&&b.LastStepFlow.AbsorptionPerSecond<=1.35+1e-8&&gm.HeroData.currentLevel==level&&gm.HeroData.currentExp==exp,"Player active capacity/minor stage/EXP isolation");
        // Old .037 uses supply5; first current .1 uses cap1.2, then current steps use cap1.35.
        double steps=Math.Round((b.ProcessedSeconds-.037)/.1);double expected=initial+5*.037+.12+Math.Max(0,steps-1)*.135;
        Require(Math.Abs(b.Current.Foundation.Amount+b.Current.Reserve.Amount-expected)<1e-7,"Old rules and new stage steps attributed once");
        foreach(var size in Sizes){ui.OpenHeroStats();yield return Panel("v11-capacity-character",size.x,size.y);ui.CloseDamageInfo();}
        string[] backups=Directory.GetFiles(SavePath,"ManualSave2.cultivation-*.snapshot.bak");Require(backups.Length==1&&File.ReadAllText(backups[0])==source,"Immutable V2 backup");Require(gm.TryManualSave(SaveSlot.ManualSave2),"V3 Save");string snapshot=gm.HeroData.cultivationJson;Require(gm.LoadAndContinue(SaveSlot.ManualSave2),"V3 Load");yield return Ready();Require(gm.HeroData.cultivationJson==snapshot,"V3 exact Load checkpoint");
        Require(result.uiIssues.Count==0,"Capacity UI overflow: "+string.Join(";",result.uiIssues));Pass("V11C Windows Development Player default V3: V2 snapshot retains V2, consent cancel/accept + backup, pending .037 at5Qi old rules, current steps capped1.2→1.35 after stage boundary/refinement1→1.08; real canonical Update, no EXP grant, 4sizes source/cap/actual UI, exact Save/Load. Owned progress fixture, method-driven, native input NOT RUN.");
    }
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
