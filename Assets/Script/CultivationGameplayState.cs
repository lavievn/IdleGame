using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace TuTienCore
{
    public static class CultivationGameplayState
    {
        public const string SaveField="cultivationJson";
        public static string Capture(CultivationSession session,CultivationPlaytestProfile profile)
        {var dto=CultivationComponentCodec.Capture(session,profile.Id);dto.configurationVersion=profile.Version;return JsonUtility.ToJson(dto,false);}
        public static CultivationSession New(CultivationPlaytestProfile p)
        {return new CultivationSession(Guid.NewGuid(),Guid.NewGuid(),p.Catalog,p.Rules,new CultivationSnapshot(new CultivationFoundation(p.StartingRealm,0),new UnrefinedQiReserve(0),new SpiritualEnergy(0),new BloodEssenceState(0,0),new FoundationInjuryState(0)));}
        public static bool TryRestore(string component,CultivationPlaytestProfile profile,out CultivationSession session,out string reason)
        {
            session=null;reason=null;if(string.IsNullOrEmpty(component))return true;
            Dictionary<string,string> fields;if(!CultivationSaveJson.TryObject(component,out fields,out reason))return false;
            string version;int v;if(!fields.TryGetValue("version",out version) || !int.TryParse(version,out v) || v!=CultivationComponentCodec.ProposedVersion){reason="Unsupported cultivation component version.";return false;}
            if(profile==null){reason="Cultivation playtest configuration is not enabled in this Save context.";return false;}
            // Every authority field must be present with its exact scalar type; no defaults from JsonUtility/template.
            foreach(var f in typeof(CultivationComponentDto).GetFields()) {
                string token;if(!fields.TryGetValue(f.Name,out token)){reason="Missing cultivation field: "+f.Name;return false;}
                int integer;double number;string text;
                if((f.FieldType==typeof(int) && !int.TryParse(token,NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out integer)) ||
                    (f.FieldType==typeof(double) && (!double.TryParse(token,NumberStyles.Float,CultureInfo.InvariantCulture,out number) || !CultivationNumber.NonNegative(number))) ||
                    (f.FieldType==typeof(bool) && token!="true" && token!="false") ||
                    (f.FieldType==typeof(string) && token!="null" && !CultivationSaveJson.TryString(token,out text)) ||
                    (f.FieldType==typeof(List<string>) && !token.StartsWith("[",StringComparison.Ordinal))) {reason="Invalid cultivation field type/value: "+f.Name;return false;}
            }
            if(fields.Count!=typeof(CultivationComponentDto).GetFields().Length){reason="Unknown cultivation fields require an explicit component version.";return false;}
            try {
                var dto=new CultivationComponentDto();JsonUtility.FromJsonOverwrite(component,dto);
                if(dto.configurationId!=profile.Id || dto.configurationVersion!=profile.Version){reason="Incompatible cultivation configuration identity/version.";return false;}
                // Durable Burning integration is deliberately unavailable: never discard/resume its debt.
                if(dto.hasBurn || dto.bloodCharges!=0 || dto.bloodProgress!=0 || dto.settledTransactions==null || dto.settledTransactions.Count!=0){reason="Blood Essence/Burning is locked in this playtest.";return false;}
                Guid owner;if(!Guid.TryParseExact(dto.ownerId,"N",out owner)){reason="Invalid cultivation owner.";return false;}
                var slot=new CultivationCandidateSlot(null);var result=slot.TryLoad(dto,profile.Catalog,profile.Rules,profile.Id,owner,Guid.NewGuid());
                if(result!=CultivationReason.None){reason="Invalid cultivation component: "+result;return false;}
                session=slot.Current;return true;
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
        public Guid Epoch {get{return session.Epoch;}}public Guid OwnerId {get{return session.OwnerId;}}
        public long TickCount {get;private set;}public double Density {get;private set;}
        double remainder;long lastFrame=-1;string published;
        public CultivationGameplayBinding(EntityDataSO hero,CultivationPlaytestProfile profile,CultivationSession session)
        {if(hero==null || profile==null || session==null)throw new ArgumentException();this.hero=hero;Profile=profile;this.session=session;Publish();}
        void Publish(){published=CultivationGameplayState.Capture(session,Profile);hero.cultivationJson=published;hero.isDirty=true;}
        public CultivationReason Advance(EntityDataSO canonical,long frame,double dt,bool paused,string nodeId)
        {
            if(IsDisposed || canonical!=hero)return CultivationReason.OwnerMismatch;
            if(canonical.cultivationJson!=published)return CultivationReason.InvalidSnapshot;
            if(frame<0 || frame<=lastFrame)return CultivationReason.DuplicateCommand;
            if(paused){lastFrame=frame;return CultivationReason.Paused;}
            if(!CultivationNumber.NonNegative(dt) || dt>Profile.MaximumFrameSeconds)return CultivationReason.InvalidDelta;
            Density=Profile.Density(nodeId);double available=remainder+dt;
            // Fixed configured game-time steps, no offline/catch-up scheduler. Carry substep only in this binding.
            while(available+1e-12>=Profile.StepSeconds) {
                var result=session.Tick(session.OwnerId,session.Epoch,TickCount,Profile.StepSeconds,false,Density);
                if(result!=CultivationReason.None)return result;
                TickCount++;available-=Profile.StepSeconds;Publish();
            }
            remainder=Math.Max(0,available);lastFrame=frame;return CultivationReason.None;
        }
        public void Dispose(){IsDisposed=true;remainder=0;}
    }
}
