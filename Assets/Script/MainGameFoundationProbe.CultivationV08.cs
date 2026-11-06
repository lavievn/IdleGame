#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using TMPro;
using TuTienCore;
using UnityEngine;

public sealed partial class MainGameFoundationProbe
{
    IEnumerator CultivationV08()
    {
        phase=1;
        Require(gm.Cultivation!=null && gm.Cultivation.Profile.Id=="balance:cultivation-early-v1" && !CultivationIntegrationGate.ProductionEnabled,"Isolated v1 profile, production gate closed");
        if(!string.IsNullOrEmpty(replayProfile)) {
            string raw=File.ReadAllText(Path.Combine(SavePath,"ManualSave1.json")),component,error;
            Require(CultivationGameplayState.TryReadHeroJson(raw,gm.Cultivation.Profile,out component,out error),"Restart source validation");
            Require(gm.LoadAndContinue(SaveSlot.ManualSave1),"Load slot copied from prior terminated Unity process");yield return Ready();
            Require(gm.HeroData.cultivationJson==component && gm.Cultivation.ActiveSeconds>0,"Process restart retains exact clock/resources without offline grant");
            string stable=gm.HeroData.cultivationJson;yield return new WaitForSecondsRealtime(.4f);
            Require(gm.HeroData.cultivationJson==stable,"Restart pause preserves checkpoint");
            var binding=gm.Cultivation;double start=binding.ActiveSeconds;ui.SetPaused(false);
            yield return Until(()=>gm.Cultivation==null || binding.ActiveSeconds>=start+1,10,"Restart resumed clock");ui.SetPaused(true);
            Require(gm.Cultivation==binding && binding.ActiveSeconds<start+2,"Restart resumes bounded game-time only");
            Pass("V08 separate Unity process reloads prior owned ManualSave1 with byte-identical cultivation checkpoint, no wall-clock/offline grant; paused checkpoint stable and sole MainGame clock resumes.");Status(1,"PASS");yield break;
        }
        var data=gm.HeroData;WorldMapNode starting;
        Require(WorldMapCatalog.TryGet(data.worldMapId,out starting) && starting.IsStart && gm.Cultivation.Profile.Density(starting.Id)==1,"Actual random New Hero start, no forced TV-20 fixture");
        var initial=gm.Cultivation;Require(initial.Current.Foundation.Amount==0 && initial.Current.Reserve.Amount==0 && data.currentLevel==1,"New baseline");
        // Natural farm: no forced enemies, HP, gear, EXP, speeds, drops or actor positions in this interval.
        double activeBefore=initial.ActiveSeconds,expected=0;int frames=0,expBefore=(int)data.currentExp;float began=Time.realtimeSinceStartup;
        ui.SetPaused(false);
        while(Time.realtimeSinceStartup-began<30 && gm.Cultivation!=null) {
            yield return new WaitForEndOfFrame();
            if(gm.Cultivation==initial && Time.timeScale>0 && !gm.IsEnteringGameplay && Time.deltaTime<=initial.Profile.MaximumFrameSeconds){expected+=Time.deltaTime;frames++;}
        }
        ui.SetPaused(true);
        Require(initial.ActiveSeconds>activeBefore && frames>0 && gm.DamageHistory.Length>0,"Bounded natural farm observed real combat and cultivation");
        // Death's terminal frame may retire before sampling; record measured vs valid sample budget explicitly.
        Require(Math.Abs((initial.ActiveSeconds-activeBefore)-expected)<=.6,"MainGame clock tracks scaled valid frame time (terminal frame bounded)");
        string farm="V08 natural farm node="+starting.Id+" realSeconds="+(Time.realtimeSinceStartup-began)+" activeSeconds="+(initial.ActiveSeconds-activeBefore)+" sampledSeconds="+expected+" damageEvents="+gm.DamageHistory.Length+" level="+data.currentLevel+" exp="+data.currentExp+" survived="+(gm.Cultivation!=null);
        File.WriteAllText(Path.Combine(Evidence,"natural-farm.txt"),farm);Pass(farm+". Unmodified real farm interval; no projected 60-minute claim.");
        if(gm.Cultivation==null){gm.OnRetryClicked();yield return Ready();}
        data=gm.HeroData;var bindingNow=gm.Cultivation;
        Call(gm,"CancelNextWave");combat.ForceClearAllMonsters();gm.activeMonsters.Clear();
        bool stance=hero.useStanceMoveSpeeds;float speed=hero.moveSpeed;hero.useStanceMoveSpeeds=false;hero.moveSpeed=0;
        double previous=bindingNow.ActiveSeconds;float exp=data.currentExp;int level=data.currentLevel;ui.SetPaused(false);
        yield return Until(()=>bindingNow.ActiveSeconds>=previous+1,10,"Stationary Qi without kills");ui.SetPaused(true);
        Require(data.currentExp==exp && data.currentLevel==level && bindingNow.Current.Foundation.Amount>0,"Idle cultivation independent of kills/EXP");
        hero.useStanceMoveSpeeds=stance;hero.moveSpeed=speed;previous=bindingNow.ActiveSeconds;bool moved=false;ui.SetPaused(false);
        while(bindingNow.ActiveSeconds<previous+1){moved|=hero.MovementDistanceThisFrame>0;yield return null;}
        ui.SetPaused(true);Require(moved && data.currentExp==exp,"Movement cultivation no kill subscription");
        Pass("V08 actual random start verified; isolated stationary/movement checks grow Qi without kills or EXP. Natural combat interval above uses unmodified actors. Domain simulation separately verifies all stage crossings.");Status(1,"PASS");

        phase=2;string paused=data.cultivationJson;double pausedTime=bindingNow.ActiveSeconds;
        yield return new WaitForSecondsRealtime(.4f);Require(data.cultivationJson==paused && bindingNow.ActiveSeconds==pausedTime,"Pause pays no time or resources");
        foreach(var size in new[]{new Vector2Int(600,338),new Vector2Int(800,450),new Vector2Int(1150,647),new Vector2Int(1360,680)}) {
            yield return Resize(size.x,size.y);ui.OpenHeroStats();yield return null;ui.RefreshCharacterProgression();
            var label=(TextMeshProUGUI)Get(ui,"progressionSummary");label.ForceMeshUpdate();
            Require(!label.isTextOverflowing && label.text.Contains("THỬ NGHIỆM") && label.text.Contains("Mật độ") && label.text.Contains("Linh khí dự trữ") && label.text.Contains("Linh lực") && label.text.Contains("EXP:"),"Character actual balance UI at "+size.x);
            Onscreen("CharacterAllocatePhysical");Onscreen("DamageClose");yield return Capture("cultivation-v08-character-"+size.x);ui.CloseDamageInfo();
        }
        Pass("V08 Character at600/800/1150/1360: native TMP no overflow, real Level/EXP, realm/stage/bar, reserve/density, rates/energy, blood/trial locked. ETA omitted until a versioned rate/source forecast is available.");Status(2,"PASS");

        phase=3;data=gm.HeroData;string snapshot=data.cultivationJson;var expectedState=Take();
        Require(gm.TryManualSave(SaveSlot.ManualSave1),"Save cultivated clock checkpoint");
        string file=Path.Combine(SavePath,"ManualSave1.json"),bytes=Convert.ToBase64String(File.ReadAllBytes(file));
        var retired=gm.Cultivation;for(int n=0;n<3;n++) {
            Require(gm.LoadAndContinue(SaveSlot.ManualSave1),"Repeated balance Load");yield return Ready();
            Require(gm.HeroData.cultivationJson==snapshot && gm.Cultivation.Epoch!=retired.Epoch,"Exact resources/fractional checkpoint under fresh epoch");Matches(expectedState,"V08 full snapshot");
        }
        Require(retired.IsDisposed && retired.Advance(gm.HeroData,Time.frameCount,.1,false,gm.HeroData.worldMapId)==CultivationReason.OwnerMismatch,"Stale callback rejected");
        Require(Convert.ToBase64String(File.ReadAllBytes(file))==bytes,"Load does not overwrite manual rollback snapshot");
        string valid=File.ReadAllText(file),invalid=valid.Replace("\\\"version\\\":2","\\\"version\\\":3");Require(invalid!=valid,"Future schema fixture");
        File.WriteAllText(Path.Combine(SavePath,"ManualSave2.json"),invalid);var stableHero=gm.HeroData;var stableBinding=gm.Cultivation;var stableLife=combat.CurrentHeroCombatState;
        Require(!gm.LoadAndContinue(SaveSlot.ManualSave2) && ReferenceEquals(stableHero,gm.HeroData) && ReferenceEquals(stableBinding,gm.Cultivation) && ReferenceEquals(stableLife,combat.CurrentHeroCombatState),"Failed candidate preserves valid state/session/life");
        File.WriteAllText(Path.Combine(SavePath,"ManualSave2.json"),valid);
        WorldMapNode origin;Require(WorldMapCatalog.TryGet(gm.HeroData.worldMapId,out origin),"Travel origin");string destination=null;
        foreach(var id in origin.Links){WorldMapNode node;if(WorldMapCatalog.TryGet(id,out node) && !node.IsBoss){destination=id;break;}}
        Require(destination!=null,"Existing non-boss linked node");if(!gm.HeroData.worldMapCleared.Contains(origin.Id))gm.HeroData.worldMapCleared.Add(origin.Id); // Permission fixture only.
        double total=stableBinding.Current.Foundation.Amount+stableBinding.Current.Reserve.Amount+2*stableBinding.PendingSeconds;
        Require(gm.TryTravelToWorldMap(destination),"Real Travel boundary");yield return Ready();
        Require(gm.Cultivation==stableBinding && stableBinding.PendingSeconds==0 && Math.Abs(stableBinding.Current.Foundation.Amount+stableBinding.Current.Reserve.Amount-total)<1e-8,"Travel settles old-source fractional time without loss");
        previous=stableBinding.ActiveSeconds;ui.SetPaused(false);yield return Until(()=>stableBinding.ActiveSeconds>=previous+.4,10,"Destination clock");ui.SetPaused(true);
        Require(stableBinding.Density==stableBinding.Profile.Density(destination) && gm.HeroData.worldMapId==destination,"Stable-node source lookup after Travel; explicit fallback1 on unbalanced destination");
        Require(gm.TryManualSave(SaveSlot.ManualSave2) && gm.LoadAndContinue(SaveSlot.ManualSave2),"Travel Save/Load");yield return Ready();
        Pass("V08 real Save/Load retains full inventory/equipment/forge/Level/EXP snapshot and fractional checkpoint; failed future candidate preserves Hero/session/life. Travel settles pending time at original source then resolves destination fallback1. Travel permission is an explicit fixture; no invented node density differences.");Status(3,"PASS");

        phase=4;var beforeDeath=gm.Cultivation;string beforeDeathJson=gm.HeroData.cultivationJson;
        Call(combat,"ApplyIncomingDamage",1000000,"[V08 fixture defeat]",null,null);yield return null;
        Require(beforeDeath.IsDisposed && gm.Cultivation==null,"Defeat retires clock");gm.OnRetryClicked();yield return Ready();
        Require(gm.HeroData.cultivationJson==beforeDeathJson && gm.Cultivation.Epoch!=beforeDeath.Epoch && combat.CurrentHeroCombatState.Data==gm.HeroData && Get(space,"cameraTarget")==hero,"Retry exact checkpoint and camera/combat identity");
        var menu=gm.Cultivation;string menuJson=gm.HeroData.cultivationJson;Require(gm.ReturnToMainMenu(),"Return Menu");yield return new WaitForSecondsRealtime(.3f);
        Require(menu.IsDisposed && gm.Cultivation==null,"Menu no clock");gm.OnContinueClicked();gm.OnConfirmResetClicked();yield return Ready();Require(gm.HeroData.cultivationJson==menuJson,"Continue exact clock no offline grant");
        gm.heroDataSO.cultivationJson=menuJson;var old=gm.Cultivation;gm.OnNewGameClicked();gm.OnConfirmResetClicked();yield return Ready();
        Require(old.IsDisposed && gm.Cultivation.OwnerId!=old.OwnerId && gm.Cultivation.ActiveSeconds==0 && gm.Cultivation.PendingSeconds==0 && gm.Cultivation.Current.Foundation.Amount==0 && gm.Cultivation.Current.Reserve.Amount==0 && gm.HeroData.currentLevel==1,"New ignores contaminated template, initial values and clock reset");
        Require(Convert.ToBase64String(File.ReadAllBytes(file))==bytes,"New retains manual rollback slot");
        Pass("V08 real defeat/Retry, Menu/Continue and New preserve/reset clock by lifecycle policy; camera/combat identity valid, manual rollback preserved. No Burning/Trial/offline progression enabled.");Status(4,"PASS");
    }
}
#endif
