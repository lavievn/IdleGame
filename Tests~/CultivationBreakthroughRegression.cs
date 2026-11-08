using System;
using TuTienCore;

partial class MotionRegression
{
    static void CultivationBreakthroughTests()
    {
        Run("revised reserve can fund three successive explicit breakthroughs without EXP or automatic success",()=>{
            var hero=HeroAt(45);int level=hero.currentLevel,exp=hero.currentExp;
            var c=CultivationFixture(reserve:1000,realm:"fixture:luyen-khi",rules:CultivationFixtureRules(absorb:0,refine:100),catalog:CultivationCatalog(100));
            var transition=new RealmBreakthroughTransition(c);c.Tick(c.OwnerId,c.Epoch,0,1,false,0);
            Check(c.Current.Foundation.ActualRealmId=="fixture:luyen-khi" && c.Current.Foundation.Amount==100 && c.Current.Reserve.Amount==900,"reserve reaches first cap, no auto realm");
            Guid first=Guid.Empty;long step=1;
            foreach(string successor in new[]{"fixture:kim-dan","fixture:nguyen-anh","fixture:next"}) {
                string actual=c.Current.Foundation.ActualRealmId;long revision=c.RealmRevision;var success=Guid.NewGuid();if(first==Guid.Empty)first=success;
                Check(transition.ApplySuccess(c.OwnerId,c.Epoch,success,revision,actual,successor,0)==CultivationReason.None,"explicit success only");
                Check(c.Current.Foundation.ActualRealmId==successor && c.Current.Foundation.Amount==0,"explicit post-success foundation transfer fixture");
                Check(c.Tick(c.OwnerId,c.Epoch,step++,1,false,0)==CultivationReason.None && c.Current.Foundation.Amount==100,"reserve can fund next bottleneck without world Qi");
            }
            Check(c.RealmRevision==3 && c.Current.Reserve.Amount==600 && hero.currentLevel==level && hero.currentExp==exp,"three distinct events, Level/EXP isolated");
            var before=c.Current;
            Check(transition.ApplySuccess(c.OwnerId,c.Epoch,first,0,"fixture:luyen-khi","fixture:kim-dan",0)==CultivationReason.DuplicateCommand && c.Current==before,"duplicate success never transitions twice");
            Check(transition.ApplySuccess(c.OwnerId,c.Epoch,Guid.NewGuid(),0,"fixture:luyen-khi","fixture:kim-dan",0)==CultivationReason.StaleAttempt && c.Current==before,"stale success rejected");
        });
        Run("revised breakthrough uses actual foundation and injury not effective blood realm",()=>{
            var c=CultivationFixture(1000,charges:1);var transition=new RealmBreakthroughTransition(c);var id=ActivateFixtureBlood(c);
            Check(c.RealmTierFor(RealmUsePurpose.CombatAbility,true)==3 && c.RealmTierFor(RealmUsePurpose.Breakthrough,true)==2,"forced combat scope only");
            Check(transition.ApplySuccess(c.OwnerId,c.Epoch,Guid.NewGuid(),0,"fixture:kim-dan","fixture:nguyen-anh",0)==CultivationReason.SettlementRequired,"blood cannot bypass trial eligibility");
            c.EndBlood(c.OwnerId,c.Epoch,id,BurningEndReason.ExplicitCancel);c.SettleBlood(c.OwnerId,c.Epoch,id);
            Check(c.Current.Foundation.Amount==900 && transition.ApplySuccess(c.OwnerId,c.Epoch,Guid.NewGuid(),0,"fixture:kim-dan","fixture:nguyen-anh",0)==CultivationReason.ProgressNotReady,"paid foundation now below cap");
        });
        Run("revised breakthrough rejects missing unknown downward successors and invalid transfer atomically",()=>{
            var c=CultivationFixture(1000);var transition=new RealmBreakthroughTransition(c);var before=c.Current;
            foreach(string target in new[]{null,"unknown","fixture:kim-dan","fixture:luyen-khi"})
                Check(transition.ApplySuccess(c.OwnerId,c.Epoch,Guid.NewGuid(),0,"fixture:kim-dan",target,0)==CultivationReason.NoSuccessor && c.Current==before,"explicit ascending successor required");
            Check(transition.ApplySuccess(c.OwnerId,c.Epoch,Guid.NewGuid(),0,"fixture:kim-dan","fixture:nguyen-anh",-1)==CultivationReason.InvalidValue && c.Current==before,"invalid transfer rejects before mutation");
            Check(transition.ApplySuccess(c.OwnerId,Guid.NewGuid(),Guid.NewGuid(),0,"fixture:kim-dan","fixture:nguyen-anh",0)==CultivationReason.OwnerMismatch,"stale epoch");
        });
    }
}
