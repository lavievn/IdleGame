using System;
using TuTienCore;

partial class MotionRegression
{
    // These IDs are fixture-only, not production realm content or successor policy.
    static RealmProgressionEvaluator RealmFixtureEvaluator()
    { return new RealmProgressionEvaluator(new[]{"fixture:luyen-khi","fixture:truc-co","fixture:last"}); }
    static void RealmProgressionTests()
    {
        Run("v05 realm boundaries preserve raw internal level and never advance realm",()=>{
            var e=RealmFixtureEvaluator();
            int[] total={1,9,10,19,20,29,30,45,45,46,74};
            int[] entry={1,1,1,1,1,1,1,1,45,45,45};
            int[] expected={1,9,10,19,20,29,30,45,1,2,30};
            var stages=new[]{RealmMinorStage.SoKy,RealmMinorStage.SoKy,RealmMinorStage.TrungKy,RealmMinorStage.TrungKy,
                RealmMinorStage.HauKy,RealmMinorStage.HauKy,RealmMinorStage.VienMan,RealmMinorStage.VienMan,
                RealmMinorStage.SoKy,RealmMinorStage.SoKy,RealmMinorStage.VienMan};
            for(int i=0;i<total.Length;i++) {
                var result=e.Evaluate("fixture:luyen-khi",entry[i],total[i]);
                Check(result.IsValid && result.RealmId=="fixture:luyen-khi","realm identity "+i);
                Check(result.InternalLevel==expected[i] && result.MinorStage==stages[i],"boundary "+i);
                Check(result.AtBottleneck==(expected[i]>=30) && result.ProgressEligibleForBreakthrough==result.AtBottleneck,"progress eligibility only "+i);
            }
        });
        Run("v05 realm invalid and extreme levels fail before arithmetic",()=>{
            var e=RealmFixtureEvaluator();
            Check(e.Evaluate("unknown",1,45).Reason==RealmProgressionReason.UnknownRealm,"unknown catalog ID");
            foreach(int entry in new[]{int.MinValue,-1,0,1000,int.MaxValue})
                Check(e.Evaluate("fixture:luyen-khi",entry,45).Reason==RealmProgressionReason.InvalidEntryLevel,"entry "+entry);
            foreach(int total in new[]{int.MinValue,-1,0,1000,int.MaxValue}) {
                var result=e.Evaluate("fixture:luyen-khi",1,total);
                Check(result.Reason==RealmProgressionReason.InvalidTotalLevel && result.InternalLevel==0 && !result.AtBottleneck,"total "+total);
            }
            Check(e.Evaluate("fixture:luyen-khi",46,45).Reason==RealmProgressionReason.TotalBeforeEntry,"total before entry");
            Check(e.Evaluate("fixture:luyen-khi",1,999).InternalLevel==999,"cap raw internal");
            Check(e.Evaluate("fixture:last",999,999).InternalLevel==1,"cap entry starts one");
        });
        Run("v05 realm catalog is copied and exact with invalid catalogs rejected",()=>{
            var ids=new[]{"fixture:luyen-khi"};var e=new RealmProgressionEvaluator(ids);ids[0]="mutated";
            Check(e.Evaluate("fixture:luyen-khi",1,1).IsValid && !e.Evaluate("mutated",1,1).IsValid,"owned catalog copy");
            Check(!e.Evaluate("Fixture:luyen-khi",1,1).IsValid && !e.Evaluate(null,1,1).IsValid,"exact ID");
            foreach(var bad in new[]{new string[0],new[]{"x","x"},new[]{" x"},new string[]{null}}) {
                bool rejected=false;try {new RealmProgressionEvaluator(bad);}catch(ArgumentException){rejected=true;}
                Check(rejected,"invalid catalog rejected");
            }
        });
    }
}
