using System;
using System.Collections.Generic;

namespace TuTienCore
{
    public sealed partial class CultivationSession
    {
        public readonly Guid OwnerId,Epoch;public readonly CultivationRules Rules;public readonly CultivationRealmEvaluator Realms;
        public CultivationSnapshot Current {get;private set;}public long RealmRevision {get;private set;}
        readonly HashSet<Guid> commands=new HashSet<Guid>();long lastStep=-1;
        public CultivationSession(Guid owner,Guid epoch,CultivationRealmEvaluator realms,CultivationRules rules,CultivationSnapshot initial)
        {
            if(owner==Guid.Empty || epoch==Guid.Empty || realms==null || rules==null)throw new ArgumentException("Owner, epoch, catalog and explicit rules required.");
            OwnerId=owner;Epoch=epoch;Realms=realms;Rules=rules;
            CultivationReason reason;if(!ValidateSnapshot(initial,out reason))throw new ArgumentException("Invalid cultivation snapshot: "+reason);
            Current=initial;
        }
        public bool ValidateSnapshot(CultivationSnapshot s,out CultivationReason reason)
        {
            reason=CultivationReason.InvalidSnapshot;
            if(s==null || s.Foundation==null || s.Reserve==null || s.Energy==null || s.Blood==null || s.Injury==null)return false;
            var realm=Realms.Evaluate(s.Foundation.ActualRealmId,s.Foundation.Amount);
            if(!realm.IsValid) {reason=realm.Reason;return false;}
            if(!CultivationNumber.NonNegative(s.Reserve.Amount) || !CultivationNumber.NonNegative(s.Energy.Current) || !CultivationNumber.NonNegative(s.Injury.Severity) ||
                !CultivationNumber.NonNegative(s.Blood.AccumulationProgress) || s.Blood.AccumulationProgress>=1 || s.Blood.Charges<0 || s.Energy.Current>Rules.EnergyMaximum(s.Injury.Severity))return false;
            if(realm.BloodCapacity==0 && (s.Blood.Charges!=0 || s.Blood.AccumulationProgress!=0))return false;
            var ids=new HashSet<Guid>();foreach(var id in s.SettledTransactions)if(id==Guid.Empty || !ids.Add(id))return false;
            var b=s.Burning;
            if(b!=null && (b.Id==Guid.Empty || b.OwnerId!=OwnerId || ids.Contains(b.Id) || b.ChargesUsed<1 || !CultivationNumber.NonNegative(b.ReferenceFoundation) ||
                !CultivationNumber.NonNegative(b.DebtRatio) || b.DebtRatio>1 || !CultivationNumber.NonNegative(b.TotalDebt) || !CultivationNumber.NonNegative(b.RemainingSeconds) ||
                !Enum.IsDefined(typeof(BurningStatus),b.Status) || (b.Status==BurningStatus.Active && (b.RemainingSeconds<=0 || b.EndReason.HasValue)) ||
                (b.Status==BurningStatus.SettlementPending && (!b.EndReason.HasValue || !Enum.IsDefined(typeof(BurningEndReason),b.EndReason.Value))) || realm.BloodCapacity==0))return false;
            reason=CultivationReason.None;return true;
        }
        CultivationReason Check(Guid owner,Guid epoch,bool pendingAllowed=false)
        {
            if(owner!=OwnerId || epoch!=Epoch)return CultivationReason.OwnerMismatch;
            if(!pendingAllowed && Current.Burning!=null && Current.Burning.Status==BurningStatus.SettlementPending)return CultivationReason.SettlementRequired;
            return CultivationReason.None;
        }
        CultivationReason Command(Guid owner,Guid epoch,Guid command,bool pendingAllowed=false)
        {var r=Check(owner,epoch,pendingAllowed);return r!=CultivationReason.None?r:command==Guid.Empty?CultivationReason.InvalidSession:commands.Contains(command)?CultivationReason.DuplicateCommand:CultivationReason.None;}
        bool Commit(CultivationSnapshot candidate)
        {CultivationReason reason;if(!ValidateSnapshot(candidate,out reason))return false;Current=candidate;return true;}
        CultivationSnapshot Copy(double? foundation=null,double? reserve=null,double? energy=null,BloodEssenceState blood=null,double? injury=null,BurningTransaction burning=null,bool replaceBurning=false,IEnumerable<Guid> settled=null,string realmId=null)
        {return new CultivationSnapshot(foundation.HasValue || realmId!=null?new CultivationFoundation(realmId??Current.Foundation.ActualRealmId,foundation??Current.Foundation.Amount):Current.Foundation,
            reserve.HasValue?new UnrefinedQiReserve(reserve.Value):Current.Reserve,
            energy.HasValue?new SpiritualEnergy(energy.Value):Current.Energy,blood??Current.Blood,injury.HasValue?new FoundationInjuryState(injury.Value):Current.Injury,
            replaceBurning?burning:Current.Burning,settled??Current.SettledTransactions);}
        BloodEssenceState Accumulate(BloodEssenceState blood,double progress,int capacity)
        {
            if(capacity==0 || blood.Charges>=capacity)return blood; // Hold excess charges; no deletion, no accrual at/over cap.
            double total=blood.AccumulationProgress+progress;int room=capacity-blood.Charges;
            int grants=(int)Math.Min(room,Math.Floor(total));int charges=blood.Charges+grants;
            return new BloodEssenceState(charges,charges>=capacity?blood.AccumulationProgress:total-grants);
        }
        public CultivationReason Tick(Guid owner,Guid epoch,long step,double dt,bool paused,double worldQiDensity)
        {
            var reason=Check(owner,epoch);if(reason!=CultivationReason.None)return reason;
            if(paused)return CultivationReason.Paused;
            if(!CultivationNumber.NonNegative(dt) || dt>Rules.MaximumStepSeconds || !CultivationNumber.NonNegative(worldQiDensity))return CultivationReason.InvalidDelta;
            if(step<0 || step<=lastStep)return CultivationReason.DuplicateCommand;
            CultivationRealmDefinition realm;Realms.TryGet(Current.Foundation.ActualRealmId,out realm);
            double absorbed=worldQiDensity*Rules.AbsorptionRate*Rules.AbsorptionEfficiency*dt;
            double reserve=Current.Reserve.Amount+absorbed;
            double yield=Rules.RefinementEfficiency*Rules.RefinementFactor(Current.Injury.Severity);
            double gain=Math.Min(realm.Thresholds[3]-Current.Foundation.Amount,Math.Min(reserve,Rules.RefinementRate*dt)*yield);
            double foundation=Current.Foundation.Amount+gain;double consumed=yield>0?gain/yield:0;
            double progress=Rules.BloodAccumulationRate*dt;
            double recovery=Rules.SpiritualRecoveryRate*Rules.SpiritualRecoveryModifier*dt;
            if(!CultivationNumber.NonNegative(absorbed) || !CultivationNumber.NonNegative(reserve) || !CultivationNumber.NonNegative(gain) || !CultivationNumber.NonNegative(progress) || !CultivationNumber.NonNegative(recovery))return CultivationReason.Overflow;
            var blood=Accumulate(Current.Blood,progress,Realms.Evaluate(realm.Id,foundation).BloodCapacity);
            var burn=Current.Burning;
            if(burn!=null) {
                double remaining=Math.Max(0,burn.RemainingSeconds-dt);
                burn=burn.Change(burn.ChargesUsed,remaining,remaining==0?BurningStatus.SettlementPending:BurningStatus.Active,remaining==0?(BurningEndReason?)BurningEndReason.Expired:null);
            }
            var candidate=Copy(foundation,Math.Max(0,reserve-consumed),Math.Min(Rules.EnergyMaximum(Current.Injury.Severity),Current.Energy.Current+recovery),blood,burning:burn,replaceBurning:true);
            if(!Commit(candidate))return CultivationReason.InvalidSnapshot;lastStep=step;return CultivationReason.None;
        }
        public CultivationReason AccelerateBlood(Guid owner,Guid epoch,Guid command,double progress)
        {
            var r=Command(owner,epoch,command);if(r!=CultivationReason.None)return r;if(!CultivationNumber.NonNegative(progress))return CultivationReason.InvalidValue;
            var realm=Realms.Evaluate(Current.Foundation.ActualRealmId,Current.Foundation.Amount);if(realm.BloodCapacity==0)return CultivationReason.Locked;
            if(Current.Blood.Charges>=realm.BloodCapacity)return CultivationReason.AtCapacity;
            if(!CultivationNumber.NonNegative(Current.Blood.AccumulationProgress+progress))return CultivationReason.Overflow;
            if(!Commit(Copy(blood:Accumulate(Current.Blood,progress,realm.BloodCapacity))))return CultivationReason.InvalidSnapshot;commands.Add(command);return CultivationReason.None;
        }
        public CultivationReason SpendSpiritualEnergy(Guid owner,Guid epoch,Guid command,double amount)
        {
            var r=Command(owner,epoch,command);if(r!=CultivationReason.None)return r;
            if(!CultivationNumber.NonNegative(amount) || amount>Current.Energy.Current)return CultivationReason.InvalidValue;
            Commit(Copy(energy:Current.Energy.Current-amount));commands.Add(command);return CultivationReason.None;
        }
        public CultivationReason RestoreSpiritualEnergy(Guid owner,Guid epoch,Guid command,double amount,string source)
        {
            var r=Command(owner,epoch,command);if(r!=CultivationReason.None)return r;
            if(!CultivationNumber.NonNegative(amount) || string.IsNullOrWhiteSpace(source))return CultivationReason.InvalidValue;
            double room=Math.Max(0,Rules.EnergyMaximum(Current.Injury.Severity)-Current.Energy.Current);
            Commit(Copy(energy:Current.Energy.Current+Math.Min(room,amount)));commands.Add(command);return CultivationReason.None;
        }
        public CultivationReason ClampSpiritualEnergy(Guid owner,Guid epoch)
        {var r=Check(owner,epoch);if(r!=CultivationReason.None)return r;Commit(Copy(energy:Math.Min(Current.Energy.Current,Rules.EnergyMaximum(Current.Injury.Severity))));return CultivationReason.None;}
        public CultivationReason HealInjury(Guid owner,Guid epoch,Guid command,double amount,string treatmentSource)
        {
            var r=Command(owner,epoch,command);if(r!=CultivationReason.None)return r;
            if(!CultivationNumber.NonNegative(amount) || string.IsNullOrWhiteSpace(treatmentSource))return CultivationReason.InvalidValue;
            Commit(Copy(injury:Math.Max(0,Current.Injury.Severity-amount)));commands.Add(command);return CultivationReason.None;
        }
        public CultivationReason CanAttemptBreakthrough()
        {if(Current.Burning!=null)return CultivationReason.SettlementRequired;if(Current.Injury.Severity>Rules.MaximumInjuryForTrial)return CultivationReason.InjuryBlocked;return Realms.Evaluate(Current.Foundation.ActualRealmId,Current.Foundation.Amount).AtBottleneck?CultivationReason.None:CultivationReason.ProgressNotReady;}
    }
}
