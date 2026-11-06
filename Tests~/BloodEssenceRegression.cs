using System;
using System.Collections.Generic;
using TuTienCore;

partial class MotionRegression
{
    static Guid ActivateFixtureBlood(CultivationSession c)
    {double hp;var id=Guid.NewGuid();Check(c.ActivateBlood(c.OwnerId,c.Epoch,id,20,100,out hp)==CultivationReason.None && hp==100,"fixture first burn/heal");return id;}
    static void BloodEssenceTests()
    {
        Run("revised blood first activation requires owner charge alive HP strictly below30percent",()=>{
            var c=CultivationFixture(1000,charges:3);double hp;var before=c.Current;
            foreach(double current in new[]{30d,31,100,0,-1,double.NaN})Check(c.ActivateBlood(c.OwnerId,c.Epoch,Guid.NewGuid(),current,100,out hp)==CultivationReason.HpCondition && c.Current==before,"first HP condition");
            Check(c.ActivateBlood(Guid.NewGuid(),c.Epoch,Guid.NewGuid(),20,100,out hp)==CultivationReason.OwnerMismatch,"owner condition");
            var empty=CultivationFixture(1000);Check(empty.ActivateBlood(empty.OwnerId,empty.Epoch,Guid.NewGuid(),20,100,out hp)==CultivationReason.NoCharge,"HP alone insufficient");
            var burn=ActivateFixtureBlood(c);Check(c.Current.Blood.Charges==2 && c.Current.Burning.Id==burn && c.Current.Burning.RemainingSeconds==30,"charge consumed/duration");
            Check(c.ActivateBlood(c.OwnerId,c.Epoch,Guid.NewGuid(),20,100,out hp)==CultivationReason.InvalidSession && c.Current.Blood.Charges==2,"cannot heal using second first-activation");
        });
        Run("revised blood extensions add time and linear locked debt without extra healing or realm stacking",()=>{
            var c=CultivationFixture(1000,charges:3,rules:CultivationFixtureRules(refine:0));var id=ActivateFixtureBlood(c);
            c.Tick(c.OwnerId,c.Epoch,0,5,false,0);Check(c.Current.Burning.RemainingSeconds==25,"time consumed");
            var command=Guid.NewGuid();Check(c.ExtendBlood(c.OwnerId,c.Epoch,command,id)==CultivationReason.None && c.Current.Burning.RemainingSeconds==55,"adds30 not reset");
            Check(c.ExtendBlood(c.OwnerId,c.Epoch,command,id)==CultivationReason.DuplicateCommand,"duplicate extension no charge");
            Check(c.ExtendBlood(c.OwnerId,c.Epoch,Guid.NewGuid(),id)==CultivationReason.None && c.Current.Burning.RemainingSeconds==85,"third charge adds30");
            Check(c.Current.Burning.ReferenceFoundation==1000 && c.Current.Burning.TotalDebt==300 && c.Current.Foundation.Amount==1000,"three100 costs deferred, no compounding");
            Check(c.Current.Foundation.ActualRealmId=="fixture:kim-dan" && c.RealmTierFor(RealmUsePurpose.CombatAbility,true)==3 && c.RealmTierFor(RealmUsePurpose.CombatAbility,false)==2,"one forced combat realm only");
            foreach(RealmUsePurpose purpose in new[]{RealmUsePurpose.Breakthrough,RealmUsePurpose.PermanentLearning,RealmUsePurpose.Sect,RealmUsePurpose.MapEntry,RealmUsePurpose.ProgressionReward})Check(c.RealmTierFor(purpose,true)==2,"actual realm for "+purpose);
            Check(c.ExtendBlood(c.OwnerId,c.Epoch,Guid.NewGuid(),id)==CultivationReason.NoCharge,"cannot extend without charge");
        });
        Run("revised blood modifiers use one owned source and only approved absolute stats",()=>{
            var c=CultivationFixture(1000,charges:1);var id=ActivateFixtureBlood(c);var hero=HeroAt(1);
            var mods=new List<CombatStatModifier>{new CombatStatModifier {sourceId="equipment:fixture",stat=CombatStatId.Attack,flat=4}};
            var values=new Dictionary<CombatStatId,float>{{CombatStatId.Attack,14},{CombatStatId.MagicalAttack,0}};
            Check(BloodEssenceModifiers.Reconcile(c,c.OwnerId,c.Epoch,mods,values)==CultivationReason.None,"first owned modifiers");
            Check(BloodEssenceModifiers.Reconcile(c,c.OwnerId,c.Epoch,mods,values)==CultivationReason.None && mods.Count==3,"no duplicate modifier/source on refresh");
            Check(CombatStats.GetWithModifiers(hero,mods,CombatStatId.Attack,10)==28,"absolute source14 doubles including gear, no formula change");
            foreach(var forbidden in new[]{CombatStatId.MaxHealth,CombatStatId.AttackSpeed,CombatStatId.MovementSpeed,CombatStatId.PhysicalDamagePercent,CombatStatId.CooldownReductionPercent})
                Check(BloodEssenceModifiers.Reconcile(c,c.OwnerId,c.Epoch,mods,new Dictionary<CombatStatId,float>{{forbidden,1}})==CultivationReason.InvalidValue && mods.Count==3,"forbidden stat rejected atomically");
            c.Tick(c.OwnerId,c.Epoch,0,30,false,0);Check(c.Current.Burning.Status==BurningStatus.SettlementPending,"expiry retains liability");
            Check(BloodEssenceModifiers.Reconcile(c,c.OwnerId,c.Epoch,mods,values)==CultivationReason.None && mods.Count==1 && mods[0].sourceId=="equipment:fixture","expiry removes only own burn prefix");
            Check(c.SettleBlood(c.OwnerId,c.Epoch,id)==CultivationReason.None,"settlement");
        });
        Run("revised blood locked reference survives growth and settlement is exactly once",()=>{
            var c=CultivationFixture(1000,charges:3,catalog:CultivationCatalog(2000));var id=ActivateFixtureBlood(c);
            c.Tick(c.OwnerId,c.Epoch,0,1,false,10);Check(c.Current.Foundation.Amount==1010,"foundation grew while active");
            c.ExtendBlood(c.OwnerId,c.Epoch,Guid.NewGuid(),id);c.ExtendBlood(c.OwnerId,c.Epoch,Guid.NewGuid(),id);
            Check(c.Current.Burning.TotalDebt==300 && c.Current.Burning.ReferenceFoundation==1000,"debt never repriced to1010");
            Check(c.EndBlood(c.OwnerId,c.Epoch,id,BurningEndReason.Menu)==CultivationReason.None,"explicit terminal reason");
            Check(c.SettleBlood(c.OwnerId,c.Epoch,id)==CultivationReason.None && c.Current.Foundation.Amount==710 && c.Current.Injury.Severity==0 && c.Current.Burning==null,"subtract once and clear after settlement");
            var settled=c.Current;Check(c.SettleBlood(c.OwnerId,c.Epoch,id)==CultivationReason.AlreadySettled && c.Current==settled,"duplicate terminal cannot charge twice");
        });
        Run("revised blood deficit becomes injury only and excess charges survive minor-stage regression",()=>{
            var hero=HeroAt(45);int level=hero.currentLevel;
            var c=CultivationFixture(1000,charges:3,rules:CultivationFixtureRules(debt:.5));var id=ActivateFixtureBlood(c);
            c.ExtendBlood(c.OwnerId,c.Epoch,Guid.NewGuid(),id);c.ExtendBlood(c.OwnerId,c.Epoch,Guid.NewGuid(),id);
            c.EndBlood(c.OwnerId,c.Epoch,id,BurningEndReason.Death);c.SettleBlood(c.OwnerId,c.Epoch,id);
            Check(c.Current.Foundation.Amount==0 && c.Current.Injury.Severity==500 && hero.currentLevel==level,"only unpayable500 injury, no negative foundation/Level loss");
            Check(c.Realms.Evaluate(c.Current.Foundation.ActualRealmId,0).MinorStage==RealmMinorStage.SoKy && c.Current.Foundation.ActualRealmId=="fixture:kim-dan","minor only regression");
            var excess=CultivationFixture(1000,charges:4,rules:CultivationFixtureRules(debt:.8));id=ActivateFixtureBlood(excess);
            excess.EndBlood(excess.OwnerId,excess.Epoch,id,BurningEndReason.ExplicitCancel);excess.SettleBlood(excess.OwnerId,excess.Epoch,id);
            Check(excess.Current.Foundation.Amount==200 && excess.Current.Blood.Charges==3 && excess.Realms.Evaluate("fixture:kim-dan",200).BloodCapacity==1,"hold excess charges instead of deletion");
            excess.Tick(excess.OwnerId,excess.Epoch,0,1,false,0);Check(excess.Current.Blood.Charges==3,"no extra accumulation while above reduced cap");
        });
        Run("revised blood all lifecycle ends retain debt until explicit identity-checked settlement",()=>{
            foreach(BurningEndReason why in Enum.GetValues(typeof(BurningEndReason))) {
                var c=CultivationFixture(1000,charges:1,rules:CultivationFixtureRules(refine:0));var id=ActivateFixtureBlood(c);
                if(why==BurningEndReason.Expired)c.Tick(c.OwnerId,c.Epoch,0,30,false,0);else Check(c.EndBlood(c.OwnerId,c.Epoch,id,why)==CultivationReason.None,"end "+why);
                Check(c.Current.Burning!=null && c.Current.Burning.EndReason==why && c.Current.Foundation.Amount==1000,"cancel cannot erase debt "+why);
                Check(c.ExtendBlood(c.OwnerId,c.Epoch,Guid.NewGuid(),id)==CultivationReason.SettlementRequired,"pending blocks use");
                Check(c.SettleBlood(c.OwnerId,Guid.NewGuid(),id)==CultivationReason.OwnerMismatch,"stale owner epoch");
                Check(c.SettleBlood(c.OwnerId,c.Epoch,Guid.NewGuid())==CultivationReason.InvalidSession,"wrong transaction");
                Check(c.SettleBlood(c.OwnerId,c.Epoch,id)==CultivationReason.None && c.Current.Foundation.Amount==900,"settle "+why);
            }
        });
        Run("revised recovery distinguishes regain foundation heal injury and restore spiritual energy",()=>{
            var c=CultivationFixture(400,100,injury:20);c.Tick(c.OwnerId,c.Epoch,0,1,false,0);
            Check(c.Current.Foundation.Amount>400 && c.Current.Injury.Severity==20,"Qi never heals injury");double foundation=c.Current.Foundation.Amount;
            var passive=new InjuryTreatmentSession(Guid.NewGuid(),c.OwnerId,c.Epoch,InjuryTreatmentSource.PassiveNurturing,.1,1,10);
            Check(passive.Tick(c,0,2,false)==CultivationReason.None,"slow nurturing");Near((float)c.Current.Injury.Severity,19.8f,"separate passive healing");
            var treatmentId=Guid.NewGuid();var treatment=new InjuryTreatmentSession(treatmentId,c.OwnerId,c.Epoch,InjuryTreatmentSource.ExplicitExternalTreatment,10,5,10);
            Check(treatment.Tick(c,0,1,false)==CultivationReason.None && treatment.State==InjuryTreatmentState.Completed,"explicit fixture treatment budget");
            Near((float)c.Current.Injury.Severity,14.8f,"external treatment heals five");
            var duplicate=new InjuryTreatmentSession(treatmentId,c.OwnerId,c.Epoch,InjuryTreatmentSource.ExplicitExternalTreatment,10,5,10);
            Check(duplicate.Tick(c,0,1,false)==CultivationReason.DuplicateCommand,"same treatment/step cannot heal twice");
            Check(c.Current.Foundation.Amount==foundation,"healing does not restore cultivation");
            double injury=c.Current.Injury.Severity;c.RestoreSpiritualEnergy(c.OwnerId,c.Epoch,Guid.NewGuid(),100,"fixture:energy");Check(c.Current.Injury.Severity==injury && c.Current.Foundation.Amount==foundation,"energy restore separate");
            Check(c.CanAttemptBreakthrough()==CultivationReason.InjuryBlocked,"actual injury blocks trial independently of energy");
            for(int i=0;i<5;i++)Check(c.ClampSpiritualEnergy(c.OwnerId,c.Epoch)==CultivationReason.None,"refresh clamp");
            Near((float)c.Current.Energy.Current,97.04f,"injury maximum applied once across refreshes");
            Near((float)c.Rules.RefinementFactor(injury),.9852f,"single configured refinement penalty");
        });
    }
}
