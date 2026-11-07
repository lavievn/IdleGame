#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using TMPro;
namespace TuTienCore
{
 public static class DevelopmentV13GameplayScenario
 {
  const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
  static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
  static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
  static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
  static void R(bool ok,string why){if(!ok)throw new InvalidOperationException(why);}
  static IEnumerator Wait(Func<bool> ready,float seconds,string label){float end=Time.realtimeSinceStartup+seconds;while(!ready()){R(Time.realtimeSinceStartup<end,"Timed out: "+label);yield return null;}}
  static IEnumerator Ready(GameManager gm,UIManager ui){yield return Wait(()=>!gm.IsEnteringGameplay,30,"entry");ui.SetPaused(true);yield return null;}
  static void Clear(GameManager gm,CombatManager combat){Call(gm,"CancelNextWave");combat.ForceClearAllMonsters();gm.activeMonsters.Clear();}
  static List<CombatManager.ActiveMonsterInfo> Lives(CombatManager combat)=>(List<CombatManager.ActiveMonsterInfo>)Get(combat,"activeMonsters");
  static void Weapon(GameManager gm,WeaponFamily f)
  {
   if(f==WeaponFamily.Unarmed){if(WeaponBasicAttack.Equipped(gm.HeroData)!=null)R(gm.UnequipGear(EquipmentSlot.Weapon),"fixture unequip");return;}
   var gear=f==WeaponFamily.Sword?EquipmentFoundation.CreateDemoSword():f==WeaponFamily.Bow?EquipmentFoundation.CreateDemoBow():EquipmentFoundation.CreateDemoStaff();
   R(EquipmentRuntime.AddInstance(gm.HeroData,gear)&&gm.EquipGear(gear.instanceId,EquipmentSlot.Weapon),"fixture equip "+f);
  }
  static void StartGroup(GameManager gm,CombatManager combat,HeroController hero,EnvironmentManager space,int count,bool overlap)
  {
   Clear(gm,combat);Set(gm,"retryWaveSize",count);Call(gm,"CallNextWave");R(Lives(combat).Count==count,"real pooled wave "+count);
   Vector2 p=space.Position(hero.heroRect);int i=0;foreach(var m in Lives(combat)){m.currentHP=1;m.maxHP=1;m.attackTimer=1000;m.controller.moveSpeed=0;m.controller.ReceiveAlert(hero,true);space.SetPosition(m.controller.Rect,p+new Vector2(-8-(overlap?0:4*i++),0));m.controller.UpdateHealthBar(1,1);}
  }
  public static IEnumerator Run(GameManager gm,SaveManager saves,UIManager ui,CombatManager combat,HeroController hero,EnvironmentManager space,string evidence,Func<int,int,IEnumerator> resize,Func<string,IEnumerator> capture,Action<string> pass)
  {
   R(!CultivationIntegrationGate.HeavenlyTribulationEnabled&&saves.TrialConfiguration!=null&&gm.Cultivation.Profile.Id==saves.TrialConfiguration.cultivationConfiguration,"fixture gate only; canonical profile");
   File.WriteAllLines(Path.Combine(evidence,"gate-domain.txt"),HeavenlyGateV13Fixtures.Run());
   var audit=new List<string>{"family,count,overlap,releases,accepted_hits,killed"};
   foreach(var family in new[]{WeaponFamily.Unarmed,WeaponFamily.Sword,WeaponFamily.Bow,WeaponFamily.Staff}){
    Weapon(gm,family);foreach(int count in new[]{2,3})foreach(bool overlap in new[]{true,false}){
     StartGroup(gm,combat,hero,space,count,overlap);var targets=new List<CombatManager.ActiveMonsterInfo>(Lives(combat));long release=combat.HeroReleaseCount,hits=combat.AcceptedHeroHitCount;combat.CurrentHeroCombatState.AttackTimer=0;ui.SetPaused(false);
     yield return Wait(()=>combat.AcceptedHeroHitCount>hits,8,"single basic accepted "+family);ui.SetPaused(true);
     int dead=targets.FindAll(x=>x.currentHP==0).Count;R(combat.HeroReleaseCount-release==1&&combat.AcceptedHeroHitCount-hits==1&&combat.LastImpactRelease==combat.HeroReleaseCount&&dead==1&&Lives(combat).Count==count-1,"one real basic release, one accepted kill "+family+"/"+count);
     audit.Add(family+","+count+","+overlap+",1,1,1");
    }
   }
   // Intentionally choose the legacy AoE branch in an owned fixture; canonical basic is restored before trials.
   StartGroup(gm,combat,hero,space,3,true);hero.BindBasicAttackData(gm.HeroData,true);hero.ChangeAttackMode((int)AttackMode.RangedMagic);var legacy=Lives(combat).ToArray();long legacyRelease=combat.HeroReleaseCount,legacyHits=combat.AcceptedHeroHitCount;combat.CurrentHeroCombatState.AttackTimer=1000;ui.SetPaused(false);
   yield return Wait(()=>combat.AcceptedHeroHitCount>legacyHits,8,"legacy magic impact");ui.SetPaused(true);
   R(combat.HeroReleaseCount-legacyRelease==1&&combat.AcceptedHeroHitCount-legacyHits==3&&Array.TrueForAll(legacy,x=>x.currentHP==0),"legacy spell intentionally has multi-target explosion");audit.Add("LegacyAoE,3,True,1,3,3");hero.BindBasicAttackData(gm.HeroData,false);File.WriteAllLines(Path.Combine(evidence,"single-target.csv"),audit);
   pass("16 native basic cases: one release, one accepted hit and kill for 2/3 enemies overlap/spread; deliberate legacy AoE control kills three. No reproduced basic multi-kill.");
   var rows=new List<string>{"fixture,family,outcome,combat_seconds,hero_hp_start,hero_hp_before_respawn,first_incoming_damage,reserve,penalty,hero_releases,accepted_hero_hits,saw_projectile"};
   var approaches=new List<string>{"fixture,family,exploration_at_reveal,wall_x_at_reveal,world_pan_before_combat"};
   foreach(string name in new[]{"weak","sword","bow","staff","unarmed","same-hp-armor"}){
    var family=name=="sword"?WeaponFamily.Sword:name=="bow"?WeaponFamily.Bow:name=="staff"?WeaponFamily.Staff:WeaponFamily.Unarmed;
    Weapon(gm,family);var h=UnityEngine.Object.Instantiate(gm.HeroData);h.name="V13 owned "+name;h.tribulationJson="";h.currentLevel=1;h.currentExp=0;h.gold=777;h.professionProgress=new List<ProfessionProgress>{new ProfessionProgress{profession=SupportProfession.Forging,level=2,experience=7}};
    h.combatStatModifiers.RemoveAll(x=>x.sourceId!=null&&x.sourceId.StartsWith("fixture:gate:"));
    h.ApplyHeroBalance();int power=name=="weak"||name=="same-hp-armor"?78:185;int wantedHP=name=="weak"||name=="same-hp-armor"?24:600;
    h.combatStatModifiers.Add(new CombatStatModifier{sourceId="fixture:gate:power",stat=CombatStatId.Attack,flat=power-WeaponBasicAttack.Physical(h)});
    h.combatStatModifiers.Add(new CombatStatModifier{sourceId="fixture:gate:magic",stat=CombatStatId.MagicalAttack,flat=power-WeaponBasicAttack.Magical(h)});
    h.combatStatModifiers.Add(new CombatStatModifier{sourceId="fixture:gate:hp",stat=CombatStatId.MaxHealth,flat=wantedHP-h.GetCalculatedHealth(WeaponBasicAttack.Resolve(h))});
    if(name=="same-hp-armor")h.combatStatModifiers.Add(new CombatStatModifier{sourceId="fixture:gate:armor",stat=CombatStatId.Armor,flat=900});
    var profile=saves.CultivationProfile;using(var binding=new CultivationGameplayBinding(h,profile,HeavenlyGateV13Fixtures.Session(profile))){R(binding.Advance(h,0,.037,false,h.worldMapId)==CultivationReason.None,"fractional source fixture");}
    R(saves.SaveGame(h,SaveSlot.ManualSave2),"fixture seed slot");UnityEngine.Object.Destroy(h);R(gm.LoadAndContinue(SaveSlot.ManualSave2),"load canonical bottleneck fixture");yield return Ready(gm,ui);
    var skyVariant=name=="bow"?HeavenlyTrialView.SkyVariant.B:name=="staff"?HeavenlyTrialView.SkyVariant.Still:HeavenlyTrialView.SkyVariant.A;
    Set(combat,"trialSky",skyVariant);
    var source=gm.HeroData;var bindingBefore=gm.Cultivation;var body=hero.heroRect.GetComponent<CultivationSpriteBridge>().BodyGraphic.sprite;
    string map=source.worldMapId,inventory=JsonUtility.ToJson(new Bag(source));int wave=source.worldMapWaveProgress,level=source.currentLevel;float exp=source.currentExp;int hpStart=combat.CurrentHeroHP;R(hpStart==wantedHP&&WeaponBasicAttack.DamageSource(source)==power,"fixture exact HP/attack provenance "+name+" HP="+hpStart+" ATK="+WeaponBasicAttack.DamageSource(source));
    long releaseStart=combat.HeroReleaseCount,hitStart=combat.AcceptedHeroHitCount;int life=combat.CurrentHeroCombatState.LifeVersion;
    ui.OpenHeroStats();ui.RefreshCharacterProgression();R(gm.TribulationLockedReason=="","actual eligible fixture");
    if(name=="sword"){
     foreach(var size in new[]{new Vector2Int(600,338),new Vector2Int(800,450),new Vector2Int(1150,647),new Vector2Int(1360,680)}){yield return resize(size.x,size.y);ui.RefreshCharacterProgression();Fit((TextMeshProUGUI)Get(ui,"progressionSummary"),"character "+size.x);R(!((TextMeshProUGUI)Get(ui,"trialHud")).isActiveAndEnabled,"result HUD never covers Character");yield return capture("v13-character-"+size.x);}
    }
    var heroBeforeTrial=space.Position(hero.heroRect);
    if(name=="sword"){
     var btn=(RectTransform)Get(ui,"trialButton");Vector2 point=RectTransformUtility.WorldToScreenPoint(null,btn.TransformPoint(btn.rect.center));
     R(ui.HitControl(point)!=null&&ui.HitControl(point).GetRect()==btn,"real Character trial button hit region");ui.HandleMouseClick(point);R(gm.HasTribulationLiability,"trial UI handler started owned combat "+gm.LastTribulationNotice);
    }else R(gm.RequestTribulation(),"start real owned trial "+name+" "+gm.LastTribulationNotice);R(gm.HeroData==source&&combat.HeavenlyGateHP==300&&combat.RegisteredHeroCount==1&&gm.activeMonsters.Count==0&&Lives(combat).Count==0&&Vector2.Distance(heroBeforeTrial,space.Position(hero.heroRect))<.01f,"same Hero stays in place, no enemies before exploration");
    R(!gm.RequestTribulation()&&!gm.TryManualSave(SaveSlot.ManualSave1)&&!gm.LoadAndContinue(SaveSlot.ManualSave2)&&!gm.TryTravelToWorldMap(map)&&!gm.ReturnToMainMenu(),"single active attempt blocks competing lifecycle transitions");
    var view=combat.TrialView;
    var targets=Lives(combat);
    R(combat.HeavenlyGateApproaching && view!=null && view.IsShowingStill &&
      view.transform.GetSiblingIndex()==0, "exploration starts with the trial backdrop before any gate is spawned");
    long incomingStart=combat.TrialStrikeCount;
    int firstIncoming=0;
    if(name=="weak")File.Copy(Path.Combine(saves.SaveDirectory,"AutoSave.json"),Path.Combine(saves.SaveDirectory,"V13InterruptedFixture.json"),true);
    if(name=="sword"){
     ui.SetPaused(true);int seal=combat.HeavenlyGateHP;double clock=bindingBefore.ActiveSeconds;
     foreach(var size in new[]{new Vector2Int(600,338),new Vector2Int(800,450),new Vector2Int(1150,647),new Vector2Int(1360,680)}){yield return resize(size.x,size.y);ui.RefreshCharacterProgression();Fit((TextMeshProUGUI)Get(ui,"trialHud"),"trial HUD "+size.x);yield return capture("v13-trial-"+size.x);}
     R(combat.HeavenlyGateHP==seal&&bindingBefore.ActiveSeconds==clock&&
       combat.GateExplorationDistance==0&&targets.Count==0,"pause does not bank exploration or spawn a gate");
     // This fixture holds the normal basic windup so we can observe a full
     // video loop and capture a live strike at all sizes on one encounter.
     combat.CurrentHeroCombatState.AttackTimer=999;
     ui.SetPaused(false);
    }
    float walkDeadline=Time.realtimeSinceStartup+30;
    while(targets.Count==0)
    {
     R(Time.realtimeSinceStartup<walkDeadline && gm.TribulationFighting,"explore before seal "+name);
     R(combat.GateExplorationDistance<800 && combat.TrialStrikeCount==incomingStart &&
       !combat.TrialWarning && combat.CurrentHeroHP==hpStart && combat.HeroReleaseCount==releaseStart,
       "no gate, attacks or lightning during exploration");
     yield return null;
    }
    double explored=combat.GateExplorationDistance;
    R(targets.Count==1 && explored>=800 && explored<=800+hero.MovementBudgetThisFrame+.1f &&
       view.GroundScroll>0,"one gate spawns on the first real movement step reaching 800");
    var gate=targets[0];
    var gateRect=gate.controller.Rect;
    var gateArt=(RectTransform)gateRect.GetChild(0);
    float firstWallX=space.Position(gateArt).x;
    float firstGroundScroll=view.GroundScroll;
    float rightStoneEdge=firstWallX+gateArt.rect.width*(1047f/1672f-gateArt.pivot.x);
    R(rightStoneEdge<space.BattleArea.rect.xMin && !hero.CanAttack(gate.controller,space) &&
      space.Position(gateRect).x<space.Position(hero.heroRect).x &&
      Mathf.Abs(space.Position(gateRect).y-view.GroundY)<.01f &&
      gateRect.GetComponent<CultivationSpriteBridge>()==null &&
      gateArt.GetComponent<UnityEngine.UI.Image>().sprite!=null &&
      gateArt.rect.width>gateRect.rect.width*2 && Mathf.Abs(gateArt.rect.height-864)<.01f,
      "large fixed gate starts completely offscreen to the left with a compact receiver");
    bool capturedEntry=false;
    while(combat.HeavenlyGateApproaching)
    {
     R(Time.realtimeSinceStartup<walkDeadline && gm.TribulationFighting,"approach revealed seal "+name);
     R(combat.TrialStrikeCount==incomingStart && !combat.TrialWarning && combat.CurrentHeroHP==hpStart &&
       combat.HeavenlyGateHP==300 && combat.HeroReleaseCount==releaseStart,"both sides wait until Hero reaches the seal");
     if(name=="sword" && !capturedEntry && space.Position(gateArt).x>space.BattleArea.rect.xMin)
     {
      capturedEntry=true;
      yield return capture("v014-wall-enters");
     }
     yield return null;
    }
    float worldPan=view.GroundScroll-firstGroundScroll;
    R(hero.CanAttack(gate.controller,space) && worldPan>0 &&
       Mathf.Abs(space.Position(gateArt).x-firstWallX-worldPan)<.15f &&
       space.Position(gateArt).x>space.BattleArea.rect.xMin,
       "the camera moved the fixed wall by exactly the same amount as the terrain");
    approaches.Add(FormattableString.Invariant($"{name},{family},{explored:0.###},{firstWallX:0.###},{worldPan:0.###}"));
    bool projectile=combat.PendingProjectileCount>0;
    var music=MapBgmController.Instance;
    var trialTrack=(AudioSource)Get(music,"trialSource");
    R(trialTrack!=null&&trialTrack.clip.name=="HeavenlyGateTheme"&&trialTrack.loop&&trialTrack.isPlaying&&
       Mathf.Abs(trialTrack.volume-MapBgmController.CurrentVolume)<.001f&&
       Array.TrueForAll(music.GetComponents<AudioSource>(),a=>a==trialTrack||!a.isPlaying),
       "only the dedicated trial track plays, looping at the global BGM volume");
    if(name=="sword"){
     R(capturedEntry,"wall entered the frame while Hero was still approaching");
     yield return Wait(()=>Mathf.Abs(space.Position(gateArt).x-
       (space.BattleArea.rect.xMin+space.BattleArea.rect.width*.35f))<1,5,"camera settles on gate");
     yield return Wait(()=>view.IsShowingVideo,8,"A decoded first frame");
     var decoder=(UnityEngine.Video.VideoPlayer)Get(view,"video");
     R(decoder.url.EndsWith("TribulationSky_A.mp4")&&decoder.isLooping&&
        decoder.audioOutputMode==UnityEngine.Video.VideoAudioOutputMode.None&&
        decoder.targetTexture.width==640&&decoder.targetTexture.height==360&&
        view.GetComponents<UnityEngine.Video.VideoPlayer>().Length==1,"A source, one silent decoder and 640x360 target");
     bool pauseChecked=false;
     var proof=new List<string>{"width,height,frame,encounter,strike,target_life,target_spawn,hero_life,battle_version,hp_before,hp_after"};
     var activeReceipt=TribulationSaveTransaction.Read(gm.HeroData.tribulationJson);
     foreach(var size in new[]{new Vector2Int(600,338),new Vector2Int(800,450),new Vector2Int(1150,647),new Vector2Int(1360,680)})
     {
      yield return resize(size.x,size.y);
      ui.RefreshCharacterProgression();
      Fit((TextMeshProUGUI)Get(ui,"trialHud"),"live trial HUD "+size.x);
      var heroPoint=RectTransformUtility.WorldToScreenPoint(null,hero.heroRect.position);
      var gatePoint=RectTransformUtility.WorldToScreenPoint(null,gateArt.position);
      var receiverPoint=RectTransformUtility.WorldToScreenPoint(null,gateRect.position);
      R(Mathf.Abs(receiverPoint.y-gatePoint.y-100)<1f,"wall is 100 physical pixels lower at "+size.x);
      R(heroPoint.x<size.x*.8f&&Mathf.Abs(gatePoint.x-size.x*.35f)<2&&
        gatePoint.x<heroPoint.x,"gate visual center at 35%, Hero approaches from the right at "+size.x);
      yield return Wait(()=>combat.TrialWarning&&view.Lightning.HasWarning,4,"live warning "+size.x);
      long last=combat.TrialStrikeCount;
      int hp=combat.CurrentHeroHP;
      if(!pauseChecked)
      {
       ui.SetPaused(true);yield return null;
       yield return Wait(()=>view.VideoPaused,2,"paused decoder");
       long frame=view.VideoFrame;
       yield return capture("v014-warning-"+size.x);
       yield return new WaitForSecondsRealtime(.4f);
       R(combat.TrialStrikeCount==last&&combat.CurrentHeroHP==hp&&combat.TrialWarning&&
         view.Lightning.HasWarning&&view.VideoFrame==frame,"Pause freezes warning, combat HP and video frame");
       ui.SetPaused(false);
       pauseChecked=true;
      }
      yield return Wait(()=>combat.TrialStrikeCount>last,4,"real lightning impact "+size.x);
      var strike=combat.LastTrialStrike;
      R(combat.TrialStrikeCount==last+1 && strike.Encounter.ToString("N")==activeReceipt.encounterId &&
        strike.Strike>0 && strike.TargetLife==gate.lifeId && strike.TargetSpawn==gate.controller.SpawnVersion &&
        strike.HeroLife==life && strike.HPBefore==hp && strike.HPAfter==combat.CurrentHeroHP &&
        strike.HPAfter<hp && view.Lightning.HasImpact && !view.Lightning.HasWarning,"one accepted impact with active identities and actual HP loss");
      if(firstIncoming==0)firstIncoming=hp-strike.HPAfter;
      Canvas.ForceUpdateCanvases();
      ui.RefreshCharacterProgression();
      R(view.Lightning.transform.GetSiblingIndex()==space.BattleArea.childCount-1&&
        !view.Lightning.canvasRenderer.cull&&view.Lightning.DrawnVertices>=100,
        "live lightning mesh actually built and paints after actors");
      proof.Add(string.Join(",",new[]{size.x.ToString(),size.y.ToString(),view.VideoFrame.ToString(),activeReceipt.encounterId,
        strike.Strike.ToString(),strike.TargetLife.ToString(),strike.TargetSpawn.ToString(),strike.HeroLife.ToString(),
        strike.BattleVersion.ToString(),strike.HPBefore.ToString(),strike.HPAfter.ToString()}));
      yield return capture("v014-impact-"+size.x);
     }
     yield return Wait(()=>view.Loops>0,12,"A loop point");
     File.WriteAllLines(Path.Combine(evidence,"lightning-impacts.csv"),proof);
     pass("A decoded and looped silently with one 640x360 RenderTexture. Four live impact sizes show actual HP loss. Pause froze warning and video frame.");
     combat.CurrentHeroCombatState.AttackTimer=0;
     saves.FailNextTrialWrite="BeforeReplace";
    }
    if(name=="bow"){
     yield return Wait(()=>view.IsShowingVideo,8,"B decoded first frame");
     var decoder=(UnityEngine.Video.VideoPlayer)Get(view,"video");
     R(decoder.url.EndsWith("TribulationSky_B.mp4")&&view.GetComponents<UnityEngine.Video.VideoPlayer>().Length==1,"B selected with one decoder");
     yield return capture("v014-video-b");
     pass("B selected and decoded in the actual Bow trial.");
    }
    if(name=="staff"){
     R(view.IsShowingStill&&view.GetComponents<UnityEngine.Video.VideoPlayer>().Length==0,"still mode needs no decoder");
     yield return capture("v014-fallback");
     pass("PNG fallback visible in the actual Staff trial, with no decoder or RenderTexture.");
    }
    projectile|=combat.PendingProjectileCount>0;
    float end=Time.realtimeSinceStartup+30;
    while(gm.TribulationFighting){R(Time.realtimeSinceStartup<end,"combat timeout "+name);projectile|=combat.PendingProjectileCount>0;if(combat.CurrentHeroHP<hpStart&&firstIncoming==0)firstIncoming=hpStart-combat.CurrentHeroHP;R(hero.heroRect.GetComponent<CultivationSpriteBridge>().BodyGraphic.sprite==body,"same Hero sprite in every weapon mode");yield return null;}
    long finalIncoming=combat.TrialStrikeCount;
    if(name=="sword"){
     R(gm.HasTribulationLiability&&gm.HeroData==source&&gm.Cultivation==bindingBefore&&gm.LastTribulationNotice.StartsWith("Chưa ghi kết quả"),"save fault keeps original canonical Hero; no false success");
     string expected=saves.TrialSnapshotHash(SaveSlot.AutoSave);R(gm.RetryTribulationCommit()&&!gm.RetryTribulationCommit()&&saves.TrialSnapshotHash(SaveSlot.AutoSave)!=expected,"one accepted terminal retry");
     var flare=GameObject.Find("Committed seal break");
     Canvas.ForceUpdateCanvases();
     R(flare!=null&&flare.GetComponent<HeavenlyLightningVisual>().DrawnVertices>0,
       "committed break flare draws through entry transition");
     yield return capture("v014-seal-break");
    }
    yield return Ready(gm,ui);var receipt=TribulationSaveTransaction.Read(gm.HeroData.tribulationJson);var b=gm.Cultivation;
    bool success=name!="weak";
    R(receipt.outcome==(success?"Success":"Failure")&&b.Current.Foundation.Amount==(success?0:3100)&&b.Current.Foundation.ActualRealmId==(success?"fixture:truc-co":"realm:luyen-khi"),"real combat outcome, stage and loss "+name+" outcome="+receipt.outcome+" HP="+hpStart+" endHP="+gm.LastTribulationHeroHP+" foundation="+b.Current.Foundation.Amount);
    R(b.Current.Reserve.Amount==1234&&b.Current.Injury.Severity==0&&b.Current.Blood.Charges==0&&b.PendingSeconds==.037&&gm.HeroData.currentLevel==level&&gm.HeroData.currentExp==exp&&gm.HeroData.worldMapId==map&&gm.HeroData.worldMapWaveProgress==wave&&JsonUtility.ToJson(new Bag(gm.HeroData))==inventory,"exact reserve/clock/injury/EXP/equipment/map preserved");
    R(source==null&&bindingBefore.IsDisposed&&combat.CurrentHeroCombatState.Data==gm.HeroData&&combat.CurrentHeroCombatState.LifeVersion!=life&&Get(space,"cameraTarget")==hero&&combat.RegisteredHeroCount==1&&!combat.HeavenlyGateActive&&combat.PendingProjectileCount==0&&combat.CurrentHeroHP>0,"candidate swap, old Hero destroyed, fresh life and camera/wave restored");
    R(view==null && combat.TrialView==null && combat.TrialStrikeCount==finalIncoming &&
      finalIncoming>incomingStart && !combat.TrialWarning &&
      GameObject.Find("Heavenly Trial Environment")==null && GameObject.Find("Trial lightning")==null,
      "environment, decoder, lightning and pending strike retired on result "+name);
    R(!trialTrack.isPlaying,"dedicated music stopped on returning from the trial");
    R(!success||b.Capacity.Absorption==3&&b.Capacity.Refinement==2,"new realm capacities derived immediately");
    R((family!=WeaponFamily.Bow&&family!=WeaponFamily.Staff)||projectile,"actual ranged projectile observed");
    if(!success){R(!gm.RequestTribulation()&&gm.TryManualSave(SaveSlot.ManualSave1),"failed Hero must regain bottleneck; save terminal failure for restart");ui.RefreshCharacterProgression();yield return capture("v13-failure-hud");}
    if(firstIncoming==0)firstIncoming=hpStart-gm.LastTribulationHeroHP;
    rows.Add(string.Join(",",new[]{name,family.ToString(),receipt.outcome,gm.LastTribulationCombatSeconds.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture),hpStart.ToString(),gm.LastTribulationHeroHP.ToString(),firstIncoming.ToString(),b.Current.Reserve.Amount.ToString(),receipt.penalty.ToString(),(combat.HeroReleaseCount-releaseStart).ToString(),(combat.AcceptedHeroHitCount-hitStart).ToString(),projectile.ToString()}));
    pass("Real seal combat "+name+" "+receipt.outcome+" at "+gm.LastTribulationCombatSeconds.ToString("0.###")+"s; reserve=1234, no EXP/items, canonical life restored");
   }
   ui.RefreshCharacterProgression();yield return capture("v13-success-hud");
   R(gm.TryManualSave(SaveSlot.ManualSave2),"terminal success slot for restart");string exact=gm.HeroData.cultivationJson,receiptJson=gm.HeroData.tribulationJson;
   for(int i=0;i<2;i++){R(gm.LoadAndContinue(SaveSlot.ManualSave2),"terminal load "+i);yield return Ready(gm,ui);R(gm.HeroData.cultivationJson==exact&&gm.HeroData.tribulationJson==receiptJson,"terminal load exact no replay");}
   string selectedHash=saves.TrialSnapshotHash(SaveSlot.ManualSave2);var bound=gm.Cultivation;WorldMapNode node;R(WorldMapCatalog.TryGet(gm.HeroData.worldMapId,out node),"post-trial map node");
   string travel=null;foreach(var link in node.Links){WorldMapNode linked;if(WorldMapCatalog.TryGet(link,out linked)&&!linked.IsBoss){travel=link;break;}}R(travel!=null,"fixture travel neighbor");
   if(!gm.HeroData.worldMapCleared.Contains(node.Id))gm.HeroData.worldMapCleared.Add(node.Id); // Explicit fixture permission, no trial reward.
   R(gm.TryTravelToWorldMap(travel),"real Travel after settled trial");yield return Ready(gm,ui);
   R(gm.HeroData.worldMapId==travel&&gm.Cultivation==bound&&bound.PendingSeconds==0&&saves.TrialSnapshotHash(SaveSlot.ManualSave2)==selectedHash,"Travel settles old fraction; terminal manual snapshot intact");
   File.WriteAllLines(Path.Combine(evidence,"gate-approach.csv"),approaches);
   pass("All trial Heroes explored 800 units before the wall spawned outside the left edge. Camera scrolled wall and ground together. Wall is 100 pixels lower at four sizes; the dedicated track stopped after each result.");
   File.WriteAllLines(Path.Combine(evidence,"trial-combat.csv"),rows);ui.OpenHeroStats();ui.RefreshCharacterProgression();yield return capture("v13-result-character");ui.CloseDamageInfo();pass("Save failure/retry, repeated Load, pause, four native Character/trial HUD sizes and fixture-only lifecycle PASS; native input NOT RUN.");
  }
  static void Fit(TextMeshProUGUI label,string caption){R(label!=null&&label.isActiveAndEnabled,caption+" visible");label.ForceMeshUpdate();R(!label.isTextOverflowing,caption+" overflow: "+label.text);}
  [Serializable] sealed class Bag {public List<InventoryStack> inventory;public List<EquipmentInstance> equipment;public List<ProfessionProgress> forge;public long gold;public Bag(EntityDataSO h){inventory=h.inventory;equipment=h.equipmentItems;forge=h.professionProgress;gold=h.gold;}}
  public static IEnumerator Replay(GameManager gm,SaveManager saves,UIManager ui,Action<string> pass)
  {
   foreach(var slot in new[]{SaveSlot.ManualSave1,SaveSlot.ManualSave2}){
    R(gm.LoadAndContinue(slot),"fresh process terminal "+slot);yield return Ready(gm,ui);var receipt=TribulationSaveTransaction.Read(gm.HeroData.tribulationJson);
    R(receipt.outcome==(slot==SaveSlot.ManualSave1?"Failure":"Success")&&gm.HeroData.cultivationJson==receipt.resultCultivation&&gm.Cultivation.Current.Reserve.Amount==1234,"exact terminal restart no repeat reward or loss");
   }
   File.Copy(Path.Combine(saves.SaveDirectory,"V13InterruptedFixture.json"),Path.Combine(saves.SaveDirectory,"AutoSave.json"),true);
   var current=gm.HeroData;string hash=saves.TrialSnapshotHash(SaveSlot.AutoSave);R(!gm.LoadAndContinue(SaveSlot.AutoSave)&&gm.HeroData==current&&saves.TrialSnapshotHash(SaveSlot.AutoSave)==hash&&!saves.ResetAutoSave(),"fresh process active liability refuses resume/award/loss/reset");
   pass("Fresh Windows process restores exact success and failure; interrupted active checkpoint remains blocked and unchanged.");
  }
 }
}
#endif
