using System;
using TuTienCore;

partial class MotionRegression
{
    static CultivationRealmEvaluator CultivationCatalog(double cap=1000)
    {
        var thresholds=new[]{0d,cap*.3,cap*.6,cap};CultivationRealmEvaluator catalog;CultivationReason reason;
        Check(CultivationRealmEvaluator.TryCreate(new[]{
            new CultivationRealmDefinition("fixture:luyen-khi",0,thresholds),
            new CultivationRealmDefinition("fixture:kim-dan",2,thresholds,true,new[]{1,2,3,4},true),
            new CultivationRealmDefinition("fixture:nguyen-anh",3,thresholds,true,new[]{4,5,6,7}),
            new CultivationRealmDefinition("fixture:next",4,thresholds,true,new[]{7,8,9,10})},out catalog,out reason),"fixture-only catalog "+reason);
        return catalog;
    }
    static CultivationRules CultivationFixtureRules(double absorb=1,double refine=10,double efficiency=1,double bloodRate=0,double debt=.1)
    {return new CultivationRules(60,absorb,1,refine,efficiency,100,1,1,bloodRate,debt,2,.001,.002,10,BloodCapacityOverflowPolicy.HoldExistingStopAccumulation);}
    static CultivationSession CultivationFixture(double foundation=0,double reserve=0,int charges=0,double injury=0,string realm="fixture:kim-dan",CultivationRules rules=null,CultivationRealmEvaluator catalog=null)
    {return new CultivationSession(Guid.NewGuid(),Guid.NewGuid(),catalog??CultivationCatalog(),rules??CultivationFixtureRules(),
        new CultivationSnapshot(new CultivationFoundation(realm,foundation),new UnrefinedQiReserve(reserve),new SpiritualEnergy(0),new BloodEssenceState(charges,0),new FoundationInjuryState(injury)));}
    static void CultivationCoreTests()
    {
        Run("revised cultivation Level EXP and foundation have independent authorities",()=>{
            var hero=HeroAt(1);var c=CultivationFixture(realm:"fixture:luyen-khi");var before=c.Current;
            hero.AddExp(hero.expToNextLevel);Check(hero.currentLevel==2 && c.Current==before,"EXP cannot grant cultivation");
            int level=hero.currentLevel,exp=hero.currentExp,points=hero.statPoints,growth=hero.meleeGrowthBonus;
            Check(c.Tick(c.OwnerId,c.Epoch,0,1,false,10)==CultivationReason.None && c.Current.Foundation.Amount==10,"world Qi refines");
            Check(hero.currentLevel==level && hero.currentExp==exp && hero.statPoints==points && hero.meleeGrowthBonus==growth,"Qi cannot grant EXP/growth");
        });
        Run("revised cultivation thresholds validated and stages can regress without changing actual realm",()=>{
            var catalog=CultivationCatalog();double[] amounts={0,299,300,599,600,999,1000,600,300,0};
            var expected=new[]{RealmMinorStage.SoKy,RealmMinorStage.SoKy,RealmMinorStage.TrungKy,RealmMinorStage.TrungKy,RealmMinorStage.HauKy,RealmMinorStage.HauKy,RealmMinorStage.VienMan,RealmMinorStage.HauKy,RealmMinorStage.TrungKy,RealmMinorStage.SoKy};
            for(int i=0;i<amounts.Length;i++) {var result=catalog.Evaluate("fixture:kim-dan",amounts[i]);Check(result.IsValid && result.MinorStage==expected[i] && result.ActualRealmId=="fixture:kim-dan","foundation boundary/regression "+i);}
            foreach(var thresholds in new[]{new double[0],new[]{0d,1,2},new[]{0d,1,1,3},new[]{0d,3,2,4},new[]{1d,2,3,4},new[]{0d,1,2,double.NaN}}) {
                CultivationRealmEvaluator invalid;CultivationReason reason;
                Check(!CultivationRealmEvaluator.TryCreate(new[]{new CultivationRealmDefinition("fixture:bad",0,thresholds)},out invalid,out reason) && invalid==null,"bad threshold catalog rejected atomically");
            }
            Check(!catalog.Evaluate("unknown",0).IsValid && !catalog.Evaluate("fixture:kim-dan",-1).IsValid,"invalid realm/foundation");
        });
        Run("revised Qi absorption refinement and bottleneck conserve reserve without kill dependency",()=>{
            var c=CultivationFixture(rules:CultivationFixtureRules(refine:5,efficiency:.8));
            Check(c.Tick(c.OwnerId,c.Epoch,0,1,false,10)==CultivationReason.None,"active game-time tick");
            Near((float)c.Current.Foundation.Amount,4,"five input at80percent output four");Near((float)c.Current.Reserve.Amount,5,"remaining unrefined input");
            var capped=CultivationFixture(1000,50,realm:"fixture:luyen-khi");
            Check(capped.Tick(capped.OwnerId,capped.Epoch,0,1,false,10)==CultivationReason.None && capped.Current.Foundation.Amount==1000 && capped.Current.Reserve.Amount==60,"bottleneck stores newly absorbed Qi");
            Check(capped.Tick(capped.OwnerId,capped.Epoch,1,1,false,0)==CultivationReason.None && capped.Current.Reserve.Amount==60,"no reserve consumption at cap");
        });
        Run("revised Qi clock rejects invalid delta duplicate steps stale owners and overflow without mutation",()=>{
            var c=CultivationFixture();var before=c.Current;
            foreach(double dt in new[]{-1d,double.NaN,double.PositiveInfinity,double.NegativeInfinity,61,double.MaxValue})
                Check(c.Tick(c.OwnerId,c.Epoch,0,dt,false,1)==CultivationReason.InvalidDelta && c.Current==before,"invalid dt");
            Check(c.Tick(c.OwnerId,c.Epoch,0,1,true,100)==CultivationReason.Paused && c.Current==before,"pause no grant");
            Check(c.Tick(Guid.NewGuid(),c.Epoch,0,1,false,1)==CultivationReason.OwnerMismatch,"wrong owner");
            Check(c.Tick(c.OwnerId,c.Epoch,0,1,false,1)==CultivationReason.None,"single clock step");before=c.Current;
            Check(c.Tick(c.OwnerId,c.Epoch,0,1,false,1)==CultivationReason.DuplicateCommand && c.Current==before,"no duplicate scheduler payout");
            var huge=CultivationFixture(reserve:double.MaxValue);before=huge.Current;
            Check(huge.Tick(huge.OwnerId,huge.Epoch,0,1,false,double.MaxValue)==CultivationReason.Overflow && huge.Current==before,"overflow atomic rejection");
        });
        Run("revised spiritual Spend Restore Clamp are separate from cultivation and injury",()=>{
            var c=CultivationFixture(400,100,injury:20);var foundation=c.Current.Foundation;
            Check(c.RestoreSpiritualEnergy(c.OwnerId,c.Epoch,Guid.NewGuid(),1000,"fixture:recovery")==CultivationReason.None,"restore clamps maximum");
            Check(c.Current.Energy.Current==96 && c.Current.Foundation==foundation,"injury-derived max96 without foundation mutation");
            Check(c.SpendSpiritualEnergy(c.OwnerId,c.Epoch,Guid.NewGuid(),96)==CultivationReason.None && c.Current.Energy.Current==0,"energy depletion");
            Check(c.Current.Foundation.ActualRealmId=="fixture:kim-dan" && c.Current.Foundation.Amount==400 && c.Current.Injury.Severity==20,"energy is not foundation or healing");
            var before=c.Current;Check(c.SpendSpiritualEnergy(c.OwnerId,c.Epoch,Guid.NewGuid(),1)==CultivationReason.InvalidValue && c.Current==before,"overspend rejected");
            c.Tick(c.OwnerId,c.Epoch,0,1,false,0);Check(c.Current.Foundation.Amount>400 && c.Current.Injury.Severity==20,"refinement regains foundation but never injury");
        });
        Run("revised blood passive accumulation unlock and capacity are explicit and preserve full progress",()=>{
            var locked=CultivationFixture(realm:"fixture:luyen-khi",rules:CultivationFixtureRules(bloodRate:1));
            locked.Tick(locked.OwnerId,locked.Epoch,0,10,false,0);Check(locked.Current.Blood.Charges==0,"no blood before approved catalog unlock");
            var c=CultivationFixture(rules:CultivationFixtureRules(refine:0,bloodRate:.1));
            c.Tick(c.OwnerId,c.Epoch,0,10,false,0);Check(c.Current.Blood.Charges==1,"KimDan So capacity1");
            var blood=c.Current.Blood;c.Tick(c.OwnerId,c.Epoch,1,60,false,0);Check(c.Current.Blood==blood,"no accumulation beyond capacity");
            Check(c.AccelerateBlood(c.OwnerId,c.Epoch,Guid.NewGuid(),100)==CultivationReason.AtCapacity,"acceleration cannot exceed capacity");
            var partial=CultivationFixture(rules:CultivationFixtureRules(refine:0,bloodRate:.1));
            partial.Tick(partial.OwnerId,partial.Epoch,0,5,false,0);
            Check(partial.Current.Blood.Charges==0 && partial.Current.Blood.AccumulationProgress==.5,"half charge banked");
            var unchanged=partial.Current;
            Check(partial.AccelerateBlood(partial.OwnerId,partial.Epoch,Guid.NewGuid(),.6)==CultivationReason.AtCapacity && partial.Current==unchanged,"external acceleration exceeding free capacity rejected atomically");
            Check(partial.AccelerateBlood(partial.OwnerId,partial.Epoch,Guid.NewGuid(),.5)==CultivationReason.None && partial.Current.Blood.Charges==1 && partial.Current.Blood.AccumulationProgress==0,"fraction consumed exactly once when filling capacity");
            var higher=CultivationFixture(600);Check(higher.AccelerateBlood(higher.OwnerId,higher.Epoch,Guid.NewGuid(),2.5)==CultivationReason.None && higher.Current.Blood.Charges==2 && higher.Current.Blood.AccumulationProgress==.5,"external fixture acceleration, no production item");
        });
    }
}
