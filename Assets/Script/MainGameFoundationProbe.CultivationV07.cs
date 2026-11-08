#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using TuTienCore;
using UnityEngine;

public sealed partial class MainGameFoundationProbe
{
    readonly HashSet<RealmMinorStage> v07Seen=new HashSet<RealmMinorStage>();
    IEnumerator WatchCultivation(Func<bool> condition,float seconds,string description)
    {
        float deadline=Time.realtimeSinceStartup+seconds;
        while(true) {
            var c=gm.Cultivation;Require(c!=null,"Canonical cultivation disappeared: "+description);
            v07Seen.Add(c.Profile.Catalog.Evaluate(c.Current.Foundation.ActualRealmId,c.Current.Foundation.Amount).MinorStage);
            if(condition())yield break;
            Require(Time.realtimeSinceStartup<deadline,"Timeout: "+description);yield return null;
        }
    }
    IEnumerator CultivationV07()
    {
        phase=1;var data=gm.HeroData;var initial=gm.Cultivation;
        Require(initial!=null && initial.Profile.Id=="playtest:cultivation-v07" && data.currentLevel==1 && data.currentExp==0 && initial.Current.Blood.Charges==0,"New playtest Hero authority");
        string actual=initial.Current.Foundation.ActualRealmId;
        Call(gm,"CancelNextWave");combat.ForceClearAllMonsters();gm.activeMonsters.Clear();
        bool oldStance=hero.useStanceMoveSpeeds;float oldSpeed=hero.moveSpeed;double idleWalked=0;int idleFirstFrame=Time.frameCount,idleLastSample=-1,idleSamples=0;
        hero.useStanceMoveSpeeds=false;hero.moveSpeed=0;var position=space.Position(hero.heroRect);
        long start=initial.TickCount;double foundation=initial.Current.Foundation.Amount,reserve=initial.Current.Reserve.Amount;
        ui.SetPaused(false);
        yield return WatchCultivation(()=>{if(hero.MovementFrame>idleFirstFrame && hero.MovementFrame!=idleLastSample){idleLastSample=hero.MovementFrame;idleSamples++;idleWalked+=hero.MovementDistanceThisFrame;}return initial.TickCount>=start+10;},10,"standing idle Qi");ui.SetPaused(true);
        Require(initial.Current.Foundation.Amount>foundation && initial.Current.Reserve.Amount>reserve && data.currentExp==0 && data.currentLevel==1 && idleSamples>0 && idleWalked==0 && hero.EffectiveMoveSpeed()==0,"Stationary no-kill Qi: F="+foundation+"->"+initial.Current.Foundation.Amount+" reserve="+reserve+"->"+initial.Current.Reserve.Amount+" EXP="+data.currentExp+" Level="+data.currentLevel+" screenDistance="+Vector2.Distance(position,space.Position(hero.heroRect))+" walked="+idleWalked+" samples="+idleSamples+" movement="+hero.MovementDistanceThisFrame+" speed="+hero.EffectiveMoveSpeed());
        hero.useStanceMoveSpeeds=oldStance;hero.moveSpeed=oldSpeed;start=initial.TickCount;
        bool moved=false;ui.SetPaused(false);
        yield return WatchCultivation(()=>{moved|=hero.MovementDistanceThisFrame>0;return moved && initial.TickCount>=start+10;},10,"moving Qi");ui.SetPaused(true);
        Require(data.currentExp==0 && data.currentLevel==1 && initial.Current.Foundation.Amount>foundation,"Movement Qi, no kills/EXP");
        ui.OpenHeroStats();yield return null;ui.RefreshCharacterProgression();yield return Capture("cultivation-v07-so-ky"); // Keep panel open while normal game-time advances to verify polling.
        int traces=gm.DamageHistory.Length;start=initial.TickCount;Call(gm,"CallNextWave");ui.SetPaused(false);
        yield return WatchCultivation(()=>gm.DamageHistory.Length>traces && initial.TickCount>=start+5,30,"real combat and Qi clock");
        yield return WatchCultivation(()=>initial.Current.Foundation.Amount==20,40,"configured minor stages and bottleneck");ui.SetPaused(true);
        yield return new WaitForSecondsRealtime(.25f);
        Require(((TextMeshProUGUI)Get(ui,"progressionSummary")).text.Contains("Bình cảnh"),"Open Character polls canonical progression without an explicit refresh");ui.CloseDamageInfo();
        Require(v07Seen.Contains(RealmMinorStage.SoKy) && v07Seen.Contains(RealmMinorStage.TrungKy) && v07Seen.Contains(RealmMinorStage.HauKy) && v07Seen.Contains(RealmMinorStage.VienMan),"Every actual stage observed under MainGame clock");
        Require(ReferenceEquals(initial,gm.Cultivation) && initial.Current.Foundation.ActualRealmId==actual && combat.CurrentHeroCombatState.Data==data,"No automatic realm/combat authority replacement");
        Pass("V07 real MainGame New/idle/moving/natural combat use the sole GameManager clock; Qi grows without kills during idle/movement, EXP stays independent, all four configured minor stages observed. Stationary speed0 (zero accumulated actor steps; camera settling is presentation) and initial combat clear are explicit fixtures, not production movement changes.");Status(1,"PASS");

        phase=2;var paused=initial.Current;long pausedTicks=initial.TickCount;
        yield return new WaitForSecondsRealtime(.5f);
        Require(initial.Current==paused && initial.TickCount==pausedTicks,"Pause cannot pay resources");
        reserve=initial.Current.Reserve.Amount;start=initial.TickCount;ui.SetPaused(false);
        yield return WatchCultivation(()=>initial.TickCount>=start+10,10,"reserve after bottleneck");ui.SetPaused(true);
        Require(initial.Current.Foundation.Amount==20 && initial.Current.Reserve.Amount>reserve && initial.Current.Foundation.ActualRealmId==actual,"Bottleneck preserves reserve without auto breakthrough");
        foreach(var size in new[]{new Vector2Int(600,338),new Vector2Int(800,450),new Vector2Int(1150,647),new Vector2Int(1360,680)}) {
            yield return Resize(size.x,size.y);ui.OpenHeroStats();yield return null;ui.RefreshCharacterProgression();
            var label=(TextMeshProUGUI)Get(ui,"progressionSummary");label.ForceMeshUpdate();
            Require(!label.isTextOverflowing && label.text.Contains("PLAYTEST") && label.text.Contains("Bình cảnh") && label.text.Contains("Linh khí dự trữ") && label.text.Contains("Linh lực") && label.text.Contains("EXP:"),"Canonical cultivation UI at "+size.x);
            Onscreen("CharacterAllocatePhysical");Onscreen("DamageClose");
            var fill=(UnityEngine.UI.Image)Get(ui,"cultivationFill");var track=(UnityEngine.UI.Image)Get(ui,"cultivationTrack");
            Require(fill.gameObject.activeInHierarchy && Math.Abs(fill.rectTransform.rect.width-track.rectTransform.rect.width)<.1,"Actual foundation bar at cap");
            yield return Capture("cultivation-v07-character-"+size.x);ui.CloseDamageInfo();
        }
        Pass("V07 pause/resume at actual bottleneck retains foundation and continues reserve; Character shows canonical Level/EXP, realm/stage/bar/reserve/rates/energy and locked blood/trial at600/800/1150/1360 without overflow.");Status(2,"PASS");

        var foundationBeforeExp=gm.Cultivation.Current;string componentBeforeExp=data.cultivationJson;int levelBeforeExp=data.currentLevel,pointsBeforeExp=data.statPoints;
        data.AddExp(data.expToNextLevel-data.currentExp);combat.RefreshRegisteredHeroStats(hero,false); // Explicit EXP fixture, not natural farm.
        Require(data.currentLevel==levelBeforeExp+1 && data.statPoints==pointsBeforeExp+1 && gm.Cultivation.Current==foundationBeforeExp && data.cultivationJson==componentBeforeExp,"Bottleneck does not lock Level/EXP or derive cultivation from Level Up");
        phase=3;string snapshot=data.cultivationJson;int level=data.currentLevel,exp=data.currentExp;string gear=GearCore(data);int maxHp=combat.MaxHeroHP;
        Require(gm.TryManualSave(SaveSlot.ManualSave1),"Save real cultivated Hero");string saveFile=Path.Combine(SavePath,"ManualSave1.json"),bytes=Convert.ToBase64String(File.ReadAllBytes(saveFile));
        var retired=initial;var retiredSnapshot=retired.Current;
        ui.OpenHeroStats();Require(gm.LoadAndContinue(SaveSlot.ManualSave1),"Load while Character open");yield return Ready();data=gm.HeroData;
        Require(retired.IsDisposed && !ReferenceEquals(initial,gm.Cultivation) && gm.Cultivation.Epoch!=retired.Epoch && data.cultivationJson==snapshot && data.currentLevel==level && data.currentExp==exp && GearCore(data)==gear && combat.MaxHeroHP==maxHp,"Canonical session and unrelated state roundtrip");
        Require(retired.Advance(data,Time.frameCount,.1,false,data.worldMapId)==CultivationReason.OwnerMismatch && retired.Current==retiredSnapshot,"Retired callback cannot grant after Load");
        for(int i=0;i<2;i++){Require(gm.LoadAndContinue(SaveSlot.ManualSave1),"Repeated real Load");yield return Ready();Require(gm.HeroData.cultivationJson==snapshot,"No repeated resource grant");}
        Require(Convert.ToBase64String(File.ReadAllBytes(saveFile))==bytes,"Load does not rewrite manual snapshot");
        ui.OpenHeroStats();yield return null;ui.RefreshCharacterProgression();Require(((TextMeshProUGUI)Get(ui,"progressionSummary")).text.Contains("Bình cảnh"),"UI rebound after Load");ui.CloseDamageInfo();
        var stableHero=gm.HeroData;var stableBinding=gm.Cultivation;var stableLife=combat.CurrentHeroCombatState;int damageBefore=gm.DamageHistory.Length;
        string valid=File.ReadAllText(saveFile),bad=valid.Replace("\\\"version\\\":1","\\\"version\\\":2");Require(bad!=valid,"Future component fixture");
        File.WriteAllText(Path.Combine(SavePath,"ManualSave2.json"),bad);
        ui.SetPaused(false);yield return WatchCultivation(()=>gm.DamageHistory.Length>damageBefore,30,"combat before failed Load");ui.SetPaused(true);ui.OpenHeroStats();
        Require(!gm.LoadAndContinue(SaveSlot.ManualSave2) && ReferenceEquals(stableHero,gm.HeroData) && ReferenceEquals(stableBinding,gm.Cultivation) && ReferenceEquals(stableLife,combat.CurrentHeroCombatState),"Failed candidate must keep live Hero/session/combat while panel open");ui.CloseDamageInfo();
        File.WriteAllText(Path.Combine(SavePath,"ManualSave2.json"),valid); // Repair only this deliberately corrupted owned fixture.
        data=gm.HeroData;WorldMapNode origin;Require(WorldMapCatalog.TryGet(data.worldMapId,out origin),"Origin node");string destination=null;
        foreach(string id in origin.Links){WorldMapNode n;if(WorldMapCatalog.TryGet(id,out n) && !n.IsBoss && gm.Cultivation.Profile.Density(id)!=gm.Cultivation.Profile.Density(origin.Id)){destination=id;break;}}
        Require(destination!=null,"Normal neighbor with different configured/fallback Qi");if(!data.worldMapCleared.Contains(origin.Id))data.worldMapCleared.Add(origin.Id); // Explicit permission fixture, not natural map clear evidence.
        var travelBinding=gm.Cultivation;double travelReserve=travelBinding.Current.Reserve.Amount;
        Require(gm.TryTravelToWorldMap(destination),"Actual travel with fixture permission");yield return Ready();
        Require(ReferenceEquals(travelBinding,gm.Cultivation) && gm.Cultivation.Current.Reserve.Amount==travelReserve,"Travel retains session and reserve through paused entry");
        start=travelBinding.TickCount;ui.SetPaused(false);yield return WatchCultivation(()=>travelBinding.TickCount>=start+3,10,"new map Qi source");ui.SetPaused(true);
        Require(travelBinding.Density==travelBinding.Profile.Density(destination) && travelBinding.Current.Reserve.Amount>travelReserve,"Travel changes actual absorption source only");
        Require(gm.TryManualSave(SaveSlot.ManualSave2) && gm.LoadAndContinue(SaveSlot.ManualSave2),"Save/Load after Travel");yield return Ready();
        Pass("V07 explicit EXP fixture levels up normally at bottleneck without changing cultivation; actual Save/Load/repeated Load preserve cultivation plus Level/EXP/gear/stats; stale binding rejected, failed future Load during combat keeps Hero/session/life, Travel updates Qi source without resetting reserve. Travel clear permission is fixture-supplied.");Status(3,"PASS");

        phase=4;var beforeDeath=gm.Cultivation;string deathSnapshot=gm.HeroData.cultivationJson;
        Call(combat,"ApplyIncomingDamage",1000000,"[V07 fixture defeat]",null,null);yield return null;
        Require(beforeDeath.IsDisposed && gm.Cultivation==null,"Defeat retires clock");gm.OnRetryClicked();yield return Ready();
        Require(gm.Cultivation!=null && gm.Cultivation.Epoch!=beforeDeath.Epoch && gm.HeroData.cultivationJson==deathSnapshot,"Retry restores exact snapshot under fresh epoch");
        Require(combat.CurrentHeroCombatState.Data==gm.HeroData && Get(space,"cameraTarget")==hero,"Combat and camera still bound to canonical Hero");
        var menuBinding=gm.Cultivation;var menuSnapshot=menuBinding.Current;Require(gm.ReturnToMainMenu(),"Return Menu persists then retires session");
        yield return new WaitForSecondsRealtime(.3f);Require(menuBinding.IsDisposed && menuBinding.Current==menuSnapshot && gm.Cultivation==null,"No disposed/menu ticks");
        gm.OnContinueClicked();gm.OnConfirmResetClicked();yield return Ready();Require(gm.Cultivation!=null && gm.Cultivation.Epoch!=menuBinding.Epoch && gm.Cultivation.Current.Foundation.Amount==20,"Title Continue restores current cultivation");
        gm.heroDataSO.cultivationJson=gm.HeroData.cultivationJson; // Deliberately dirty template; New must ignore it.
        var previous=gm.Cultivation;string manual=Convert.ToBase64String(File.ReadAllBytes(saveFile));gm.OnNewGameClicked();gm.OnConfirmResetClicked();yield return Ready();
        Require(previous.IsDisposed && gm.Cultivation.OwnerId!=previous.OwnerId && gm.HeroData.currentLevel==1 && gm.HeroData.currentExp==0 && gm.Cultivation.Current.Foundation.Amount==0 && gm.Cultivation.Current.Reserve.Amount==0,"New Hero resets template/session/clock, no old cultivation");
        Require(Convert.ToBase64String(File.ReadAllBytes(saveFile))==manual,"New Run preserves manual snapshot");ui.OpenHeroStats();yield return null;ui.RefreshCharacterProgression();yield return Capture("cultivation-v07-new-after-continue");
        Pass("V07 fixture defeat invokes real Retry with fresh epoch and retained snapshot; Menu disposes clock, title Continue restores session; New resets cultivated template and owner while preserving manual slots. Camera/combat identity unchanged. Burning/trial remain unavailable.");Status(4,"PASS");
    }
}
#endif
