using System;
using System.Collections.Generic;

namespace TuTienCore
{
    public sealed class LegacyLevelRealmProgressionState
    {
        public string RealmId { get; private set; }
        public int EntryLevel { get; private set; }
        public int TotalLevel { get; private set; }
        public int Experience { get; private set; }
        public LegacyLevelRealmProgressionState(string realmId,int entryLevel,int totalLevel,int experience)
        { RealmId=realmId;EntryLevel=entryLevel;TotalLevel=totalLevel;Experience=experience; }
    }
    public enum RealmTransitionReason
    { None, InvalidAttempt, AlreadyApplied, StaleRevision, ProgressNotReady, MissingSuccessor, UnknownSuccessor, SameRealm }

    // Plain-data, in-memory transition session. This is not a trial simulator or
    // a durable ledger. A future adapter must preserve session identity until cancel/load.
    public sealed class LegacyLevelRealmBreakthroughTransition
    {
        private readonly LegacyLevelRealmProgressionEvaluator evaluator;
        private readonly HashSet<Guid> applied = new HashSet<Guid>();
        public LegacyLevelRealmProgressionState Current { get; private set; }
        public long Revision { get; private set; }
        public LegacyLevelRealmBreakthroughTransition(LegacyLevelRealmProgressionEvaluator evaluator,LegacyLevelRealmProgressionState initial)
        {
            if(evaluator==null)throw new ArgumentNullException("evaluator");
            if(initial==null || initial.Experience<0 || !evaluator.Evaluate(initial.RealmId,initial.EntryLevel,initial.TotalLevel).IsValid)
                throw new ArgumentException("Initial realm state must be valid.","initial");
            this.evaluator=evaluator;Current=initial;
        }
        // Called only after an external authority explicitly confirms success.
        // ProgressEligible does not call this method and no successor is inferred.
        public bool TryApplySuccess(Guid attemptId,long expectedRevision,string explicitSuccessor,out RealmTransitionReason reason)
        {
            reason=RealmTransitionReason.None;
            if(attemptId==Guid.Empty)reason=RealmTransitionReason.InvalidAttempt;
            else if(applied.Contains(attemptId))reason=RealmTransitionReason.AlreadyApplied;
            else if(expectedRevision!=Revision)reason=RealmTransitionReason.StaleRevision;
            else if(!evaluator.Evaluate(Current.RealmId,Current.EntryLevel,Current.TotalLevel).ProgressEligibleForBreakthrough)
                reason=RealmTransitionReason.ProgressNotReady;
            else if(string.IsNullOrWhiteSpace(explicitSuccessor))reason=RealmTransitionReason.MissingSuccessor;
            else if(explicitSuccessor==Current.RealmId)reason=RealmTransitionReason.SameRealm;
            else if(!evaluator.Evaluate(explicitSuccessor,Current.TotalLevel,Current.TotalLevel).IsValid)
                reason=RealmTransitionReason.UnknownSuccessor;
            if(reason!=RealmTransitionReason.None)return false;
            Current=new LegacyLevelRealmProgressionState(explicitSuccessor,Current.TotalLevel,Current.TotalLevel,Current.Experience);
            applied.Add(attemptId);Revision++;
            return true;
        }
    }
}
