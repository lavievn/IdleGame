using System;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;
using TuTienCore;
using UnityEngine;

partial class MotionRegression
{
    static CultivationPlaytestDefinition V08Definition()
    {
        // Mono has no native JsonUtility object-array deserialization. Native verifier reads this exact asset.
        var d=V07Definition();d.schemaVersion=2;d.id="balance:cultivation-early-v1";d.realms[0].thresholds=new[]{0d,900,2100,3600};
        d.qiSources=new[]{new CultivationQiSource{nodeId="TV-20",density=1},new CultivationQiSource{nodeId="TL-16",density=1},new CultivationQiSource{nodeId="XD-11",density=1},new CultivationQiSource{nodeId="TM-12",density=1},new CultivationQiSource{nodeId="HT-17",density=1}};
        d.fallbackDensity=1;d.absorptionRate=2;return d;
    }
    static CultivationPlaytestProfile V08Profile()
    {CultivationPlaytestProfile p;string reason;Check(CultivationPlaytestProfile.TryCreate(V08Definition(),id=>true,out p,out reason),"balance profile "+reason);return p;}
    static void BalanceNear(double a,double b,string message,double tolerance=1e-6){Check(Math.Abs(a-b)<tolerance,message+": "+a+" vs "+b);}
    static void CultivationBalanceTests()
    {
        Run("v08 actual balance asset agrees with Mono fixture and initial state bounds",()=>{
            string raw=File.ReadAllText("Assets/Editor/CultivationBalanceV1.json");System.Collections.Generic.Dictionary<string,string> fields;string reason;
            Check(CultivationSaveJson.TryObject(raw,out fields,out reason),"balance JSON");var d=V08Definition();
            foreach(var name in new[]{"schemaVersion","version","fallbackDensity","absorptionRate","absorptionEfficiency","refinementRate","refinementEfficiency","initialFoundation","initialReserve","initialEnergy","maximumFrameSeconds","stepSeconds","maximumEnergy","recoveryRate"})
                BalanceNear(double.Parse(fields[name],CultureInfo.InvariantCulture),Convert.ToDouble(typeof(CultivationPlaytestDefinition).GetField(name).GetValue(d)),"asset scalar "+name);
            Check(raw.Contains(d.id) && Regex.IsMatch(fields["realms"],@"\[\s*0,\s*900,\s*2100,\s*3600\s*\]"),"asset ID and thresholds");
            foreach(var q in d.qiSources)Check(fields["qiSources"].Contains(q.nodeId),"asset starting override");
            foreach(string kind in new[]{"negative","over-cap","energy","reserve","clock","overflow"}) {
                var bad=V08Definition();if(kind=="negative")bad.initialFoundation=-1;if(kind=="over-cap")bad.initialFoundation=3601;if(kind=="energy")bad.initialEnergy=101;
                if(kind=="reserve")bad.initialReserve=double.NaN;if(kind=="clock")bad.maximumFrameSeconds=2;if(kind=="overflow"){bad.absorptionRate=double.MaxValue;bad.qiSources[0].density=double.MaxValue;}
                CultivationPlaytestProfile ignored;Check(!CultivationPlaytestProfile.TryCreate(bad,id=>true,out ignored,out reason),"reject "+kind);
            }
        });
        Run("v08 full 60 minute runtime scheduler at 30 60 144 FPS and deterministic checkpoint replay",()=>{
            var p=V08Profile();CultivationBalanceMeasurement sixty=null;
            foreach(int hz in new[]{30,60,144}) {
                var h=HeroAt(1);var m=CultivationBalanceSimulation.Run(p,h,"TV-20",hz);
                Console.WriteLine("MEASURE "+JsonUtility.ToJson(m,false));if(hz==60)sixty=m;
            }
            var replay=CultivationBalanceSimulation.Run(p,HeroAt(1),"TV-20",60,true);
            var identical=CultivationBalanceSimulation.Run(p,HeroAt(1),"TV-20",60);
            Check(JsonUtility.ToJson(sixty,false)==JsonUtility.ToJson(identical,false),"Identical initial state and frame sequence produce identical measured output");
            BalanceNear(sixty.foundation,replay.foundation,"checkpoint foundation");BalanceNear(sixty.reserve,replay.reserve,"checkpoint reserve");
            BalanceNear(sixty.activeSeconds,replay.activeSeconds,"checkpoint clock");BalanceNear(sixty.vienManSeconds,replay.vienManSeconds,"deterministic stage timestamp");
            Check(replay.checkpoints>2000,"high frequency real serialize/restore checkpoints");Console.WriteLine("MEASURE "+JsonUtility.ToJson(replay,false));
            foreach(string node in new[]{"TL-16","XD-11","TM-12","HT-17"}) {
                var m=CultivationBalanceSimulation.Run(p,HeroAt(1),node,10);Check(m.vienManSeconds>0,"each actual start");Console.WriteLine("MEASURE "+JsonUtility.ToJson(m,false));
            }
        });
        Run("v08 fractional checkpoint guards pause source settlement and stale identity",()=>{
            var p=V08Profile();var h=HeroAt(1);var b=new CultivationGameplayBinding(h,p,CultivationGameplayState.New(p));
            for(int i=0;i<1000;i++) {
                Check(b.Advance(h,0,.037,false,"TV-20")==CultivationReason.None,"fractional frame");
                var stable=h.cultivationJson;Check(b.Advance(h,1,.4,true,"TV-20")==CultivationReason.Paused && h.cultivationJson==stable,"pause preserves exact checkpoint");
                CultivationSession s;string reason;Check(CultivationGameplayState.TryRestore(stable,p,out s,out reason),"fractional restore "+reason);
                b.Dispose();Check(b.Advance(h,2,.1,false,"TV-20")==CultivationReason.OwnerMismatch,"stale binding");b=new CultivationGameplayBinding(h,p,s);
            }
            BalanceNear(b.ActiveSeconds,37,"1000 high frequency checkpoints keep time");
            Check(b.SettlePending()==CultivationReason.None,"pre-travel settlement");BalanceNear(b.Current.Foundation.Amount,37,"settled foundation");BalanceNear(b.Current.Reserve.Amount,37,"settled reserve");
            var custom=V08Definition();custom.qiSources[1].density=3;CultivationPlaytestProfile travel;string error;
            Check(CultivationPlaytestProfile.TryCreate(custom,id=>true,out travel,out error),"travel profile");var th=HeroAt(1);var tb=new CultivationGameplayBinding(th,travel,CultivationGameplayState.New(travel));
            tb.Advance(th,0,.04,false,"TV-20");tb.Advance(th,1,.06,false,"TL-16");tb.SettlePending();
            BalanceNear(tb.Current.Foundation.Amount,.1,"Travel time");BalanceNear(tb.Current.Reserve.Amount,.34,"old source .08 plus new .36 minus consumed .1");
            string original=h.cultivationJson;
            foreach(string bad in new[]{original.Replace("\"version\":2","\"version\":3"),original.Replace("\"configurationVersion\":1","\"configurationVersion\":2"),original.Replace(p.Id,"other"),Regex.Replace(original,@"""clockRemainder""\s*:\s*[^,}]+","\"clockRemainder\":0.1"),Regex.Replace(original,@"""clockElapsed""\s*:\s*[^,}]+","\"clockElapsed\":1e20"),original.Replace("\"clockNodeId\":\"TV-20\",","")}) {
                CultivationSession rejected;Check(bad!=original && !CultivationGameplayState.TryRestore(bad,p,out rejected,out error),"invalid clock/config candidate");
            }
            CultivationSession old;Check(!CultivationGameplayState.TryRestore(original,V07Profile(),out old,out error),"historical profile rejects balance Save");
            Check(!CultivationGameplayState.TryRestore(original,null,out old,out error),"production context rejects balance Save");
        });
    }
}
