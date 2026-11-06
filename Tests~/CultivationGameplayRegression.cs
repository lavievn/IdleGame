using System;
using TuTienCore;
using UnityEngine;

partial class MotionRegression
{
    static CultivationPlaytestDefinition V07Definition()
    {return new CultivationPlaytestDefinition {schemaVersion=1,version=1,id="playtest:unit-v07",title="Unit fixture",startingRealm="realm:luyen-khi",
        realms=new[]{new CultivationPlaytestRealm {id="realm:luyen-khi",name="Luyện Khí",successor="",tier=0,thresholds=new[]{0d,6,12,20}}},
        qiSources=new[]{new CultivationQiSource{nodeId="fixture:a",density=3},new CultivationQiSource{nodeId="fixture:b",density=5}},
        fallbackDensity=2,absorptionRate=1,absorptionEfficiency=1,refinementRate=1,refinementEfficiency=1,maximumFrameSeconds=.5,stepSeconds=.1,maximumEnergy=100,recoveryRate=2};}
    static CultivationPlaytestProfile V07Profile()
    {CultivationPlaytestProfile p;string reason;Check(CultivationPlaytestProfile.TryCreate(V07Definition(),id=>id.StartsWith("fixture:"),out p,out reason),"unit profile "+reason);return p;}
    static void CultivationGameplayTests()
    {
        Run("v07 profile validates version identity thresholds explicit successor density and clock",()=>{
            foreach(string bad in new[]{"version","id","threshold","successor","density","duplicate","unknown-node","clock","rate"}) {
                var d=V07Definition();if(bad=="version")d.schemaVersion=2;if(bad=="id")d.id="production";if(bad=="threshold")d.realms[0].thresholds=new[]{0d,6,6,20};
                if(bad=="successor")d.realms[0].successor="unknown";if(bad=="density")d.fallbackDensity=double.NaN;
                if(bad=="duplicate")d.qiSources[1].nodeId=d.qiSources[0].nodeId;if(bad=="unknown-node")d.qiSources[0].nodeId="unknown";
                if(bad=="clock")d.stepSeconds=0;if(bad=="rate")d.refinementRate=-1;
                CultivationPlaytestProfile p;string reason;Check(!CultivationPlaytestProfile.TryCreate(d,id=>id.StartsWith("fixture:"),out p,out reason) && p==null && reason!=null,"invalid config "+bad);
            }
            var profile=V07Profile();Check(profile.Density("fixture:a")==3 && profile.Density("other")==2 && profile.ExplicitSuccessor(profile.StartingRealm)=="","explicit sources plus fallback, no successor invented");
        });
        Run("v07 one fixed clock is frame-rate independent and rejects duplicate invalid paused disposed callbacks",()=>{
            foreach(int hz in new[]{30,60,144}) {
                var p=V07Profile();var h=HeroAt(1);var b=new CultivationGameplayBinding(h,p,CultivationGameplayState.New(p));int exp=h.currentExp;
                for(int frame=0;frame<10*hz;frame++)Check(b.Advance(h,frame,1d/hz,false,"fixture:a")==CultivationReason.None,"fixed tick");
                Check(b.TickCount==100,"same tick count at "+hz);Near((float)b.Current.Foundation.Amount,10,"ten foundation at "+hz);Near((float)b.Current.Reserve.Amount,20,"twenty reserve at "+hz);
                Check(h.currentLevel==1 && h.currentExp==exp,"Qi no EXP");var snapshot=b.Current;
                Check(b.Advance(h,10*hz-1,.1,false,"fixture:a")==CultivationReason.DuplicateCommand && b.Current==snapshot,"no double clock");
                Check(b.Advance(h,10*hz,.1,true,"fixture:a")==CultivationReason.Paused && b.Current==snapshot,"pause no progress");
                foreach(double dt in new[]{-1d,double.NaN,double.PositiveInfinity,1})Check(b.Advance(h,10*hz+1,dt,false,"fixture:a")==CultivationReason.InvalidDelta && b.Current==snapshot,"bad frame");
                b.Dispose();Check(b.Advance(h,10*hz+2,.1,false,"fixture:a")==CultivationReason.OwnerMismatch && b.Current==snapshot,"disposed owner cannot grant");
            }
        });
        Run("v07 canonical component roundtrip validates before rebind and never grants on repeated Load",()=>{
            var p=V07Profile();var h=HeroAt(45);var b=new CultivationGameplayBinding(h,p,CultivationGameplayState.New(p));
            for(int i=0;i<140;i++)b.Advance(h,i,.1,false,"fixture:a");string component=h.cultivationJson;CultivationSession loaded;string reason;
            for(int i=0;i<3;i++) {
                Check(CultivationGameplayState.TryRestore(component,p,out loaded,out reason),"component Load "+reason);
                Near((float)loaded.Current.Foundation.Amount,14,"foundation kept");Near((float)loaded.Current.Reserve.Amount,28,"reserve kept");
                Check(loaded.Current.Foundation.ActualRealmId==p.StartingRealm && loaded.Current.Blood.Charges==0 && loaded.Current.Burning==null,"no realm/charge inference from Level45");
            }
            Check(h.currentLevel==45 && b.Current.Foundation.Amount<20,"Level independent");
            var prior=b.Current;string saved=h.cultivationJson;h.AddExp(h.expToNextLevel-h.currentExp);Check(h.currentLevel==46 && b.Current==prior && h.cultivationJson==saved,"EXP Level Up does not mutate attached cultivation snapshot");
        });
        Run("v07 malformed future unknown partial config or Burning component rejects without mutation",()=>{
            var p=V07Profile();var h=HeroAt(1);var b=new CultivationGameplayBinding(h,p,CultivationGameplayState.New(p));var current=b.Current;
            string original=h.cultivationJson;
            foreach(string bad in new[]{original.Replace("\"version\":1","\"version\":2"),original.Replace(p.Id,"unknown"),original.Replace("\"configurationVersion\":1","\"configurationVersion\":2"),
                original.Replace("realm:luyen-khi","unknown"),original.Replace("\"foundation\":0,",""),original.Replace("\"hasBurn\":false","\"hasBurn\":true"),"{\"version\":1,\"version\":1}","{}"}) {
                CultivationSession loaded;string reason=null;Check(bad!=original && !CultivationGameplayState.TryRestore(bad,p,out loaded,out reason) && loaded==null && b.Current==current,"invalid candidate retained prior state "+reason);
            }
            string component,error;Check(CultivationGameplayState.TryReadHeroJson("{\"currentLevel\":45}",p,out component,out error) && component==null,"legacy absence is explicit");
            Check(!CultivationGameplayState.TryReadHeroJson("{\"cultivationJson\":null}",p,out component,out error),"null is not legacy absence");
            Check(!CultivationGameplayState.TryRestore(original,null,out var ignored,out error),"ordinary Save context cannot erase/reinterpret playtest component");
        });
        Run("v07 long fixed simulation preserves bottleneck reserve and Travel only changes Qi source",()=>{
            var p=V07Profile();var h=HeroAt(1);var b=new CultivationGameplayBinding(h,p,CultivationGameplayState.New(p));
            for(int i=0;i<10000;i++)b.Advance(h,i,.1,false,i<5000?"fixture:a":"fixture:b");
            Near((float)b.Current.Foundation.Amount,20,"cap");Near((float)b.Current.Reserve.Amount,3980,"total absorbed4000 minus foundation20");
            Check(b.TickCount==10000 && b.Density==5 && b.Current.Foundation.ActualRealmId==p.StartingRealm && h.currentLevel==1,"no auto breakthrough or EXP");
            Check(p.Catalog.Evaluate(p.StartingRealm,b.Current.Foundation.Amount).AtBottleneck,"actual evaluator cap");
        });
    }
}
