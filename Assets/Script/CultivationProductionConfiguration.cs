using System;
using System.Collections.Generic;
using UnityEngine;

namespace TuTienCore
{
    public enum CultivationConfigurationReason { None, MissingAsset, InvalidEnvelope, WorldQiInvalid, WorldQiVersionMismatch, InvalidBalance, InvalidSource }
    [Serializable] public sealed class CultivationProductionDefinition
    {
        public int schemaVersion,version,worldQiVersion,capacityVersion;public string id,worldQiId,worldQiSha256,capacityId,capacitySha256;
        public CultivationPlaytestDefinition cultivation;
    }
    public static partial class CultivationProductionConfiguration
    {
        public const string ResourceName="CultivationProductionV1";
        static string Fingerprint(string text)
        {using(var h=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(h.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text))).Replace("-","").ToLowerInvariant();}
        public static bool TryLoad(out CultivationPlaytestProfile profile,out WorldQiDistribution qi,out CultivationConfigurationReason reason,out string detail)
        {return TryLoadVersion(CultivationIntegrationGate.CapacityEnabled?3:CultivationIntegrationGate.WorldQiV2Enabled?2:1,out profile,out qi,out reason,out detail);}
        public static bool TryLoadVersion(int version,out CultivationPlaytestProfile profile,out WorldQiDistribution qi,out CultivationConfigurationReason reason,out string detail)
        {
            profile=null;qi=null;detail=null;reason=CultivationConfigurationReason.MissingAsset;
            var name=ResourceForVersion(version);if(name==null){reason=CultivationConfigurationReason.InvalidEnvelope;return false;}var config=Resources.Load<TextAsset>(name);var sources=Resources.Load<TextAsset>(version==1?"WorldQiDistributionV1":"WorldQiDistributionV2");if(config==null || sources==null)return false;
            try {
                var d=new CultivationProductionDefinition();JsonUtility.FromJsonOverwrite(config.text,d);
                if(d.id!=IdentityForVersion(version) || d.version!=version || d.cultivation==null || (version>=2 && d.cultivation.schemaVersion!=3)){reason=CultivationConfigurationReason.InvalidEnvelope;return false;}
                CultivationCapacityDefinition capacity=null;
                if(version==3){var asset=Resources.Load<TextAsset>("CultivationCapacityV1");if(asset==null){reason=CultivationConfigurationReason.MissingAsset;return false;}string capText=asset.text.Replace("\r\n","\n").TrimStart('\uFEFF').TrimEnd()+"\n";if(Fingerprint(capText)!=d.capacitySha256){reason=CultivationConfigurationReason.InvalidBalance;detail="CapacityFingerprintMismatch";return false;}var cap=JsonUtility.FromJson<CultivationCapacityDefinition>(asset.text);if(cap.id!=d.capacityId||cap.version!=d.capacityVersion){reason=CultivationConfigurationReason.InvalidBalance;return false;}capacity=cap;}
                string canonical=sources.text.Replace("\r\n","\n").TrimStart('\uFEFF').TrimEnd()+"\n";
                if(d.worldQiSha256!=Fingerprint(canonical)){reason=CultivationConfigurationReason.WorldQiVersionMismatch;detail="World Qi content fingerprint mismatch; install an explicit versioned configuration.";return false;}
                var world=new WorldQiDefinition();JsonUtility.FromJsonOverwrite(sources.text,world);
                var ids=new List<string>();var starts=new List<string>();foreach(var node in WorldMapCatalog.All){ids.Add(node.Id);if(node.IsStart)starts.Add(node.Id);}
                WorldQiReason why;if(!WorldQiDistribution.TryCreate(world,ids,starts,out qi,out why)){reason=CultivationConfigurationReason.WorldQiInvalid;detail=why.ToString();return false;}
                return TryCreate(d,world,qi,id=>{WorldMapNode n;return WorldMapCatalog.TryGet(id,out n);},out profile,out reason,out detail,capacity);
            }catch(Exception e) when(e is ArgumentException || e is FormatException){reason=CultivationConfigurationReason.InvalidEnvelope;detail=e.Message;return false;}
        }
        public static bool TryCreate(CultivationProductionDefinition d,WorldQiDefinition world,WorldQiDistribution qi,Func<string,bool> knownNode,out CultivationPlaytestProfile profile,out CultivationConfigurationReason reason,out string detail,CultivationCapacityDefinition capacity=null)
        {
            profile=null;detail=null;reason=CultivationConfigurationReason.InvalidEnvelope;
            if(d==null || d.schemaVersion!=1 || d.version<1 || string.IsNullOrWhiteSpace(d.id) || !d.id.StartsWith("production:",StringComparison.Ordinal) || d.cultivation==null || d.cultivation.id!=d.id || d.cultivation.version!=d.version || (d.cultivation.schemaVersion!=2 && d.cultivation.schemaVersion!=3) || world==null || qi==null)return false;
            reason=CultivationConfigurationReason.WorldQiVersionMismatch;if(d.worldQiId!=qi.Id || d.worldQiVersion!=qi.Version || (d.version==3?d.worldQiVersion!=2:d.version!=d.worldQiVersion))return false;
            var b=d.cultivation;if((d.version==3)!=(capacity!=null)){detail="CapacityVersionBoundary";return false;}reason=CultivationConfigurationReason.InvalidBalance;
            if(b.initialFoundation!=0 || b.initialReserve!=0 || b.initialEnergy!=0 || b.absorptionRate!=2 || b.absorptionEfficiency!=1 || b.refinementRate!=1 || b.refinementEfficiency!=1 || b.realms==null || b.realms.Length!=1 ||
                b.startingRealm!="realm:luyen-khi" || b.realms[0].id!=b.startingRealm || !string.IsNullOrEmpty(b.realms[0].successor) || b.realms[0].thresholds==null || b.realms[0].thresholds.Length!=4)return false;
            double[] thresholds={0,900,2100,3600};for(int i=0;i<4;i++)if(b.realms[0].thresholds[i]!=thresholds[i])return false;
            // NodeQiRate is absolute environmental Qi/second. Normalize into the existing density*absorption pipeline.
            // Keep domain absorption coefficient2 and refinement cap1; never multiply advertised5Qi/s into10Qi/s.
            b.qiSources=new CultivationQiSource[world.nodes.Length];
            for(int i=0;i<world.nodes.Length;i++){double rate;if(!qi.TryRate(world.nodes[i].nodeId,out rate)){reason=CultivationConfigurationReason.InvalidSource;return false;}b.qiSources[i]=new CultivationQiSource{nodeId=world.nodes[i].nodeId,density=rate/(b.absorptionRate*b.absorptionEfficiency)};}
            b.fallbackDensity=.5; // Explicit legacy worldMapVersion0 baseline only; all133 production nodes are required above.
            if(!CultivationPlaytestProfile.TryCreate(b,knownNode,out profile,out detail,true,capacity))return false;
            reason=CultivationConfigurationReason.None;return true;
        }
    }
}
