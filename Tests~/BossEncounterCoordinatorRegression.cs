using System;
using System.Collections.Generic;
using System.IO;
using TuTienCore;
using UnityEngine;

partial class MotionRegression
{
    static BossEncounterGate BossFixtureGate()
    {
        Resources.Register("WorldMapCatalog",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapCatalog.txt")));
        Resources.Register("WorldMapPolygonSeeds",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapPolygonSeeds.txt")));
        return new BossEncounterGate(WorldMapCatalog.All);
    }
    static BossEntryResult BossFixturePermit(BossEncounterGate gate,string bossId="TV-39")
    {WorldMapNode node;Check(WorldMapCatalog.TryGet(bossId,out node),"existing boss");return gate.Evaluate(new BossEntryRequest(node.Links[0],bossId,true,true,true,new BossEncounterDefinition("fixture:boss:"+bossId,1,bossId)));}
    static string WorldCatalogSnapshot()
    {string s="";foreach(var n in WorldMapCatalog.All)s+=n.Id+"|"+n.IsBoss+"|"+n.SuggestedLevel+"|"+string.Join(",",n.Links.ToArray())+"\n";return s;}
    static void BossEncounterCoordinatorTests()
    {
        Run("v05 boss gate recognizes exactly six existing bosses without opening production travel",()=>{
            var gate=BossFixtureGate();string before=WorldCatalogSnapshot();var recognized=new HashSet<string>();
            foreach(var n in WorldMapCatalog.All) {
                if(n.IsBoss) {
                    Check(BossFixturePermit(gate,n.Id).Allowed,"fixture permission for boss "+n.Id);recognized.Add(n.Id);
                    var hero=HeroAt(1);WorldMapCatalog.StartNew(hero);hero.worldMapId=n.Links[0];hero.worldMapCleared.Add(n.Links[0]);
                    Check(WorldMapCatalog.TravelBlockReason(hero,n.Id)!=null,"production gate remains blocked");
                } else {
                    var result=gate.Evaluate(new BossEntryRequest(n.Id,n.Id,true,true,true,null));
                    Check(result.Reason==BossEntryReason.NotBoss,"normal node not a boss "+n.Id);
                }
            }
            Check(WorldMapCatalog.All.Count==133 && recognized.SetEquals(new[]{"TV-39","TL-02","XD-03","HT-01","HT-27","TM-25"}),"exact catalog flags");
            Check(WorldCatalogSnapshot()==before,"world topology metadata unchanged");
        });
        Run("v05 boss gate blocks missing spec adjacency clear permission and invalid inputs",()=>{
            var gate=BossFixtureGate();WorldMapNode boss;WorldMapCatalog.TryGet("TV-39",out boss);string origin=boss.Links[0];
            var d=new BossEncounterDefinition("fixture:boss",1,boss.Id);
            Check(gate.Evaluate(null).Reason==BossEntryReason.MissingRequest,"missing request");
            Check(gate.Evaluate(new BossEntryRequest("unknown",boss.Id,true,true,true,d)).Reason==BossEntryReason.UnknownNode,"unknown node");
            Check(gate.Evaluate(new BossEntryRequest("TV-20",boss.Id,true,true,true,d)).Reason==BossEntryReason.NotAdjacent,"not adjacent");
            Check(gate.Evaluate(new BossEntryRequest(origin,boss.Id,false,true,true,d)).Reason==BossEntryReason.CurrentNodeNotCleared,"not cleared");
            Check(gate.Evaluate(new BossEntryRequest(origin,boss.Id,true,true,true,null)).Reason==BossEntryReason.MissingDefinition,"no boss spec");
            Check(gate.Evaluate(new BossEntryRequest(origin,boss.Id,true,false,true,d)).Reason==BossEntryReason.PermissionDenied,"explicit permission denied");
            Check(gate.Evaluate(new BossEntryRequest(origin,boss.Id,true,true,false,d)).Reason==BossEntryReason.CallerNotReady,"busy/dead caller");
            Check(gate.Evaluate(new BossEntryRequest(origin,boss.Id,true,true,true,new BossEncounterDefinition("fixture:bad",0,boss.Id))).Reason==BossEntryReason.InvalidDefinition,"bad revision");
        });
        Run("v05 boss entry fighting resolving and terminal acknowledgement do not grant rewards",()=>{
            var c=new BossEncounterCoordinator();var entry=BossFixturePermit(BossFixtureGate());var encounter=Guid.NewGuid();var attempt=Guid.NewGuid();BossCoordinatorReason reason;
            Check(c.TryEnter(encounter,attempt,entry,out reason) && c.State==BossEncounterState.Entry,"entry");
            Check(!c.TryCompleteResolution(encounter,attempt,true,out reason) && c.State==BossEncounterState.Entry,"cannot skip combat");
            Check(c.TryStartFighting(encounter,attempt,out reason),"fighting");
            Check(c.TryResolve(encounter,attempt,BossEncounterOutcome.Won,out reason) && c.State==BossEncounterState.Resolving,"resolve does not auto-win");
            Check(!c.TryCompleteResolution(encounter,attempt,false,out reason) && reason==BossCoordinatorReason.ResolutionNotAuthorized && c.TerminalTransitionCount==0,"no implicit commit");
            Check(c.TryCompleteResolution(encounter,attempt,true,out reason) && c.State==BossEncounterState.Won && c.TerminalTransitionCount==1,"logical terminal exactly once");
            Check(!c.TryResolve(encounter,attempt,BossEncounterOutcome.Lost,out reason) && reason==BossCoordinatorReason.AlreadyTerminal,"late loss rejected");
            Check(!c.TryCompleteResolution(encounter,attempt,true,out reason) && c.TerminalTransitionCount==1,"duplicate win ack rejected");
            Check(!c.TryCancel(encounter,attempt,out reason) && c.State==BossEncounterState.Won,"won cannot be cancelled");
        });
        Run("v05 boss loss cancellation and explicit retry reject stale or reused attempt callbacks",()=>{
            var gate=BossFixtureGate();var entry=BossFixturePermit(gate);var c=new BossEncounterCoordinator();BossCoordinatorReason reason;
            var encounter=Guid.NewGuid();var old=Guid.NewGuid();Check(c.TryEnter(encounter,old,entry,out reason),"enter");
            Check(c.TryStartFighting(encounter,old,out reason) && c.TryResolve(encounter,old,BossEncounterOutcome.Lost,out reason) && c.TryCompleteResolution(encounter,old,true,out reason),"loss");
            Check(!c.TryCompleteResolution(encounter,old,true,out reason) && c.TerminalTransitionCount==1,"duplicate loss");
            var next=Guid.NewGuid();Check(!c.TryRetry(encounter,old,next,false,entry,out reason) && reason==BossCoordinatorReason.RetryNotAuthorized,"retry policy explicit");
            Check(!c.TryRetry(encounter,old,old,true,entry,out reason) && reason==BossCoordinatorReason.DuplicateAttempt,"identity cannot be reused");
            Check(c.TryRetry(encounter,old,next,true,entry,out reason) && c.State==BossEncounterState.Entry,"new attempt");
            Check(!c.TryResolve(encounter,old,BossEncounterOutcome.Won,out reason) && reason==BossCoordinatorReason.StaleCallback,"old attempt callback");
            Check(!c.TryCancel(Guid.NewGuid(),next,out reason) && reason==BossCoordinatorReason.StaleCallback,"other encounter callback");
            Check(c.TryCancel(encounter,next,out reason) && c.State==BossEncounterState.Cancelled && c.TerminalTransitionCount==2,"cancel terminal");
            Check(!c.TryCancel(encounter,next,out reason) && c.TerminalTransitionCount==2,"duplicate cancel");
            Check(c.TryRetry(encounter,next,Guid.NewGuid(),true,entry,out reason),"cancelled may retry only explicitly");
        });
        Run("v05 boss gate snapshots node inputs and invalid transitions do not mutate world or wave",()=>{
            var origin=new WorldMapNode {Id="fixture-origin"};var boss=new WorldMapNode {Id="fixture-boss",IsBoss=true};origin.Links.Add(boss.Id);
            var gate=new BossEncounterGate(new[]{origin,boss});origin.Links.Clear();boss.IsBoss=false;
            var permit=gate.Evaluate(new BossEntryRequest(origin.Id,"fixture-boss",true,true,true,new BossEncounterDefinition("fixture:boss",1,"fixture-boss")));
            Check(permit.Allowed,"snapshot owns flags and links");
            var c=new BossEncounterCoordinator();BossCoordinatorReason reason;var e=Guid.NewGuid();var a=Guid.NewGuid();
            Check(!c.TryEnter(Guid.Empty,a,permit,out reason) && c.State==BossEncounterState.Idle,"empty identity blocked");
            Check(!c.TryEnter(e,a,null,out reason) && c.State==BossEncounterState.Idle,"no gate authorization");
            Check(c.TryEnter(e,a,permit,out reason),"valid explicit entry");
            Check(!c.TryResolve(e,a,BossEncounterOutcome.Won,out reason) && c.State==BossEncounterState.Entry,"no phase skip");
            var hero=HeroAt(1);int wave=hero.completedWavesInMap,level=hero.currentLevel,exp=hero.currentExp;
            Check(c.TryCancel(e,a,out reason),"cancel");
            Check(hero.completedWavesInMap==wave && hero.currentLevel==level && hero.currentExp==exp,"no wave EXP or level side effects");
        });
    }
}
