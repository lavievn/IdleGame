using System;
using System.Collections.Generic;

namespace TuTienCore
{
    public sealed class BossEncounterDefinition
    {
        public string Id {get;private set;}
        public int Revision {get;private set;}
        public string NodeId {get;private set;}
        public BossEncounterDefinition(string id,int revision,string nodeId) {Id=id;Revision=revision;NodeId=nodeId;}
    }
    public enum BossEntryReason { None, MissingRequest, UnknownNode, NotBoss, InvalidOrigin, NotAdjacent, CurrentNodeNotCleared, MissingDefinition, InvalidDefinition, PermissionDenied, CallerNotReady }
    public sealed class BossEntryRequest
    {
        public readonly string OriginNodeId,TargetNodeId;
        public readonly bool CurrentNodeCleared,EntryPermission,CallerReady;
        public readonly BossEncounterDefinition Definition;
        // Permission and readiness represent policies owned by the caller, not inferred balance.
        public BossEntryRequest(string origin,string target,bool cleared,bool permission,bool ready,BossEncounterDefinition definition)
        {OriginNodeId=origin;TargetNodeId=target;CurrentNodeCleared=cleared;EntryPermission=permission;CallerReady=ready;Definition=definition;}
    }
    public sealed class BossEntryPermit
    {
        public string OriginNodeId {get;private set;}
        public string BossNodeId {get;private set;}
        public BossEncounterDefinition Definition {get;private set;}
        internal BossEntryPermit(BossEntryRequest request) {OriginNodeId=request.OriginNodeId;BossNodeId=request.TargetNodeId;Definition=request.Definition;}
    }
    public sealed class BossEntryResult
    {
        public BossEntryReason Reason {get;private set;}
        public BossEntryPermit Permit {get;private set;}
        public bool Allowed {get{return Reason==BossEntryReason.None && Permit!=null;}}
        internal BossEntryResult(BossEntryReason reason,BossEntryRequest request)
        {Reason=reason;if(reason==BossEntryReason.None)Permit=new BossEntryPermit(request);}
    }
    // Snapshot of existing world nodes, with no Resources load, Hero or map mutation.
    public sealed class BossEncounterGate
    {
        sealed class Node {public bool Boss;public HashSet<string> Links;}
        readonly Dictionary<string,Node> nodes=new Dictionary<string,Node>(StringComparer.Ordinal);
        public BossEncounterGate(IEnumerable<WorldMapNode> worldNodes)
        {
            if(worldNodes==null)throw new ArgumentNullException("worldNodes");
            foreach(var n in worldNodes) {
                if(n==null || string.IsNullOrWhiteSpace(n.Id) || n.Id!=n.Id.Trim() || nodes.ContainsKey(n.Id))
                    throw new ArgumentException("World nodes must have unique, exact IDs.","worldNodes");
                nodes.Add(n.Id,new Node {Boss=n.IsBoss,Links=new HashSet<string>(n.Links,StringComparer.Ordinal)});
            }
        }
        public BossEntryResult Evaluate(BossEntryRequest request)
        {
            var reason=BossEntryReason.None;Node origin=null,target=null;
            if(request==null)reason=BossEntryReason.MissingRequest;
            else if(request.OriginNodeId==null || request.TargetNodeId==null || !nodes.TryGetValue(request.OriginNodeId,out origin) || !nodes.TryGetValue(request.TargetNodeId,out target))reason=BossEntryReason.UnknownNode;
            else if(!target.Boss)reason=BossEntryReason.NotBoss;
            else if(origin.Boss)reason=BossEntryReason.InvalidOrigin;
            else if(!origin.Links.Contains(request.TargetNodeId))reason=BossEntryReason.NotAdjacent;
            else if(!request.CurrentNodeCleared)reason=BossEntryReason.CurrentNodeNotCleared;
            else if(request.Definition==null)reason=BossEntryReason.MissingDefinition;
            else if(string.IsNullOrWhiteSpace(request.Definition.Id) || request.Definition.Id!=request.Definition.Id.Trim() || request.Definition.Revision<1 || request.Definition.NodeId!=request.TargetNodeId)reason=BossEntryReason.InvalidDefinition;
            else if(!request.EntryPermission)reason=BossEntryReason.PermissionDenied;
            else if(!request.CallerReady)reason=BossEntryReason.CallerNotReady;
            return new BossEntryResult(reason,request);
        }
    }
    public enum BossEncounterState { Idle, Entry, Fighting, Resolving, Won, Lost, Cancelled }
    public enum BossEncounterOutcome { Won, Lost }
    public enum BossCoordinatorReason { None, InvalidIdentity, EntryBlocked, InvalidState, DuplicateAttempt, StaleCallback, AlreadyTerminal, ResolutionNotAuthorized, RetryNotAuthorized, RetryDefinitionMismatch }

    // In-memory state only. No callback grants rewards, changes map or marks a save paid.
    public sealed class BossEncounterCoordinator
    {
        readonly HashSet<Guid> usedAttempts=new HashSet<Guid>();
        BossEncounterOutcome pendingOutcome;
        public BossEncounterState State {get;private set;}
        public Guid EncounterId {get;private set;}
        public Guid AttemptId {get;private set;}
        public BossEntryPermit Permit {get;private set;}
        public int TerminalTransitionCount {get;private set;}
        public bool IsTerminal {get{return State==BossEncounterState.Won || State==BossEncounterState.Lost || State==BossEncounterState.Cancelled;}}
        bool Match(Guid encounter,Guid attempt,out BossCoordinatorReason reason)
        {
            reason=encounter==Guid.Empty || attempt==Guid.Empty?BossCoordinatorReason.InvalidIdentity:
                encounter!=EncounterId || attempt!=AttemptId?BossCoordinatorReason.StaleCallback:BossCoordinatorReason.None;
            return reason==BossCoordinatorReason.None;
        }
        void Enter(Guid encounter,Guid attempt,BossEntryPermit permit)
        {EncounterId=encounter;AttemptId=attempt;Permit=permit;usedAttempts.Add(attempt);State=BossEncounterState.Entry;}
        public bool TryEnter(Guid encounter,Guid attempt,BossEntryResult entry,out BossCoordinatorReason reason)
        {
            reason=encounter==Guid.Empty || attempt==Guid.Empty?BossCoordinatorReason.InvalidIdentity:
                State!=BossEncounterState.Idle?BossCoordinatorReason.InvalidState:
                entry==null || !entry.Allowed?BossCoordinatorReason.EntryBlocked:BossCoordinatorReason.None;
            if(reason!=BossCoordinatorReason.None)return false;Enter(encounter,attempt,entry.Permit);return true;
        }
        public bool TryStartFighting(Guid encounter,Guid attempt,out BossCoordinatorReason reason)
        {
            if(!Match(encounter,attempt,out reason))return false;
            if(State!=BossEncounterState.Entry) {reason=BossCoordinatorReason.InvalidState;return false;}
            State=BossEncounterState.Fighting;return true;
        }
        public bool TryResolve(Guid encounter,Guid attempt,BossEncounterOutcome outcome,out BossCoordinatorReason reason)
        {
            if(!Match(encounter,attempt,out reason))return false;
            if(IsTerminal) {reason=BossCoordinatorReason.AlreadyTerminal;return false;}
            if(State!=BossEncounterState.Fighting || !Enum.IsDefined(typeof(BossEncounterOutcome),outcome)) {reason=BossCoordinatorReason.InvalidState;return false;}
            pendingOutcome=outcome;State=BossEncounterState.Resolving;return true;
        }
        // Explicit caller acknowledgement; fixture true is NOT evidence of a durable save commit.
        public bool TryCompleteResolution(Guid encounter,Guid attempt,bool resolutionAuthorized,out BossCoordinatorReason reason)
        {
            if(!Match(encounter,attempt,out reason))return false;
            if(IsTerminal) {reason=BossCoordinatorReason.AlreadyTerminal;return false;}
            if(State!=BossEncounterState.Resolving) {reason=BossCoordinatorReason.InvalidState;return false;}
            if(!resolutionAuthorized) {reason=BossCoordinatorReason.ResolutionNotAuthorized;return false;}
            State=pendingOutcome==BossEncounterOutcome.Won?BossEncounterState.Won:BossEncounterState.Lost;
            TerminalTransitionCount++;return true;
        }
        public bool TryCancel(Guid encounter,Guid attempt,out BossCoordinatorReason reason)
        {
            if(!Match(encounter,attempt,out reason))return false;
            if(IsTerminal) {reason=BossCoordinatorReason.AlreadyTerminal;return false;}
            if(State==BossEncounterState.Idle) {reason=BossCoordinatorReason.InvalidState;return false;}
            State=BossEncounterState.Cancelled;TerminalTransitionCount++;return true;
        }
        public bool TryRetry(Guid encounter,Guid previousAttempt,Guid newAttempt,bool retryAuthorized,BossEntryResult freshEntry,out BossCoordinatorReason reason)
        {
            if(!Match(encounter,previousAttempt,out reason))return false;
            if(State!=BossEncounterState.Lost && State!=BossEncounterState.Cancelled)reason=BossCoordinatorReason.InvalidState;
            else if(!retryAuthorized)reason=BossCoordinatorReason.RetryNotAuthorized;
            else if(newAttempt==Guid.Empty)reason=BossCoordinatorReason.InvalidIdentity;
            else if(usedAttempts.Contains(newAttempt))reason=BossCoordinatorReason.DuplicateAttempt;
            else if(freshEntry==null || !freshEntry.Allowed)reason=BossCoordinatorReason.EntryBlocked;
            else if(freshEntry.Permit.BossNodeId!=Permit.BossNodeId || freshEntry.Permit.Definition.Id!=Permit.Definition.Id || freshEntry.Permit.Definition.Revision!=Permit.Definition.Revision)reason=BossCoordinatorReason.RetryDefinitionMismatch;
            if(reason!=BossCoordinatorReason.None)return false;
            Enter(encounter,newAttempt,freshEntry.Permit);return true;
        }
    }
}
