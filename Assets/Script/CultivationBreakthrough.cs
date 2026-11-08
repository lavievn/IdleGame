using System;

namespace TuTienCore
{
    public sealed partial class CultivationSession
    {
        internal CultivationReason ApplyBreakthrough(Guid owner,Guid epoch,Guid successEvent,long expectedRealmRevision,string expectedActualRealm,string explicitSuccessor,double foundationAfterSuccess)
        {
            var reason=Command(owner,epoch,successEvent);if(reason!=CultivationReason.None)return reason;
            if(expectedRealmRevision!=RealmRevision || expectedActualRealm!=Current.Foundation.ActualRealmId)return CultivationReason.StaleAttempt;
            reason=CanAttemptBreakthrough();if(reason!=CultivationReason.None)return reason;
            CultivationRealmDefinition target;if(string.IsNullOrWhiteSpace(explicitSuccessor) || explicitSuccessor==expectedActualRealm || !Realms.TryGet(explicitSuccessor,out target))return CultivationReason.NoSuccessor;
            CultivationRealmDefinition actual;Realms.TryGet(expectedActualRealm,out actual);
            if(target.CombatTier<=actual.CombatTier)return CultivationReason.NoSuccessor;
            if(!Realms.Evaluate(explicitSuccessor,foundationAfterSuccess).IsValid)return CultivationReason.InvalidValue;
            // Post-success foundation is explicit transfer policy, not an inferred balance rule.
            if(!Commit(Copy(foundation:foundationAfterSuccess,realmId:explicitSuccessor)))return CultivationReason.InvalidSnapshot;
            commands.Add(successEvent);RealmRevision++;return CultivationReason.None;
        }
    }
    // v2: success events + actual foundation/thresholds; no Level or EXP input.
    public sealed class RealmBreakthroughTransition
    {
        readonly CultivationSession session;
        public RealmBreakthroughTransition(CultivationSession session) {if(session==null)throw new ArgumentNullException("session");this.session=session;}
        public CultivationReason ApplySuccess(Guid owner,Guid epoch,Guid successEvent,long expectedRealmRevision,string expectedActualRealm,string explicitSuccessor,double foundationAfterSuccess)
        {return session.ApplyBreakthrough(owner,epoch,successEvent,expectedRealmRevision,expectedActualRealm,explicitSuccessor,foundationAfterSuccess);}
    }
}
