using System;
using System.Collections.Generic;

namespace TuTienCore
{
    public sealed partial class CultivationSession
    {
        public const double BloodChargeDurationSeconds=30;
        public CultivationReason ActivateBlood(Guid owner,Guid epoch,Guid attempt,double currentHp,double maximumHp,out double hpAfter)
        {
            hpAfter=currentHp;var reason=Command(owner,epoch,attempt);if(reason!=CultivationReason.None)return reason;
            if(Current.Burning!=null)return CultivationReason.InvalidSession;
            if(Current.SettledTransactions.Contains(attempt))return CultivationReason.AlreadySettled;
            if(!CultivationNumber.NonNegative(currentHp) || !CultivationNumber.NonNegative(maximumHp) || maximumHp<=0 || currentHp<=0 || currentHp>maximumHp || currentHp/maximumHp>=.3)return CultivationReason.HpCondition;
            if(Current.Blood.Charges<1)return CultivationReason.NoCharge;
            if(Realms.Evaluate(Current.Foundation.ActualRealmId,Current.Foundation.Amount).BloodCapacity==0)return CultivationReason.Locked;
            var burn=new BurningTransaction(attempt,OwnerId,Current.Foundation.Amount,Rules.DebtRatio,1,BloodChargeDurationSeconds,BurningStatus.Active,Current.Foundation.ActualRealmId);
            if(!Commit(Copy(blood:new BloodEssenceState(Current.Blood.Charges-1,Current.Blood.AccumulationProgress),burning:burn,replaceBurning:true)))return CultivationReason.Overflow;
            commands.Add(attempt);hpAfter=maximumHp;return CultivationReason.None;
        }
        public CultivationReason ExtendBlood(Guid owner,Guid epoch,Guid command,Guid burningSession)
        {
            var reason=Command(owner,epoch,command);if(reason!=CultivationReason.None)return reason;
            var b=Current.Burning;if(b==null || b.Id!=burningSession || b.Status!=BurningStatus.Active)return CultivationReason.InvalidSession;
            if(Current.Blood.Charges<1)return CultivationReason.NoCharge;if(b.ChargesUsed==int.MaxValue)return CultivationReason.Overflow;
            var next=b.Change(b.ChargesUsed+1,b.RemainingSeconds+BloodChargeDurationSeconds,BurningStatus.Active,null);
            if(!Commit(Copy(blood:new BloodEssenceState(Current.Blood.Charges-1,Current.Blood.AccumulationProgress),burning:next,replaceBurning:true)))return CultivationReason.Overflow;
            commands.Add(command);return CultivationReason.None; // no HP output: extension never heals again
        }
        public CultivationReason EndBlood(Guid owner,Guid epoch,Guid burningSession,BurningEndReason endReason)
        {
            var reason=Check(owner,epoch,true);if(reason!=CultivationReason.None)return reason;
            if(Current.SettledTransactions.Contains(burningSession))return CultivationReason.AlreadySettled;
            var b=Current.Burning;if(b==null || b.Id!=burningSession || !Enum.IsDefined(typeof(BurningEndReason),endReason))return CultivationReason.InvalidSession;
            if(b.Status==BurningStatus.SettlementPending)return CultivationReason.SettlementRequired;
            Commit(Copy(burning:b.Change(b.ChargesUsed,b.RemainingSeconds,BurningStatus.SettlementPending,endReason),replaceBurning:true));return CultivationReason.None;
        }
        public CultivationReason SettleBlood(Guid owner,Guid epoch,Guid burningSession)
        {
            var reason=Check(owner,epoch,true);if(reason!=CultivationReason.None)return reason;
            if(Current.SettledTransactions.Contains(burningSession))return CultivationReason.AlreadySettled;
            var b=Current.Burning;if(b==null || b.Id!=burningSession || b.Status!=BurningStatus.SettlementPending)return CultivationReason.InvalidSession;
            double paid=Math.Min(Current.Foundation.Amount,b.TotalDebt),deficit=b.TotalDebt-paid;
            double injury=Current.Injury.Severity+deficit;if(!CultivationNumber.NonNegative(injury))return CultivationReason.Overflow;
            var settled=new List<Guid>(Current.SettledTransactions);settled.Add(b.Id);
            var candidate=Copy(foundation:Current.Foundation.Amount-paid,injury:injury,energy:Math.Min(Current.Energy.Current,Rules.EnergyMaximum(injury)),burning:null,replaceBurning:true,settled:settled);
            if(!Commit(candidate))return CultivationReason.InvalidSnapshot;return CultivationReason.None;
        }
        public int RealmTierFor(RealmUsePurpose purpose,bool abilitySupportsForcedExecution=false)
        {
            int tier=Realms.Evaluate(Current.Foundation.ActualRealmId,Current.Foundation.Amount).CombatTier;
            bool active=Current.Burning!=null && Current.Burning.Status==BurningStatus.Active;
            return active && purpose==RealmUsePurpose.CombatAbility && abilitySupportsForcedExecution?tier+1:tier;
        }
    }
    public static class BloodEssenceModifiers
    {
        public static string OwnerPrefix(Guid owner) {return "cultivation:burn:"+owner.ToString("N")+":";}
        static bool Allowed(CombatStatId stat)
        {return stat==CombatStatId.Attack || stat==CombatStatId.MagicalAttack || stat==CombatStatId.Armor || stat==CombatStatId.MagicResistance || stat==CombatStatId.HealthRegeneration || stat==CombatStatId.KillHealBonus;}
        // Caller supplies evaluated ABSOLUTE values excluding this owner's burn prefix.
        // Flat delta doubles those values including existing flat gear, without changing math.
        public static CultivationReason Reconcile(CultivationSession session,IList<CombatStatModifier> modifiers,IDictionary<CombatStatId,float> unbuffedValues)
        {
            if(session==null || modifiers==null || unbuffedValues==null)return CultivationReason.InvalidValue;
            var planned=new List<CombatStatModifier>();var b=session.Current.Burning;string prefix=OwnerPrefix(session.OwnerId);
            if(b!=null && b.Status==BurningStatus.Active)
                foreach(var pair in unbuffedValues) {
                    if(!Allowed(pair.Key) || !CultivationNumber.NonNegative(pair.Value))return CultivationReason.InvalidValue;
                    double delta=pair.Value*(session.Rules.AbsoluteStatFactor-1);
                    if(!CultivationNumber.NonNegative(delta) || delta>float.MaxValue)return CultivationReason.Overflow;
                    planned.Add(new CombatStatModifier {sourceId=prefix+b.Id.ToString("N"),stat=pair.Key,flat=(float)delta,percent=0});
                }
            for(int i=modifiers.Count-1;i>=0;i--)if(modifiers[i]!=null && modifiers[i].sourceId!=null && modifiers[i].sourceId.StartsWith(prefix,StringComparison.Ordinal))modifiers.RemoveAt(i);
            foreach(var m in planned)modifiers.Add(m);return CultivationReason.None;
        }
    }
}
