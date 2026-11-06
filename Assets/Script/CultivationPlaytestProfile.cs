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
    }
    [Serializable] public sealed class CultivationPlaytestRealm
    {public string id,name,successor;public int tier;public double[] thresholds;}
    [Serializable] public sealed class CultivationQiSource {public string nodeId;public double density;}

    // Immutable validated data; Editor-only launcher supplies the JSON. No built-in balance.
    public sealed class CultivationPlaytestProfile
    {
        public readonly string Id,Title,StartingRealm;public readonly int Version;
        public readonly double FallbackDensity,MaximumFrameSeconds,StepSeconds;
        public readonly CultivationRealmEvaluator Catalog;public readonly CultivationRules Rules;
        readonly Dictionary<string,string> names=new Dictionary<string,string>(StringComparer.Ordinal);
        readonly Dictionary<string,string> successors=new Dictionary<string,string>(StringComparer.Ordinal);
        readonly Dictionary<string,double> sources=new Dictionary<string,double>(StringComparer.Ordinal);
        CultivationPlaytestProfile(CultivationPlaytestDefinition d,CultivationRealmEvaluator catalog,CultivationRules rules)
        {
            Id=d.id;Version=d.version;Title=d.title;StartingRealm=d.startingRealm;FallbackDensity=d.fallbackDensity;
            MaximumFrameSeconds=d.maximumFrameSeconds;StepSeconds=d.stepSeconds;Catalog=catalog;Rules=rules;
            foreach(var r in d.realms){names.Add(r.id,r.name);successors.Add(r.id,r.successor);}
            foreach(var q in d.qiSources)sources.Add(q.nodeId,q.density);
        }
        public string RealmName(string id) {string name;return names.TryGetValue(id,out name)?name:id;}
        public string ExplicitSuccessor(string id) {string next;return successors.TryGetValue(id,out next)?next:null;}
        public double Density(string nodeId) {double value;return nodeId!=null && sources.TryGetValue(nodeId,out value)?value:FallbackDensity;}
        public static bool TryCreate(CultivationPlaytestDefinition d,Func<string,bool> knownNode,out CultivationPlaytestProfile profile,out string reason)
        {
            profile=null;reason="Invalid playtest configuration.";
            if(d==null || d.schemaVersion!=1 || d.version<1 || string.IsNullOrWhiteSpace(d.id) || !d.id.StartsWith("playtest:",StringComparison.Ordinal) || d.id!=d.id.Trim() ||
                string.IsNullOrWhiteSpace(d.title) || d.realms==null || d.qiSources==null ||
                !CultivationNumber.NonNegative(d.fallbackDensity) || !CultivationNumber.NonNegative(d.maximumFrameSeconds) ||
                !CultivationNumber.NonNegative(d.stepSeconds) || d.stepSeconds<=0 || d.maximumFrameSeconds<d.stepSeconds || d.maximumFrameSeconds/d.stepSeconds>100)return false;
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
                var rules=new CultivationRules(d.stepSeconds,d.absorptionRate,d.absorptionEfficiency,d.refinementRate,d.refinementEfficiency,
                    d.maximumEnergy,d.recoveryRate,1,0,0,1,0,0,0,BloodCapacityOverflowPolicy.HoldExistingStopAccumulation);
                profile=new CultivationPlaytestProfile(d,catalog,rules);reason=null;return true;
            } catch(ArgumentException e){reason=e.Message;return false;}
        }
    }
}
