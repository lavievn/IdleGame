using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TuTienCore
{
    public enum CultivationReason { None, InvalidValue, UnknownRealm, InvalidThresholds, InvalidCatalog, MissingPolicy, InvalidDelta, DuplicateCommand, OwnerMismatch, Paused, NoCharge, HpCondition, InvalidSession, SettlementRequired, AlreadySettled, InjuryBlocked, NoSuccessor, StaleAttempt, Overflow, UnsupportedVersion, InvalidSnapshot, AtCapacity, Locked }
    public enum BloodCapacityOverflowPolicy { Unspecified, HoldExistingStopAccumulation }
    public enum BurningStatus { Active, SettlementPending }
    public enum BurningEndReason { Expired, Death, Retry, Load, Travel, NewGame, Menu, SaveBoundary, CrashRecovery, ExplicitCancel }
    public enum RealmUsePurpose { CombatAbility, Breakthrough, PermanentLearning, Sect, MapEntry, ProgressionReward }
    public static class CultivationNumber
    {
        public static bool NonNegative(double n) {return !double.IsNaN(n) && !double.IsInfinity(n) && n>=0;}
    }
    public sealed class CultivationFoundation
    {
        public readonly string ActualRealmId;public readonly double Amount;
        public CultivationFoundation(string id,double amount) {ActualRealmId=id;Amount=amount;}
    }
    public sealed class UnrefinedQiReserve {public readonly double Amount;public UnrefinedQiReserve(double amount) {Amount=amount;}}
    public sealed class SpiritualEnergy {public readonly double Current;public SpiritualEnergy(double current) {Current=current;}}
    public sealed class BloodEssenceState
    {
        public readonly int Charges;public readonly double AccumulationProgress;
        public BloodEssenceState(int charges,double progress) {Charges=charges;AccumulationProgress=progress;}
    }
    public sealed class FoundationInjuryState {public readonly double Severity;public FoundationInjuryState(double severity) {Severity=severity;}}
    public sealed class BurningTransaction
    {
        public readonly Guid Id,OwnerId;public readonly double ReferenceFoundation,DebtRatio,RemainingSeconds;
        public readonly int ChargesUsed;public readonly BurningStatus Status;public readonly BurningEndReason? EndReason;
        public double TotalDebt {get{return ReferenceFoundation*DebtRatio*ChargesUsed;}}
        public BurningTransaction(Guid id,Guid owner,double reference,double ratio,int used,double remaining,BurningStatus status,BurningEndReason? reason=null)
        {Id=id;OwnerId=owner;ReferenceFoundation=reference;DebtRatio=ratio;ChargesUsed=used;RemainingSeconds=remaining;Status=status;EndReason=reason;}
        internal BurningTransaction Change(int used,double remaining,BurningStatus status,BurningEndReason? reason)
        {return new BurningTransaction(Id,OwnerId,ReferenceFoundation,DebtRatio,used,remaining,status,reason);}
    }
    public sealed class CultivationSnapshot
    {
        public readonly CultivationFoundation Foundation;public readonly UnrefinedQiReserve Reserve;
        public readonly SpiritualEnergy Energy;public readonly BloodEssenceState Blood;public readonly FoundationInjuryState Injury;
        public readonly BurningTransaction Burning;public readonly ReadOnlyCollection<Guid> SettledTransactions;
        public CultivationSnapshot(CultivationFoundation foundation,UnrefinedQiReserve reserve,SpiritualEnergy energy,BloodEssenceState blood,FoundationInjuryState injury,
            BurningTransaction burning=null,IEnumerable<Guid> settled=null)
        {Foundation=foundation;Reserve=reserve;Energy=energy;Blood=blood;Injury=injury;Burning=burning;SettledTransactions=new List<Guid>(settled??new Guid[0]).AsReadOnly();}
    }
    public sealed class CultivationRealmDefinition
    {
        public readonly string Id;public readonly int CombatTier;public readonly bool BloodUnlocked,IsBloodEntryRealm;
        public readonly ReadOnlyCollection<double> Thresholds;public readonly ReadOnlyCollection<int> BloodCapacities;
        // Thresholds: explicit [0, Trung, Hau, VienMan]. No production catalog is supplied.
        public CultivationRealmDefinition(string id,int combatTier,IEnumerable<double> thresholds,bool bloodUnlocked=false,IEnumerable<int> capacities=null,bool isBloodEntryRealm=false)
        {Id=id;CombatTier=combatTier;Thresholds=new List<double>(thresholds??new double[0]).AsReadOnly();BloodUnlocked=bloodUnlocked;
            BloodCapacities=new List<int>(capacities??new int[0]).AsReadOnly();IsBloodEntryRealm=isBloodEntryRealm;}
    }
    public sealed class CultivationRealmResult
    {
        public readonly CultivationReason Reason;public readonly string ActualRealmId;public readonly RealmMinorStage MinorStage;public readonly bool AtBottleneck;public readonly int BloodCapacity,CombatTier;
        public bool IsValid {get{return Reason==CultivationReason.None;}}
        internal CultivationRealmResult(CultivationReason reason,string id,RealmMinorStage stage=RealmMinorStage.Invalid,bool bottle=false,int capacity=0,int tier=0)
        {Reason=reason;ActualRealmId=id;MinorStage=stage;AtBottleneck=bottle;BloodCapacity=capacity;CombatTier=tier;}
    }
    public sealed class CultivationRealmEvaluator
    {
        readonly Dictionary<string,CultivationRealmDefinition> catalog=new Dictionary<string,CultivationRealmDefinition>(StringComparer.Ordinal);
        private CultivationRealmEvaluator() {}
        public static bool TryCreate(IEnumerable<CultivationRealmDefinition> definitions,out CultivationRealmEvaluator evaluator,out CultivationReason reason)
        {
            evaluator=null;reason=CultivationReason.InvalidCatalog;if(definitions==null)return false;var candidate=new CultivationRealmEvaluator();var tiers=new HashSet<int>();
            foreach(var d in definitions) {
                if(d==null || string.IsNullOrWhiteSpace(d.Id) || d.Id!=d.Id.Trim() || d.CombatTier<0 || d.CombatTier==int.MaxValue || !tiers.Add(d.CombatTier) || candidate.catalog.ContainsKey(d.Id))return false;
                if(d.Thresholds.Count!=4 || d.Thresholds[0]!=0) {reason=CultivationReason.InvalidThresholds;return false;}
                for(int i=0;i<4;i++)if(!CultivationNumber.NonNegative(d.Thresholds[i]) || (i>0 && d.Thresholds[i]<=d.Thresholds[i-1])) {reason=CultivationReason.InvalidThresholds;return false;}
                if(d.BloodUnlocked) {
                    if(d.BloodCapacities.Count!=4 || (d.IsBloodEntryRealm && d.BloodCapacities[0]!=1))return false;
                    for(int i=0;i<4;i++)if(d.BloodCapacities[i]<1 || (i>0 && (long)d.BloodCapacities[i]!=(long)d.BloodCapacities[i-1]+1))return false;
                } else if(d.BloodCapacities.Count!=0 || d.IsBloodEntryRealm)return false;
                candidate.catalog.Add(d.Id,d);
            }
            if(candidate.catalog.Count==0)return false;evaluator=candidate;reason=CultivationReason.None;return true;
        }
        public bool TryGet(string id,out CultivationRealmDefinition definition) {definition=null;return id!=null && catalog.TryGetValue(id,out definition);}
        public CultivationRealmResult Evaluate(string actualRealmId,double foundation)
        {
            CultivationRealmDefinition d;if(!TryGet(actualRealmId,out d))return new CultivationRealmResult(CultivationReason.UnknownRealm,actualRealmId);
            if(!CultivationNumber.NonNegative(foundation) || foundation>d.Thresholds[3])return new CultivationRealmResult(CultivationReason.InvalidValue,actualRealmId);
            int stage=foundation>=d.Thresholds[3]?3:foundation>=d.Thresholds[2]?2:foundation>=d.Thresholds[1]?1:0;
            return new CultivationRealmResult(CultivationReason.None,actualRealmId,(RealmMinorStage)(stage+1),stage==3,d.BloodUnlocked?d.BloodCapacities[stage]:0,d.CombatTier);
        }
    }
    // Required explicit configuration. No default MainGame balance or map density.
    public sealed class CultivationRules
    {
        public readonly double MaximumStepSeconds,AbsorptionRate,AbsorptionEfficiency,RefinementRate,RefinementEfficiency;
        public readonly double MaximumSpiritualEnergy,SpiritualRecoveryRate,SpiritualRecoveryModifier,BloodAccumulationRate,DebtRatio,AbsoluteStatFactor;
        public readonly double InjuryRefinementPenalty,InjuryEnergyPenalty,MaximumInjuryForTrial;
        public readonly BloodCapacityOverflowPolicy ExcessCharges;
        public CultivationRules(double maximumStep,double absorptionRate,double absorptionEfficiency,double refinementRate,double refinementEfficiency,
            double maximumEnergy,double recoveryRate,double recoveryModifier,double bloodRate,double debtRatio,double absoluteFactor,
            double injuryRefinePenalty,double injuryEnergyPenalty,double trialInjuryLimit,BloodCapacityOverflowPolicy excessCharges)
        {
            foreach(double n in new[]{maximumStep,absorptionRate,absorptionEfficiency,refinementRate,refinementEfficiency,maximumEnergy,recoveryRate,recoveryModifier,bloodRate,debtRatio,absoluteFactor,injuryRefinePenalty,injuryEnergyPenalty,trialInjuryLimit})
                if(!CultivationNumber.NonNegative(n))throw new ArgumentException("Configuration values must be finite/nonnegative.");
            if(maximumStep<=0 || absorptionEfficiency>1 || refinementEfficiency>1 || debtRatio>1 || absoluteFactor<1 || excessCharges!=BloodCapacityOverflowPolicy.HoldExistingStopAccumulation)
                throw new ArgumentException("Explicit bounded rates and excess-charge policy required.");
            MaximumStepSeconds=maximumStep;AbsorptionRate=absorptionRate;AbsorptionEfficiency=absorptionEfficiency;RefinementRate=refinementRate;RefinementEfficiency=refinementEfficiency;
            MaximumSpiritualEnergy=maximumEnergy;SpiritualRecoveryRate=recoveryRate;SpiritualRecoveryModifier=recoveryModifier;BloodAccumulationRate=bloodRate;DebtRatio=debtRatio;AbsoluteStatFactor=absoluteFactor;
            InjuryRefinementPenalty=injuryRefinePenalty;InjuryEnergyPenalty=injuryEnergyPenalty;MaximumInjuryForTrial=trialInjuryLimit;ExcessCharges=excessCharges;
        }
        public double RefinementFactor(double injury) {return Math.Max(0,1-injury*InjuryRefinementPenalty);}
        public double EnergyMaximum(double injury) {return MaximumSpiritualEnergy*Math.Max(0,1-injury*InjuryEnergyPenalty);}
    }
}
