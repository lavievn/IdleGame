using System;
using System.Collections.Generic;

namespace TuTienCore
{
    [Serializable] public sealed class CultivationPlaytestDefinition
    {
        public int schemaVersion,version;public string id,title,startingRealm;
        public CultivationPlaytestRealm[] realms;public CultivationQiSource[] qiSources;
        public double fallbackDensity,absorptionRate,absorptionEfficiency,refinementRate,refinementEfficiency;
        public double maximumFrameSeconds,stepSeconds,maximumEnergy,recoveryRate;
        public double initialFoundation,initialReserve,initialEnergy;
    }
    [Serializable] public sealed class CultivationPlaytestRealm
    {public string id,name,successor;public int tier;public double[] thresholds;}
    [Serializable] public sealed class CultivationQiSource {public string nodeId;public double density;}

    // Immutable validated data; Editor-only launcher supplies the JSON. No built-in balance.
    public sealed class CultivationPlaytestProfile
    {
        public readonly string Id,Title,StartingRealm;public readonly int Version;
        public readonly double FallbackDensity,MaximumFrameSeconds,StepSeconds,InitialFoundation,InitialReserve,InitialEnergy;
        public readonly int SaveVersion;public readonly bool IsProduction;
        public readonly CultivationRealmEvaluator Catalog;public readonly CultivationRules Rules;
        readonly Dictionary<string,string> names=new Dictionary<string,string>(StringComparer.Ordinal);
        readonly Dictionary<string,string> successors=new Dictionary<string,string>(StringComparer.Ordinal);
        readonly Dictionary<string,double> sources=new Dictionary<string,double>(StringComparer.Ordinal);
        CultivationPlaytestProfile(CultivationPlaytestDefinition d,CultivationRealmEvaluator catalog,CultivationRules rules,bool production)
        {
            IsProduction=production;
            Id=d.id;Version=d.version;Title=d.title;StartingRealm=d.startingRealm;FallbackDensity=d.fallbackDensity;
            MaximumFrameSeconds=d.maximumFrameSeconds;StepSeconds=d.stepSeconds;Catalog=catalog;Rules=rules;
            SaveVersion=d.schemaVersion;InitialFoundation=d.initialFoundation;InitialReserve=d.initialReserve;InitialEnergy=d.initialEnergy;
            foreach(var r in d.realms){names.Add(r.id,r.name);successors.Add(r.id,r.successor);}
            foreach(var q in d.qiSources)sources.Add(q.nodeId,q.density);
        }
        public string RealmName(string id) {string name;return names.TryGetValue(id,out name)?name:id;}
        public string ExplicitSuccessor(string id) {string next;return successors.TryGetValue(id,out next)?next:null;}
        public double Density(string nodeId) {double value;return nodeId!=null && sources.TryGetValue(nodeId,out value)?value:FallbackDensity;}
        public bool HasNode(string nodeId){return sources.ContainsKey(nodeId??"");}
        public double EnvironmentalQiPerSecond(string nodeId){return Density(nodeId)*Rules.AbsorptionRate*Rules.AbsorptionEfficiency;}
        public static bool TryCreate(CultivationPlaytestDefinition d,Func<string,bool> knownNode,out CultivationPlaytestProfile profile,out string reason,bool production=false,CultivationCapacityDefinition capacityDefinition=null)
        {
            profile=null;reason="Invalid playtest configuration.";
            if(d==null || (d.schemaVersion!=1 && d.schemaVersion!=2 && d.schemaVersion!=3) || d.version<1 || string.IsNullOrWhiteSpace(d.id) || !(production?(d.schemaVersion==2 || d.schemaVersion==3) && d.id.StartsWith("production:",StringComparison.Ordinal):d.schemaVersion==1?d.id.StartsWith("playtest:",StringComparison.Ordinal):d.id.StartsWith("balance:",StringComparison.Ordinal)) || d.id!=d.id.Trim() ||
                string.IsNullOrWhiteSpace(d.title) || d.realms==null || d.qiSources==null ||
                !CultivationNumber.NonNegative(d.fallbackDensity) || !CultivationNumber.NonNegative(d.maximumFrameSeconds) ||
                !CultivationNumber.NonNegative(d.stepSeconds) || d.stepSeconds<=0 || d.maximumFrameSeconds<d.stepSeconds || d.maximumFrameSeconds/d.stepSeconds>100 || d.maximumFrameSeconds>1 ||
                !CultivationNumber.NonNegative(d.initialFoundation) || !CultivationNumber.NonNegative(d.initialReserve) || !CultivationNumber.NonNegative(d.initialEnergy) ||
                (d.schemaVersion==1 && (d.initialFoundation!=0 || d.initialReserve!=0 || d.initialEnergy!=0)))return false;
            var defs=new List<CultivationRealmDefinition>();var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var r in d.realms) {
                if(r==null || string.IsNullOrWhiteSpace(r.name) || string.IsNullOrWhiteSpace(r.id) || !ids.Add(r.id))return false;
                defs.Add(new CultivationRealmDefinition(r.id,r.tier,r.thresholds)); // Blood remains locked in this profile.
            }
            if(!ids.Contains(d.startingRealm))return false;
            foreach(var r in d.realms)if(!string.IsNullOrEmpty(r.successor)) {
                CultivationPlaytestRealm target=Array.Find(d.realms,x=>x.id==r.successor);
                if(target==null || target.tier<=r.tier)return false;
            }
            var nodes=new HashSet<string>(StringComparer.Ordinal);
            foreach(var q in d.qiSources)if(q==null || string.IsNullOrWhiteSpace(q.nodeId) || !nodes.Add(q.nodeId) ||
                !CultivationNumber.NonNegative(q.density) || knownNode==null || !knownNode(q.nodeId))return false;
            CultivationRealmEvaluator catalog;CultivationReason error;
            if(!CultivationRealmEvaluator.TryCreate(defs,out catalog,out error)){reason=error.ToString();return false;}
            try {
                CultivationCapacityCatalog capacity=null;
                if(capacityDefinition!=null){if(!CultivationCapacityCatalog.TryCreate(capacityDefinition,out capacity,out reason))return false;foreach(var realm in d.realms)if(!capacity.HasRealm(realm.id)){reason="MissingCapacityRealm";return false;}}
                var rules=new CultivationRules(d.stepSeconds,d.absorptionRate,d.absorptionEfficiency,d.refinementRate,d.refinementEfficiency,
                    d.maximumEnergy,d.recoveryRate,1,0,0,1,0,0,0,BloodCapacityOverflowPolicy.HoldExistingStopAccumulation,capacity);
                var candidate=new CultivationPlaytestProfile(d,catalog,rules,production);
                var initial=CultivationGameplayState.New(candidate); // Validate initial state against the same runtime rules.
                foreach(var q in d.qiSources)if(!CultivationNumber.NonNegative(q.density*rules.AbsorptionRate*rules.AbsorptionEfficiency*d.maximumFrameSeconds))return false;
                if(!CultivationNumber.NonNegative(d.fallbackDensity*rules.AbsorptionRate*rules.AbsorptionEfficiency*d.maximumFrameSeconds))return false;
                profile=candidate;reason=null;return true;
            } catch(ArgumentException e){reason=e.Message;return false;}
        }
    }
}
