using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace TuTienCore
{
    [Serializable] public class CultivationGameplayComponentV2 : CultivationComponentDto
    {
        public double clockRemainder,clockElapsed;
        public string clockNodeId;
    }
    [Serializable] public sealed class CultivationGameplayComponentV3 : CultivationGameplayComponentV2
    {public string clockConfigurationId;public int clockConfigurationVersion;}
    internal sealed class CultivationClockState
    {
        public readonly double Remainder,Elapsed;public readonly string NodeId,ConfigurationId;public readonly int ConfigurationVersion;
        public CultivationClockState(double remainder=0,double elapsed=0,string node=null,string configurationId=null,int configurationVersion=0){Remainder=remainder;Elapsed=elapsed;NodeId=node;ConfigurationId=configurationId;ConfigurationVersion=configurationVersion;}
    }
    public sealed partial class CultivationSession {internal CultivationClockState GameplayClock;}
    public static class CultivationGameplayState
    {
        public const string SaveField="cultivationJson";
        public static string Capture(CultivationSession session,CultivationPlaytestProfile profile)
        {using(CultivationPerformance.Measure(CultivationPerformance.Part.CheckpointJson))return CaptureCore(session,profile);}
        static string CaptureCore(CultivationSession session,CultivationPlaytestProfile profile)
        {
            var dto=CultivationComponentCodec.Capture(session,profile.Id,profile.SaveVersion==3?(CultivationComponentDto)new CultivationGameplayComponentV3():profile.SaveVersion==2?new CultivationGameplayComponentV2():null);dto.configurationVersion=profile.Version;
            if(profile.SaveVersion==1)return JsonUtility.ToJson(dto,false);
            var current=(CultivationGameplayComponentV2)dto;
            var clock=session.GameplayClock??new CultivationClockState();
            current.version=profile.SaveVersion;current.clockRemainder=clock.Remainder;current.clockElapsed=clock.Elapsed;current.clockNodeId=clock.NodeId;
            if(profile.SaveVersion==3){var c=(CultivationGameplayComponentV3)current;c.clockConfigurationId=clock.ConfigurationId??profile.Id;c.clockConfigurationVersion=clock.ConfigurationId==null?profile.Version:clock.ConfigurationVersion;}
            return JsonUtility.ToJson(current,false);
        }
        public static CultivationSession New(CultivationPlaytestProfile p)
        {return new CultivationSession(Guid.NewGuid(),Guid.NewGuid(),p.Catalog,p.Rules,new CultivationSnapshot(new CultivationFoundation(p.StartingRealm,p.InitialFoundation),new UnrefinedQiReserve(p.InitialReserve),new SpiritualEnergy(p.InitialEnergy),new BloodEssenceState(0,0),new FoundationInjuryState(0)));}
        public static bool TryRestore(string component,CultivationPlaytestProfile profile,out CultivationSession session,out string reason)
        {
            session=null;reason=null;if(string.IsNullOrEmpty(component))return true;
            Dictionary<string,string> fields;if(!CultivationSaveJson.TryObject(component,out fields,out reason))return false;
            if(profile!=null && profile.IsProduction){CultivationPlaytestProfile resolved;if(!CultivationProductionConfiguration.TryResolve(component,profile,out resolved,out reason))return false;profile=resolved;}
            string version;int v;if(!fields.TryGetValue("version",out version) || !int.TryParse(version,out v) || (v!=1 && v!=2 && v!=3)){reason="Unsupported cultivation component version.";return false;}
            if(profile==null){reason="Cultivation playtest configuration is not enabled in this Save context.";return false;}
            if(v!=profile.SaveVersion){reason="Explicit cultivation schema migration required.";return false;}
            var dtoType=v==1?typeof(CultivationComponentDto):v==2?typeof(CultivationGameplayComponentV2):typeof(CultivationGameplayComponentV3);
            // Every authority field must be present with its exact scalar type; no defaults from JsonUtility/template.
            foreach(var f in dtoType.GetFields()) {
                string token;if(!fields.TryGetValue(f.Name,out token)){reason="Missing cultivation field: "+f.Name;return false;}
                int integer;double number;string text;
                if((f.FieldType==typeof(int) && !int.TryParse(token,NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out integer)) ||
                    (f.FieldType==typeof(double) && (!double.TryParse(token,NumberStyles.Float,CultureInfo.InvariantCulture,out number) || !CultivationNumber.NonNegative(number))) ||
                    (f.FieldType==typeof(bool) && token!="true" && token!="false") ||
                    (f.FieldType==typeof(string) && token!="null" && !CultivationSaveJson.TryString(token,out text)) ||
                    (f.FieldType==typeof(List<string>) && !token.StartsWith("[",StringComparison.Ordinal))) {reason="Invalid cultivation field type/value: "+f.Name;return false;}
            }
            if(fields.Count!=dtoType.GetFields().Length){reason="Unknown cultivation fields require an explicit component version.";return false;}
            try {
                CultivationComponentDto dto=v==1?new CultivationComponentDto():v==2?(CultivationComponentDto)new CultivationGameplayComponentV2():new CultivationGameplayComponentV3();JsonUtility.FromJsonOverwrite(component,dto);
                // Native JsonUtility can round an authoritative double by one ULP on read.
                // Reuse the structurally validated invariant tokens for exact .NET roundtrip.
                foreach(var field in dtoType.GetFields())if(field.FieldType==typeof(double))
                    field.SetValue(dto,double.Parse(fields[field.Name],NumberStyles.Float,CultureInfo.InvariantCulture));
                CultivationClockState clock=null;
                if(v>=2) {
                    var c=(CultivationGameplayComponentV2)dto;
                    if(c.clockRemainder>=profile.StepSeconds || c.clockElapsed<c.clockRemainder || c.clockElapsed>1e12 ||
                        (c.clockRemainder>0 && string.IsNullOrEmpty(c.clockNodeId)) || (c.clockNodeId!=null && c.clockNodeId.Length>128))
                        {reason="Invalid cultivation clock checkpoint.";return false;}
                    string sourceId=dto.configurationId;int sourceVersion=dto.configurationVersion;
                    if(v==3){var provenance=(CultivationGameplayComponentV3)dto;sourceId=provenance.clockConfigurationId;sourceVersion=provenance.clockConfigurationVersion;CultivationPlaytestProfile source;if(!CultivationProductionConfiguration.TryClockSource(sourceId,sourceVersion,profile,out source) || source.StepSeconds!=profile.StepSeconds){reason="Unsupported pending clock source configuration.";return false;}}
                    if(c.clockRemainder>0 && profile.IsProduction){CultivationPlaytestProfile pending;if(!CultivationProductionConfiguration.TryClockSource(sourceId,sourceVersion,profile,out pending) || !pending.HasNode(c.clockNodeId)){reason="UnknownPendingQiNode";return false;}}
                    clock=new CultivationClockState(c.clockRemainder,c.clockElapsed,c.clockNodeId,sourceId,sourceVersion);
                    dto.version=1; // Domain v1 payload is unchanged; clock schema validated separately above.
                }
                if(dto.configurationId!=profile.Id || dto.configurationVersion!=profile.Version){reason="Incompatible cultivation configuration identity/version.";return false;}
                // Durable Burning integration is deliberately unavailable: never discard/resume its debt.
                if(dto.hasBurn || dto.bloodCharges!=0 || dto.bloodProgress!=0 || dto.settledTransactions==null || dto.settledTransactions.Count!=0){reason="Blood Essence/Burning is locked in this playtest.";return false;}
                Guid owner;if(!Guid.TryParseExact(dto.ownerId,"N",out owner)){reason="Invalid cultivation owner.";return false;}
                var slot=new CultivationCandidateSlot(null);var result=slot.TryLoad(dto,profile.Catalog,profile.Rules,profile.Id,owner,Guid.NewGuid());
                if(result!=CultivationReason.None){reason="Invalid cultivation component: "+result;return false;}
                session=slot.Current;session.GameplayClock=clock;return true;
            } catch(ArgumentException e){reason="Invalid cultivation component: "+e.Message;return false;}
        }
        public static bool TryReadHeroJson(string json,CultivationPlaytestProfile profile,out string component,out string reason)
        {
            component=null;Dictionary<string,string> fields;if(!CultivationSaveJson.TryObject(json,out fields,out reason))return false;
            string token;if(!fields.TryGetValue(SaveField,out token))return true; // Actual legacy, not template data.
            if(token=="null" || !CultivationSaveJson.TryString(token,out component)){reason="Invalid cultivation component boundary.";return false;}
            CultivationSession candidate;return TryRestore(component,profile,out candidate,out reason);
        }
    }
    // One binding per canonical Hero. This type exposes snapshots, never the mutable domain session.
    public sealed class CultivationGameplayBinding : IDisposable
    {
        readonly EntityDataSO hero;readonly CultivationSession session;
        public readonly CultivationPlaytestProfile Profile;
        public bool IsDisposed {get;private set;}public CultivationSnapshot Current {get{return session.Current;}}
        public CultivationCapacity Capacity {get{return session.Capacity;}}public CultivationStepFlow LastStepFlow {get{return session.LastStepFlow;}}
        public Guid Epoch {get{return session.Epoch;}}public Guid OwnerId {get{return session.OwnerId;}}
        public long TickCount {get;private set;}public double Density {get;private set;}
        double remainder,elapsed;string pendingNode,pendingConfigurationId;int pendingConfigurationVersion;long lastFrame=-1;string published;
        public double PendingSeconds {get{return remainder;}}public double ActiveSeconds {get{return elapsed;}}
        public double ProcessedSeconds {get{return elapsed-remainder;}}
        public CultivationGameplayBinding(EntityDataSO hero,CultivationPlaytestProfile profile,CultivationSession session)
        {if(hero==null || profile==null || session==null)throw new ArgumentException();this.hero=hero;Profile=profile;this.session=session;
            if(profile.SaveVersion>=2 && session.GameplayClock!=null){remainder=session.GameplayClock.Remainder;elapsed=session.GameplayClock.Elapsed;pendingNode=session.GameplayClock.NodeId;pendingConfigurationId=session.GameplayClock.ConfigurationId;pendingConfigurationVersion=session.GameplayClock.ConfigurationVersion;}
            if(pendingConfigurationId==null){pendingConfigurationId=profile.Id;pendingConfigurationVersion=profile.Version;}
            Density=profile.Density(pendingNode??hero.worldMapId);Publish();}
        void Publish(){if(Profile.SaveVersion>=2)session.GameplayClock=new CultivationClockState(remainder,elapsed,pendingNode,pendingConfigurationId,pendingConfigurationVersion);published=CultivationGameplayState.Capture(session,Profile);hero.cultivationJson=published;hero.isDirty=true;}
        public CultivationReason Advance(EntityDataSO canonical,long frame,double dt,bool paused,string nodeId)
        {
            if(IsDisposed || canonical!=hero)return CultivationReason.OwnerMismatch;
            if(canonical.cultivationJson!=published)return CultivationReason.InvalidSnapshot;
            if(frame<0 || frame<=lastFrame)return CultivationReason.DuplicateCommand;
            if(paused){lastFrame=frame;return CultivationReason.Paused;}
            if(!CultivationNumber.NonNegative(dt) || dt>Profile.MaximumFrameSeconds)return CultivationReason.InvalidDelta;
            if(elapsed+dt>1e12)return CultivationReason.InvalidDelta;
            if(Profile.IsProduction && !string.IsNullOrEmpty(nodeId) && !Profile.HasNode(nodeId))return CultivationReason.InvalidValue;
            if(Profile.SaveVersion>=2 && (pendingNode!=nodeId || pendingConfigurationId!=Profile.Id || pendingConfigurationVersion!=Profile.Version)) {
                var settle=SettlePending();if(settle!=CultivationReason.None)return settle;
            }
            pendingNode=nodeId;pendingConfigurationId=Profile.Id;pendingConfigurationVersion=Profile.Version;Density=Profile.Density(nodeId);double available=remainder+dt;
            // Fixed configured game-time steps, no offline/catch-up scheduler. Carry substep only in this binding.
            while(available+1e-12>=Profile.StepSeconds) {
                var result=session.Tick(session.OwnerId,session.Epoch,TickCount,Profile.StepSeconds,false,Density);
                if(result!=CultivationReason.None)return result;
                TickCount++;available-=Profile.StepSeconds;if(Profile.SaveVersion==1)Publish();
            }
            remainder=Math.Max(0,available);elapsed+=dt;lastFrame=frame;
            if(Profile.SaveVersion>=2)Publish();return CultivationReason.None;
        }
        // Settle elapsed time at its original source before a validated Travel changes the node.
        public CultivationReason SettlePending()
        {
            if(IsDisposed)return CultivationReason.OwnerMismatch;
            if(hero.cultivationJson!=published)return CultivationReason.InvalidSnapshot;
            if(Profile.SaveVersion<2 || remainder==0)return CultivationReason.None;
            CultivationPlaytestProfile source;if(!CultivationProductionConfiguration.TryClockSource(pendingConfigurationId,pendingConfigurationVersion,Profile,out source))return CultivationReason.InvalidSnapshot;
            var result=session.TickFromSource(session.OwnerId,session.Epoch,TickCount,remainder,false,source.Density(pendingNode),source.Rules);
            if(result!=CultivationReason.None)return result;
            TickCount++;remainder=0;Publish();return CultivationReason.None;
        }
        public void Dispose(){IsDisposed=true;remainder=0;}
    }
}
