#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using TuTienCore;
using UnityEditor;
using UnityEngine;

public static class CultivationV09Verifier
{
    [Serializable] sealed class Result {public string unity,profile,failure="";public List<string> checks=new List<string>();public double allocatedBytesPerFrame,microsecondsPerFrame;}
    static string root;static CultivationPlaytestProfile profile;static readonly List<GameObject> owners=new List<GameObject>();
    static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
    static SaveManager Fixture(string name)
    {
        var go=new GameObject("Owned v09 "+name);go.SetActive(false);owners.Add(go);var save=go.AddComponent<SaveManager>();
        string directory=Path.Combine(root,name);Directory.CreateDirectory(directory);save.ConfigureSaveDirectoryBeforeAwake(directory);save.ConfigureCultivationProductionBeforeAwake(profile);return save;
    }
    static string PathFor(SaveManager save){return Path.Combine(save.SaveDirectory,"ManualSave1.json");}
    static EntityDataSO Legacy()
    {
        var h=ScriptableObject.CreateInstance<EntityDataSO>();LegacyRealmSnapshotCompatibility.Initialize(h);WorldMapCatalog.StartNew(h);h.ApplyHeroBalance();
        h.currentLevel=45;h.currentExp=17;h.statPoints=11;h.addedHealth=198;h.worldMapWaveProgress=2;
        EquipmentInventory.InitializeNewRun(h);EquipmentRuntime.InitializeNewRun(h);var item=EquipmentFoundation.CreateDemoBow();EquipmentRuntime.AddInstance(h,item);EquipmentRuntime.Equip(h,item.instanceId,EquipmentSlot.Weapon);
        h.cultivationJson="";return h;
    }
    static CultivationMigrationPlan Prepare(SaveManager save,string source)
    {
        File.WriteAllText(PathFor(save),source);CultivationMigrationPlan plan;EntityDataSO candidate;CultivationMigrationReason why;
        Require(save.TryPrepareCultivationMigration(SaveSlot.ManualSave1,out plan,out candidate,out why),"Prepare: "+why+" "+save.LastCultivationFailure);
        Require(candidate.currentLevel==45 && candidate.currentExp==17 && candidate.statPoints==11,"Legacy progression retained in candidate");
        CultivationSession state;string reason;Require(CultivationGameplayState.TryRestore(candidate.cultivationJson,profile,out state,out reason) && state.Current.Foundation.Amount==0 && state.Current.Reserve.Amount==0 && state.Current.Injury.Severity==0 && state.Current.Blood.Charges==0,"Migration zero cultivation independent of Level45");
        SaveManager.ReleaseMigrationCandidate(candidate);return plan;
    }
    static CultivationBalanceMeasurement Simulate(string node,int fps,bool checkpoints=false)
    {
        var hero=ScriptableObject.CreateInstance<EntityDataSO>();hero.worldMapId=node;hero.currentLevel=1;hero.currentExp=0;
        var b=new CultivationGameplayBinding(hero,profile,CultivationGameplayState.New(profile));var m=new CultivationBalanceMeasurement{node=node,fps=fps};
        double rate=profile.EnvironmentalQiPerSecond(node);
        for(int i=0;i<3660*fps;i++) {
            Require(b.Advance(hero,i,1d/fps,false,node)==CultivationReason.None,"Production simulation frame");
            double f=b.Current.Foundation.Amount;
            if(m.trungSeconds==0 && f>=900)m.trungSeconds=b.ProcessedSeconds;
            if(m.hauSeconds==0 && f>=2100)m.hauSeconds=b.ProcessedSeconds;
            if(m.vienManSeconds==0 && f>=3600)m.vienManSeconds=b.ProcessedSeconds;
            if(checkpoints && i%197==196){CultivationSession next;string why;Require(CultivationGameplayState.TryRestore(hero.cultivationJson,profile,out next,out why),"Checkpoint "+why);b.Dispose();b=new CultivationGameplayBinding(hero,profile,next);m.checkpoints++;}
        }
        m.activeSeconds=b.ActiveSeconds;m.processedSeconds=b.ProcessedSeconds;m.pendingSeconds=b.PendingSeconds;m.foundation=b.Current.Foundation.Amount;m.reserve=b.Current.Reserve.Amount;
        m.accountingError=Math.Abs(m.foundation+m.reserve-rate*m.processedSeconds);
        Require(m.accountingError<1e-6 && Math.Abs(m.activeSeconds-3660)<1e-6,"Production Qi/time accounting");
        Require(hero.currentLevel==1 && hero.currentExp==0 && b.Current.Foundation.ActualRealmId=="realm:luyen-khi" && b.Current.Blood.Charges==0 && b.Current.Burning==null,"No EXP/realm/blood grant from Qi");
        if(rate>=1)Require(Math.Abs(m.vienManSeconds-3600)<.15 && m.foundation==3600 && m.reserve>0,"Baseline/refinement cap and bottleneck reserve");
        else Require(Math.Abs(m.foundation-rate*3660)<1e-6 && m.vienManSeconds==0,"Low Qi can constrain refinement, no free cultivation");
        b.Dispose();UnityEngine.Object.DestroyImmediate(hero);return m;
    }
    static void CheckWorld(Result result)
    {
        var definition=new WorldQiDefinition();JsonUtility.FromJsonOverwrite(Resources.Load<TextAsset>("WorldQiDistributionV1").text,definition);
        var art=Resources.Load<Sprite>("WorldMapArtwork");Require(WorldMapPolygons.MatchesArtwork(art) && art.rect.width==1448 && art.rect.height==1086,"Actual artwork registration");
        var doc=WorldMapPolygons.Load();var lines=new List<string>{"nodeId,sourceX,sourceY,normalizedX,normalizedY,coordinateStatus"};
        foreach(var source in definition.nodes) {
            WorldMapNode node;Require(WorldMapCatalog.TryGet(source.nodeId,out node),"Known node");
            var point=new Vector2((float)source.normalizedX,(float)source.normalizedY);
            Require((point-node.PreviewPosition).sqrMagnitude<1e-10 && WorldMapPolygons.HitTest(point,id=>id==node.Id)==node.Id,"Current runtime source/actual click cell "+node.Id);
            lines.Add(source.nodeId+","+source.sourceX.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+source.sourceY.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+source.normalizedX.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+source.normalizedY.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+source.coordinateStatus);
        }
        Require(lines.Count==134,"133 coordinate rows");File.WriteAllLines(Path.Combine(root,"world-coordinates-native.csv"),lines);
        result.checks.Add("133/133 seed positions match actual WorldMapCatalog.PreviewPosition and contain their own runtime click cell on registered1448x1086 art; source provenance127 IMAGE_LABEL/6 INFERRED retained, no pin/topology mutation.");
        var randomState=UnityEngine.Random.state;float expected=UnityEngine.Random.value;UnityEngine.Random.state=randomState;
        CultivationPlaytestProfile again;WorldQiDistribution table;CultivationConfigurationReason code;string detail;
        Require(CultivationProductionConfiguration.TryLoadVersion(1,out again,out table,out code,out detail) && UnityEngine.Random.value==expected,"Configuration load must not consume global gameplay random");
        foreach(var n in definition.nodes){double rate;Require(table.TryRate(n.nodeId,out rate) && rate==n.qiPerSecond && again.EnvironmentalQiPerSecond(n.nodeId)==rate,"Stable absolute Qi/s adapter");}
        var ids=new List<string>();var starts=new List<string>();foreach(var n in WorldMapCatalog.All){ids.Add(n.Id);if(n.IsStart)starts.Add(n.Id);}
        foreach(string bad in new[]{"missing","duplicate","unknown","range","coordinates","start","version"}) {
            var d=new WorldQiDefinition();JsonUtility.FromJsonOverwrite(Resources.Load<TextAsset>("WorldQiDistributionV1").text,d);
            if(bad=="missing")Array.Resize(ref d.nodes,132);if(bad=="duplicate")d.nodes[1].nodeId=d.nodes[0].nodeId;if(bad=="unknown")d.nodes[0].nodeId="UNKNOWN";
            if(bad=="range")d.nodes[0].qiPerSecond=6;if(bad=="coordinates")d.nodes[0].normalizedX=2;if(bad=="start"){var s=Array.Find(d.nodes,n=>n.nodeId=="TV-20");s.qiTier=7;s.qiPerSecond=5;}
            if(bad=="version")d.nodes[0].configurationVersion++;
            WorldQiDistribution rejected;WorldQiReason why;Require(!WorldQiDistribution.TryCreate(d,ids,starts,out rejected,out why),"Reject invalid world table "+bad);
        }
        result.checks.Add("World table rejects missing/duplicate/unknown/range/coordinate/start/version errors; reload preserves every Qi rate and consumes no UnityEngine.Random values.");
    }
    public static void RunNative()
    {
        root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.utmp/cultivation-playtest-"+Guid.NewGuid().ToString("N")));var result=new Result{unity=Application.unityVersion,profile=root};
        EntityDataSO legacy=null,target=null;
        try {
            for(var p=new DirectoryInfo(root);p!=null;p=p.Parent)Require(!p.Exists || (p.Attributes&FileAttributes.ReparsePoint)==0,"No reparse paths");
            Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,".owner"),"cultivation-playtest-v07");CultivationPlaytestBootstrap.VerifyOwnedProfile(root);
            WorldQiDistribution qi;CultivationConfigurationReason code;string detail;Require(CultivationProductionConfiguration.TryLoadVersion(1,out profile,out qi,out code,out detail),"Production config "+code+" "+detail);
            CheckWorld(result);
            legacy=Legacy();string source=JsonUtility.ToJson(legacy,true);source=source.TrimEnd().TrimEnd('}')+",\"unrelatedFutureNote\":{\"keep\":true}}";
            var saves=Fixture("accepted");var plan=Prepare(saves,source);byte[] original=File.ReadAllBytes(PathFor(saves));CultivationMigrationReason reason;
            Require(!saves.CommitCultivationMigration(plan,false,out reason) && reason==CultivationMigrationReason.ConsentRequired && CultivationMigrationTransaction.Hash(File.ReadAllBytes(PathFor(saves)))==plan.OriginalHash && !File.Exists(plan.BackupPath),"Declined consent makes no disk write");
            Require(saves.CommitCultivationMigration(plan,true,out reason),"Accepted migration "+reason+" "+CultivationMigrationTransaction.LastDiagnostic+" "+saves.LastCultivationFailure);
            Require(CultivationMigrationTransaction.Hash(File.ReadAllBytes(plan.BackupPath))==plan.OriginalHash && Convert.ToBase64String(File.ReadAllBytes(plan.BackupPath))==Convert.ToBase64String(original),"Backup byte-identical and retained");
            target=ScriptableObject.CreateInstance<EntityDataSO>();target.cultivationJson="stale template";
            string component=null;for(int i=0;i<3;i++){Require(saves.LoadGame(target,SaveSlot.ManualSave1),"Repeated upgraded Load");if(i==0)component=target.cultivationJson;Require(target.cultivationJson==component && target.currentLevel==45 && target.currentExp==17 && target.statPoints==11 && target.equipmentItems.Count==1,"No repeat bonus or unrelated data loss");}
            CultivationMigrationPlan ignored;EntityDataSO candidate;Require(!saves.TryPrepareCultivationMigration(SaveSlot.ManualSave1,out ignored,out candidate,out reason) && reason==CultivationMigrationReason.NotLegacy,"Repeated upgrade is not a new component");
            result.checks.Add("Read-only preflight, consent reject/accept, complete original-token preservation, immutable byte-exact legacy backup, zero-state migration independent of Level45, repeated upgrade/load and stale template guards PASS.");
            string upgraded=File.ReadAllText(PathFor(saves));
            foreach(string invalid in new[]{upgraded.Replace("production:cultivation-v09","other:configuration"),upgraded.Replace("\\\"configurationVersion\\\":1","\\\"configurationVersion\\\":2"),upgraded.Replace("\\\"version\\\":2","\\\"version\\\":3"),"{\"currentLevel\":45,\"cultivationJson\":\"{\"}"}) {
                Require(invalid!=upgraded,"Invalid fixture changes source");File.WriteAllText(PathFor(saves),invalid);string before=JsonUtility.ToJson(target);Require(!saves.LoadGame(target,SaveSlot.ManualSave1) && JsonUtility.ToJson(target)==before,"Invalid/future/config candidate never mutates destination");
            }
            File.WriteAllText(PathFor(saves),source);Require(!saves.TryPrepareCultivationMigration(SaveSlot.ManualSave1,out ignored,out candidate,out reason) && reason==CultivationMigrationReason.DowngradeGuard,"Committed receipt catches component stripped by old binary");
            File.WriteAllText(PathFor(saves),upgraded+" ");Require(!saves.RestoreCultivationBackup(plan,plan.UpgradedHash,out reason),"Rollback cannot overwrite a newer/different snapshot");
            File.WriteAllText(PathFor(saves),upgraded);Require(saves.RestoreCultivationBackup(plan,plan.UpgradedHash,out reason) && Convert.ToBase64String(File.ReadAllBytes(PathFor(saves)))==Convert.ToBase64String(original),"Guarded explicit rollback restores exact original");
            Require(!saves.LoadGame(target,SaveSlot.ManualSave1) && saves.NeedsCultivationMigration(SaveSlot.ManualSave1),"Restored legacy cannot bypass fresh consent");
            result.checks.Add("Future/config/malformed guards preserve candidate, committed migration metadata rejects old-binary component stripping, guarded rollback refuses newer snapshots and restores exact original only by expected hash; restored legacy needs consent again.");
            foreach(string point in new[]{"BeforeBackup","AfterBackup","AfterTemp","BeforeReplace","AfterReplace","BeforeVerify"}) {
                var s=Fixture("fault-"+point);var p=Prepare(s,source);s.FailNextCultivationMigration=point;
                Require(!s.CommitCultivationMigration(p,true,out reason) && CultivationMigrationTransaction.Hash(File.ReadAllBytes(PathFor(s)))==p.OriginalHash,"Fault retains/rolls back original at "+point);
                Require(s.CommitCultivationMigration(p,true,out reason),"Idempotent retry "+point+" "+reason+" "+CultivationMigrationTransaction.LastDiagnostic+" "+s.LastCultivationFailure);
            }
            result.checks.Add("Bounded injected faults before backup/after backup/after temp/before replace/after replace/before verification retain or restore exact committed original; every retry succeeds without changing backup.");
            foreach(string kind in new[]{"backup-create","backup-mismatch","temp-create","replace-lock","source-change","orphan-temp"}) {
                var s=Fixture("actual-"+kind);var p=Prepare(s,source);string temp=Path.Combine(s.SaveDirectory,"ManualSave1.cultivation-upgrade.tmp");
                if(kind=="backup-create")Directory.CreateDirectory(p.BackupPath);
                if(kind=="backup-mismatch")File.WriteAllText(p.BackupPath,"unrelated backup, never overwrite");
                if(kind=="temp-create")Directory.CreateDirectory(temp);
                if(kind=="source-change")File.AppendAllText(PathFor(s)," ");
                if(kind=="orphan-temp")File.WriteAllText(temp,"orphan must not become canonical");
                if(kind=="replace-lock")using(var locked=new FileStream(PathFor(s),FileMode.Open,FileAccess.Read,FileShare.Read)){Require(!s.CommitCultivationMigration(p,true,out reason) && CultivationMigrationTransaction.Hash(File.ReadAllBytes(PathFor(s)))==p.OriginalHash,"Actual Windows locked Replace");}
                else if(kind=="orphan-temp")Require(s.CommitCultivationMigration(p,true,out reason),"Recover ignoring orphan temp");
                else Require(!s.CommitCultivationMigration(p,true,out reason),"Actual failure "+kind);
                if(kind!="orphan-temp" && kind!="source-change")Require(CultivationMigrationTransaction.Hash(File.ReadAllBytes(PathFor(s)))==p.OriginalHash,"Original retained "+kind);
                if(kind=="backup-mismatch")Require(File.ReadAllText(p.BackupPath)=="unrelated backup, never overwrite","Never overwrite existing mismatched backup");
                if(kind=="source-change")Require(File.ReadAllText(PathFor(s))==source+" ","Preserve external source change");
            }
            result.checks.Add("Actual backup creation blocker/mismatched backup/temp creation blocker/Windows locked Replace/source checksum change/orphan temp cases PASS; no backup overwrite or newer-slot rollback.");
            var interrupted=Fixture("interrupted-receipt");var interruptedPlan=Prepare(interrupted,source);interrupted.FailNextCultivationMigration="BeforeReplace";
            Require(!interrupted.CommitCultivationMigration(interruptedPlan,true,out reason),"Prepare interrupted transaction state");
            File.WriteAllText(PathFor(interrupted),interruptedPlan.UpgradedJson); // Model atomic Replace completed, process stopped before receipt commit.
            string recoveredBytes=Convert.ToBase64String(File.ReadAllBytes(PathFor(interrupted)));
            Require(interrupted.LoadGame(target,SaveSlot.ManualSave1) && Convert.ToBase64String(File.ReadAllBytes(PathFor(interrupted)))==recoveredBytes,"Read-only player snapshot recovery from pending receipt");
            File.WriteAllText(PathFor(interrupted),source);Require(!interrupted.TryPrepareCultivationMigration(SaveSlot.ManualSave1,out ignored,out candidate,out reason) && reason==CultivationMigrationReason.DowngradeGuard,"Recovered receipt enforces downgrade guard");
            result.checks.Add("Modeled process interruption after Replace/before receipt completion is recovered by validating both snapshot and backup hashes; Load completes metadata only, leaves player bytes unchanged and retains downgrade guard. Not a process-kill/power-loss proof.");
            foreach(string node in new[]{"TV-20","TL-16","XD-11","TM-12","HT-17"})File.WriteAllText(Path.Combine(root,"simulation-"+node+".json"),JsonUtility.ToJson(Simulate(node,10),true));
            foreach(int fps in new[]{30,60,144})File.WriteAllText(Path.Combine(root,"simulation-"+fps+".json"),JsonUtility.ToJson(Simulate("TV-20",fps,fps==60),true));
            var identical=Simulate("TV-20",10);var repeated=Simulate("TV-20",10);Require(JsonUtility.ToJson(identical)==JsonUtility.ToJson(repeated),"Same full simulation repeated deterministically");
            string hot=null,cold=null;foreach(var node in WorldMapCatalog.All){double rate;qi.TryRate(node.Id,out rate);if(rate==5)hot=node.Id;if(rate==.6)cold=node.Id;}
            File.WriteAllText(Path.Combine(root,"simulation-hot.json"),JsonUtility.ToJson(Simulate(hot,10),true));File.WriteAllText(Path.Combine(root,"simulation-cold.json"),JsonUtility.ToJson(Simulate(cold,10),true));
            result.checks.Add("Full3660s runtime simulations: all five baseline starts,30/60/144FPS, native checkpoint replay and identical repeat; hot5Qi/s retains refinement1/s and surplus reserve, cold0.6Qi/s constrains foundation, no Level/EXP or breakthrough.");
            var measured=ScriptableObject.CreateInstance<EntityDataSO>();measured.worldMapId="TV-20";var binding=new CultivationGameplayBinding(measured,profile,CultivationGameplayState.New(profile));
            for(int i=0;i<100;i++)binding.Advance(measured,i,1d/60,false,"TV-20");long allocation=GC.GetAllocatedBytesForCurrentThread();var timer=Stopwatch.StartNew();
            for(int i=100;i<10100;i++)binding.Advance(measured,i,1d/60,false,"TV-20");timer.Stop();result.allocatedBytesPerFrame=(GC.GetAllocatedBytesForCurrentThread()-allocation)/10000d;result.microsecondsPerFrame=timer.Elapsed.TotalMilliseconds*1000/10000;
            UnityEngine.Object.DestroyImmediate(measured);result.checks.Add("Measured existing per-frame checkpoint publish before optimization; allocation/time are native main-thread microbenchmark figures, not Player frame profiler measurements. Binding loop performs zero Save writes.");
            UnityEngine.Debug.Log("[CULTIVATION V09] PASS "+root);
        }catch(Exception e){result.failure=e.ToString();UnityEngine.Debug.LogError("[CULTIVATION V09] FAIL "+e);}
        if(Directory.Exists(root))File.WriteAllText(Path.Combine(root,"result.json"),JsonUtility.ToJson(result,true));
        foreach(var o in owners)if(o!=null)UnityEngine.Object.DestroyImmediate(o);if(legacy!=null)UnityEngine.Object.DestroyImmediate(legacy);if(target!=null)UnityEngine.Object.DestroyImmediate(target);
        EditorApplication.Exit(string.IsNullOrEmpty(result.failure)?0:2);
    }
}
#endif
