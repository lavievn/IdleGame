using System;
using System.Collections.Generic;
namespace TuTienCore
{
    public enum TribulationPhase { Endurance, Damage, Resolving, Committed, Failed, Cancelled }
    public enum TribulationReason { None, Locked, MissingPolicy, InvalidIdentity, InvalidValue, Ineligible, Stale, Duplicate, WrongPhase, Paused, NotReady, Terminal, Overflow }
    public enum TribulationCancelCause { Load, NewGame, Retry, Travel, Menu, Disabled, Explicit }
    // Explicit test values, never a Resources asset/default balance or production approval.
    public sealed class TribulationFixturePolicy
    {
        public readonly string Id,Successor;public readonly double EnduranceSeconds,DamageDeadline,RequiredDamage,MaximumStep,FoundationAfterSuccess;
        public TribulationFixturePolicy(string id,string successor,double enduranceSeconds,double damageDeadline,double requiredDamage,double maximumStep,double foundationAfterSuccess)
        {
            if(id==null||!id.StartsWith("fixture:",StringComparison.Ordinal)||string.IsNullOrWhiteSpace(successor)||
                !CultivationNumber.NonNegative(enduranceSeconds)||enduranceSeconds<=0||!CultivationNumber.NonNegative(damageDeadline)||damageDeadline<=0||
                !CultivationNumber.NonNegative(requiredDamage)||requiredDamage<=0||!CultivationNumber.NonNegative(maximumStep)||maximumStep<=0||!CultivationNumber.NonNegative(foundationAfterSuccess))throw new ArgumentException("Complete explicit fixture policy required.");
            Id=id;Successor=successor;EnduranceSeconds=enduranceSeconds;DamageDeadline=damageDeadline;RequiredDamage=requiredDamage;MaximumStep=maximumStep;FoundationAfterSuccess=foundationAfterSuccess;
        }
    }
    // Accepted damage/HP after the existing combat pipeline; no alternate damage formula.
    // A future combat adapter must bind these values to the actual actor lifetimes.
    public sealed class TribulationCombatReceipt
    {
        public readonly Guid Owner,Epoch,Attempt,Event,HeroLife,TargetLife;
        public readonly TribulationPhase Phase;public readonly int AcceptedDamage,CurrentHP,MaximumHP;
        public TribulationCombatReceipt(Guid owner,Guid epoch,Guid attempt,Guid eventId,Guid heroLife,Guid targetLife,TribulationPhase phase,int acceptedDamage,int currentHP,int maximumHP)
        {Owner=owner;Epoch=epoch;Attempt=attempt;Event=eventId;HeroLife=heroLife;TargetLife=targetLife;Phase=phase;AcceptedDamage=acceptedDamage;CurrentHP=currentHP;MaximumHP=maximumHP;}
    }
    // One in-memory fixture attempt. No scheduler, Save field, cost, reward, RNG or failure penalty.
    public sealed class HeavenlyTribulationAttempt
    {
        readonly CultivationSession session;readonly TribulationFixturePolicy policy;
        readonly string actualRealm;readonly long revision;
        readonly HashSet<Guid> receipts=new HashSet<Guid>();long lastStep=-1;
        public readonly Guid Owner,Epoch,Id,HeroLife,TargetLife;
        public TribulationPhase Phase {get;private set;}
        public double PhaseSeconds {get;private set;}public double AcceptedIncoming {get;private set;}public double AcceptedOutgoing {get;private set;}
        public TribulationCancelCause? CancelCause {get;private set;}
        public bool IsTerminal=>Phase==TribulationPhase.Committed||Phase==TribulationPhase.Failed||Phase==TribulationPhase.Cancelled;
        HeavenlyTribulationAttempt(CultivationSession session,TribulationFixturePolicy policy,Guid id,Guid life,Guid target)
        {this.session=session;this.policy=policy;Owner=session.OwnerId;Epoch=session.Epoch;Id=id;HeroLife=life;TargetLife=target;actualRealm=session.Current.Foundation.ActualRealmId;revision=session.RealmRevision;Phase=TribulationPhase.Endurance;}
        public static TribulationReason TryCreateProduction(out HeavenlyTribulationAttempt attempt)
        {attempt=null;return TribulationReason.Locked;}
        public static TribulationReason TryCreateFixture(CultivationSession session,TribulationFixturePolicy policy,Guid id,Guid heroLife,Guid targetLife,out HeavenlyTribulationAttempt attempt)
        {
            attempt=null;if(policy==null)return TribulationReason.MissingPolicy;
            if(session==null||id==Guid.Empty||heroLife==Guid.Empty||targetLife==Guid.Empty||heroLife==targetLife)return TribulationReason.InvalidIdentity;
            if(session.CanAttemptBreakthrough()!=CultivationReason.None)return TribulationReason.Ineligible;
            CultivationRealmDefinition source,target;
            if(!session.Realms.TryGet(session.Current.Foundation.ActualRealmId,out source)||!session.Realms.TryGet(policy.Successor,out target)||target.CombatTier<=source.CombatTier||!session.Realms.Evaluate(policy.Successor,policy.FoundationAfterSuccess).IsValid)return TribulationReason.MissingPolicy;
            attempt=new HeavenlyTribulationAttempt(session,policy,id,heroLife,targetLife);return TribulationReason.None;
        }
        TribulationReason Check(Guid owner,Guid epoch,Guid attempt,Guid life)
        {
            if(owner!=Owner||epoch!=Epoch||attempt!=Id||life!=HeroLife)return TribulationReason.Stale;
            if(IsTerminal)return TribulationReason.Terminal;
            if(session.RealmRevision!=revision||session.Current.Foundation.ActualRealmId!=actualRealm)return TribulationReason.Stale;
            if(session.CanAttemptBreakthrough()!=CultivationReason.None)return TribulationReason.Ineligible;
            return TribulationReason.None;
        }
        public TribulationReason Advance(Guid owner,Guid epoch,Guid attempt,Guid life,long step,double dt,bool paused)
        {
            var reason=Check(owner,epoch,attempt,life);if(reason!=TribulationReason.None)return reason;
            if(paused)return TribulationReason.Paused;
            if(Phase!=TribulationPhase.Endurance&&Phase!=TribulationPhase.Damage)return TribulationReason.WrongPhase;
            if(step<0||step<=lastStep)return TribulationReason.Duplicate;
            if(!CultivationNumber.NonNegative(dt)||dt>policy.MaximumStep)return TribulationReason.InvalidValue;
            double next=PhaseSeconds+dt;if(!CultivationNumber.NonNegative(next))return TribulationReason.Overflow;
            PhaseSeconds=next;lastStep=step;
            // Fixture deadline is exclusive: at/after deadline fails. Production policy remains undecided.
            if(Phase==TribulationPhase.Damage&&PhaseSeconds>=policy.DamageDeadline)Phase=TribulationPhase.Failed;
            return TribulationReason.None;
        }
        public TribulationReason Observe(TribulationCombatReceipt receipt,bool paused)
        {
            if(receipt==null)return TribulationReason.InvalidValue;
            var reason=Check(receipt.Owner,receipt.Epoch,receipt.Attempt,receipt.HeroLife);if(reason!=TribulationReason.None)return reason;
            if(paused)return TribulationReason.Paused;
            if(receipt.Phase!=Phase||(Phase!=TribulationPhase.Endurance&&Phase!=TribulationPhase.Damage))return TribulationReason.WrongPhase;
            if(receipt.TargetLife!=TargetLife||receipt.Event==Guid.Empty)return TribulationReason.InvalidIdentity;
            if(receipts.Contains(receipt.Event))return TribulationReason.Duplicate;
            if(receipt.AcceptedDamage<0||receipt.MaximumHP<=0||receipt.CurrentHP<0||receipt.CurrentHP>receipt.MaximumHP)return TribulationReason.InvalidValue;
            double total=(Phase==TribulationPhase.Endurance?AcceptedIncoming:AcceptedOutgoing)+receipt.AcceptedDamage;
            if(!CultivationNumber.NonNegative(total))return TribulationReason.Overflow;
            receipts.Add(receipt.Event);
            if(Phase==TribulationPhase.Endurance)AcceptedIncoming=total;else AcceptedOutgoing=total;
            if(receipt.CurrentHP==0)Phase=TribulationPhase.Failed;
            return TribulationReason.None;
        }
        public TribulationReason CompletePhase(Guid owner,Guid epoch,Guid attempt,Guid life,int currentHP,int maximumHP,bool paused)
        {
            var reason=Check(owner,epoch,attempt,life);if(reason!=TribulationReason.None)return reason;
            if(paused)return TribulationReason.Paused;
            if(currentHP<=0||maximumHP<=0||currentHP>maximumHP)return TribulationReason.InvalidValue;
            if(Phase==TribulationPhase.Endurance){if(PhaseSeconds<policy.EnduranceSeconds)return TribulationReason.NotReady;Phase=TribulationPhase.Damage;PhaseSeconds=0;return TribulationReason.None;}
            if(Phase==TribulationPhase.Damage){if(PhaseSeconds>=policy.DamageDeadline||AcceptedOutgoing<policy.RequiredDamage)return TribulationReason.NotReady;Phase=TribulationPhase.Resolving;return TribulationReason.None;}
            return TribulationReason.WrongPhase;
        }
        // In-memory fixture success only. Never claims a durable Save transaction.
        public CultivationReason CommitFixtureSuccess(Guid owner,Guid epoch,Guid attempt,Guid life,int currentHP,int maximumHP,bool paused)
        {
            var reason=Check(owner,epoch,attempt,life);if(reason!=TribulationReason.None)return reason==TribulationReason.Terminal?CultivationReason.DuplicateCommand:CultivationReason.StaleAttempt;
            if(paused)return CultivationReason.Paused;
            if(currentHP<=0||maximumHP<=0||currentHP>maximumHP)return CultivationReason.HpCondition;
            if(Phase!=TribulationPhase.Resolving)return CultivationReason.ProgressNotReady;
            var applied=new RealmBreakthroughTransition(session).ApplySuccess(Owner,Epoch,Id,revision,actualRealm,policy.Successor,policy.FoundationAfterSuccess);
            if(applied==CultivationReason.None)Phase=TribulationPhase.Committed;return applied;
        }
        public TribulationReason Cancel(Guid owner,Guid epoch,Guid attempt,Guid life,TribulationCancelCause cause)
        {
            // Cancellation must remain possible even if progress/injury changed while the fixture was running.
            if(owner!=Owner||epoch!=Epoch||attempt!=Id||life!=HeroLife)return TribulationReason.Stale;
            if(IsTerminal)return TribulationReason.Terminal;if(!Enum.IsDefined(typeof(TribulationCancelCause),cause))return TribulationReason.InvalidValue;
            Phase=TribulationPhase.Cancelled;CancelCause=cause;return TribulationReason.None;
        }
    }
}
