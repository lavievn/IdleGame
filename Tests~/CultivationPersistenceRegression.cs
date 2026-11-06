using System;
using TuTienCore;

partial class MotionRegression
{
    const string CultivationFixtureConfiguration="fixture:cultivation-v1";
    static void CultivationPersistenceTests()
    {
        Run("revised DTO candidate load retains active debt locks recovery and rejects stale epoch callbacks",()=>{
            var c=CultivationFixture(1000,50,3,rules:CultivationFixtureRules(refine:0));var id=ActivateFixtureBlood(c);
            c.ExtendBlood(c.OwnerId,c.Epoch,Guid.NewGuid(),id);c.Tick(c.OwnerId,c.Epoch,0,5,false,0);
            var source=c.Current;var dto=CultivationComponentCodec.Capture(c,CultivationFixtureConfiguration);var slot=new CultivationCandidateSlot(c);var epoch=Guid.NewGuid();
            Check(slot.TryLoad(dto,c.Realms,c.Rules,CultivationFixtureConfiguration,c.OwnerId,epoch)==CultivationReason.None,"valid candidate");
            var loaded=slot.Current;Check(loaded!=c && c.Current==source,"candidate load did not mutate prior owner state");
            Check(loaded.Current.Burning.Status==BurningStatus.SettlementPending && loaded.Current.Burning.EndReason==BurningEndReason.Load && loaded.Current.Burning.TotalDebt==200 && loaded.Current.Burning.ReferenceFoundation==1000,"active debt preserved, no restart benefit");
            Check(loaded.Current.Foundation.Amount==1000 && loaded.Current.Reserve.Amount==50 && loaded.Current.Blood.Charges==1 && loaded.Current.Burning.RemainingSeconds==55,"Load grants no Qi/charge/progression or offline time");
            Check(loaded.SettleBlood(c.OwnerId,c.Epoch,id)==CultivationReason.OwnerMismatch,"old callback epoch cannot reach loaded Hero");
            Check(loaded.SettleBlood(c.OwnerId,epoch,id)==CultivationReason.None && loaded.Current.Foundation.Amount==800,"explicit recovery settlement");
            var paid=CultivationComponentCodec.Capture(loaded,CultivationFixtureConfiguration);Check(slot.TryLoad(paid,c.Realms,c.Rules,CultivationFixtureConfiguration,c.OwnerId,Guid.NewGuid())==CultivationReason.None,"paid snapshot reload");
            Check(slot.Current.SettleBlood(c.OwnerId,slot.Current.Epoch,id)==CultivationReason.AlreadySettled && slot.Current.Current.Foundation.Amount==800,"receipt prevents paying twice after reconstruction");
        });
        Run("revised DTO rejects future missing unknown invalid and hidden liability before swapping candidate",()=>{
            var c=CultivationFixture(1000,20,2);var burn=ActivateFixtureBlood(c);var slot=new CultivationCandidateSlot(c);
            foreach(string field in new[]{"future","legacy","realm","foundation","reserve","energy","injury","charges","progress","burn-owner","burn-realm","hidden-debt","duplicate-receipt","active-receipt","bad-config"}) {
                var dto=CultivationComponentCodec.Capture(c,CultivationFixtureConfiguration);
                if(field=="future")dto.version=2;if(field=="legacy")dto.version=0;if(field=="realm")dto.actualRealmId="unknown";
                if(field=="foundation")dto.foundation=-1;if(field=="reserve")dto.unrefinedQi=double.NaN;if(field=="energy")dto.spiritualEnergy=double.PositiveInfinity;
                if(field=="injury")dto.injury=-1;if(field=="charges")dto.bloodCharges=-1;if(field=="progress")dto.bloodProgress=1;
                if(field=="burn-owner")dto.burnOwnerId=Guid.NewGuid().ToString("N");if(field=="burn-realm")dto.burnActualRealmId="fixture:nguyen-anh";
                if(field=="hidden-debt")dto.hasBurn=false;if(field=="duplicate-receipt") {var id=Guid.NewGuid().ToString("N");dto.settledTransactions.Add(id);dto.settledTransactions.Add(id);}
                if(field=="active-receipt")dto.settledTransactions.Add(burn.ToString("N"));if(field=="bad-config")dto.configurationId="unknown";
                Check(slot.TryLoad(dto,c.Realms,c.Rules,CultivationFixtureConfiguration,c.OwnerId,Guid.NewGuid())!=CultivationReason.None && slot.Current==c,"failed candidate preserved current: "+field);
            }
            Check(slot.TryLoad(null,c.Realms,c.Rules,CultivationFixtureConfiguration,c.OwnerId,Guid.NewGuid())==CultivationReason.LegacyMigrationRequired,"no free cultivation/charges from absent component or EXP");
        });
        Run("revised crash recovery retains cost and manual snapshot rollback remains timeline-local",()=>{
            var c=CultivationFixture(1000,charges:1);var beforeBurn=CultivationComponentCodec.Capture(c,CultivationFixtureConfiguration);var id=ActivateFixtureBlood(c);
            var active=CultivationComponentCodec.Capture(c,CultivationFixtureConfiguration);var slot=new CultivationCandidateSlot(c);
            Check(slot.TryLoad(active,c.Realms,c.Rules,CultivationFixtureConfiguration,c.OwnerId,Guid.NewGuid(),BurningEndReason.CrashRecovery)==CultivationReason.None,"crash reconstructed candidate");
            Check(slot.Current.Current.Burning.EndReason==BurningEndReason.CrashRecovery && slot.Current.Current.Burning.TotalDebt==100,"crash cannot erase liability");
            slot.Current.SettleBlood(c.OwnerId,slot.Current.Epoch,id);
            Check(slot.TryLoad(beforeBurn,c.Realms,c.Rules,CultivationFixtureConfiguration,c.OwnerId,Guid.NewGuid())==CultivationReason.None,"manual old snapshot rollback explicitly allowed in fixture");
            Check(slot.Current.Current.Foundation.Amount==1000 && slot.Current.Current.Blood.Charges==1 && slot.Current.Current.Burning==null,"whole old timeline restored, no external ledger claim");
        });
    }
}
