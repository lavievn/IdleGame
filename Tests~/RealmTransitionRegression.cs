using System;
using TuTienCore;

partial class MotionRegression
{
    static void RealmTransitionTests()
    {
        Run("v05 late policy A success preserves total EXP and discards excess internal progress",()=>{
            foreach(int total in new[]{30,31,45,78,998,999}) {
                var e=RealmFixtureEvaluator();var original=new RealmProgressionState("fixture:luyen-khi",1,total,87);
                var session=new RealmBreakthroughTransition(e,original);RealmTransitionReason reason;
                Check(session.Current==original && session.Revision==0,"evaluation alone never breaks through");
                Check(session.TryApplySuccess(Guid.NewGuid(),0,"fixture:truc-co",out reason) && reason==RealmTransitionReason.None,"explicit success");
                var next=session.Current;
                Check(next.RealmId=="fixture:truc-co" && next.TotalLevel==total && next.EntryLevel==total && next.Experience==87,"policy A immutable state");
                Check(original.EntryLevel==1 && original.RealmId=="fixture:luyen-khi","original untouched");
                Check(e.Evaluate(next.RealmId,next.EntryLevel,next.TotalLevel).InternalLevel==1,"no excess carry");
                if(total<=970)Check(e.Evaluate(next.RealmId,next.EntryLevel,total+29).AtBottleneck,"next boundary exactly 29 increments later");
            }
        });
        Run("v05 duplicate success and stale revision cannot advance twice",()=>{
            var session=new RealmBreakthroughTransition(RealmFixtureEvaluator(),new RealmProgressionState("fixture:luyen-khi",1,45,9));
            RealmTransitionReason reason;var attempt=Guid.NewGuid();
            Check(session.TryApplySuccess(attempt,0,"fixture:truc-co",out reason),"first callback");
            var once=session.Current;
            Check(!session.TryApplySuccess(attempt,0,"fixture:last",out reason) && reason==RealmTransitionReason.AlreadyApplied,"duplicate callback");
            Check(!session.TryApplySuccess(attempt,1,"fixture:last",out reason) && reason==RealmTransitionReason.AlreadyApplied,"duplicate token with new revision");
            Check(!session.TryApplySuccess(Guid.NewGuid(),0,"fixture:last",out reason) && reason==RealmTransitionReason.StaleRevision,"stale callback");
            Check(session.Current==once && session.Revision==1 && once.RealmId=="fixture:truc-co","one transition only");
        });
        Run("v05 transition rejects early terminal missing or unknown successors without mutation",()=>{
            var e=RealmFixtureEvaluator();RealmTransitionReason reason;
            var early=new RealmBreakthroughTransition(e,new RealmProgressionState("fixture:luyen-khi",1,29,5));
            var before=early.Current;
            Check(!early.TryApplySuccess(Guid.NewGuid(),0,"fixture:truc-co",out reason) && reason==RealmTransitionReason.ProgressNotReady,"early");
            Check(early.Current==before && early.Revision==0,"early immutable");
            var terminal=new RealmBreakthroughTransition(e,new RealmProgressionState("fixture:last",1,999,5));before=terminal.Current;
            Check(e.Evaluate(before.RealmId,1,999).ProgressEligibleForBreakthrough,"progress is independent of successor policy");
            Check(!terminal.TryApplySuccess(Guid.NewGuid(),0,null,out reason) && reason==RealmTransitionReason.MissingSuccessor,"no successor supplied for final realm");
            Check(!terminal.TryApplySuccess(Guid.NewGuid(),0,"unknown",out reason) && reason==RealmTransitionReason.UnknownSuccessor,"unknown successor");
            Check(!terminal.TryApplySuccess(Guid.NewGuid(),0,"fixture:last",out reason) && reason==RealmTransitionReason.SameRealm,"same realm");
            Check(!terminal.TryApplySuccess(Guid.Empty,0,"fixture:luyen-khi",out reason) && reason==RealmTransitionReason.InvalidAttempt,"missing identity");
            Check(terminal.Current==before && terminal.Revision==0,"rejects do not change state");
        });
    }
}
