using System;
using System.Collections.Generic;

namespace TuTienCore
{
    public enum WorldQiReason { None, InvalidSchema, InvalidIdentity, InvalidTier, InvalidRate, InvalidCoordinates, DuplicateNode, UnknownNode, MissingNode, InvalidStartingRate }
    [Serializable] public sealed class WorldQiTier {public int tier;public double qiPerSecond;public string label;}
    [Serializable] public sealed class WorldQiNode
    {
        public string nodeId,coordinateStatus,provenance;public double sourceX,sourceY,normalizedX,normalizedY,qiPerSecond;
        public int qiTier,generatorVersion,configurationVersion,seed;public bool manualOverride;
    }
    [Serializable] public sealed class WorldQiDefinition
    {
        public int schemaVersion,version,seed,generatorVersion,artworkWidth,artworkHeight;
        public string id,coordinateOrigin,coordinateSourceSha256,artworkSha256;
        public WorldQiTier[] tiers;public WorldQiNode[] nodes;
    }
    // Immutable table loaded once. Runtime never samples a bitmap or random field.
    public sealed class WorldQiDistribution
    {
        public readonly string Id;public readonly int Version,Seed,GeneratorVersion,Count;
        readonly Dictionary<string,double> rates=new Dictionary<string,double>(StringComparer.Ordinal);
        readonly Dictionary<string,int> tiers=new Dictionary<string,int>(StringComparer.Ordinal);
        WorldQiDistribution(WorldQiDefinition d)
        {Id=d.id;Version=d.version;Seed=d.seed;GeneratorVersion=d.generatorVersion;Count=d.nodes.Length;foreach(var n in d.nodes){rates.Add(n.nodeId,n.qiPerSecond);tiers.Add(n.nodeId,n.qiTier);}}
        public bool TryRate(string id,out double rate){return rates.TryGetValue(id??"",out rate);}
        public bool TryTier(string id,out int tier){return tiers.TryGetValue(id??"",out tier);}
        public static bool TryCreate(WorldQiDefinition d,IEnumerable<string> expectedNodes,IEnumerable<string> startingNodes,out WorldQiDistribution table,out WorldQiReason reason)
        {
            table=null;reason=WorldQiReason.InvalidSchema;
            if(d==null || d.schemaVersion!=1 || d.version<1 || d.generatorVersion<1 || d.tiers==null || d.tiers.Length!=8 || d.nodes==null)return false;
            reason=WorldQiReason.InvalidIdentity;if(string.IsNullOrWhiteSpace(d.id) || !d.id.StartsWith("world-qi:",StringComparison.Ordinal) || expectedNodes==null || startingNodes==null)return false;
            var levels=new Dictionary<int,double>();double previous=0;
            foreach(var t in d.tiers) {
                reason=WorldQiReason.InvalidTier;if(t==null || t.tier<0 || t.tier>7 || levels.ContainsKey(t.tier) || string.IsNullOrWhiteSpace(t.label))return false;
                reason=WorldQiReason.InvalidRate;if(!CultivationNumber.NonNegative(t.qiPerSecond) || t.qiPerSecond<.6 || t.qiPerSecond>5)return false;
                levels.Add(t.tier,t.qiPerSecond);
            }
            for(int i=0;i<8;i++){if(!levels.ContainsKey(i) || levels[i]<=previous){reason=WorldQiReason.InvalidTier;return false;}previous=levels[i];}
            if(levels[0]!=.6 || levels[2]!=1 || levels[7]!=5){reason=WorldQiReason.InvalidRate;return false;}
            var expected=new HashSet<string>(expectedNodes,StringComparer.Ordinal);var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var n in d.nodes) {
                reason=WorldQiReason.UnknownNode;if(n==null || string.IsNullOrWhiteSpace(n.nodeId) || !expected.Contains(n.nodeId))return false;
                reason=WorldQiReason.DuplicateNode;if(!seen.Add(n.nodeId))return false;
                reason=WorldQiReason.InvalidRate;if(!levels.ContainsKey(n.qiTier) || n.qiPerSecond!=levels[n.qiTier])return false;
                reason=WorldQiReason.InvalidIdentity;if(n.seed!=d.seed || n.generatorVersion!=d.generatorVersion || n.configurationVersion!=d.version || string.IsNullOrWhiteSpace(n.provenance))return false;
                reason=WorldQiReason.InvalidCoordinates;
                if(d.artworkWidth<=0 || d.artworkHeight<=0 || !CultivationNumber.NonNegative(n.normalizedX) || n.normalizedX>1 || !CultivationNumber.NonNegative(n.normalizedY) || n.normalizedY>1 ||
                    !CultivationNumber.NonNegative(n.sourceX) || n.sourceX>d.artworkWidth || !CultivationNumber.NonNegative(n.sourceY) || n.sourceY>d.artworkHeight ||
                    Math.Abs(n.sourceX/d.artworkWidth-n.normalizedX)>1e-6 || Math.Abs(1-n.sourceY/d.artworkHeight-n.normalizedY)>1e-6 || string.IsNullOrWhiteSpace(n.coordinateStatus))return false;
            }
            reason=WorldQiReason.MissingNode;if(expected.Count!=133 || seen.Count!=133 || !expected.SetEquals(seen))return false;
            reason=WorldQiReason.InvalidStartingRate;foreach(string id in startingNodes){var n=Array.Find(d.nodes,x=>x.nodeId==id);if(n==null || n.qiTier!=2 || n.qiPerSecond!=1)return false;}
            table=new WorldQiDistribution(d);reason=WorldQiReason.None;return true;
        }
    }
}
