using System;
using TuTienCore;

partial class MotionRegression
{
    // Compiled only by baseline.sh against the unmodified PR #19 sources.
    static void ExpBaselineProbe()
    {
        Run("PRE-FIX eligible actual bottleneck must retain Level",()=>{
            var p=HeavenlyGateV13Fixtures.Profile();var s=HeavenlyGateV13Fixtures.Session(p);
            var gate=HeavenlyGateV13Fixtures.Configuration();var h=HeroAt(1);
            using(var binding=new CultivationGameplayBinding(h,p,s))
            {
                Check(gate.IsValid && gate.cultivationConfiguration==p.Id &&
                    p.ExplicitSuccessor(s.Current.Foundation.ActualRealmId)==gate.targetRealm &&
                    p.Catalog.Evaluate(s.Current.Foundation.ActualRealmId,s.Current.Foundation.Amount).AtBottleneck,"actual eligible path");
                h.AddExp(h.expToNextLevel);
                Check(h.currentLevel==1,"Level advanced to "+h.currentLevel+"; AddExp has no cultivation/bank integration");
            }
        });
        Run("PRE-FIX level 999 must conserve newly earned EXP",()=>{
            var h=HeroAt(999);h.currentExp=h.expToNextLevel-1;int before=h.currentExp;h.AddExp(100);
            Check(h.currentExp-before==100,"100 EXP silently clamped; no reserve field");
        });
    }
}
