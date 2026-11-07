using System;
using System.Collections.Generic;
namespace TuTienCore
{
    [Serializable] public sealed class HeavenlyGateConfiguration
    {
        public int schemaVersion,version,hp,damagePerStrike,receiptLifetimeSteps;
        public string id,title,cultivationConfiguration,sourceRealm,targetRealm,penaltyPolicyId,phase;
        public double strikeInterval,targetRange,armor,magicResistance;
        public bool IsFixture=>id!=null&&id.StartsWith("fixture:",StringComparison.Ordinal);
        public bool IsValid=>schemaVersion==1&&version>0&&IsFixture&&!string.IsNullOrWhiteSpace(title)&&!string.IsNullOrWhiteSpace(cultivationConfiguration)&&!string.IsNullOrWhiteSpace(sourceRealm)&&!string.IsNullOrWhiteSpace(targetRealm)&&sourceRealm!=targetRealm&&!string.IsNullOrWhiteSpace(penaltyPolicyId)&&phase=="SealCombat"&&hp>0&&damagePerStrike>0&&receiptLifetimeSteps>=0&&receiptLifetimeSteps<=120&&CultivationNumber.NonNegative(strikeInterval)&&strikeInterval>=.05&&strikeInterval<=60&&CultivationNumber.NonNegative(targetRange)&&targetRange>0&&CultivationNumber.NonNegative(armor)&&CultivationNumber.NonNegative(magicResistance);
        public HeavenlyGateConfiguration Copy()=>new HeavenlyGateConfiguration{schemaVersion=schemaVersion,version=version,id=id,title=title,cultivationConfiguration=cultivationConfiguration,sourceRealm=sourceRealm,targetRealm=targetRealm,penaltyPolicyId=penaltyPolicyId,phase=phase,hp=hp,damagePerStrike=damagePerStrike,receiptLifetimeSteps=receiptLifetimeSteps,strikeInterval=strikeInterval,targetRange=targetRange,armor=armor,magicResistance=magicResistance};
    }
    public enum HeavenlyGateOutcome { Fighting, Success, Failure }
    // The approved v1.3 seal fight replaces no historical v1.2 two-phase fixtures.
    public sealed class HeavenlyGateEncounter
    {
        public readonly Guid EncounterId,AttemptId,Owner,Epoch,TargetId;public readonly int HeroLife;public readonly long RealmRevision;
        public readonly string SourceRealm;readonly CultivationSession session;readonly HeavenlyGateConfiguration config;readonly HashSet<Guid> receipts=new HashSet<Guid>();
        public HeavenlyGateOutcome Outcome {get;private set;}public long Step {get;private set;}public double Seconds {get;private set;}
        public HeavenlyGateEncounter(CultivationSession session,HeavenlyGateConfiguration config,Guid encounter,Guid attempt,Guid target,int life)
        {
            if(session==null||config==null||!config.IsValid||life<=0||encounter==Guid.Empty||attempt==Guid.Empty||target==Guid.Empty||session.CanAttemptBreakthrough()!=CultivationReason.None||session.Current.Foundation.ActualRealmId!=config.sourceRealm)throw new ArgumentException("Eligible actual session and complete explicit fixture configuration required");
            CultivationRealmDefinition source,destination;if(!session.Realms.TryGet(config.sourceRealm,out source)||!session.Realms.TryGet(config.targetRealm,out destination)||destination.CombatTier<=source.CombatTier)throw new ArgumentException("Known ascending successor required");
            this.session=session;this.config=config.Copy();EncounterId=encounter;AttemptId=attempt;TargetId=target;Owner=session.OwnerId;Epoch=session.Epoch;HeroLife=life;RealmRevision=session.RealmRevision;SourceRealm=config.sourceRealm;
        }
        public TribulationReason Advance(long step,double dt,bool paused)
        {
            if(Outcome!=HeavenlyGateOutcome.Fighting)return TribulationReason.Terminal;if(paused)return TribulationReason.Paused;
            if(step<=Step)return TribulationReason.Duplicate;if(!CultivationNumber.NonNegative(dt)||dt>1)return TribulationReason.InvalidValue;Step=step;Seconds+=dt;return TribulationReason.None;
        }
        public TribulationReason Accept(Guid owner,Guid epoch,Guid attempt,Guid target,int life,Guid eventId,long step,int heroHP,int gateHP)
        {
            if(Outcome!=HeavenlyGateOutcome.Fighting)return TribulationReason.Terminal;
            if(owner!=Owner||epoch!=Epoch||attempt!=AttemptId||target!=TargetId||life!=HeroLife||session.RealmRevision!=RealmRevision||session.Current.Foundation.ActualRealmId!=SourceRealm)return TribulationReason.Stale;
            if(step>Step||step<Step-config.receiptLifetimeSteps)return TribulationReason.Stale;if(eventId==Guid.Empty||heroHP<0||gateHP<0)return TribulationReason.InvalidValue;if(!receipts.Add(eventId))return TribulationReason.Duplicate;
            if(heroHP==0)Outcome=HeavenlyGateOutcome.Failure;else if(gateHP==0)Outcome=HeavenlyGateOutcome.Success;return TribulationReason.None;
        }
    }
}
