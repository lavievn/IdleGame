#if DEVELOPMENT_BUILD && UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Diagnostics;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;
using TuTienCore;
using Debug=UnityEngine.Debug;
// Explicit CLI-only owned acceptance host. Never attached to the production scene.
public sealed partial class WindowsDevelopmentAcceptanceV10:MonoBehaviour
{
    const string Owner="idlegame-owned-windows-v10";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static readonly string RootArgument=Arg("-ownedV10Root");static string root;static WindowsDevelopmentAcceptanceV10 instance;
    GameManager gm;SaveManager saves;UIManager ui;HeroController hero;CombatManager combat;EnvironmentManager space;InventoryPanelUI inventory;
    string fault;bool measuring;long advanceTicks,advanceCount,advanceBytes,allocationStart;bool allocationSupported;
    ProfilerRecorder allocation,cpu;readonly List<double> frames=new List<double>(12000),allocations=new List<double>(12000),cpus=new List<double>(12000);
    [Serializable] sealed class Result {public string unity,platform,mode,saveDirectory,profile,sourceHead,failure="",nativeInput="NOT RUN";public bool development;public List<string> checks=new List<string>(),uiIssues=new List<string>();}
    [Serializable] sealed class Performance {public int frames,gen0Collections;public bool allocationRecorderValid,cpuRecorderValid,advanceAllocationSupported;public long allocationControl,advanceCalls,heapBefore,heapAfter;public double seconds,frameMeanMs,frameP95Ms,frameP99Ms,cpuMeanMs,cpuP95Ms,cpuP99Ms,allocationMeanBytes,advanceMeanMicroseconds,advanceMeanAllocatedBytes;public long snapshotWrites;}
    [Serializable] sealed class Snapshot {public int level,wave,progress,damage,maxHP,points,hpAdded,physicalAdded;public float exp;public long gold;public string map,gear,gearCore,inventory,legacyEquipped,weapon,roots;}
    readonly Result result=new Result();string Evidence=>Path.Combine(root,"evidence");string SavePath=>Path.Combine(root,"saves");
    static string Arg(string key){var a=Environment.GetCommandLineArgs();for(int i=0;i+1<a.Length;i++)if(a[i]==key)return a[i+1];return null;}
    static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
    static object Get(object o,string n)=>o.GetType().GetField(n,Flags).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,Flags).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,Flags).Invoke(o,a);
    static void VerifyRoot()
    {
        Require(RootArgument!=null && Path.IsPathRooted(RootArgument),"Explicit absolute owned root required");root=Path.GetFullPath(RootArgument);
        Require(Regex.IsMatch(Path.GetFileName(root),"^idlegame-v10-[0-9a-f]{32}$"),"Owned profile name required");
        foreach(string path in new[]{root,Path.Combine(root,"saves"),Path.Combine(root,"evidence")})for(var p=new DirectoryInfo(path);p!=null;p=p.Parent)Require(!p.Exists || (p.Attributes&FileAttributes.ReparsePoint)==0,"No reparse paths");
        Require(File.ReadAllText(Path.Combine(root,".owner"))==Owner && File.ReadAllText(Path.Combine(Path.GetDirectoryName(root),".owned-player-root"))==Owner,"Owned parent/profile markers required");
        string personal=Path.GetFullPath(Path.Combine(Application.persistentDataPath,"Saves"));Require(!root.StartsWith(personal,StringComparison.OrdinalIgnoreCase) && !personal.StartsWith(root,StringComparison.OrdinalIgnoreCase),"Never personal Saves");
    }
    public static bool UseOwnedDefaultBalance()
    {
        if(RootArgument==null)return false;
        VerifyRoot();CombatBalance.ResetDevProfiles();Debug.Log("[WINDOWS V10] OWNED_DEFAULT_BALANCE no personal DEVB read/write");return true;
    }
    static bool IsOwnedGateMode(string mode) =>
        mode=="v13trial" || mode=="v13trialreplay" || mode=="v15trialprofile" ||
        mode=="v15trialbalance" || mode=="v15trialmanual";

    public static void ConfigureBeforeAwake(SaveManager manager)
    {
        if(RootArgument==null)return;
        try{VerifyRoot();manager.ConfigureSaveDirectoryBeforeAwake(Path.Combine(root,"saves"));
            var mode=Arg("-ownedV10Mode");
            if (mode=="v15playable" || mode=="v15playablereplay" || mode=="v16playable" || mode=="v16playablereplay" || mode=="combatrefinement" || mode=="combatvisualhp")
                manager.ConfigureOwnedDevelopmentLoopBeforeAwake();
            if(IsOwnedGateMode(Arg("-ownedV10Mode"))){var p=HeavenlyGateV13Fixtures.Profile();manager.ConfigureCultivationPlaytestBeforeAwake(p,false);manager.ConfigureTribulationFixtureBeforeAwake(HeavenlyGateV13Fixtures.Configuration(),HeavenlyGateV13Fixtures.Penalty());manager.GetComponent<GameManager>().ConfigureCultivationPlaytest(p);}Debug.Log("[WINDOWS V10] SAVE_DIRECTORY_BEFORE_AWAKE "+manager.SaveDirectory);}
        catch{Application.Quit(2);throw;}
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Boot()
    {
        if(RootArgument==null)return;
        try{VerifyRoot();Directory.CreateDirectory(Path.Combine(root,"evidence"));var owner=new GameObject("Explicit owned Windows acceptance v10");DontDestroyOnLoad(owner);instance=owner.AddComponent<WindowsDevelopmentAcceptanceV10>();}
        catch(Exception e){Debug.LogError("[WINDOWS V10] Invalid owned context: "+e);Application.Quit(2);}
    }
    public static long BeginAdvance(){if(instance==null || !instance.measuring)return 0;instance.allocationStart=instance.allocationSupported?GC.GetAllocatedBytesForCurrentThread():0;return Stopwatch.GetTimestamp();}
    public static void EndAdvance(long began){if(began==0 || instance==null)return;instance.advanceTicks+=Stopwatch.GetTimestamp()-began;instance.advanceCount++;if(instance.allocationSupported)instance.advanceBytes+=GC.GetAllocatedBytesForCurrentThread()-instance.allocationStart;}
    void Observe(string message,string stack,LogType type){if(type==LogType.Exception || type==LogType.Assert)fault=message+"\n"+stack;}
    IEnumerator Start()
    {
        Application.logMessageReceived+=Observe;result.unity=Application.unityVersion;result.platform=Application.platform.ToString();result.development=Debug.isDebugBuild;result.mode=Arg("-ownedV10Mode")??"matrix";result.profile=root;result.sourceHead=Arg("-ownedV10Head");
        var nested=new Stack<IEnumerator>();nested.Push(Run());
        while(nested.Count>0){object next=null;bool moved=false;Exception error=null;try{if(fault!=null)throw new InvalidOperationException(fault);moved=nested.Peek().MoveNext();if(moved)next=nested.Peek().Current;}catch(Exception e){error=e;}
            if(error!=null){result.failure=error.ToString();break;}if(!moved){nested.Pop();continue;}var child=next as IEnumerator;if(child!=null)nested.Push(child);else yield return next;}
        measuring=false;allocation.Dispose();cpu.Dispose();File.WriteAllText(Path.Combine(Evidence,"player-result.json"),JsonUtility.ToJson(result,true));
        Debug.Log("[WINDOWS V10] "+(result.failure==""?"PASS":"FAIL")+" "+root+" "+result.failure);Application.logMessageReceived-=Observe;Application.Quit(result.failure==""?0:2);
    }
    void Pass(string text){result.checks.Add(text);Debug.Log("[WINDOWS V10] PASS "+text);}
    void AssertIsolated(){VerifyRoot();Require(saves.SaveDirectory==SavePath,"Player Save isolation lost");}
    IEnumerator Until(Func<bool> predicate,float seconds,string label){float end=Time.realtimeSinceStartup+seconds;while(!predicate()){Require(Time.realtimeSinceStartup<end,"Timeout "+label);yield return null;}}
    IEnumerator Ready(){yield return Until(()=>!gm.IsEnteringGameplay,30,"entry");ui.SetPaused(true);yield return null;AssertIsolated();}
    IEnumerator Resize(int w,int h){
        var window=UnityEngine.Object.FindFirstObjectByType<TransparentWindow>();
        if(window!=null)yield return Until(()=>!(bool)Get(window,"resizing"),5,"native resize settlement before fixture resolution");
        Screen.SetResolution(w,h,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.4f);yield return null;
        if(Screen.width!=w || Screen.height!=h)result.uiIssues.Add("Resolution unavailable: requested"+w+"x"+h+" got"+Screen.width+"x"+Screen.height);
        Canvas.ForceUpdateCanvases();if(space!=null)space.SyncBattleProjection();
    }
    IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(Evidence,name+".png"));yield return new WaitForSecondsRealtime(.1f);}
    IEnumerator Panel(string name,int w,int h){yield return Resize(w,h);foreach(var text in UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None)){if(!text.isActiveAndEnabled)continue;Require(text.gameObject.name!="LayoutGuideLabel","No unintended Inventory debug guide");text.ForceMeshUpdate();if(text.isTextOverflowing)result.uiIssues.Add(name+" "+w+"x"+h+" TMP overflow: "+text.transform.parent.gameObject.name+"/"+text.text);}if(name=="inventory-equipment"){
            var summary=(TextMeshProUGUI)Get(inventory,"summary");var button=Find("InventoryGrantCharms");var a=new Vector3[4];var b=new Vector3[4];summary.rectTransform.GetWorldCorners(a);button.GetWorldCorners(b);
            Require(a[0].y>=b[1].y+1,"Inventory summary must remain above footer buttons at"+w);
        }yield return Capture(name+"-"+w);}
    void WriteResult(){File.WriteAllText(Path.Combine(Evidence,"player-result.json"),JsonUtility.ToJson(result,true));}
    IEnumerator Run()
    {
        yield return new WaitForSecondsRealtime(1);
        gm=UnityEngine.Object.FindFirstObjectByType<GameManager>();saves=SaveManager.Instance;ui=UIManager.Instance;hero=UnityEngine.Object.FindFirstObjectByType<HeroController>();combat=UnityEngine.Object.FindFirstObjectByType<CombatManager>();space=UnityEngine.Object.FindFirstObjectByType<EnvironmentManager>();
        Require(gm!=null && saves!=null && ui!=null && Debug.isDebugBuild,"Real development Player managers");Require(!ui.inventoryLayoutDebug,"Player Inventory debug layout is off by default");AssertIsolated();result.saveDirectory=saves.SaveDirectory;
        if(result.mode=="combatvisualhp")
        {
            yield return Resize(800,450);
            yield return CombatVisualHpHotfixSmoke.Run(gm,saves,ui,combat,hero,space,Resize,Capture,Pass);
            Require(result.uiIssues.Count == 0,"Hotfix VFX resolutions");
            yield break;
        }
        if(result.mode=="combatrefinement")
        {
            yield return Resize(800,450);
            yield return CombatUiRefinementSmoke.Run(gm,saves,ui,combat,hero,space,Resize,Capture,Pass);
            Require(result.uiIssues.Count == 0,"Refinement HUD resolutions");
            yield break;
        }
        if(result.mode=="v16playablereplay")
        {
            Development016Smoke.Replay(gm, saves, Pass);
            yield break;
        }
        if(result.mode=="v16playable")
        {
            yield return Resize(800,450);
            yield return Development016Smoke.Run(gm, saves, ui, combat, hero, space, Capture, Pass);
            yield break;
        }
        if(result.mode=="v15playable")
        {
            yield return Resize(800,450);
            yield return Development015Smoke.Run(gm, saves, ui, combat, Evidence, Capture, Pass, true);
            yield break;
        }
        if(result.mode=="v15playablereplay")
        {
            yield return Development015Smoke.Replay(gm, saves, ui, Pass);
            yield break;
        }
        if(result.mode=="v13trialreplay"){yield return DevelopmentV13GameplayScenario.Replay(gm,saves,ui,Pass);yield break;}
        if(result.mode=="v15trialprofile"){yield return ProfileTrialV15();yield break;}
        if(result.mode=="v15trialbalance"){yield return BalanceTrialV15();yield break;}
        if(result.mode=="v15trialmanual"){yield return ManualTrialV15();yield break;}
        if(result.mode=="v13trial"){gm.OnNewGameClicked();gm.OnConfirmResetClicked();yield return Ready();yield return DevelopmentV13GameplayScenario.Run(gm,saves,ui,combat,hero,space,Evidence,Resize,Capture,Pass);Require(result.uiIssues.Count==0,"V13 UI sizes");yield break;}
        Require(saves.CultivationProfile!=null && saves.CultivationProfile.IsProduction && Resources.Load<TextAsset>("WorldQiDistributionV1")!=null,"Packaged default production resources");
        if(result.mode=="replay"){
            string component,error;Require(CultivationGameplayState.TryReadHeroJson(File.ReadAllText(Path.Combine(SavePath,"ManualSave2.json")),saves.CultivationProfile,out component,out error),"Restart source");
            Require(gm.LoadAndContinue(SaveSlot.ManualSave2),"Separate Player process Load");yield return Ready();Require(gm.HeroData.cultivationJson==component,"Restart exact checkpoint; no offline award");Pass("Separate OS Player process loads migrated ManualSave2 with exact checkpoint and retained compatibility metadata/backup; no offline grant.");yield break;}
        if(result.mode=="observe"){Pass("Observe-only owned Player ready for real manual input; no method-driven gameplay fixture.");WriteResult();while(true)yield return new WaitForSecondsRealtime(2);}
        if(result.mode=="v12tribulation"){yield return TribulationV12();yield break;}
        if(result.mode=="v12performance"){yield return PerformanceV12();yield break;}
        if(result.mode=="v11capacity"){yield return CapacityV11();yield break;}
        if(result.mode=="v11qi"){yield return QiUpgradeV11();yield break;}
        Require(result.mode=="matrix","Explicit matrix/replay/observe/v11qi mode only");
        foreach(var size in Sizes)yield return Panel("menu",size.x,size.y);
        gm.OnNewGameClicked();gm.OnConfirmResetClicked();yield return Ready();Require(gm.Cultivation.Current.Foundation.Amount==0 && gm.HeroData.currentLevel==1,"Default New zero cultivation");
        Pass("Startup/New/confirmation invoked through existing methods as a fixture, not native mouse; default production configuration.");
        yield return ProfileFarm();if(gm.Cultivation==null){gm.OnRetryClicked();yield return Ready();}
        string pause=gm.HeroData.cultivationJson;yield return new WaitForSecondsRealtime(.4f);Require(gm.HeroData.cultivationJson==pause,"Pause checkpoint stable");
        foreach(var size in Sizes){ui.OpenHeroStats();yield return Panel("character",size.x,size.y);ui.CloseDamageInfo();}
        Memory("before-loads");var expected=CanonicalExpected(gm.HeroData);string checkpoint=gm.HeroData.cultivationJson;Require(gm.TryManualSave(SaveSlot.ManualSave1),"Current Save");var retired=gm.Cultivation;
        for(int i=0;i<3;i++){var previousHero=gm.HeroData;Require(gm.LoadAndContinue(SaveSlot.ManualSave1),"Repeated Load");yield return Ready();Require(gm.HeroData.cultivationJson==checkpoint && retired.IsDisposed,"Load exact resources/fractions; retire old owner");Matches(expected,"Player Load");Require(previousHero==null,"Old native Hero destroyed after Load");Memory("load-"+i);}
        string path=Path.Combine(SavePath,"ManualSave2.json"),valid=File.ReadAllText(Path.Combine(SavePath,"ManualSave1.json"));
        foreach(string bad in new[]{valid.Replace("\\\"version\\\":"+gm.Cultivation.Profile.SaveVersion,"\\\"version\\\":99"),valid.Replace(gm.Cultivation.Profile.Id,"other:configuration"),"{\"currentLevel\":1,\"cultivationJson\":\"{\"}"}){Require(bad!=valid,"Invalid fixture changes source");File.WriteAllText(path,bad);var current=gm.HeroData;var binding=gm.Cultivation;Require(!gm.LoadAndContinue(SaveSlot.ManualSave2) && gm.HeroData==current && gm.Cultivation==binding,"Failed candidate does not swap Hero/session");}
        Pass("Current/repeated Load/full normalized equipment/forge/inventory/stat snapshot/future/config/malformed candidate-before-swap PASS in Player.");
        var legacy=UnityEngine.Object.Instantiate(gm.HeroData);legacy.cultivationJson="";string original=JsonUtility.ToJson(legacy,true);UnityEngine.Object.Destroy(legacy);File.WriteAllText(path,original);var canonical=gm.HeroData;var oldBinding=gm.Cultivation;string live=JsonUtility.ToJson(canonical);
        Require(!gm.LoadAndContinue(SaveSlot.ManualSave2) && gm.IsCultivationMigrationPending,"Legacy opens real consent");foreach(var size in Sizes)yield return Panel("consent",size.x,size.y);
        gm.OnCancelResetClicked();Require(!gm.IsCultivationMigrationPending && gm.HeroData==canonical && gm.Cultivation==oldBinding && JsonUtility.ToJson(canonical)==live && File.ReadAllText(path)==original,"Cancel no write or live mutation");
        Require(!gm.LoadAndContinue(SaveSlot.ManualSave2) && gm.IsCultivationMigrationPending,"Repeat consent");gm.OnConfirmResetClicked();yield return Ready();Require(!gm.IsCultivationMigrationPending && gm.HeroData!=canonical,"Commit before swap");
        string[] backups=Directory.GetFiles(SavePath,"ManualSave2.cultivation-*.legacy.bak");Require(backups.Length==1 && File.ReadAllText(backups[0])==original,"Immutable full legacy backup");string backup=Convert.ToBase64String(File.ReadAllBytes(backups[0]));
        gm.OnConfirmResetClicked();Require(gm.LoadAndContinue(SaveSlot.ManualSave2),"Duplicate consent/load no bonus");yield return Ready();Require(Convert.ToBase64String(File.ReadAllBytes(backups[0]))==backup && gm.Cultivation.Current.Foundation.Amount==0 && gm.Cultivation.Current.Reserve.Amount==0,"No migration gifts; immutable backup");
        Pass("Real Player legacy modal cancel/accept/duplicate consent/Load, zero-state migration, immutable backup and canonical swap PASS; actions are method fixtures, not native input.");
        yield return PanelsAndWeapons();
        WorldMapNode origin;Require(WorldMapCatalog.TryGet(gm.HeroData.worldMapId,out origin),"Travel origin");string target=null;foreach(var id in origin.Links){WorldMapNode n;if(WorldMapCatalog.TryGet(id,out n) && !n.IsBoss){target=id;break;}}
        Require(target!=null,"Linked Travel fixture");if(!gm.HeroData.worldMapCleared.Contains(origin.Id))gm.HeroData.worldMapCleared.Add(origin.Id);
        var travelBinding=gm.Cultivation;double settled=travelBinding.Current.Foundation.Amount+travelBinding.Current.Reserve.Amount+travelBinding.Profile.EnvironmentalQiPerSecond(origin.Id)*travelBinding.PendingSeconds;
        Require(gm.TryTravelToWorldMap(target),"Travel with explicit permission fixture");yield return Ready();
        Require(gm.Cultivation==travelBinding && travelBinding.PendingSeconds==0 && Math.Abs(travelBinding.Current.Foundation.Amount+travelBinding.Current.Reserve.Amount-settled)<1e-8,"Travel settles old source exactly once");
        double travelTime=travelBinding.ActiveSeconds;ui.SetPaused(false);yield return Until(()=>travelBinding.ActiveSeconds>=travelTime+.4,10,"Destination active clock");ui.SetPaused(true);
        Require(gm.Cultivation.Density==gm.Cultivation.Profile.Density(target),"Destination Qi source after active tick");Require(gm.TryManualSave(SaveSlot.ManualSave1) && gm.LoadAndContinue(SaveSlot.ManualSave1),"Travel Save/Load");yield return Ready();
        Call(combat,"ApplyIncomingDamage",1000000,"[V10 owned defeat fixture]",null,null);yield return null;gm.OnRetryClicked();yield return Ready();Memory("retry");
        string saved=gm.HeroData.cultivationJson;Require(gm.ReturnToMainMenu(),"Menu");gm.OnContinueClicked();gm.OnConfirmResetClicked();yield return Ready();Require(gm.HeroData.cultivationJson==saved,"Continue exact state");
        gm.OnNewGameClicked();gm.OnConfirmResetClicked();yield return Ready();Require(gm.HeroData.currentLevel==1 && gm.Cultivation.Current.Foundation.Amount==0 && gm.Cultivation.ActiveSeconds==0,"New fresh owner/zero state");
        Memory("new");File.WriteAllLines(Path.Combine(Evidence,"player-lifecycle.csv"),memory);
        Pass("Travel (permission fixture), Save/Load, defeat/Retry, Menu/Continue/New, camera/combat binding and no stale clock PASS. Migrated ManualSave2 remains for restart.");
    }
    readonly List<string> memory=new List<string>{"phase,managedHeapBytes,entityDataObjects,heroBodies,snapshotWrites"};
    void Memory(string phase){int bodies=0;foreach(var image in hero.heroRect.GetComponentsInChildren<Image>(true))if(image.name=="Cultivation Sprite Body")bodies++;Require(bodies==1,"One native body after "+phase);memory.Add(phase+","+UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong()+","+Resources.FindObjectsOfTypeAll<EntityDataSO>().Length+","+bodies+","+saves.SuccessfulSnapshotWrites);}
    static readonly Vector2Int[] Sizes={new Vector2Int(600,338),new Vector2Int(800,450),new Vector2Int(1150,647),new Vector2Int(1360,680)};
    static double Mean(List<double> x){double total=0;foreach(double n in x)total+=n;return x.Count==0?-1:total/x.Count;}
    IEnumerator ProfileFarm()
    {
        yield return Resize(800,450);allocation=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame");cpu=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread");
        long before=GC.GetAllocatedBytesForCurrentThread();var control=new byte[1024*1024];control[0]=1;long delta=GC.GetAllocatedBytesForCurrentThread()-before;GC.KeepAlive(control);allocationSupported=delta>=1024*1024;
        var p=new Performance{allocationRecorderValid=allocation.Valid,cpuRecorderValid=cpu.Valid,advanceAllocationSupported=allocationSupported,allocationControl=delta,heapBefore=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong()};
        int gen0=GC.CollectionCount(0);long writes=saves.SuccessfulSnapshotWrites;var binding=gm.Cultivation;double active=binding.ActiveSeconds;float began=Time.realtimeSinceStartup;measuring=true;ui.SetPaused(false);
        while(Time.realtimeSinceStartup-began<30 && gm.Cultivation!=null){yield return new WaitForEndOfFrame();frames.Add(Time.unscaledDeltaTime*1000);if(allocation.Valid)allocations.Add(allocation.LastValue);if(cpu.Valid)cpus.Add(cpu.LastValue*1e-6);}
        ui.SetPaused(true);measuring=false;p.seconds=Time.realtimeSinceStartup-began;p.frames=frames.Count;p.gen0Collections=GC.CollectionCount(0)-gen0;p.snapshotWrites=saves.SuccessfulSnapshotWrites-writes;p.heapAfter=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();p.frameMeanMs=Mean(frames);frames.Sort();p.frameP95Ms=frames.Count==0?-1:frames[(int)((frames.Count-1)*.95)];p.frameP99Ms=Percentile(frames,.99);p.cpuMeanMs=Mean(cpus);cpus.Sort();p.cpuP95Ms=Percentile(cpus,.95);p.cpuP99Ms=Percentile(cpus,.99);p.allocationMeanBytes=Mean(allocations);p.advanceCalls=advanceCount;p.advanceMeanMicroseconds=advanceCount==0?-1:advanceTicks*1e6/Stopwatch.Frequency/advanceCount;p.advanceMeanAllocatedBytes=allocationSupported && advanceCount>0?(double)advanceBytes/advanceCount:-1;
        File.WriteAllText(Path.Combine(Evidence,"player-performance.json"),JsonUtility.ToJson(p,true));
        Require(binding.ActiveSeconds>active && gm.DamageHistory.Length>0,"Player natural combat/cultivation observed");
        Pass("Unmodified Player farm after method-driven New: seconds="+p.seconds+" active="+(binding.ActiveSeconds-active)+" damageEvents="+gm.DamageHistory.Length+" level="+gm.HeroData.currentLevel+" exp="+gm.HeroData.currentExp+" survived="+(gm.Cultivation!=null)+"; no gear/HP/EXP/monster overrides during interval. Not native-input acceptance.");yield return Capture("natural-farm");
    }
    IEnumerator PanelsAndWeapons()
    {
        Require(gm.GrantDevWeaponSamples(),"Explicit sample fixture");Require(EquipmentRuntime.AddInstance(gm.HeroData,EquipmentFoundation.CreateDemoSword()),"Sword fixture");inventory=(InventoryPanelUI)Get(ui,"inventoryUI");
        foreach(var size in Sizes){Press("InventoryButton");yield return null;yield return Panel("inventory-equipment",size.x,size.y);var sword=gm.HeroData.equipmentItems.Find(x=>x.templateId=="sample:iron_sword");Select(sword.instanceId);Press("InventoryForge");yield return null;yield return Panel("forge",size.x,size.y);Press("Forge_Close");yield return null;Press("InventoryClose");yield return Resize(size.x,size.y);Press("WorldMapButton");yield return Panel("world-map",size.x,size.y);Press("WorldMapClose");yield return Resize(size.x,size.y);}
        var body=hero.heroRect.GetComponent<CultivationSpriteBridge>().BodyGraphic;var sprite=body.sprite;
        foreach(var family in new[]{WeaponFamily.Sword,WeaponFamily.Bow,WeaponFamily.Staff,WeaponFamily.Unarmed,WeaponFamily.Sword}){
            if(family==WeaponFamily.Unarmed)Require(gm.UnequipGear(EquipmentSlot.Weapon),"Unequip fixture");
            else{var gear=family==WeaponFamily.Sword?EquipmentFoundation.CreateDemoSword():family==WeaponFamily.Bow?EquipmentFoundation.CreateDemoBow():EquipmentFoundation.CreateDemoStaff();Require(EquipmentRuntime.AddInstance(gm.HeroData,gear) && gm.EquipGear(gear.instanceId,EquipmentSlot.Weapon),"Equip fixture");}
            Require(body.sprite==sprite && hero.attackMode==WeaponBasicAttack.Mode(family),"Unified Hero bitmap and distinct mechanics across modes");
            combat.ForceClearAllMonsters();gm.activeMonsters.Clear();Call(gm,"CancelNextWave");Call(gm,"CallNextWave");
            var enemies=new List<CombatManager.ActiveMonsterInfo>((List<CombatManager.ActiveMonsterInfo>)Get(combat,"activeMonsters"));var hp=new List<int>();foreach(var e in enemies)hp.Add(e.currentHP);
            bool damaged=false,projectile=false;int traces=gm.DamageHistory.Length;float deadline=Time.realtimeSinceStartup+30;ui.SetPaused(false);
            while(!damaged || (family==WeaponFamily.Bow || family==WeaponFamily.Staff) && !projectile){
                Require(Time.realtimeSinceStartup<deadline && gm.Cultivation!=null,"Bounded actual Player combat "+family);
                for(int i=0;i<enemies.Count;i++)damaged|=enemies[i].currentHP<hp[i];
                foreach(var shot in (IList)Get(combat,"projectiles"))if((bool)Get(shot,"fromHero") && ReferenceEquals(Get(shot,"hero"),combat.CurrentHeroCombatState)){
                    Require((AttackMode)Get(shot,"attackMode")==hero.attackMode && (bool)Get(shot,"staffBasic")==(family==WeaponFamily.Staff) && !(bool)Get(shot,"magic"),"Correct primary-owned basic projectile path");projectile=true;}
                Require(body.sprite==sprite,"Player combat retains same body bitmap");yield return null;
            }
            ui.SetPaused(true);Pass("Actual Player combat "+family+" mode="+hero.attackMode+" damageEvents="+(gm.DamageHistory.Length-traces)+" primaryProjectile="+projectile+" sameSprite=True; sample gear/forced wave fixture after unmodified farm.");yield return Capture("weapon-"+family);
        }
        Pass("Player Inventory/Equipment/Forge/World Map rendered at requested sizes; UI issues listed separately. Sample weapon fixtures preserve same Hero bitmap; combat mechanics retain prior native/Mono regression evidence.");
    }
    RectTransform Find(string name)
    {
        var hud=(RectTransform)Get(ui,"hudCanvas");
        foreach(var r in hud.GetComponentsInChildren<RectTransform>(true))if(r.gameObject.name==name)return r;
        throw new InvalidOperationException("Missing UI "+name);
    }
    void Press(string name)
    {
        Canvas.ForceUpdateCanvases();var rect=Find(name);var p=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
        var hit=ui.HitControl(p);Require(hit!=null && hit.GetRect()==rect,"Wrong hit-test for "+name+" active="+rect.gameObject.activeInHierarchy+" entry="+gm.IsEnteringGameplay+" pending="+gm.HasPendingConfirmation+" title="+gm.preGameUI.activeInHierarchy+" inventory="+(inventory!=null && inventory.IsOpen)+" forge="+(inventory!=null && inventory.IsForgeOpen)+(hit==null?" (none)":" ("+hit.gameObject.name+")"));
        ui.HandlePointerClick(p,p);
    }
    void Select(string id)
    {
        var current=EquipmentRuntime.Find(gm.HeroData,id);
        Require(current!=null,"Requested gear is not in the current canonical inventory.");
        if(current.isEquipped)
        {
            Require(current.equippedSlot==EquipmentSlot.Weapon,"This probe selects awarded swords only.");
            Press("EquipmentSlot0");return;
        }
        var entries=(IList)Get(inventory,"entries");int at=-1;
        for(int i=0;i<entries.Count;i++)if((string)Get(entries[i],"id")==id){at=i;break;}
        Require(at>=0 && at<(int)Get(inventory,"bagPageSize"),"Awarded gear missing from current bag page.");Press("BagCell"+at);
    }
    string GearCore(EntityDataSO d)
    {
        var rows=new List<string>();
        foreach(var x in d.equipmentItems)
        {
            string row=x.instanceId+"|"+x.templateId+"|"+x.level+"|"+x.rarity+"|"+x.refineLevel+"|"+x.highestRefineLevel+"|"+x.specialForgeCount+"|"+x.isEquipped+"|"+x.equippedSlot;
            foreach(var stat in x.mainStats)row+="|main:"+JsonUtility.ToJson(stat);
            foreach(var stat in x.extraStats)row+="|extra:"+JsonUtility.ToJson(stat);
            foreach(var history in x.forgeHistory)row+="|history:"+JsonUtility.ToJson(history);
            rows.Add(row);
        }
        rows.Sort(StringComparer.Ordinal);return string.Join("\n",rows);
    }
    Snapshot CanonicalExpected(EntityDataSO source)
    {
        string core=GearCore(source);var copy=UnityEngine.Object.Instantiate(source);
        copy.NormalizeRoots();copy.NormalizeMapProgress();copy.NormalizeCombatStats();
        EquipmentInventory.Normalize(copy);EquipmentRuntime.Normalize(copy);
        Require(copy.equipmentItems.Count==source.equipmentItems.Count && GearCore(copy)==core,"Normalization changed authoritative gear, not just legacy metadata.");
        var snapshot=Take(copy);UnityEngine.Object.Destroy(copy);return snapshot;
    }
    Snapshot Take(EntityDataSO data=null)
    {
        var d=data!=null?data:gm.HeroData;var gear=new List<string>();var stacks=new List<string>();
        foreach(var item in d.equipmentItems)gear.Add(JsonUtility.ToJson(item));
        foreach(var item in d.inventory)stacks.Add(JsonUtility.ToJson(item));gear.Sort(StringComparer.Ordinal);stacks.Sort(StringComparer.Ordinal);
        return new Snapshot {level=d.currentLevel,exp=d.currentExp,gold=d.gold,map=d.worldMapId,wave=d.worldMapLoopWave,progress=d.worldMapWaveProgress,
            damage=WeaponBasicAttack.DamageSource(d),maxHP=d.GetCalculatedHealth(hero.attackMode),gear=string.Join("\n",gear),gearCore=GearCore(d),inventory=string.Join("\n",stacks),
            legacyEquipped=d.equippedItemId,weapon=WeaponBasicAttack.Identity(d),points=d.statPoints,hpAdded=d.addedHealth,physicalAdded=d.addedDamage,
            roots=string.Join(",",d.spiritRoots)+"|"+string.Join(",",d.rootTiers)+"|"+string.Join(",",d.rootWeights)};
    }
    void Matches(Snapshot expected,string label)
    {
        var actual=Take();
        File.WriteAllText(Path.Combine(Evidence,"expected-snapshot.json"),JsonUtility.ToJson(expected,true));
        File.WriteAllText(Path.Combine(Evidence,"actual-snapshot.json"),JsonUtility.ToJson(actual,true));
        Require(JsonUtility.ToJson(actual)==JsonUtility.ToJson(expected),label+" snapshot differs (progress/inventory/equipment/forge/stats).");
        Require(combat.CurrentHeroCombatState.Data==gm.HeroData && combat.MaxHeroHP==expected.maxHP,label+" combat state stale.");Unique();
    }
    void Unique()
    {
        var ids=new HashSet<string>();foreach(var item in gm.HeroData.equipmentItems)Require(ids.Add(item.instanceId),"Duplicate equipment instance ID.");
    }
}
#endif
