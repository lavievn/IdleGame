using System;
namespace TuTienCore
{
    public sealed class HeavenlyGatePenaltyPolicy
    {
        public readonly string Id;public readonly int Version;public readonly double RequestedLoss;
        public HeavenlyGatePenaltyPolicy(string id,int version,double requestedLoss){if(id==null||!(id.StartsWith("fixture:",StringComparison.Ordinal)||id=="development:gate-penalty-015")||version<1||!CultivationNumber.NonNegative(requestedLoss)||requestedLoss<=0)throw new ArgumentException("Explicit versioned gate penalty required.");Id=id;Version=version;RequestedLoss=requestedLoss;}
        public double Loss(CultivationRealmEvaluator catalog,CultivationSnapshot state)
        {
            var actual=catalog.Evaluate(state.Foundation.ActualRealmId,state.Foundation.Amount);CultivationRealmDefinition realm;if(!actual.IsValid||!catalog.TryGet(actual.ActualRealmId,out realm))throw new ArgumentException("Actual realm required");
            int stage=(int)actual.MinorStage-1,maxDrop=actual.CombatTier<2?1:2;
            return Math.Min(RequestedLoss,Math.Max(0,state.Foundation.Amount-realm.Thresholds[Math.Max(0,stage-maxDrop)]));
        }
    }
    public sealed partial class CultivationSession
    {
        internal CultivationReason ApplyGatePenalty(Guid owner,Guid epoch,Guid command,long revision,string realm,HeavenlyGatePenaltyPolicy policy)
        {
            var reason=Command(owner,epoch,command);if(reason!=CultivationReason.None)return reason;
            if(revision!=RealmRevision||Current.Foundation.ActualRealmId!=realm)return CultivationReason.StaleAttempt;
            reason=CanAttemptBreakthrough();if(reason!=CultivationReason.None)return reason;if(policy==null)return CultivationReason.MissingPolicy;
            double loss=policy.Loss(Realms,Current);if(!Commit(Copy(foundation:Current.Foundation.Amount-loss)))return CultivationReason.InvalidSnapshot;
            commands.Add(command);RealmRevision++;return CultivationReason.None;
        }
    }
}
