#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using TuTienCore;
public static class WorldCultivationBalanceV10Verifier
{
    [Serializable] sealed class Point {public double seconds,foundation,reserve;}
    [Serializable] sealed class Measurement {public string model,node;public int fps,checkpoints;public double qi,capacity,trungSeconds,hauSeconds,vienManSeconds,foundation60,reserve60,foundation,reserve,activeSeconds,accountingError;public List<Point> timeline=new List<Point>();}
    [Serializable] sealed class Result {public string unity,profile,failure="",activation="V2 READY - ACTIVATION BLOCKED",worldSha256,modelSha256;public List<string> checks=new List<string>();public List<Measurement> simulations=new List<Measurement>();}
    static string root;static WorldQiDefinition world;static CultivationRefinementCatalogV10 models;static string policyHash;
    static readonly List<UnityEngine.Object> objects=new List<UnityEngine.Object>();
    static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
    static string Hash(string text)=>CultivationMigrationTransaction.Hash(System.Text.Encoding.UTF8.GetBytes(text.Replace("\r\n","\n").TrimStart('\uFEFF').TrimEnd()+"\n"));
    static CultivationPlaytestProfile Profile(CultivationRefinementModelV10 model,string node,double capacity=1)
    {
        var d=JsonUtility.FromJson<CultivationProductionDefinition>(Resources.Load<TextAsset>("CultivationProductionV1").text).cultivation;
        d.id="production:fixture-v10-"+model.id+"-"+policyHash.Substring(0,12)+"-capacity-"+capacity.ToString("R",CultureInfo.InvariantCulture);d.version=models.version;
        d.refinementRate=CultivationRefinementModelsV10.ResolveInputCapacity(model,Array.Find(world.nodes,n=>n.nodeId==node).qiPerSecond,capacity);
        d.qiSources=new CultivationQiSource[world.nodes.Length];for(int i=0;i<world.nodes.Length;i++)d.qiSources[i]=new CultivationQiSource{nodeId=world.nodes[i].nodeId,density=world.nodes[i].qiPerSecond/2};
        CultivationPlaytestProfile profile;string reason;Require(CultivationPlaytestProfile.TryCreate(d,id=>{WorldMapNode n;return WorldMapCatalog.TryGet(id,out n);},out profile,out reason,true),"Explicit offline policy profile "+reason);return profile;
    }
    static CultivationGameplayBinding Restore(EntityDataSO hero,CultivationGameplayBinding old,CultivationPlaytestProfile profile)
    {
        string component=hero.cultivationJson;CultivationSession state;string why;Require(CultivationGameplayState.TryRestore(component,profile,out state,out why),"Native checkpoint "+why);
        var next=new CultivationGameplayBinding(hero,profile,state);Require(next.Epoch!=old.Epoch && next.OwnerId==old.OwnerId && hero.cultivationJson==component,"Fresh epoch/exact checkpoint without grants");old.Dispose();return next;
    }
    static Measurement Simulate(CultivationRefinementModelV10 model,string node,int fps,double characterCapacity=1)
    {
        var hero=ScriptableObject.CreateInstance<EntityDataSO>();hero.currentLevel=1;hero.currentExp=0;hero.worldMapId=node;
        var profile=Profile(model,node,characterCapacity);var b=new CultivationGameplayBinding(hero,profile,CultivationGameplayState.New(profile));var m=new Measurement{model=model.id+(characterCapacity==1?"":"-capacity"+characterCapacity),node=node,fps=fps,qi=profile.EnvironmentalQiPerSecond(node),capacity=profile.Rules.RefinementRate};double nextPoint=60;
        try{
            for(int frame=0;frame<3660*fps;frame++){
                Require(b.Advance(hero,frame,1d/fps,false,node)==CultivationReason.None,"Simulation accepted frame");double f=b.Current.Foundation.Amount,t=b.ProcessedSeconds;
                if(m.trungSeconds==0 && f>=900)m.trungSeconds=t;if(m.hauSeconds==0 && f>=2100)m.hauSeconds=t;if(m.vienManSeconds==0 && f>=3600)m.vienManSeconds=t;
                if(t+1e-7>=nextPoint){m.timeline.Add(new Point{seconds=t,foundation=f,reserve=b.Current.Reserve.Amount});if(nextPoint==3600){m.foundation60=f;m.reserve60=b.Current.Reserve.Amount;}nextPoint+=60;}
                if(fps==60 && frame%997==996){b=Restore(hero,b,profile);m.checkpoints++;}
            }
            m.activeSeconds=b.ActiveSeconds;m.foundation=b.Current.Foundation.Amount;m.reserve=b.Current.Reserve.Amount;m.accountingError=Math.Abs(m.foundation+m.reserve-m.qi*b.ProcessedSeconds);
            Require(m.accountingError<1e-6 && Math.Abs(m.activeSeconds-3660)<1e-6,"Full simulation Qi/time conservation");
            Require(Math.Abs(m.foundation-Math.Min(3600,Math.Min(m.qi,m.capacity)*b.ProcessedSeconds))<1e-6,"Same domain pipeline matches capacity/input bound");
            Require(hero.currentLevel==1 && hero.currentExp==0 && b.Current.Foundation.ActualRealmId=="realm:luyen-khi" && b.Current.Blood.Charges==0 && b.Current.Burning==null,"No EXP/realm/blood/bonus from source");
            if(m.qi>=1)Require(m.vienManSeconds>0 && m.vienManSeconds<=3600.15,"Start/high source reaches cap without automatic breakthrough");
            else Require(m.vienManSeconds==0 && m.reserve<1e-6,"Low source constrained, no free reserve");
            return m;
        }finally{b.Dispose();UnityEngine.Object.DestroyImmediate(hero);}
    }
    static void Clock(CultivationRefinementModelV10 model)
    {
        string start=Array.Find(world.nodes,n=>n.nodeId=="TV-20").nodeId,hot=Array.Find(world.nodes,n=>n.qiPerSecond==5).nodeId;
        var h=ScriptableObject.CreateInstance<EntityDataSO>();h.worldMapId=start;var p=Profile(model,start);var b=new CultivationGameplayBinding(h,p,CultivationGameplayState.New(p));
        try{
            Require(b.Advance(h,0,.037,false,start)==CultivationReason.None,"Fractional initial frame");string paused=h.cultivationJson;
            Require(b.Advance(h,1,.2,true,start)==CultivationReason.Paused && h.cultivationJson==paused,"Pause no award/checkpoint mutation");
            var old=b;b=Restore(h,b,p);Require(Math.Abs(b.PendingSeconds-.037)<1e-12 && old.Advance(h,2,.1,false,start)==CultivationReason.OwnerMismatch,"Remainder retained; stale callback retired");
            double paid=b.Current.Foundation.Amount+b.Current.Reserve.Amount+p.EnvironmentalQiPerSecond(start)*b.PendingSeconds;
            Require(b.SettlePending()==CultivationReason.None && Math.Abs(b.Current.Foundation.Amount+b.Current.Reserve.Amount-paid)<1e-9,"Old-source settlement before rule/source change");
            h.worldMapId=hot;p=Profile(model,hot);b=Restore(h,b,p);
            Require(b.Advance(h,3,.063,false,hot)==CultivationReason.None && b.Advance(h,4,.037,false,hot)==CultivationReason.None,"New-source fixed substep");
            Require(Math.Abs(b.Current.Foundation.Amount+b.Current.Reserve.Amount-(.037+.1*5))<1e-9,"Travel old/new Qi attribution without duplicate time");
        }finally{b.Dispose();UnityEngine.Object.DestroyImmediate(h);}
    }
    static void Compatibility(CultivationPlaytestProfile production,WorldQiDistribution table)
    {
        var raw=File.ReadAllText("Tools/data/CultivationProductionV2.proposal.json");var d=JsonUtility.FromJson<CultivationProductionDefinition>(raw);
        Require(d.worldQiSha256==Hash(File.ReadAllText("Tools/data/WorldQiDistributionV2.json")),"V2 proposal fingerprint");CultivationPlaytestProfile p2;CultivationConfigurationReason code;string detail;
        Require(CultivationProductionConfiguration.TryCreate(d,world,table,id=>{WorldMapNode n;return WorldMapCatalog.TryGet(id,out n);},out p2,out code,out detail),"Owned v2 config validation "+code+" "+detail);
        var owner=new GameObject("Inactive owned v1 compatibility");owner.SetActive(false);objects.Add(owner);var saves=owner.AddComponent<SaveManager>();string path=Path.Combine(root,"compat");Directory.CreateDirectory(path);saves.ConfigureSaveDirectoryBeforeAwake(path);saves.ConfigureCultivationProductionBeforeAwake(production);
        var h=ScriptableObject.CreateInstance<EntityDataSO>();objects.Add(h);LegacyRealmSnapshotCompatibility.Initialize(h);WorldMapCatalog.StartNew(h);h.ApplyHeroBalance();EquipmentInventory.InitializeNewRun(h);EquipmentRuntime.InitializeNewRun(h);
        var b=new CultivationGameplayBinding(h,production,CultivationGameplayState.New(production));Require(b.Advance(h,0,.037,false,h.worldMapId)==CultivationReason.None,"V1 fractional fixture");
        Require(saves.SaveGame(h,SaveSlot.ManualSave1),"Owned v1 Save");string file=Path.Combine(path,"ManualSave1.json"),bytes=Convert.ToBase64String(File.ReadAllBytes(file));var loaded=ScriptableObject.CreateInstance<EntityDataSO>();objects.Add(loaded);
        Require(saves.LoadGame(loaded,SaveSlot.ManualSave1) && loaded.cultivationJson==h.cultivationJson,"Runtime v1 Save still loads unchanged");
        CultivationSession rejected;string reason;Require(!CultivationGameplayState.TryRestore(h.cultivationJson,p2,out rejected,out reason),"No silent v1-to-v2 retag");
        h.cultivationJson=CultivationGameplayState.Capture(CultivationGameplayState.New(p2),p2);Require(!saves.SaveGame(h,SaveSlot.ManualSave1) && Convert.ToBase64String(File.ReadAllBytes(file))==bytes,"V1 writer rejects v2 instead of overwriting supported slot");b.Dispose();
    }
    public static void RunNative()
    {
        root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.utmp/cultivation-playtest-"+Guid.NewGuid().ToString("N")));var result=new Result{unity=Application.unityVersion,profile=root};
        string v1=File.ReadAllText("Assets/Resources/WorldQiDistributionV1.json"),config1=File.ReadAllText("Assets/Resources/CultivationProductionV1.json");
        try{
            for(var d=new DirectoryInfo(root);d!=null;d=d.Parent)Require(!d.Exists || (d.Attributes&FileAttributes.ReparsePoint)==0,"Owned root no reparse");Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,".owner"),"cultivation-playtest-v07");CultivationPlaytestBootstrap.VerifyOwnedProfile(root);
            string text=File.ReadAllText("Tools/data/WorldQiDistributionV2.json"),policies=File.ReadAllText("Tools/data/CultivationRefinementModelsV1.json");world=JsonUtility.FromJson<WorldQiDefinition>(text);models=JsonUtility.FromJson<CultivationRefinementCatalogV10>(policies);result.worldSha256=Hash(text);result.modelSha256=Hash(policies);policyHash=Hash(text+policies);
            Require(world.id=="world-qi:atlas-v2" && world.version==2 && world.generatorVersion==2 && models.schemaVersion==1 && models.version==1 && models.models.Length==3 && models.worldQiId==world.id && models.worldQiVersion==2,"Explicit fixture policy/data identities");
            var ids=new List<string>();var starts=new List<string>();foreach(var n in WorldMapCatalog.All){ids.Add(n.Id);if(n.IsStart)starts.Add(n.Id);}
            WorldQiDistribution table;WorldQiReason reason;Require(WorldQiDistribution.TryCreate(world,ids,starts,out table,out reason),"V2 native validation "+reason);
            foreach(var n in world.nodes){WorldMapNode actual;Require(WorldMapCatalog.TryGet(n.nodeId,out actual) && Math.Abs(actual.PreviewPosition.x-n.normalizedX)<1e-6 && Math.Abs(actual.PreviewPosition.y-n.normalizedY)<1e-6,"Actual133-node coordinate preservation");}
            int hotspots=0;var regions=new HashSet<string>();foreach(var n in world.nodes)if(n.qiTier==7){hotspots++;WorldMapNode actual;WorldMapCatalog.TryGet(n.nodeId,out actual);regions.Add(actual.Continent);}Require(hotspots==4 && regions.Count==3,"Four reviewed proposed hotspots across three catalog regions");
            for(int i=0;i<world.nodes.Length;i++)for(int j=i+1;j<world.nodes.Length;j++){var a=world.nodes[i];var b=world.nodes[j];double dx=a.normalizedX-b.normalizedX,dy=a.normalizedY-b.normalizedY;if(dx*dx+dy*dy<.085*.085 && !a.manualOverride && !b.manualOverride)Require(Math.Abs(a.qiPerSecond-b.qiPerSecond)<=2,"Bounded near-pair quantization transition");}
            result.checks.Add("V2 native validator:133 valid unchanged coordinates, starts1Qi/s, four hotspots across three catalog regions; bounded spatial transition. Six inferred source labels retained.");
            CultivationPlaytestProfile production;WorldQiDistribution current;CultivationConfigurationReason why;string detail;Require(CultivationProductionConfiguration.TryLoad(out production,out current,out why,out detail) && current.Version==1,"Default Resources still v1");Compatibility(production,table);result.checks.Add("Owned native v1 Save/Load/fractional component remains valid; v2 config validates only as explicit proposal, wrong-config load/write rejected before mutation.");
            foreach(var model in models.models){foreach(double qi in new[]{.6,1,1.8,2.5,3.5,5}){
                string node=Array.Find(world.nodes,n=>n.qiPerSecond==qi).nodeId;var a=Simulate(model,node,30);var b=Simulate(model,node,60);result.simulations.Add(a);result.simulations.Add(b);
                Require(Math.Abs(a.foundation-b.foundation)<1e-6 && Math.Abs(a.reserve-b.reserve)<1e-6 && Math.Abs(a.vienManSeconds-b.vienManSeconds)<.11,"Cross-FPS/native checkpoint determinism");
                if(qi==1 || qi==5){var c=Simulate(model,node,144);result.simulations.Add(c);Require(Math.Abs(a.foundation-c.foundation)<1e-6 && Math.Abs(a.reserve-c.reserve)<1e-6 && Math.Abs(a.vienManSeconds-c.vienManSeconds)<.11,"144FPS extreme consistency");}
            }Clock(model);result.checks.Add("Model"+model.id+": full3660s all six rates,30/60FPS with native checkpoints,1/5Qi at144FPS, pause/fractional Save/Load/Travel old-source attribution; no EXP/automatic breakthrough.");}
            var capacity=models.models[2];foreach(double qi in new[]{1d,2.5d,5d})result.simulations.Add(Simulate(capacity,Array.Find(world.nodes,n=>n.qiPerSecond==qi).nodeId,30,1.5));
            result.checks.Add("Additional C capacity1.5 sensitivity is a hypothetical character parameter, not a baseline art/root/stat grant or production rule.");
            bool invalid=false;try{CultivationRefinementModelsV10.ResolveInputCapacity(models.models[1],double.NaN);}catch(ArgumentException){invalid=true;}Require(invalid,"Reject nonfinite policy input");
            Require(File.ReadAllText("Assets/Resources/WorldQiDistributionV1.json")==v1 && File.ReadAllText("Assets/Resources/CultivationProductionV1.json")==config1,"V1 source/config immutable");Debug.Log("[WORLD BALANCE V10] PASS "+root);
        }catch(Exception e){result.failure=e.ToString();Debug.LogError("[WORLD BALANCE V10] FAIL "+e);}
        if(Directory.Exists(root))File.WriteAllText(Path.Combine(root,"result.json"),JsonUtility.ToJson(result,true));foreach(var o in objects)if(o!=null)UnityEngine.Object.DestroyImmediate(o);EditorApplication.Exit(result.failure==""?0:2);
    }
}
#endif
