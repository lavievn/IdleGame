#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
namespace TuTienCore
{
    // Explicit fixture-only bridge. No subscription, attack timer, HP write or new damage calculation.
    public sealed class TribulationFixtureCombatBridge
    {
        readonly HeroCombatState actor;readonly EntityDataSO data;readonly int lifeVersion;
        public readonly HeavenlyTribulationAttempt Attempt;
        public bool IsCurrent=>actor.Data==data&&data!=null&&actor.LifeVersion==lifeVersion;
        public TribulationFixtureCombatBridge(HeavenlyTribulationAttempt attempt,HeroCombatState actor)
        {
            if(attempt==null||actor==null||actor.Data==null||actor.LifeVersion<=0||actor.CurrentHP<=0||actor.MaxHP<=0||actor.CurrentHP>actor.MaxHP)throw new ArgumentException("Living fixture actor lifetime required.");
            Attempt=attempt;this.actor=actor;data=actor.Data;lifeVersion=actor.LifeVersion;
        }
        public TribulationReason ObserveAccepted(Guid eventId,Guid targetLife,TribulationPhase phase,int acceptedDamage,bool paused)
        {
            if(!IsCurrent)return TribulationReason.Stale;
            return Attempt.Observe(new TribulationCombatReceipt(Attempt.Owner,Attempt.Epoch,Attempt.Id,eventId,Attempt.HeroLife,targetLife,phase,acceptedDamage,actor.CurrentHP,actor.MaxHP),paused);
        }
        public CultivationReason CommitSuccess(bool paused)
        {
            if(!IsCurrent)return CultivationReason.StaleAttempt;
            return Attempt.CommitFixtureSuccess(Attempt.Owner,Attempt.Epoch,Attempt.Id,Attempt.HeroLife,actor.CurrentHP,actor.MaxHP,paused);
        }
    }
}
#endif
