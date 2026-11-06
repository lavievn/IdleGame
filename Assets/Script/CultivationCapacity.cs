using System;
using System.Collections.Generic;
namespace TuTienCore
{
    [Serializable] public sealed class CultivationCapacityDefinition
    {public string id;public int version;public double maximumRate,injuryAbsorptionPenalty,injuryRefinementPenalty;public CultivationRealmCapacityDefinition[] realms;}
    [Serializable] public sealed class CultivationRealmCapacityDefinition
    {public string realmId;public double baseAbsorption,baseRefinement;public double[] absorptionStageFactors,refinementStageFactors;}
    public enum CultivationModifierSource { Root, CultivationArt, EquipmentOrTreasure }
    // Explicit cultivation extension only; no general combat-stat or effective-realm input.
    public sealed class CultivationCapacityModifier
    {
        public readonly string Id;public readonly CultivationModifierSource Source;
        public readonly double AbsorptionFlat,RefinementFlat,AbsorptionMultiplier,RefinementMultiplier;
        public CultivationCapacityModifier(string id,CultivationModifierSource source,double absorptionFlat=0,double refinementFlat=0,double absorptionMultiplier=1,double refinementMultiplier=1)
        {
            if(string.IsNullOrWhiteSpace(id)||id.Length>128||!Enum.IsDefined(typeof(CultivationModifierSource),source)||double.IsNaN(absorptionFlat)||double.IsNaN(refinementFlat)||Math.Abs(absorptionFlat)>1000||Math.Abs(refinementFlat)>1000||!CultivationNumber.NonNegative(absorptionMultiplier)||!CultivationNumber.NonNegative(refinementMultiplier)||absorptionMultiplier>4||refinementMultiplier>4)throw new ArgumentException("Bounded cultivation modifier required.");
            Id=id;Source=source;AbsorptionFlat=absorptionFlat;RefinementFlat=refinementFlat;AbsorptionMultiplier=absorptionMultiplier;RefinementMultiplier=refinementMultiplier;
        }
    }
    public struct CultivationCapacity
    {
        public readonly double Absorption,Refinement;public readonly RealmMinorStage Stage;
        public CultivationCapacity(double absorption,double refinement,RealmMinorStage stage){Absorption=absorption;Refinement=refinement;Stage=stage;}
    }
    public struct CultivationStepFlow
    {
        public readonly double Seconds,EnvironmentalSupply,AbsorptionCapacity,RefinementCapacity,Absorbed,Refined;
        public CultivationStepFlow(double seconds,double supply,double absorptionCapacity,double refinementCapacity,double absorbed,double refined){Seconds=seconds;EnvironmentalSupply=supply;AbsorptionCapacity=absorptionCapacity;RefinementCapacity=refinementCapacity;Absorbed=absorbed;Refined=refined;}
        public double AbsorptionPerSecond=>Seconds>0?Absorbed/Seconds:0;
        public double RefinementPerSecond=>Seconds>0?Refined/Seconds:0;
    }
    public sealed class CultivationCapacityCatalog
    {
        sealed class Entry {public double Abs,Ref;public double[] AbsStages,RefStages;}
        readonly Dictionary<string,Entry> realms=new Dictionary<string,Entry>(StringComparer.Ordinal);
        public readonly string Id;public readonly int Version;public readonly double MaximumRate,InjuryAbsorptionPenalty,InjuryRefinementPenalty;
        CultivationCapacityCatalog(CultivationCapacityDefinition d){Id=d.id;Version=d.version;MaximumRate=d.maximumRate;InjuryAbsorptionPenalty=d.injuryAbsorptionPenalty;InjuryRefinementPenalty=d.injuryRefinementPenalty;foreach(var r in d.realms)realms.Add(r.realmId,new Entry{Abs=r.baseAbsorption,Ref=r.baseRefinement,AbsStages=(double[])r.absorptionStageFactors.Clone(),RefStages=(double[])r.refinementStageFactors.Clone()});}
        public static bool TryCreate(CultivationCapacityDefinition d,out CultivationCapacityCatalog catalog,out string reason)
        {
            catalog=null;reason="InvalidCapacityCatalog";
            if(d==null||string.IsNullOrWhiteSpace(d.id)||d.version<1||!CultivationNumber.NonNegative(d.maximumRate)||d.maximumRate<=0||d.maximumRate>1000||!CultivationNumber.NonNegative(d.injuryAbsorptionPenalty)||!CultivationNumber.NonNegative(d.injuryRefinementPenalty)||d.injuryAbsorptionPenalty>1||d.injuryRefinementPenalty>1||d.realms==null||d.realms.Length==0)return false;
            var ids=new HashSet<string>(StringComparer.Ordinal);foreach(var r in d.realms){
                if(r==null||string.IsNullOrWhiteSpace(r.realmId)||!ids.Add(r.realmId)||!CultivationNumber.NonNegative(r.baseAbsorption)||!CultivationNumber.NonNegative(r.baseRefinement)||r.baseAbsorption<=0||r.baseRefinement<=0||r.absorptionStageFactors==null||r.refinementStageFactors==null||r.absorptionStageFactors.Length!=4||r.refinementStageFactors.Length!=4)return false;
                for(int i=0;i<4;i++)if(!CultivationNumber.NonNegative(r.absorptionStageFactors[i])||!CultivationNumber.NonNegative(r.refinementStageFactors[i])||r.absorptionStageFactors[i]<1||r.refinementStageFactors[i]<1||r.baseAbsorption*r.absorptionStageFactors[i]>d.maximumRate||r.baseRefinement*r.refinementStageFactors[i]>d.maximumRate||(i>0&&(r.absorptionStageFactors[i]<=r.absorptionStageFactors[i-1]||r.refinementStageFactors[i]<=r.refinementStageFactors[i-1])))return false;
            }catalog=new CultivationCapacityCatalog(d);reason=null;return true;
        }
        public bool HasRealm(string id)=>id!=null&&realms.ContainsKey(id);
        public CultivationCapacity Evaluate(CultivationRealmEvaluator evaluator,CultivationSnapshot state,IEnumerable<CultivationCapacityModifier> modifiers=null)
        {
            if(evaluator==null||state==null)throw new ArgumentException("Actual cultivation snapshot required.");
            var actual=evaluator.Evaluate(state.Foundation.ActualRealmId,state.Foundation.Amount);Entry entry;
            if(!actual.IsValid||!realms.TryGetValue(actual.ActualRealmId,out entry)||!CultivationNumber.NonNegative(state.Injury.Severity))throw new ArgumentException("Unknown capacity realm/state.");
            int stage=(int)actual.MinorStage-1;double af=0,rf=0,am=1,rm=1;
            if(modifiers!=null){var ordered=new List<CultivationCapacityModifier>();foreach(var m in modifiers){if(m==null||ordered.Count>=64)throw new ArgumentException("At most64 explicit modifiers.");ordered.Add(m);}ordered.Sort((left,right)=>StringComparer.Ordinal.Compare(left.Id,right.Id));string last=null;foreach(var m in ordered){if(m.Id==last)throw new ArgumentException("Duplicate cultivation modifier.");last=m.Id;af+=m.AbsorptionFlat;rf+=m.RefinementFlat;am+=m.AbsorptionMultiplier-1;rm+=m.RefinementMultiplier-1;}}
            double a=Math.Min(MaximumRate,Math.Max(0,entry.Abs*entry.AbsStages[stage]+af)*Math.Max(0,Math.Min(4,am)))*Math.Max(0,1-state.Injury.Severity*InjuryAbsorptionPenalty);
            double r=Math.Min(MaximumRate,Math.Max(0,entry.Ref*entry.RefStages[stage]+rf)*Math.Max(0,Math.Min(4,rm)))*Math.Max(0,1-state.Injury.Severity*InjuryRefinementPenalty);
            return new CultivationCapacity(a,r,actual.MinorStage);
        }
    }
}
