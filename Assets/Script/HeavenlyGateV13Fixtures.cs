#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using UnityEngine;
namespace TuTienCore
{
 public static class HeavenlyGateV13Fixtures
 {
  public static HeavenlyGatePenaltyPolicy Penalty()=>new HeavenlyGatePenaltyPolicy("fixture:gate-penalty-v1",1,500);
  public static HeavenlyGateConfiguration Configuration()=>new HeavenlyGateConfiguration{schemaVersion=1,version=1,id="fixture:pha-thien-quan-v13",title="Thiên Quan · Phong Ấn",cultivationConfiguration="balance:tribulation-v13-fixture",sourceRealm="realm:luyen-khi",targetRealm="fixture:truc-co",penaltyPolicyId="fixture:gate-penalty-v1",phase="SealCombat",hp=300,damagePerStrike=12,strikeInterval=1,targetRange=200,armor=20,magicResistance=20,receiptLifetimeSteps=2};
  public static CultivationPlaytestProfile Profile()
  {
   var d=new CultivationPlaytestDefinition{schemaVersion=3,version=1,id="balance:tribulation-v13-fixture",title="Thiên Quan · OWNED FIXTURE",startingRealm="realm:luyen-khi",maximumFrameSeconds=.5,stepSeconds=.1,fallbackDensity=.5,absorptionRate=2,absorptionEfficiency=1,refinementRate=1,refinementEfficiency=1,maximumEnergy=100,recoveryRate=2,realms=new[]{new CultivationPlaytestRealm{id="realm:luyen-khi",name="Luyện Khí",tier=0,thresholds=new double[]{0,900,2100,3600},successor="fixture:truc-co"},new CultivationPlaytestRealm{id="fixture:truc-co",name="Trúc Cơ [fixture]",tier=1,thresholds=new double[]{0,1200,2800,4800},successor=""}},qiSources=new CultivationQiSource[0]};
   var caps=new CultivationCapacityDefinition{id="fixture:gate-capacity-v1",version=1,maximumRate=1000,injuryAbsorptionPenalty=.25,injuryRefinementPenalty=.5,realms=new[]{Entry("realm:luyen-khi",1.2,1),Entry("fixture:truc-co",3,2)}};
   CultivationPlaytestProfile p;string why;if(!CultivationPlaytestProfile.TryCreate(d,id=>true,out p,out why,false,caps))throw new InvalidOperationException(why);return p;
  }
  static CultivationRealmCapacityDefinition Entry(string id,double a,double r)=>new CultivationRealmCapacityDefinition{realmId=id,baseAbsorption=a,baseRefinement=r,absorptionStageFactors=new[]{1d,1.125,1.25,1.375},refinementStageFactors=new[]{1d,1.08,1.16,1.24}};
  public static CultivationSession Session(CultivationPlaytestProfile p,double foundation=3600,double reserve=1234,double injury=0)=>new CultivationSession(Guid.NewGuid(),Guid.NewGuid(),p.Catalog,p.Rules,new CultivationSnapshot(new CultivationFoundation(p.StartingRealm,foundation),new UnrefinedQiReserve(reserve),new SpiritualEnergy(17),new BloodEssenceState(0,0),new FoundationInjuryState(injury)));
  public static CultivationSession NearBottleneck(CultivationPlaytestProfile profile,double reserve=7,double remaining=0)
  {
   if(!profile.Catalog.TryGet(profile.StartingRealm,out var realm))throw new InvalidOperationException("Missing fixture starting realm.");
   return Session(profile,Math.Max(0,realm.Thresholds[3]-remaining),reserve);
  }
  public static CultivationReason ApplyFailureFixture(CultivationSession s,Guid id,HeavenlyGatePenaltyPolicy policy)=>s.ApplyGatePenalty(s.OwnerId,s.Epoch,id,s.RealmRevision,s.Current.Foundation.ActualRealmId,policy);
  public static List<string> Run()
  {
   var checks=new List<string>();Action<bool,string> r=(ok,label)=>{if(!ok)throw new Exception(label);checks.Add(label);};var p=Profile();var config=Configuration();var penalty=Penalty();var s=Session(p);var before=s.Current;
   r(!CultivationIntegrationGate.HeavenlyTribulationEnabled,"production remains locked");r(config.IsValid,"versioned fixture config valid");
   foreach(double f in new[]{0d,900,2100,3599}){bool bad=false;try{new HeavenlyGateEncounter(Session(p,f),config,Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),1);}catch(ArgumentException){bad=true;}r(bad,"actual bottleneck required "+f);}
   var a=new HeavenlyGateEncounter(s,config,Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),1);r(a.Advance(1,.1,true)==TribulationReason.Paused&&a.Seconds==0,"pause no trial time");r(a.Advance(1,.1,false)==TribulationReason.None&&a.Advance(1,.1,false)==TribulationReason.Duplicate,"monotonic trial steps");
   var eventId=Guid.NewGuid();r(a.Accept(a.Owner,Guid.NewGuid(),a.AttemptId,a.TargetId,1,eventId,1,10,10)==TribulationReason.Stale,"owner epoch mismatch");r(a.Accept(a.Owner,a.Epoch,a.AttemptId,Guid.NewGuid(),1,eventId,1,10,10)==TribulationReason.Stale,"target identity mismatch");r(a.Accept(a.Owner,a.Epoch,a.AttemptId,a.TargetId,2,eventId,1,10,10)==TribulationReason.Stale,"Hero lifetime mismatch");
   r(a.Accept(a.Owner,a.Epoch,a.AttemptId,a.TargetId,1,eventId,1,10,10)==TribulationReason.None&&a.Accept(a.Owner,a.Epoch,a.AttemptId,a.TargetId,1,eventId,1,10,10)==TribulationReason.Duplicate,"duplicate combat receipt");
   a.Advance(5,.1,false);r(a.Accept(a.Owner,a.Epoch,a.AttemptId,a.TargetId,1,Guid.NewGuid(),1,10,0)==TribulationReason.Stale,"expired success receipt rejected");r(a.Accept(a.Owner,a.Epoch,a.AttemptId,a.TargetId,1,Guid.NewGuid(),5,0,0)==TribulationReason.None&&a.Outcome==HeavenlyGateOutcome.Failure,"same-step double KO fails");r(a.Accept(a.Owner,a.Epoch,a.AttemptId,a.TargetId,1,Guid.NewGuid(),5,20,0)==TribulationReason.Terminal,"terminal cannot become success");r(s.Current==before,"combat evidence never mutates cultivation");
   var id=Guid.NewGuid();r(s.ApplyGatePenalty(s.OwnerId,s.Epoch,id,0,p.StartingRealm,penalty)==CultivationReason.None,"explicit failure penalty");r(s.Current.Foundation.Amount==3100&&s.Current.Reserve.Amount==1234&&s.Current.Injury.Severity==0&&s.Current.Foundation.ActualRealmId==p.StartingRealm,"failure preserves reserve injury actual realm");r(s.ApplyGatePenalty(s.OwnerId,s.Epoch,id,0,p.StartingRealm,penalty)==CultivationReason.DuplicateCommand,"penalty idempotent");r(s.CanAttemptBreakthrough()==CultivationReason.ProgressNotReady,"return to bottleneck required before retry");
   foreach(int tier in new[]{0,1,2,3}){
    CultivationRealmEvaluator catalog;CultivationReason why;CultivationRealmEvaluator.TryCreate(new[]{new CultivationRealmDefinition("fixture:r",tier,new double[]{0,100,300,600})},out catalog,out why);
    var state=new CultivationSnapshot(new CultivationFoundation("fixture:r",600),new UnrefinedQiReserve(17),new SpiritualEnergy(0),new BloodEssenceState(0,0),new FoundationInjuryState(0));
    r(new HeavenlyGatePenaltyPolicy("fixture:max",1,10000).Loss(catalog,state)==(tier<2?300:500),"max stage drop bound tier="+tier);r(new HeavenlyGatePenaltyPolicy("fixture:small",1,10).Loss(catalog,state)==10,"ceiling is not mandatory full penalty tier="+tier);
   }
   s=Session(p);id=Guid.NewGuid();r(new RealmBreakthroughTransition(s).ApplySuccess(s.OwnerId,s.Epoch,id,0,p.StartingRealm,config.targetRealm,0)==CultivationReason.None,"success existing transition");r(s.Current.Foundation.Amount==0&&s.Current.Reserve.Amount==1234&&s.Capacity.Absorption==3&&s.Capacity.Refinement==2,"target capacity with zero foundation and exact reserve");r(s.Tick(s.OwnerId,s.Epoch,0,.1,false,.5)==CultivationReason.None&&s.Current.Foundation.Amount>0,"refinement only on subsequent tick");
   foreach(double injury in new[]{1d,100}){bool bad=false;try{new HeavenlyGateEncounter(Session(p,injury:injury),config,Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),1);}catch(ArgumentException){bad=true;}r(bad,"injury blocks entry "+injury);}
   s=Session(p);a=new HeavenlyGateEncounter(s,config,Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),1);s.ApplyGatePenalty(s.OwnerId,s.Epoch,Guid.NewGuid(),0,p.StartingRealm,penalty);a.Advance(1,.1,false);r(a.Accept(a.Owner,a.Epoch,a.AttemptId,a.TargetId,1,Guid.NewGuid(),1,10,0)==TribulationReason.Stale,"realm revision mismatch");
   foreach(var status in new[]{BurningStatus.Active,BurningStatus.SettlementPending}){
    CultivationRealmEvaluator catalog;CultivationReason why;var c=config.Copy();c.sourceRealm="fixture:blood";c.targetRealm="fixture:next";
    CultivationRealmEvaluator.TryCreate(new[]{new CultivationRealmDefinition(c.sourceRealm,2,new double[]{0,2,5,10},true,new[]{1,2,3,4},true),new CultivationRealmDefinition(c.targetRealm,3,new double[]{0,3,6,12})},out catalog,out why);
    var owner=Guid.NewGuid();var burn=new BurningTransaction(Guid.NewGuid(),owner,10,.2,1,status==BurningStatus.Active?10:0,status,c.sourceRealm,status==BurningStatus.Active?(BurningEndReason?)null:BurningEndReason.Expired);
    var rules=new CultivationRules(.1,1,1,1,1,100,0,1,0,0,1,0,0,0,BloodCapacityOverflowPolicy.HoldExistingStopAccumulation);
    var bs=new CultivationSession(owner,Guid.NewGuid(),catalog,rules,new CultivationSnapshot(new CultivationFoundation(c.sourceRealm,10),new UnrefinedQiReserve(7),new SpiritualEnergy(0),new BloodEssenceState(1,0),new FoundationInjuryState(0),burn));
    bool rejected=false;try{new HeavenlyGateEncounter(bs,c,Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),1);}catch(ArgumentException){rejected=true;}r(rejected,"Burning/pending liability blocks gate "+status);
   }
   bool invalid=false;try{new HeavenlyGatePenaltyPolicy("production:unapproved",1,500);}catch(ArgumentException){invalid=true;}r(invalid,"fixture not production balance");
   return checks;
  }
 }
}
#endif
