using System;
using System.Collections.Generic;

namespace TuTienCore
{
    public enum RealmMinorStage { Invalid, SoKy, TrungKy, HauKy, VienMan }
    public enum RealmProgressionReason { None, UnknownRealm, InvalidEntryLevel, InvalidTotalLevel, TotalBeforeEntry }

    public sealed class RealmProgressionResult
    {
        public string RealmId { get; private set; }
        public int InternalLevel { get; private set; }
        public RealmMinorStage MinorStage { get; private set; }
        public RealmProgressionReason Reason { get; private set; }
        public bool IsValid { get { return Reason == RealmProgressionReason.None; } }
        public bool AtBottleneck { get { return IsValid && InternalLevel >= 30; } }
        // Progress only: does not assert a successor, trial success, cost or permission.
        public bool ProgressEligibleForBreakthrough { get { return AtBottleneck; } }
        internal RealmProgressionResult(string id,int level,RealmMinorStage stage,RealmProgressionReason reason)
        { RealmId=id;InternalLevel=level;MinorStage=stage;Reason=reason; }
    }

    // No built-in official realm catalog exists yet. Callers supply explicit IDs;
    // RankDatabase's legacy level mapping is deliberately not consulted.
    public sealed class LegacyLevelRealmProgressionEvaluator
    {
        // Mirrors the current EntityDataSO.AddExp limit; does not change it.
        public const int CurrentLevelCap = 999;
        private readonly HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        public LegacyLevelRealmProgressionEvaluator(IEnumerable<string> realmIds)
        {
            if(realmIds==null)throw new ArgumentNullException("realmIds");
            foreach(string id in realmIds)
                if(string.IsNullOrWhiteSpace(id) || id!=id.Trim() || !ids.Add(id))
                    throw new ArgumentException("Realm IDs must be nonempty, exact and unique.","realmIds");
            if(ids.Count==0)throw new ArgumentException("An explicit realm catalog is required.","realmIds");
        }
        public RealmProgressionResult Evaluate(string realmId,int realmEntryLevel,int totalLevel)
        {
            var reason=RealmProgressionReason.None;
            if(realmId==null || !ids.Contains(realmId))reason=RealmProgressionReason.UnknownRealm;
            else if(realmEntryLevel<1 || realmEntryLevel>CurrentLevelCap)reason=RealmProgressionReason.InvalidEntryLevel;
            else if(totalLevel<1 || totalLevel>CurrentLevelCap)reason=RealmProgressionReason.InvalidTotalLevel;
            else if(totalLevel<realmEntryLevel)reason=RealmProgressionReason.TotalBeforeEntry;
            if(reason!=RealmProgressionReason.None)
                return new RealmProgressionResult(realmId,0,RealmMinorStage.Invalid,reason);
            // Both operands are validated before subtraction. Never modulo or advance ID.
            int level=totalLevel-realmEntryLevel+1;
            var stage=level>=30?RealmMinorStage.VienMan:level>=20?RealmMinorStage.HauKy:
                level>=10?RealmMinorStage.TrungKy:RealmMinorStage.SoKy;
            return new RealmProgressionResult(realmId,level,stage,RealmProgressionReason.None);
        }
    }
}
