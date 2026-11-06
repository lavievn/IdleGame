#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TuTienCore;

// Explicit CLI integration verifier, never a startup hook or a Player feature.
// Runs the real MainGame on an ignored scene copy and a marker-owned save path.
public sealed partial class MainGameFoundationProbe : MonoBehaviour
{
    public GameObject saveOwner;
    public string profileRoot;
    public string sourceHead;
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const string Marker="idle-foundation-probe-v1";
    const string SceneCopy="Assets/InitTestSceneFoundationValidation.unity";
    [Serializable] public sealed class Result
    {
        public string unity,sourceHead,profile,evidence;
        public string phase1="NOT RUN",phase2="NOT RUN",phase3="NOT RUN",phase4="NOT RUN";
        public string failure="";
        public List<string> checks=new List<string>();
    }
    [Serializable] sealed class Snapshot
    {
        public int level,wave,progress,damage,maxHP,points,hpAdded,physicalAdded;
        public float exp;
        public long gold;
        public string map,gear,gearCore,inventory,legacyEquipped,weapon,roots;
    }
    public string replayProfile;
    public string replaySlotName="ManualSave1";
    Result result=new Result();
    GameManager gm; CombatManager combat; EnvironmentManager space; HeroController hero;
    UIManager ui; SaveManager saves; InventoryPanelUI inventory;
    string runtimeFault;
    int phase;
    string SavePath=>Path.Combine(profileRoot,"saves");
    string Evidence=>Path.Combine(profileRoot,"evidence");
    static object Get(object o,string n)=>o.GetType().GetField(n,Flags).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,Flags).SetValue(o,v);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,Flags).Invoke(o,args);
    static void Require(bool ok,string message) {if(!ok)throw new InvalidOperationException(message);}
    static bool Cli=>Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationValidationRun")>=0;

    public static void RunIsolated()
    {
        Require(Cli,"Explicit -foundationValidationRun is required; do not attach this verifier to a normal scene.");
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Refusing an existing Play session.");
        for(int i=0;i<SceneManager.sceneCount;i++)Require(!SceneManager.GetSceneAt(i).isDirty,"Refusing unsaved scene work.");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/MainGame.unity");
        var owners=UnityEngine.Object.FindObjectsByType<SaveManager>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        Require(owners.Length==1,"MainGame must contain one SaveManager.");
        var owner=owners[0].gameObject;
        Require(owner.GetComponent<GameManager>()!=null && owner.GetComponent<CombatManager>()!=null,"Managers must share the isolated inactive owner.");
        owner.SetActive(false);
        var probe=new GameObject("Explicit isolated foundation verifier").AddComponent<MainGameFoundationProbe>();
        probe.saveOwner=owner;
        probe.profileRoot=Path.GetFullPath(Path.Combine(Application.dataPath,"../.utmp/foundation-"+Guid.NewGuid().ToString("N")));
        VerifyPath(probe.profileRoot);Directory.CreateDirectory(probe.profileRoot);
        File.WriteAllText(Path.Combine(probe.profileRoot,".owner"),Marker);
        var args=Environment.GetCommandLineArgs();
        for(int i=0;i+1<args.Length;i++)if(args[i]=="-foundationSourceHead")probe.sourceHead=args[i+1];
        for(int i=0;i+1<args.Length;i++)if(args[i]=="-foundationResumeSavedProfile")probe.replayProfile=args[i+1];
        for(int i=0;i+1<args.Length;i++)if(args[i]=="-foundationResumeSlot")probe.replaySlotName=args[i+1];
        SaveSlot replaySlot;Require(Enum.TryParse(probe.replaySlotName,out replaySlot) && Enum.IsDefined(typeof(SaveSlot),replaySlot),"Explicit replay slot is valid");
        if(!string.IsNullOrEmpty(probe.replayProfile))
        {
            VerifyPath(probe.replayProfile);Require(File.ReadAllText(Path.Combine(probe.replayProfile,".owner"))==Marker,"Replay source is not owned.");
            Directory.CreateDirectory(Path.Combine(probe.profileRoot,"saves"));
            File.Copy(Path.Combine(probe.replayProfile,"saves/"+probe.replaySlotName+".json"),Path.Combine(probe.profileRoot,"saves/"+probe.replaySlotName+".json"),false);
            string receipt=probe.replaySlotName+".cultivation-migration.json";
            if(File.Exists(Path.Combine(probe.replayProfile,"saves/"+receipt)))File.Copy(Path.Combine(probe.replayProfile,"saves/"+receipt),Path.Combine(probe.profileRoot,"saves/"+receipt),false);
            foreach(string pattern in new[]{".cultivation-*.legacy.bak",".cultivation-*.snapshot.bak",".cultivation-history-*.receipt.json"})
                foreach(string backup in Directory.GetFiles(Path.Combine(probe.replayProfile,"saves"),probe.replaySlotName+pattern))File.Copy(backup,Path.Combine(probe.profileRoot,"saves/"+Path.GetFileName(backup)),false);
        }
        Directory.CreateDirectory(Path.Combine(probe.profileRoot,"evidence"));
        Require(EditorSceneManager.SaveScene(scene,SceneCopy),"Could not save owned scene copy.");
        Debug.Log("[FOUNDATION] PROFILE "+probe.profileRoot);
        EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Show();
        EditorApplication.isPlaying=true;
    }
    // Read-only replay of an already-owned slot, for diagnosing a failed
    // integration assertion without continuing gameplay or rewriting the slot.
    public static void InspectOwnedSave()
    {
        var args=Environment.GetCommandLineArgs();string profile=null;
        for(int i=0;i+1<args.Length;i++)if(args[i]=="-foundationInspectOwnedProfile")profile=args[i+1];
        Require(!string.IsNullOrEmpty(profile),"Explicit owned profile required.");VerifyPath(profile);
        Require(File.ReadAllText(Path.Combine(profile,".owner"))==Marker,"Wrong profile owner.");
        var owner=new GameObject("Inactive read-only load inspector");owner.SetActive(false);
        var manager=owner.AddComponent<SaveManager>();manager.ConfigureSaveDirectoryBeforeAwake(Path.Combine(profile,"saves"));
        var raw=ScriptableObject.CreateInstance<EntityDataSO>();var loaded=ScriptableObject.CreateInstance<EntityDataSO>();
        JsonUtility.FromJsonOverwrite(File.ReadAllText(Path.Combine(profile,"saves/ManualSave1.json")),raw);
        Require(manager.LoadGame(loaded,SaveSlot.ManualSave1),"Read-only owned Load failed.");
        File.WriteAllText(Path.Combine(profile,"evidence/inspect-raw.json"),JsonUtility.ToJson(raw,true));
        File.WriteAllText(Path.Combine(profile,"evidence/inspect-loaded.json"),JsonUtility.ToJson(loaded,true));
        loaded.ApplyHeroBalance();
        File.WriteAllText(Path.Combine(profile,"evidence/inspect-balanced.json"),JsonUtility.ToJson(loaded,true));
        Debug.Log("[FOUNDATION] Read-only inspection complete.");
        UnityEngine.Object.DestroyImmediate(raw);UnityEngine.Object.DestroyImmediate(loaded);UnityEngine.Object.DestroyImmediate(owner);
    }
    static void VerifyPath(string path)
    {
        string full=Path.GetFullPath(path),parent=Path.GetFullPath(Path.Combine(Application.dataPath,"../.utmp"));
        Require(string.Equals(Path.GetDirectoryName(full),parent,StringComparison.OrdinalIgnoreCase) && Regex.IsMatch(Path.GetFileName(full),"^foundation-[0-9a-f]{32}$"),"Refusing non-owned profile path.");
        string player=Path.GetFullPath(Path.Combine(Application.persistentDataPath,"Saves"));
        Require(!full.StartsWith(player,StringComparison.OrdinalIgnoreCase) && !player.StartsWith(full,StringComparison.OrdinalIgnoreCase),"Refusing player Saves.");
        var ancestors=new Stack<DirectoryInfo>();
        for(var p=new DirectoryInfo(full);p!=null;p=p.Parent)ancestors.Push(p);
        while(ancestors.Count>0)
        {
            var p=ancestors.Pop();
            Require(!p.Exists || (p.Attributes&FileAttributes.ReparsePoint)==0,"Refusing reparse path.");
        }
    }
    void Awake()
    {
        if(!Cli){enabled=false;throw new InvalidOperationException("Foundation probe is CLI-only.");}
        VerifyPath(profileRoot);
        Require(File.ReadAllText(Path.Combine(profileRoot,".owner"))==Marker,"Missing ownership marker.");
        Require(saveOwner!=null && !saveOwner.activeSelf,"Save owner must be inactive before Awake.");
        saves=saveOwner.GetComponent<SaveManager>();saves.ConfigureSaveDirectoryBeforeAwake(SavePath);
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationCultivationV07")>=0 || Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationCultivationV08")>=0) {
            bool balance=Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationCultivationV08")>=0;
            var profile=CultivationPlaytestBootstrap.ReadProfile(File.ReadAllText(balance?"Assets/Editor/CultivationBalanceV1.json":"Assets/Editor/CultivationPlaytestV07.json"));
            saves.ConfigureCultivationPlaytestBeforeAwake(profile,true);
            saveOwner.GetComponent<GameManager>().ConfigureCultivationPlaytest(profile);
        }
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationCultivationV09")>=0) {
            CultivationPlaytestProfile profile;WorldQiDistribution table;CultivationConfigurationReason code;string detail;
            Require(CultivationProductionConfiguration.TryLoadVersion(1,out profile,out table,out code,out detail),"Owned production configuration "+code+" "+detail);
            saves.ConfigureCultivationProductionBeforeAwake(profile);saveOwner.GetComponent<GameManager>().ConfigureCultivationPlaytest(profile);
        }
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationCultivationV11B")>=0 && !CultivationIntegrationGate.WorldQiV2Enabled) {
            CultivationPlaytestProfile profile;WorldQiDistribution qi;CultivationConfigurationReason code;string why;
            Require(CultivationProductionConfiguration.TryLoadVersion(2,out profile,out qi,out code,out why),"Isolated v2 "+why);
            saves.ConfigureCultivationProductionBeforeAwake(profile);saveOwner.GetComponent<GameManager>().ConfigureCultivationPlaytest(profile);
        }
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationCultivationV11C")>=0 && !CultivationIntegrationGate.CapacityEnabled) {
            CultivationPlaytestProfile profile;WorldQiDistribution qi;CultivationConfigurationReason code;string why;
            Require(CultivationProductionConfiguration.TryLoadVersion(3,out profile,out qi,out code,out why),"Isolated capacity "+why);
            saves.ConfigureCultivationProductionBeforeAwake(profile);saveOwner.GetComponent<GameManager>().ConfigureCultivationPlaytest(profile);
        }
        saveOwner.SetActive(true);AssertIsolated();
        result.unity=Application.unityVersion;result.sourceHead=sourceHead;result.profile=profileRoot;result.evidence=Evidence;
        Application.logMessageReceived+=ObserveLog;
    }
    void ObserveLog(string text,string stack,LogType type)
    {
        if(type!=LogType.Exception && type!=LogType.Error && type!=LogType.Assert)return;
        if(stack.Contains("UnityEditor.Search.") || text.StartsWith("Curl error") || text.Contains("Cannot Pause a disabled VideoPlayer"))return;
        if(text.StartsWith("[FOUNDATION]"))return;
        runtimeFault=text+"\n"+stack;
    }
    IEnumerator Start()
    {
        var pending=new Stack<IEnumerator>();pending.Push(Run());
        while(pending.Count>0)
        {
            object next=null;bool moved=false;Exception error=null;
            try
            {
                if(runtimeFault!=null)throw new InvalidOperationException("Runtime fault: "+runtimeFault);
                moved=pending.Peek().MoveNext();if(moved)next=pending.Peek().Current;
            }
            catch(Exception e){error=e;}
            if(error!=null)
            {
                result.failure="Phase "+phase+": "+error;
                Status(phase,"FAIL");WriteResult();Debug.LogError("[FOUNDATION] FAIL "+result.failure);
                Application.logMessageReceived-=ObserveLog;EditorApplication.Exit(2);yield break;
            }
            if(!moved){pending.Pop();continue;}
            var nested=next as IEnumerator;
            if(nested!=null){pending.Push(nested);continue;}
            yield return next;
        }
        WriteResult();Debug.Log("[FOUNDATION] COMPLETED "+Evidence);
        Application.logMessageReceived-=ObserveLog;EditorApplication.Exit(0);
    }
    void Status(int n,string value)
    {
        if(n==1)result.phase1=value;else if(n==2)result.phase2=value;else if(n==3)result.phase3=value;else if(n==4)result.phase4=value;
    }
    void Pass(string message){result.checks.Add(message);Debug.Log("[FOUNDATION] PASS "+message);WriteResult();}
    void WriteResult(){File.WriteAllText(Path.Combine(Evidence,"result.json"),JsonUtility.ToJson(result,true));}
    void AssertIsolated()
    {
        VerifyPath(profileRoot);
        Require(File.ReadAllText(Path.Combine(profileRoot,".owner"))==Marker && saves!=null && saves.SaveDirectory==SavePath,"Save isolation lost; stop before I/O.");
    }
    IEnumerator Until(Func<bool> condition,float seconds,string message)
    {
        float deadline=Time.realtimeSinceStartup+seconds;
        while(!condition()) {Require(Time.realtimeSinceStartup<deadline,"Timeout: "+message);yield return null;}
    }
    IEnumerator Ready()
    {
        yield return Until(()=>!gm.IsEnteringGameplay,20,"gameplay entry");
        ui.SetPaused(true);AssertIsolated();
        yield return null;
    }
    RectTransform Find(string name)
    {
        var hud=(RectTransform)Get(ui,"hudCanvas");
        foreach(var r in hud.GetComponentsInChildren<RectTransform>(true))if(r.gameObject.name==name)return r;
        throw new InvalidOperationException("Missing UI "+name);
    }
    void Press(string name)
    {
        var rect=Find(name);var p=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
        var hit=ui.HitControl(p);Require(hit!=null && hit.GetRect()==rect,"Wrong hit-test for "+name+(hit==null?" (none)":" ("+hit.gameObject.name+")"));
        ui.HandlePointerClick(p,p);
    }
    IEnumerator Resize(int width,int height)
    {
        var a=typeof(EditorWindow).Assembly;var t=a.GetType("UnityEditor.GameViewSizes");
        var sizes=typeof(ScriptableSingleton<>).MakeGenericType(t).GetProperty("instance").GetValue(null);
        var group=t.GetMethod("GetGroup").Invoke(sizes,new object[]{0});
        var size=Activator.CreateInstance(a.GetType("UnityEditor.GameViewSize"),new object[]{Enum.ToObject(a.GetType("UnityEditor.GameViewSizeType"),1),width,height,"Foundation "+width+"x"+height});
        group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
        int index=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null)-1;
        var view=EditorWindow.GetWindow(a.GetType("UnityEditor.GameView"));
        view.GetType().GetProperty("selectedSizeIndex",Flags).SetValue(view,index);view.Focus();
        yield return null;yield return null;
        Require(Screen.width==width && Screen.height==height,"Game View did not resize.");
        if(space!=null)space.SyncBattleProjection();
    }
    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(Evidence,name+".png"));yield return null;
    }
    IEnumerator OpenInventory()
    {
        Press("InventoryButton");yield return null;Require(inventory.IsOpen && ui.IsPaused,"Inventory did not open/pause.");
        if(ManagementWindowLayout.Expanded)yield return Resize(ManagementWindowLayout.TargetWidth,ManagementWindowLayout.TargetHeight);
    }
    IEnumerator CloseInventory()
    {
        Press("InventoryClose");yield return Resize(800,450);Require(!inventory.IsOpen,"Inventory did not close.");
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
    List<CombatManager.ActiveMonsterInfo> Enemies=>(List<CombatManager.ActiveMonsterInfo>)Get(combat,"activeMonsters");
    object FirstDrop(LootDropPresentation loot)=>( (IList)Get(loot,"drops") )[0];

    float ViewX(RectTransform rect)=>RectTransformUtility.WorldToScreenPoint(null,rect.position).x/Screen.width;
    IEnumerator CameraV03()
    {
        phase=1;Require(space.useMultiZoneCamera && space.directionalCameraFraming && !space.cameraUseCombatCenter,"Wrong MainGame camera mode.");
        var lines=new List<string>{"event,width,fpsRequested,time,heroViewport,monsterViewport,distance,awareness,hp"};
        int[] widths={600,800,1150,1360},heights={338,450,647,680},rates={30,60,144,60};
        QualitySettings.vSyncCount=0;
        // Disposable fixture removes the current encounter; real Hero.TickMovement
        // and Environment.Update/LateUpdate still own all walking and pan writes.
        combat.ClearWaveKeepingMagicVisuals();((IList)Get(gm,"activeMonsters")).Clear();Call(gm,"CancelNextWave");
        bool awarenessOnly=Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationAwarenessOnly")>=0;
        for(int i=0;i<(awarenessOnly?1:widths.Length);i++)
        {
            yield return Resize(widths[i],heights[i]);Application.targetFrameRate=rates[i];ui.SetPaused(false);
            float end=Time.realtimeSinceStartup+12;int frames=0;float sum=0;
            while(Time.realtimeSinceStartup<end){yield return new WaitForEndOfFrame();frames++;sum+=Time.unscaledDeltaTime;}
            float x=ViewX(hero.heroRect);
            lines.Add("moving,"+Screen.width+","+rates[i]+","+Time.time+","+x+",,,, ");
            Require(Mathf.Abs(x-.65f)<=.015f,"MainGame moving viewport "+x+" at "+widths[i]);
            Pass("V03 camera actual MainGame "+Screen.width+"x"+Screen.height+" requestedFPS="+rates[i]+" measuredFPS="+(frames/sum)+" movingViewport="+x+" error="+Mathf.Abs(x-.65f));
            ui.SetPaused(true);float position=space.Position(hero.heroRect).x,velocity=space.MultiZoneVelocity;
            yield return new WaitForSecondsRealtime(.5f);
            Require(Mathf.Abs(space.Position(hero.heroRect).x-position)<.001f && Mathf.Abs(space.MultiZoneVelocity-velocity)<.001f,"Pause moved camera/Hero.");
            yield return Capture("v03-camera-"+widths[i]);
        }
        phase=2;yield return Resize(800,450);Application.targetFrameRate=60;
        Call(gm,"CallNextWave");var observed=new List<CombatManager.ActiveMonsterInfo>(Enemies);
        var visible=new HashSet<int>();var alert=new Dictionary<int,MonsterAwareness>();var hp=new Dictionary<int,int>();
        foreach(var m in observed){if(awarenessOnly)m.currentHP=m.maxHP=10000;alert[m.lifeId]=MonsterAwareness.Unaware;hp[m.lifeId]=m.maxHP;}
        if(awarenessOnly)combat.CurrentHeroCombatState.CurrentHP=combat.CurrentHeroCombatState.MaxHP=10000;
        ui.SetPaused(false);float deadline=Time.realtimeSinceStartup+25;
        float stationary=0f;bool settled=false;float lastPosition=space.Position(hero.heroRect).x;
        while(Time.realtimeSinceStartup<deadline && combat.HasLivingHero)
        {
            yield return new WaitForEndOfFrame();
            stationary=hero.MovementDistanceThisFrame<.001f?stationary+Time.deltaTime:0f;
            if(stationary>=6f && !settled){Require(Mathf.Abs(ViewX(hero.heroRect)-.65f)<=.002f,"Actual combat stop6 framing");settled=true;Pass("V03 combat stop6 viewport="+ViewX(hero.heroRect));}
            float nowPosition=space.Position(hero.heroRect).x;Require(Mathf.Abs(nowPosition-lastPosition)<=48.01f,"Unexpected camera/Hero jump");lastPosition=nowPosition;
            foreach(var m in observed)
            {
                if(m.controller==null || !m.controller.IsAlive || m.controller.SpawnVersion!=m.lootKey.Spawn)continue;
                float vx=ViewX(m.controller.Rect),hx=ViewX(hero.heroRect);
                float distance=Mathf.Abs(space.Position(hero.heroRect).x-space.Position(m.controller.Rect).x);
                string state="";
                if(vx>=0 && vx<=1 && visible.Add(m.lifeId))state+="visible;";
                if(alert[m.lifeId]!=m.controller.Awareness){state+=m.controller.Awareness+";";alert[m.lifeId]=m.controller.Awareness;}
                if(m.currentHP<hp[m.lifeId]){state+="damage;";hp[m.lifeId]=m.currentHP;}
                if(state!="")lines.Add(state+","+Screen.width+",60,"+Time.time+","+hx+","+vx+","+distance+","+m.controller.Awareness+","+m.currentHP);
            }
        }
        ui.SetPaused(true);File.WriteAllLines(Path.Combine(Evidence,"camera-awareness.csv"),lines);if(awarenessOnly)Require(settled,"Fixture must observe six continuous stationary combat seconds");
        Pass("V03 awareness observation captured; no AI, spawn, attack/sight range or damage overrides; DEVB reset in owned fixture only.");
        Status(1,"PASS");Status(2,"PASS");
    }

    void Onscreen(string name)
    {
        var r=Find(name);Require(r.gameObject.activeInHierarchy,"Inactive "+name);
        var corners=new Vector3[4];r.GetWorldCorners(corners);
        foreach(var c in corners){var p=RectTransformUtility.WorldToScreenPoint(null,c);Require(p.x>=-.5f && p.x<=Screen.width+.5f && p.y>=-.5f && p.y<=Screen.height+.5f,"Clipped control "+name+" at "+Screen.width);}
    }
    string RealmIdentity(EntityDataSO data) {return data.realmComponentVersion+"|"+data.realmId+"|"+data.realmEntryLevel;}
    void AssertLegacyLevelRealm(EntityDataSO data,int entry,int total)
    {
        RealmProgressionResult state;
        Require(data.realmComponentVersion==1 && data.realmId==LegacyRealmSnapshotCompatibility.LuyenKhiId && data.realmEntryLevel==entry && data.currentLevel==total,"Canonical Realm mismatch");
        Require(LegacyRealmSnapshotCompatibility.TryEvaluateLegacyLevelSnapshot(data,out state) && state.InternalLevel==total-entry+1,"Derived internal level mismatch");
    }
    IEnumerator DualProgressionV06()
    {
        phase=1;Require(!CultivationIntegrationGate.ProductionEnabled,"Unapproved cultivation must remain disabled");var data=gm.HeroData;
        Require(data.currentLevel==1 && data.currentExp==0,"New Hero baseline");string legacyId=data.realmId;int legacyEntry=data.realmEntryLevel;
        while(data.currentLevel<45)data.AddExp(data.expToNextLevel-data.currentExp);
        combat.RefreshRegisteredHeroStats(hero,false);
        Require(data.currentLevel==45 && data.currentExp==0 && data.statPoints==44 && data.realmId==legacyId && data.realmEntryLevel==legacyEntry,"EXP pipeline changed or inferred permanent realm");
        foreach(var size in new[]{new Vector2Int(600,338),new Vector2Int(800,450),new Vector2Int(1150,647),new Vector2Int(1360,680)})
        {
            yield return Resize(size.x,size.y);ui.OpenHeroStats();yield return null;ui.RefreshCharacterProgression();
            var label=(TMPro.TextMeshProUGUI)Get(ui,"progressionSummary");label.ForceMeshUpdate();
            Require(label.text.Contains("Level 45") && label.text.Contains(CultivationIntegrationGate.Status) && !label.text.Contains("Viên mãn") && !label.text.Contains("Sơ kỳ") && !label.text.Contains("Nội bộ"),"Level must not infer cultivation stage");
            Require(!label.isTextOverflowing,"Gated Character overflow "+size.x);Onscreen("CharacterAllocatePhysical");Onscreen("DamageClose");
            yield return Capture("dual-v06-gated-"+size.x);ui.CloseDamageInfo();
        }
        Pass("REVISED MainGame EXP to45 leaves v1 snapshot realm metadata unchanged; Character has no Level-derived stage/internal level and explicitly marks cultivation unconfigured at four actual sizes.");Status(1,"PASS");
        phase=2;Require(gm.TryManualSave(SaveSlot.ManualSave1),"Legacy-compatible player component Save in owned fixture");
        string json=File.ReadAllText(Path.Combine(SavePath,"ManualSave1.json"));
        Require(!json.Contains("\"unrefinedQi\"") && !json.Contains("\"burnReference\"") && !json.Contains("\"bloodCharges\""),"Do not publish incomplete cultivation schema into Hero Save");
        ui.OpenHeroStats();yield return null;var old=data;
        Require(gm.LoadAndContinue(SaveSlot.ManualSave1),"Retained v1 Save Load");yield return Ready();
        Require(!ReferenceEquals(old,gm.HeroData) && gm.HeroData.currentLevel==45 && gm.HeroData.statPoints==44 && gm.HeroData.realmId==legacyId && gm.HeroData.realmEntryLevel==legacyEntry,"Retained snapshot authority lost");
        ui.OpenHeroStats();yield return null;ui.RefreshCharacterProgression();Require(((TMPro.TextMeshProUGUI)Get(ui,"progressionSummary")).text.Contains(CultivationIntegrationGate.Status),"No old stage label after Load");ui.CloseDamageInfo();
        Pass("REVISED owned MainGame Save/Load keeps v1 fields/Level/points but does not create Qi/blood/debt fields or fake cultivation UI. No cultivation scheduler or Burning effect activated in game.");
        Status(2,"PASS"); // This gated mode covers two checks; unrelated phases stay NOT RUN.
    }

    IEnumerator LegacyLevelRealmV06()
    {
        phase=1;var data=gm.HeroData;AssertLegacyLevelRealm(data,1,1);int growth=data.meleeGrowthBonus;
        foreach(int target in new[]{1,9,10,19,20,29,30,45})
        {
            while(data.currentLevel<target)data.AddExp(data.expToNextLevel-data.currentExp);
            combat.RefreshRegisteredHeroStats(hero,false);AssertLegacyLevelRealm(data,1,target);
            Require(data.currentExp==0 && data.statPoints==target-1,"EXP/points duplicate or reset");
            Require(target==1?data.meleeGrowthBonus==growth:data.meleeGrowthBonus>=50*(target-1) && data.meleeGrowthBonus<=100*(target-1),"Growth changed outside existing per-level rolls");
            ui.OpenHeroStats();yield return null;ui.RefreshCharacterProgression();
            var text=(TMPro.TextMeshProUGUI)Get(ui,"progressionSummary");
            Require(text.text.Contains("Level "+target) && text.text.Contains("Luyện Khí") && !text.text.Contains("legacy/prototype") && !text.text.Contains("Trúc Cơ"),"Character Realm label wrong at "+target);
            if(target>=30)Require(text.text.Contains("Viên mãn") && text.text.Contains("Bình cảnh"),"Missing bottleneck label");
            ui.CloseDamageInfo();
        }
        foreach(var size in new[]{new Vector2Int(600,338),new Vector2Int(800,450),new Vector2Int(1150,647),new Vector2Int(1360,680)})
        {
            yield return Resize(size.x,size.y);ui.OpenHeroStats();yield return null;ui.RefreshCharacterProgression();
            var text=(TMPro.TextMeshProUGUI)Get(ui,"progressionSummary");text.ForceMeshUpdate();Require(!text.isTextOverflowing,"Realm summary overflow "+size.x);
            foreach(string name in new[]{"CharacterAllocateHP","CharacterAllocatePhysical","DamageClose"})Onscreen(name);
            yield return Capture("v06-realm-"+size.x);ui.CloseDamageInfo();
        }
        Pass("V06 real New Hero/AddExp stages1/9/10/19/20/29/30/45, Level/points/growth unchanged by Realm; Character actual Game View four sizes, no overflow; no trial button added. EXP granted by fixture, not natural long farm.");Status(1,"PASS");
        phase=2;yield return Resize(800,450);
        AssertIsolated();Require(gm.TryManualSave(SaveSlot.ManualSave1),"Save immediately after level progression");
        string expectedRealm=RealmIdentity(data);int exp=data.currentExp,points=data.statPoints;growth=data.meleeGrowthBonus;
        ui.OpenHeroStats();yield return null;var retired=data;
        Require(gm.LoadAndContinue(SaveSlot.ManualSave1),"Load while Character open");yield return Ready();data=gm.HeroData;
        AssertLegacyLevelRealm(data,1,45);Require(!ReferenceEquals(data,retired) && data.currentExp==exp && data.statPoints==points && data.meleeGrowthBonus==growth,"Load did not replace canonical snapshot faithfully");
        ui.OpenHeroStats();yield return null;ui.RefreshCharacterProgression();
        int physical=data.addedDamage;Press("CharacterAllocatePhysical");yield return null;
        Require(data.statPoints==points-1 && data.addedDamage==physical+1 && retired.statPoints==points && RealmIdentity(data)==expectedRealm,"Allocation changed Realm/retired Hero or duplicated");ui.CloseDamageInfo();
        var item=EquipmentFoundation.CreateCommon(EquipmentFoundation.DemoSword,1,4);
        Require(EquipmentRuntime.AddInstance(data,item) && EquipmentInventory.Add(data,EquipmentRefinement.MaterialId,12),"Gear/Forge fixture stock");item=EquipmentRuntime.Find(data,item.instanceId);
        Require(gm.EquipGear(item.instanceId,EquipmentSlot.Weapon) && RealmIdentity(data)==expectedRealm,"Equip changed Realm");
        string message;Require(gm.ApplyForgeTest(item.instanceId,EquipmentRefinement.Operation.NormalUp,out message) && RealmIdentity(data)==expectedRealm,"Forge changed Realm");
        Require(gm.UnequipGear(EquipmentSlot.Weapon) && RealmIdentity(data)==expectedRealm,"Unequip changed Realm");
        Pass("V06 actual Save/Load after EXP and open Character, canonical replacement, one allocation targets new Hero only; equip/Forge/unequip preserve Realm.");Status(2,"PASS");
        phase=3;
        if(Enemies.Count==0)Call(gm,"CallNextWave");Require(Enemies.Count>0,"Need live combat for failed Load");
        ui.SetPaused(false);yield return null;var live=gm.HeroData;var liveState=combat.CurrentHeroCombatState;int liveLife=liveState.LifeVersion;
        string bad=JsonUtility.ToJson(live,true).Replace("\"realmComponentVersion\": 1","\"realmComponentVersion\": 2");Require(bad!=JsonUtility.ToJson(live,true),"Future fixture must change source");
        File.WriteAllText(Path.Combine(SavePath,"ManualSave2.json"),bad);
        Require(!gm.LoadAndContinue(SaveSlot.ManualSave2) && ReferenceEquals(gm.HeroData,live) && combat.CurrentHeroCombatState==liveState && liveState.LifeVersion==liveLife && Enemies.Count>0,"Failed Load interrupted live canonical combat");
        ui.SetPaused(true);File.Delete(Path.Combine(SavePath,"ManualSave2.json")); // only owned fixture file
        WorldMapNode current;Require(WorldMapCatalog.TryGet(data.worldMapId,out current),"Current node");string destination=null;
        foreach(string id in current.Links){WorldMapNode n;if(WorldMapCatalog.TryGet(id,out n) && !n.IsBoss){destination=id;break;}}
        Require(destination!=null,"Normal neighbor");if(!data.worldMapCleared.Contains(current.Id))data.worldMapCleared.Add(current.Id); // explicit permission fixture, not five natural waves
        Require(gm.TryTravelToWorldMap(destination),"Fixture-authorized Travel");yield return Ready();data=gm.HeroData;AssertLegacyLevelRealm(data,1,45);
        Require(gm.TryManualSave(SaveSlot.ManualSave2) && gm.LoadAndContinue(SaveSlot.ManualSave2),"Load after Travel");yield return Ready();data=gm.HeroData;
        AssertLegacyLevelRealm(data,1,45);Require(data.worldMapId==destination,"Travel snapshot not restored");
        Call(combat,"ApplyIncomingDamage",1000000,"[V06 fixture defeat]",null,null);yield return null;gm.OnRetryClicked();yield return Ready();
        AssertLegacyLevelRealm(gm.HeroData,1,45);Require(combat.CurrentHeroCombatState.Data==gm.HeroData && Get(space,"cameraTarget")==hero,"Retry stale data/camera binding");
        Pass("V06 future Realm load refused during actual running combat without canonical/life/encounter replacement; fixture-authorized Travel -> Save/Load and fixture defeat -> Retry preserve Realm and binding.");Status(3,"PASS");
        phase=4;data=gm.HeroData;
        string legacy=Regex.Replace(JsonUtility.ToJson(data,true),@"^\s*""(realmComponentVersion|realmId|realmEntryLevel)""\s*:[^\r\n]*\r?\n?","",RegexOptions.Multiline);
        File.WriteAllText(Path.Combine(SavePath,"ManualSave1.json"),legacy);data.realmEntryLevel=45;
        ui.OpenHeroStats();yield return null;ui.RefreshCharacterProgression();Require(((TMPro.TextMeshProUGUI)Get(ui,"progressionSummary")).text.Contains("Sơ kỳ 1"),"Preload distinct entry fixture");
        Require(gm.LoadAndContinue(SaveSlot.ManualSave1),"MainGame conservative legacy migration");yield return Ready();AssertLegacyLevelRealm(gm.HeroData,1,45);
        ui.OpenHeroStats();yield return null;ui.RefreshCharacterProgression();Require(((TMPro.TextMeshProUGUI)Get(ui,"progressionSummary")).text.Contains("Viên mãn"),"Stale Realm UI after legacy Load");ui.CloseDamageInfo();
        var original=gm.heroDataSO;var template=UnityEngine.Object.Instantiate(original);template.realmComponentVersion=99;template.realmId="template:invalid";template.realmEntryLevel=999;
        gm.heroDataSO=template;Call(gm,"InitHeroData",false);gm.heroDataSO=original;UnityEngine.Object.Destroy(template);
        AssertLegacyLevelRealm(gm.HeroData,1,1);Require(gm.HeroData.currentExp==0 && gm.HeroData.statPoints==0,"New after legacy Load retained progression");
        Pass("V06 MainGame legacy Load resets entry from distinct live state, UI follows current canonical Hero; New Run after legacy Load overrides contaminated cloned template and starts Level1/LuyenKhi/entry1 without old EXP/points.");Status(4,"PASS");
    }

    IEnumerator DevelopmentV04()
    {
        phase=1;var data=gm.HeroData;
        var item=EquipmentFoundation.CreateCommon(EquipmentFoundation.DemoSword,1,4);
        Require(EquipmentRuntime.AddInstance(data,item) && EquipmentInventory.Add(data,EquipmentRefinement.MaterialId,12),"V04 fixture gear/material");
        item=EquipmentRuntime.Find(data,item.instanceId);
        string longName="";for(int i=0;i<28;i++)longName+=(i==0?"":" ")+"TrườngDanh"+i;
        Require(EquipmentInventory.Add(data,longName,2),"V04 long fallback item label");
        foreach(var size in new[]{new Vector2Int(600,338),new Vector2Int(1150,647)})
        {
            yield return Resize(size.x,size.y);yield return OpenInventory();
            Set(inventory,"selectedKey","stack:"+longName);Call(inventory,"SetDetailView",false);yield return null;
            var pages=(IList)Get(inventory,"detailPages");string joined="";
            for(int i=0;i<pages.Count;i++)
            {
                if(i>0)Call(inventory,"ChangeDetailPage",1);
                var text=(TMPro.TextMeshProUGUI)Get(inventory,"detail");text.ForceMeshUpdate();
                Require(!text.isTextOverflowing,"V04 long label detail clipped at "+size.x+" page "+i);joined+=" "+text.text;
            }
            for(int i=0;i<28;i++)Require(joined.Contains("TrườngDanh"+i),"V04 lost long label token "+i);
            Require(EquipmentInventory.Count(data,longName)==2,"V04 display changed item ID/quantity");
            Onscreen("InventoryClose");Onscreen("InventoryDetailNext");
            yield return Capture("v04-long-label-"+size.x);yield return CloseInventory();
        }
        Pass("V04 long fallback item name: all 28 tokens reachable through detail pages; controls in bounds; ID/quantity unchanged. 600/1150 input sizes, actual expanded Inventory size recorded in screenshots.");
        yield return OpenInventory();Select(item.instanceId);Press("InventoryForge");yield return null;
        var under=Find("InventoryClose");var point=RectTransformUtility.WorldToScreenPoint(null,under.TransformPoint(under.rect.center));
        var hit=ui.HitControl(point);Require(hit!=null && hit.GetRect()!=under,"V04 Forge modal exposes underlying close");
        ui.HandlePointerClick(point,point);yield return null;
        Require(inventory.IsOpen && inventory.IsForgeOpen && !item.isEquipped,"V04 click reached underlying Inventory");
        int stock=EquipmentInventory.Count(data,EquipmentRefinement.MaterialId);
        Require(EquipmentInventory.Add(data,EquipmentRefinement.MaterialId,3),"V04 external stock change");
        yield return new WaitForSecondsRealtime(.3f);
        Require(((TMPro.TextMeshProUGUI)Get(inventory,"forgeMaterialLabel")).text.Contains((stock+3).ToString()),"V04 Forge stale material count");
        data.equipmentItems.Remove(item);data.isDirty=true;
        yield return new WaitForSecondsRealtime(.3f);
        Require(!inventory.IsForgeOpen,"V04 Forge retained missing item");
        Require(EquipmentRuntime.AddInstance(data,item),"V04 restore owned fixture gear");
        yield return CloseInventory();
        Pass("V04 Forge modal blocks underlying close through Unity UI routing; unscaled refresh sees changed stock and closes when selected gear disappears. Not OS-native input.");Status(1,"PASS");

        phase=2;yield return Resize(800,450);
        var visual=LootDropPresentation.Install(combat);visual.SetInventoryTarget(null);
        var field=(RectTransform)Get(space,"battleArea");
        visual.Show(field,new Vector2(0,40),EquipmentFoundation.DemoSword,"v04-cosmetic-only",null);
        Require(visual.Count==1,"V04 cosmetic fixture did not start");
        visual.Advance(.3f,null);yield return Resize(600,338);visual.Advance(.35f,null);
        Require(visual.Count==1,"V04 resize during falling lost receipt");
        visual.Advance(.5f,null);
        var drops=(IList)Get(visual,"drops");var drop=drops[0];var root=(RectTransform)Get(drop,"Root");
        Require((bool)Get(drop,"Collecting"),"V04 receipt did not enter flight");
        var start=(Vector2)Get(drop,"FlightStart");
        yield return Resize(1150,647);visual.Advance(.15f,null);
        float age=(float)Get(drop,"Age");float t=Mathf.Clamp01((age-1.1f)/.55f);float ease=t*t;
        float expectedX=Mathf.Lerp(start.x,Screen.width+16f,ease);
        Require(Mathf.Abs(root.anchoredPosition.x-expectedX)<.1f,"V04 flight destination did not follow resized screen");
        Require(EquipmentInventory.Count(data,longName)==2 && EquipmentRuntime.Find(data,item.instanceId)!=null,"V04 visual mutated inventory");
        yield return Capture("v04-loot-resize");visual.Advance(1f,null);Require(visual.Count==0,"V04 receipt did not retire");
        Pass("V04 actual Game View resize during drop and UI flight; dynamic right-edge trajectory matches screen, receipt retires, inventory unchanged. Explicit Advance while paused, not natural-duration/native playback.");Status(2,"PASS");

        phase=3;while(data.statPoints<2)data.AddExp(data.expToNextLevel);
        Require(gm.TryManualSave(SaveSlot.ManualSave1),"V04 owned Save");
        yield return OpenInventory();Select(item.instanceId);Press("InventoryForge");yield return null;
        var beforeLoad=data;int oldAdded=data.addedDamage;
        Require(gm.LoadAndContinue(SaveSlot.ManualSave1),"V04 Load from Forge");yield return Ready();
        Require(!inventory.IsOpen && !inventory.IsForgeOpen,"V04 stale modal after Load");
        ui.OpenHeroStats();yield return null;ui.RefreshCharacterProgression();int points=gm.HeroData.statPoints;
        Press("CharacterAllocatePhysical");yield return null;
        Require(gm.HeroData.statPoints==points-1 && gm.HeroData.addedDamage==oldAdded+1 && beforeLoad.addedDamage==oldAdded,"V04 stale/duplicate allocation after Load");
        ui.CloseDamageInfo();data=gm.HeroData;
        Call(combat,"ApplyIncomingDamage",1000000,"[V04 fixture defeat]",null,null);yield return null;gm.OnRetryClicked();yield return Ready();
        Require(combat.CurrentHeroCombatState.Data==gm.HeroData && Get(space,"cameraTarget")==hero && !inventory.IsOpen && !inventory.IsForgeOpen,"V04 Retry stale bindings");
        yield return OpenInventory();Select(item.instanceId);Press("InventoryEquip");yield return null;
        Require(EquipmentRuntime.Find(gm.HeroData,item.instanceId).isEquipped,"V04 post-Retry Inventory stale item");
        yield return CloseInventory();
        Pass("V04 Load/Retry close old modals; one allocation changes only new canonical Hero; post-Retry Inventory equip and existing camera/combat bindings point at current actor.");Status(3,"PASS");Status(4,"PASS");
    }

    IEnumerator StabilityV03()
    {
        phase=1;var data=gm.HeroData;
        // UI fixture stock, not a production income or balance claim.
        var item=EquipmentFoundation.CreateCommon(EquipmentFoundation.DemoSword,1,4);
        Require(EquipmentRuntime.AddInstance(data,item) && EquipmentInventory.Add(data,EquipmentRefinement.MaterialId,11),"UI fixture gear/stock");
        item=EquipmentRuntime.Find(data,item.instanceId); // AddInstance stores a canonical copy.
        data.entityName="Nguyễn Bách Luyện Thiên Đồ Trường Danh";
        while(data.statPoints<8)data.AddExp(data.expToNextLevel);
        combat.RefreshRegisteredHeroStats(hero,false);
        foreach(var size in new[]{new Vector2Int(600,338),new Vector2Int(800,450),new Vector2Int(1150,647),new Vector2Int(1360,680)})
        {
            yield return Resize(size.x,size.y);ui.OpenHeroStats();yield return null;ui.RefreshCharacterProgression();
            var summary=(TMPro.TextMeshProUGUI)Get(ui,"progressionSummary");summary.ForceMeshUpdate();Require(!summary.isTextOverflowing,"Long Character name header overflow "+size.x);
            foreach(string n in new[]{"CharacterAllocateHP","CharacterAllocatePhysical","DamageClose"})Onscreen(n);
            int points=data.statPoints,physical=data.addedDamage;Press("CharacterAllocatePhysical");yield return null;
            Require(data.statPoints==points-1 && data.addedDamage==physical+1,"Repeated UI binding duplicated allocation listener");
            yield return Capture("v03-character-"+size.x);Press("DamageClose");yield return null;
            yield return OpenInventory();Select(item.instanceId);yield return null;
            Onscreen("InventoryClose");Onscreen("InventoryEquip");Onscreen("InventoryForge");
            Press("InventoryEquip");yield return null;Require(item.isEquipped,"Equip routing");
            Press("EquipmentSlot0");Press("InventoryUnequip");yield return null;Require(!item.isEquipped,"Unequip routing");Select(item.instanceId);
            Press("InventoryForge");yield return null;Require(inventory.IsForgeOpen,"Forge modal");
            foreach(string n in new[]{"Forge_Close","Forge_Up1","Forge_Up2"})Onscreen(n);
            yield return Capture("v03-forge-"+size.x);Press("Forge_Close");yield return null;Press("InventoryClose");yield return Resize(size.x,size.y);
            Press("WorldMapButton");yield return null;Onscreen("WorldMapClose");Press("WorldMapClose");yield return null;
            Pass("V03 routed UI "+size.x+"x"+size.y+": long Hero name, allocation one mutation, Inventory expansion, equip/unequip, Forge and Map open/close; NOT native mouse.");
        }
        phase=2;yield return Resize(800,450);ui.SetPaused(true);
        var expected=CanonicalExpected(data);Require(gm.TryManualSave(SaveSlot.ManualSave1),"UI fixture Save");
        yield return OpenInventory();Select(item.instanceId);Press("InventoryForge");yield return null;
        Require(gm.LoadAndContinue(SaveSlot.ManualSave1),"Load from open modal");yield return Ready();Matches(expected,"V03 modal Load");
        Require(!inventory.IsOpen && !inventory.IsForgeOpen,"Load kept stale modal");
        ui.OpenHeroStats();yield return null;ui.RefreshCharacterProgression();
        int oldPoints=gm.HeroData.statPoints;Press("CharacterAllocateHP");yield return null;
        Require(gm.HeroData.statPoints==oldPoints-1 && data.statPoints==expected.points,"UI changed old Hero after Load");ui.CloseDamageInfo();
        ui.SetPaused(false);yield return null;yield return new WaitForEndOfFrame();
        Require(Get(space,"cameraTarget")==hero && combat.CurrentHeroCombatState.Data==gm.HeroData,"Camera/combat stale after Load");ui.SetPaused(true);
        Pass("V03 Load from Forge closes modal, allocation targets fresh canonical Hero only; camera/combat rebind.");
        phase=3;data=gm.HeroData;WorldMapNode node;Require(WorldMapCatalog.TryGet(data.worldMapId,out node),"World node");string origin=node.Id,destination=null;
        foreach(var id in node.Links){WorldMapNode candidate;if(WorldMapCatalog.TryGet(id,out candidate) && !candidate.IsBoss){destination=id;break;}}
        Require(destination!=null,"Missing normal neighbor");string before=JsonUtility.ToJson(data);
        Require(!gm.TryTravelToWorldMap(destination) && JsonUtility.ToJson(data)==before,"Blocked Travel mutated Hero");
        // Deterministic lifecycle traversal, real accepted damage/death path; not natural farm timing.
        for(int region=0;region<2;region++)
        {
            string current=data.worldMapId;
            for(int w=0;w<5;w++) {
                if(Enemies.Count==0)Call(gm,"CallNextWave");
                var enemies=new List<CombatManager.ActiveMonsterInfo>(Enemies);Require(enemies.Count>0,"Missing wave");
                foreach(var m in enemies)Call(combat,"DealDamageFromHero",m,int.MaxValue,combat.CurrentHeroCombatState,null);
                Require(data.worldMapId==current,"Wave auto-travelled");
            }
            Require(WorldMapCatalog.IsCleared(data,current),"Five accepted waves did not clear");
            string next=region==0?destination:origin;Require(gm.TryTravelToWorldMap(next),"Legal/re-entry Travel");yield return Ready();data=gm.HeroData;
        }
        Require(data.worldMapId==origin && WorldMapCatalog.IsCleared(data,origin) && WorldMapCatalog.IsCleared(data,destination) && data.worldMapLoopWave==1,"Re-entry changed clear/wave state");
        expected=CanonicalExpected(data);Require(gm.TryManualSave(SaveSlot.ManualSave2) && gm.LoadAndContinue(SaveSlot.ManualSave2),"Travel Save/Load");yield return Ready();Matches(expected,"V03 re-entry Load");
        ui.SetPaused(false);yield return null;yield return new WaitForEndOfFrame();Require(Get(space,"cameraTarget")==hero,"Camera Travel/Load binding");ui.SetPaused(true);
        Call(combat,"ApplyIncomingDamage",1000000,"[V03 fixture defeat]",null,null);yield return null;gm.OnRetryClicked();yield return Ready();
        ui.SetPaused(false);yield return null;yield return new WaitForEndOfFrame();Require(Get(space,"cameraTarget")==hero && combat.CurrentHeroCombatState.Data==gm.HeroData,"Camera Retry binding");ui.SetPaused(true);
        Pass("V03 blocked Travel is mutation-free; real lifecycle clears two nodes; explicit adjacent Travel and cleared-node re-entry; Save/Load then defeat/Retry retain binding.");
        Status(1,"PASS");Status(2,"PASS");Status(3,"PASS");Status(4,"PASS");
    }

    IEnumerator RecoveryV03()
    {
        phase=3;Require(!string.IsNullOrEmpty(replayProfile),"Owned replay required");
        Require(gm.LoadAndContinue(SaveSlot.ManualSave1),"Recovery Load");yield return Ready();
        string gear=GearCore(gm.HeroData);int hpAdded=gm.HeroData.addedHealth,physical=gm.HeroData.addedDamage,points=gm.HeroData.statPoints;
        int source=WeaponBasicAttack.DamageSource(gm.HeroData),events=gm.DamageHistory.Length;
        ui.SetPaused(false);yield return Until(()=>{var rows=gm.DamageHistory;for(int i=events;i<rows.Length;i++)if(rows[i].Contains("; tổng: "+source+";"))return true;return false;},45,"natural post-Load damage");ui.SetPaused(true);
        Require(Get(space,"cameraTarget")==hero && combat.CurrentHeroCombatState.Data==gm.HeroData && GearCore(gm.HeroData)==gear && gm.HeroData.addedHealth==hpAdded && gm.HeroData.addedDamage==physical && gm.HeroData.statPoints==points,"Post-Load combat lost canonical data/binding");
        Pass("V03 recovery: actual Combat.Update after real Load consumed restored source="+source+" with current camera binding.");
        var expected=CanonicalExpected(gm.HeroData);Require(gm.TryManualSave(SaveSlot.ManualSave2) && gm.LoadAndContinue(SaveSlot.ManualSave2),"Recovery second Save/Load");yield return Ready();Matches(expected,"V03 recovery roundtrip");
        Call(combat,"ApplyIncomingDamage",1000000,"[V03 recovery fixture defeat]",null,null);yield return null;gm.OnRetryClicked();yield return Ready();
        Require(GearCore(gm.HeroData)==gear && gm.HeroData.addedHealth==hpAdded && gm.HeroData.addedDamage==physical && gm.HeroData.statPoints==points,"Load then Retry lost gear/allocation");
        events=gm.DamageHistory.Length;ui.SetPaused(false);yield return Until(()=>gm.DamageHistory.Length>events,45,"natural post-Retry combat");ui.SetPaused(true);
        Require(Get(space,"cameraTarget")==hero && combat.CurrentHeroCombatState.Data==gm.HeroData,"Retry combat stale binding");
        Pass("V03 recovery: saved active progression, real Load then fixture defeat/Retry, natural combat continues; gear/allocation and camera binding preserved.");Status(3,"PASS");Status(4,"PASS");
    }

    IEnumerator Run()
    {
        yield return null;
        gm=saveOwner.GetComponent<GameManager>();combat=saveOwner.GetComponent<CombatManager>();ui=UIManager.Instance;
        space=(EnvironmentManager)Get(gm,"environmentManager");hero=(HeroController)Get(gm,"heroController");
        Require(ui!=null && space!=null && hero!=null,"MainGame references missing.");
        AssertIsolated();ui.IsolatedValidationProfile=true;
        // A personal DEVB file may otherwise hide the level-1 gear increment.
        // Reset only static state inside this disposable test process; no file write.
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationCultivationV11C")>=0 || Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationCultivationV11B")>=0 || Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationCultivationV09UICap")>=0 || Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationCultivationV09")>=0 || Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationCultivationV08")>=0 || Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationCultivationV07")>=0 || Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationDualProgressionV06")>=0 || Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationRealmV06")>=0 || Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationDevelopmentV04")>=0 || Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationRecoveryV03")>=0 || Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationStabilityV03")>=0 || Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationCameraV03")>=0 || Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationGearDropV01")>=0 || Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationProgressionV02")>=0)CombatBalance.ResetDevProfiles();
        yield return Resize(800,450);Call(gm,"InitHeroData",false);
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationCultivationV07")>=0) {
            Require(gm.Cultivation!=null && gm.Cultivation.Current.Foundation.Amount==0 && gm.Cultivation.Current.Reserve.Amount==0,"New session starts at zero independently of Level");
            gm.HeroData.worldMapId="TV-20";WorldMapCatalog.Normalize(gm.HeroData); // Explicit starting-node fixture.
        }
        Call(gm,"DeployHero",false);yield return Ready();
        inventory=(InventoryPanelUI)Get(ui,"inventoryUI");Require(inventory!=null,"No Inventory panel.");
        ui.inventoryLayoutDebug=false;ui.inventoryUseArtTheme=false;ui.ApplyHudFromInspector();
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationCultivationV11C")>=0) {yield return CultivationV11C();yield break;}
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationCultivationV11B")>=0) {yield return CultivationV11B();yield break;}
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationCultivationV09UICap")>=0) {yield return CultivationV09UICap();yield break;}
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationCultivationV09")>=0) {yield return CultivationV09();yield break;}
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationCultivationV08")>=0) {yield return CultivationV08();yield break;}
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationCultivationV07")>=0) {yield return CultivationV07();yield break;}
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationDualProgressionV06")>=0) {yield return DualProgressionV06();yield break;}
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationRealmV06")>=0)throw new InvalidOperationException("Retired Level-based verifier. Use -foundationDualProgressionV06.");
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationDevelopmentV04")>=0) {yield return DevelopmentV04();yield break;}
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationRecoveryV03")>=0) {yield return RecoveryV03();yield break;}
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationStabilityV03")>=0) {yield return StabilityV03();yield break;}
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationCameraV03")>=0) {yield return CameraV03();yield break;}
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationProgressionV02")>=0) {
            yield return ProgressionV02();yield break;
        }
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationGearDropV01")>=0)
        {
            yield return GearDropV01();yield break;
        }
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationV01MaterialSmoke")>=0)
        {
            var cfg=gm.ProductionLoot;
            Require(cfg!=null && cfg.configured && cfg.material.enabled && cfg.material.chance==.60 &&
                cfg.material.materialId==EquipmentRefinement.MaterialId && cfg.material.minQuantity==1 && cfg.material.maxQuantity==2,
                "MainGame material prototype must be authored explicitly.");
            // Material-only regression remains available after gear is configured.
            cfg.equipment.enabled=false;
            Require(!gm.ControlledLootTestEnabled,"Production smoke must not use controlled loot A.");
            Require(!string.IsNullOrEmpty(replayProfile) && gm.LoadAndContinue(SaveSlot.ManualSave1),"Smoke requires an owned replay slot.");
            yield return Ready();
            // Deterministic smoke only: the scene copy already asserted 0.60.
            // Altering this in-memory test instance never edits MainGame balance.
            cfg.material.chance=1;
            int stock=EquipmentInventory.Count(gm.HeroData,EquipmentRefinement.MaterialId),gear=gm.HeroData.equipmentItems.Count;
            int level=gm.HeroData.currentLevel;float exp=gm.HeroData.currentExp;
            ui.SetPaused(false);
            yield return Until(()=>EquipmentInventory.Count(gm.HeroData,EquipmentRefinement.MaterialId)>stock,45,"natural production material kill");
            ui.SetPaused(true);int gained=EquipmentInventory.Count(gm.HeroData,EquipmentRefinement.MaterialId)-stock;
            var smokeLoot=combat.GetComponent<LootDropPresentation>();
            Require(gained>=1 && gained<=2 && gm.HeroData.equipmentItems.Count==gear && (smokeLoot==null || smokeLoot.Count==0),"Material must not fabricate gear or sword VFX.");
            Require(gm.HeroData.currentLevel!=level || gm.HeroData.currentExp!=exp,"Reward must follow a real combat kill.");
            AssertIsolated();Pass("V01 MATERIAL SMOKE: MainGame serialized 0.60/1-2; fixture-only chance 1; natural kill granted "+gained+" material, no gear/VFX, controlled A off.");
            yield break;
        }
        if(!string.IsNullOrEmpty(replayProfile))
        {
            phase=3;Set(gm,"controlledLootTest",true);
            var raw=ScriptableObject.CreateInstance<EntityDataSO>();
            JsonUtility.FromJsonOverwrite(File.ReadAllText(Path.Combine(SavePath,"ManualSave1.json")),raw);
            Snapshot saved=CanonicalExpected(raw);string savedId=saved.weapon;UnityEngine.Object.Destroy(raw);
            Require(gm.LoadAndContinue(SaveSlot.ManualSave1),"Replay Load1 failed.");yield return Ready();yield return Resize(800,450);Matches(saved,"Replay canonical saved snapshot");Pass("Read-only legacy metadata diagnosis confirmed; canonical snapshot and authoritative gear are identical after real Load.");
            yield return OpenInventory();Select(savedId);Press("InventoryForge");yield return null;Require(inventory.IsForgeOpen,"Replay Forge UI did not open.");
            yield return PersistenceAndFinal(saved,savedId);yield break;
        }
        phase=1;
        Press("PauseButton");Require(!ui.IsPaused,"Resume button failed.");Press("PauseButton");Require(ui.IsPaused,"Pause button failed.");
        Press("StatsButton");yield return null;Require(((GameObject)Get(ui,"damagePanel")).activeSelf,"Character panel failed.");Press("DamageClose");yield return null;
        Press("WorldMapButton");yield return null;Require(((WorldMapPanelUI)Get(ui,"worldMapUI")).IsOpen,"World map failed.");Press("WorldMapClose");yield return null;
        Press("CompactLogToggle");yield return null;Require(gm.eventLog.gameObject.activeInHierarchy,"Short log did not show.");Press("CompactLogToggle");yield return null;
        Press("SystemMenuButton");yield return null;Press("Tools");yield return null;Press("MusicVolumeButton");yield return null;
        Require(((MusicVolumePanelUI)Get(ui,"musicVolumePanel")).IsOpen,"Audio panel failed.");Press("VolumeClose");yield return null;
        Press("SystemMenuButton");yield return null;Press("Tools");yield return null;Press("DEVBButton");yield return null;
        Require(((DevBalanceUI)Get(ui,"devUI")).IsOpen,"Dev panel failed.");Press("DevCancel");yield return null;
        yield return OpenInventory();Require(Find("InventoryButton").gameObject.activeInHierarchy==false,"Underlying Inventory control still visible.");
        yield return Capture("phase1-inventory-default");yield return CloseInventory();Require(ui.IsPaused,"Inventory did not restore Pause.");
        yield return Capture("phase1-hud");Status(1,"PASS");Pass("MainGame UI panels/handlers/default backgrounds/Pause, real primary data; no cloned UI Hero.");

        phase=2;Set(gm,"controlledLootTest",true);Require(gm.ControlledLootTestEnabled,"Controlled profile unavailable.");
        int start=gm.HeroData.equipmentItems.Count;var lives=new List<CombatManager.ActiveMonsterInfo>(Enemies);
        ui.SetPaused(false);yield return Until(()=>gm.HeroData.equipmentItems.Count>start,45,"natural auto-combat kill and gear reward");ui.SetPaused(true);
        var loot=combat.GetComponent<LootDropPresentation>();Require(loot!=null && loot.Count>0,"Accepted reward has no loot graphic.");
        string itemId=gm.HeroData.equipmentItems[start].instanceId;Unique();int count=gm.HeroData.equipmentItems.Count;
        bool repeatedDeath=false;
        foreach(var old in lives)if(old.currentHP<=0){Call(combat,"HandleMonsterDeath",old);repeatedDeath=true;break;}
        Require(repeatedDeath,"No killed life found to exercise the duplicate death callback.");
        Require(gm.HeroData.equipmentItems.Count==count,"Duplicate death callback granted gear again.");
        var drop=FirstDrop(loot);var root=(RectTransform)Get(drop,"Root");float age=(float)Get(drop,"Age");var position=root.position;
        yield return new WaitForSecondsRealtime(.12f);Require((float)Get(drop,"Age")==age && root.position==position,"Pause moved the drop.");
        yield return Capture("phase2-drop-paused");ui.SetPaused(false);
        yield return Until(()=>(bool)Get(drop,"Landed"),3,"drop landing");ui.SetPaused(true);
        Require((float)Get(drop,"Age")>=.6f && !(bool)Get(drop,"Collecting"),"Drop/rest order is wrong.");
        yield return Capture("phase2-landed");ui.SetPaused(false);
        yield return Until(()=>(bool)Get(drop,"Collecting"),3,"half-second ground rest");ui.SetPaused(true);
        Require((float)Get(drop,"Age")>=1.1f && root.parent!=space.BattleArea,"Flight did not detach after rest.");
        loot.SetInventoryTarget(null);Require(((Vector2)Call(loot,"CollectionTarget")).x==Screen.width+16,"Missing target fallback is wrong.");
        ui.SetPaused(false);yield return new WaitForSeconds(.12f);ui.SetPaused(true);
        Require(((RectTransform)Get(drop,"Icon")).localScale.x<1f,"Collecting icon did not shrink.");yield return Capture("phase2-flight-fallback");
        loot.SetInventoryTarget(ui.InventoryLootTarget);ui.SetPaused(false);
        yield return Until(()=>loot.Count==0,8,"real receipt completion");ui.SetPaused(true);Unique();
        Require(EquipmentRuntime.Find(gm.HeroData,itemId)!=null,"Receipt completion lost awarded gear.");
        yield return OpenInventory();Select(itemId);int baseline=WeaponBasicAttack.DamageSource(gm.HeroData);
        Press("InventoryEquip");yield return null;Require(EquipmentRuntime.Find(gm.HeroData,itemId).isEquipped,"Equip handler failed.");
        Require(WeaponBasicAttack.DamageSource(gm.HeroData)>baseline,"Equipping awarded sword did not raise damage.");
        int afterEquip=gm.HeroData.equipmentItems.Count;
        Press("EquipmentSlot0");Press("InventoryUnequip");yield return null;
        Require(!EquipmentRuntime.Find(gm.HeroData,itemId).isEquipped && gm.HeroData.equipmentItems.Count==afterEquip,"Unequip lost/duplicated gear.");
        Select(itemId);Press("InventoryEquip");yield return null;yield return Capture("phase2-awarded-equipped");yield return CloseInventory();
        int expected=WeaponBasicAttack.DamageSource(gm.HeroData);string token="; tổng: "+expected+";";
        ui.SetPaused(false);yield return Until(()=>Array.Exists(gm.DamageHistory,x=>x.Contains(token)),35,"natural combat using equipped stats");ui.SetPaused(true);
        Pass("Natural kill → real unique gear → timed loot/paused animation/fallback → real inventory equip/unequip; natural damage trace consumes equipped total "+expected+".");Status(2,"PASS");

        phase=3;yield return OpenInventory();Press("EquipmentSlot0");Press("InventoryForge");yield return null;
        Require(inventory.IsForgeOpen,"Forge did not open for awarded equipped gear.");
        var item=EquipmentRuntime.Find(gm.HeroData,itemId);int refine=item.refineLevel;
        Require(EquipmentInventory.Count(gm.HeroData,EquipmentRefinement.MaterialId)==0,"Fresh profile unexpectedly has materials.");
        Press("Forge_Up1");yield return null;Require(item.refineLevel==refine,"Forge succeeded without materials.");
        Press("Forge_Close");Press("InventoryGrantMaterials");yield return null;
        int material=EquipmentInventory.Count(gm.HeroData,EquipmentRefinement.MaterialId);Require(material==100,"Existing test-material tool did not grant expected profile-only stock.");
        Press("InventoryForge");yield return null;Press("Forge_Up1");yield return null;
        Require(item.refineLevel==refine+1 && EquipmentInventory.Count(gm.HeroData,EquipmentRefinement.MaterialId)==material-EquipmentRefinement.RegularCost,"Normal Forge result/cost mismatch.");
        var rarity=item.rarity;Press("Forge_Up2");yield return null;
        Require(item.rarity==EquipmentRefinement.Next(rarity) && item.specialForgeCount>0 && item.forgeHistory.Count>0,"Special Forge result/history mismatch.");
        Require(EquipmentInventory.Count(gm.HeroData,EquipmentRefinement.MaterialId)==material-EquipmentRefinement.RegularCost-EquipmentRefinement.SpecialCost,"Special Forge resource mismatch.");
        Require(combat.CurrentHeroCombatState.Data==gm.HeroData,"Forge disconnected combat state.");yield return Capture("phase3-forged-equipped");
        var first=CanonicalExpected(gm.HeroData);AssertIsolated();Require(gm.TryManualSave(SaveSlot.ManualSave1),"Save1 failed while Forge open.");
        yield return PersistenceAndFinal(first,itemId);
    }
    IEnumerator ProgressionV02()
    {
        phase=1;var data=gm.HeroData;var state=combat.CurrentHeroCombatState;
        Require(data.currentLevel==1 && data.statPoints==0 && CombatBalance.HeroDev==null,"Fresh baseline required.");
        Press("StatsButton");yield return null;
        var summary=(TMPro.TextMeshProUGUI)Get(ui,"progressionSummary");
        string before=JsonUtility.ToJson(data);var dev=DevBalanceProfile.Defaults(false,space,hero.moveSpeed);dev.attack=1000;
        CombatBalance.SetDevProfile(false,dev);ui.RefreshCharacterProgression();
        Require(summary.text.Contains("DEVB Hero đang override") && summary.text.Contains("THỬ NGHIỆM CÔ LẬP"),"Override/isolation visibility missing.");
        Require(JsonUtility.ToJson(data)==before,"Visibility changed saved Hero data.");
        CombatBalance.ResetDevProfiles();ui.RefreshCharacterProgression();Require(summary.text.Contains("Baseline Hero"),"Baseline visibility missing.");
        Pass("V02 P1: live baseline/DEVB/isolation labels; reading visibility leaves Hero JSON unchanged; no settings file write.");
        phase=2;data.AddExp(CombatBalance.RequiredExp(1));combat.RefreshRegisteredHeroStats(hero,false);ui.RefreshCharacterProgression();
        Require(data.currentLevel==2 && data.statPoints==1,"Level-up point mismatch.");
        state.CurrentHP=state.MaxHP/2;state.AttackTimer=.42f;int hp=state.CurrentHP,max=state.MaxHP,life=state.LifeVersion;
        Press("CharacterAllocateHP");yield return null;
        Require(data.addedHealth==6 && data.statPoints==0 && state.MaxHP==max+6 && state.CurrentHP==Mathf.RoundToInt((float)hp/max*state.MaxHP) && state.CurrentHP<state.MaxHP,
            "HP allocation must preserve existing health ratio, not heal to full.");
        Require(state.AttackTimer==.42f && state.LifeVersion==life,"Allocation reset attack/life.");
        before=JsonUtility.ToJson(data);Require(!gm.AllocateHeroHealth() && !gm.AllocateHeroPhysicalAttack() && JsonUtility.ToJson(data)==before,"Zero points mutated data.");
        Require(!((CustomInteractable)Get(ui,"allocateHealthInput")).enabled,"Zero-point button still interactive.");
        data.AddExp(data.expToNextLevel);combat.RefreshRegisteredHeroStats(hero,false);ui.RefreshCharacterProgression();
        int physical=WeaponBasicAttack.Physical(data);Press("CharacterAllocatePhysical");yield return null;
        Require(data.addedDamage==1 && data.statPoints==0 && WeaponBasicAttack.Physical(data)==physical+1 && WeaponBasicAttack.Magical(data)==0,"Physical allocation wrong.");
        Pass("V02 P2: real UI hit-test/click handlers, EXP +1 point/level, HP +6 ratio preserved, Physical +1; zero points blocked, timer/life intact.");
        phase=3;int level=data.currentLevel;int[] boundaries={1,10,11,100,101};string[] ranks={"Luyện Khí","Luyện Khí","Trúc Cơ","Chân Tiên","Tiên Đế"};
        for(int i=0;i<boundaries.Length;i++) {data.currentLevel=boundaries[i];before=JsonUtility.ToJson(data);ui.RefreshCharacterProgression();Require(summary.text.Contains("legacy/prototype") && summary.text.Contains(ranks[i]) && JsonUtility.ToJson(data)==before,"Legacy realm mapping/presentation mutation.");}
        data.currentLevel=level;ui.RefreshCharacterProgression();
        foreach(var size in new[]{new Vector2Int(600,338),new Vector2Int(800,450)}) {
            yield return Resize(size.x,size.y);ui.RefreshCharacterProgression();summary.ForceMeshUpdate();
            Require(!summary.isTextOverflowing,"Progression header overflows at "+size.x);
            foreach(var name in new[]{"CharacterAllocateHP","CharacterAllocatePhysical","DamageClose"}) {
                var r=Find(name);Vector2 point=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));
                Require(point.x>=0 && point.x<=Screen.width && point.y>=0 && point.y<=Screen.height,"Progression control offscreen: "+name);
            }
            yield return Capture("v02-character-"+size.x);
        }
        Pass("V02 P3: realm boundaries 1/10/11/100/101; no realm Save/buff; Character header measured without overflow at 600/800.");
        var expected=CanonicalExpected(data);AssertIsolated();Require(gm.TryManualSave(SaveSlot.ManualSave1),"Progression Save failed.");
        byte[] manual=File.ReadAllBytes(Path.Combine(SavePath,"ManualSave1.json"));
        Require(gm.LoadAndContinue(SaveSlot.ManualSave1),"Progression Load failed.");yield return Ready();Matches(expected,"Progression points/allocations Load");
        ui.OpenHeroStats();yield return null;ui.RefreshCharacterProgression();
        Require(summary.text.Contains("Điểm thuộc tính: 0") && gm.HeroData.addedHealth==6 && gm.HeroData.addedDamage==1,"Loaded Character UI stale.");
        ui.SetPaused(false);yield return null;ui.SetPaused(true);
        Require(gm.HeroData.addedHealth==6 && gm.HeroData.addedDamage==1,"Pause changed allocation.");
        ui.CloseDamageInfo();gm.OnResetClicked();gm.OnConfirmResetClicked();yield return Ready();
        Require(gm.HeroData.statPoints==0 && gm.HeroData.addedHealth==0 && gm.HeroData.addedDamage==0,"New/Reset retained old points.");
        Require(Convert.ToBase64String(manual)==Convert.ToBase64String(File.ReadAllBytes(Path.Combine(SavePath,"ManualSave1.json"))),"Reset changed manual slot.");
        Pass("V02 P5: Save/Load restored points/allocations and canonical combat/UI; Pause preserved state; public Reset starts fresh and retains owned Manual1 bytes.");
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationProgressionUiOnly")>=0)yield break;
        // Reuse the already-existing natural-kill/equip/Forge/save verifier, no parallel framework.
        yield return GearDropV01();
        Pass("V02 P4: reward/equip/unequip/Forge UI handlers refreshed committed data; no new popup/VFX or production-rate edits.");
        yield return WorldCycleV02();
    }
    IEnumerator WorldCycleV02()
    {
        phase=4;var data=gm.HeroData;WorldMapNode node;
        Require(WorldMapCatalog.TryGet(data.worldMapId,out node) && node.IsStart,"Expected birth node.");
        if(data.statPoints>0) {ui.OpenHeroStats();yield return null;Press("CharacterAllocatePhysical");yield return null;ui.CloseDamageInfo();}
        ui.SetPaused(false);yield return Until(()=>WorldMapCatalog.IsCleared(gm.HeroData,gm.HeroData.worldMapId),240,"natural clear of five waves");ui.SetPaused(true);
        var rootIdentity=Take().roots;int level=data.currentLevel,allocated=data.addedDamage;string gear=GearCore(data),origin=data.worldMapId;
        string destination=null;foreach(var id in node.Links)if(WorldMapCatalog.TravelBlockReason(data,id)==null){destination=id;break;}
        Require(destination!=null && gm.TryTravelToWorldMap(destination),"No legal neighboring Travel.");yield return Ready();
        Require(data.worldMapId==destination && data.worldMapCleared.Contains(origin) && data.currentLevel==level && data.addedDamage==allocated && Take().roots==rootIdentity && GearCore(data)==gear,"Travel lost canonical progression.");
        var snapshot=CanonicalExpected(data);Require(gm.TryManualSave(SaveSlot.ManualSave2) && gm.LoadAndContinue(SaveSlot.ManualSave2),"World Save/Load failed.");yield return Ready();Matches(snapshot,"World Travel Save/Load");
        int source=WeaponBasicAttack.DamageSource(gm.HeroData);int events=gm.DamageHistory.Length;
        ui.SetPaused(false);yield return Until(()=>{
            var rows=gm.DamageHistory;for(int i=events;i<rows.Length;i++)if(rows[i].Contains("; tổng: "+source+";"))return true;return false;
        },50,"combat after world Load");ui.SetPaused(true);
        string equipment=GearCore(gm.HeroData);int hpAdded=gm.HeroData.addedHealth,damageAdded=gm.HeroData.addedDamage;
        Call(combat,"ApplyIncomingDamage",1000000,"[V02 owned fixture defeat]",null,null);yield return null;
        Require(!gm.CanAllocateHeroAttribute,"Defeated Hero accepts allocation.");gm.OnRetryClicked();yield return Ready();
        Require(GearCore(gm.HeroData)==equipment && gm.HeroData.addedHealth==hpAdded && gm.HeroData.addedDamage==damageAdded && gm.HeroData.worldMapCleared.Contains(origin),"Retry lost equipment/allocation/clear state.");
        Require(gm.TryManualSave(SaveSlot.ManualSave2) && gm.ReturnToMainMenu() && gm.LoadAndContinue(SaveSlot.ManualSave2),"Return Title / Continue failed.");yield return Ready();
        ui.OpenHeroStats();yield return null;ui.RefreshCharacterProgression();
        Require(combat.CurrentHeroCombatState.Data==gm.HeroData && gm.HeroData.addedDamage==damageAdded,"Title/Load left stale Hero binding.");
        yield return Capture("v02-character-after-travel-retry-load");
        Pass("V02 P6: natural five-wave birth-node clear, explicit legal Travel, root/level/gear/allocated stats Save/Load, post-Load Combat.Update, fixture defeat/Retry and Return Title/Continue; no boss or schema changes.");
    }
    IEnumerator GearDropV01()
    {
        Require(string.IsNullOrEmpty(replayProfile),"Gear cycle requires a fresh owned character.");
        var cfg=gm.ProductionLoot;
        Require(cfg.configured && !gm.ControlledLootTestEnabled && cfg.material.enabled && cfg.material.chance==.60 &&
            cfg.material.materialId==EquipmentRefinement.MaterialId && cfg.material.minQuantity==1 && cfg.material.maxQuantity==2,
            "MainGame material/test flags changed.");
        Require(cfg.equipment.enabled && cfg.equipment.chance==.08 && cfg.equipment.selectionPolicy==EquipmentRewardConfig.SelectionPolicy.Uniform &&
            cfg.equipment.templates.Count==2,"MainGame gear must be enabled at 8%, Uniform.");
        for(int i=0;i<2;i++) {
            var t=cfg.equipment.templates[i];Require(t.templateId==(i==0?EquipmentFoundation.DemoSword:EquipmentFoundation.DemoBow) && t.itemLevel==1 && t.mainStat==4,
                "Authored prototype must be level 1, Attack4 Sword/Bow.");
        }
        Require(gm.HeroData.equipmentItems.Count==0 && EquipmentInventory.Count(gm.HeroData,EquipmentRefinement.MaterialId)==0,"Fresh character has no dev gear/material.");
        Require(CombatBalance.HeroDev==null && CombatBalance.MonsterDev==null && WeaponBasicAttack.DamageSource(gm.HeroData)==10,"Gear cycle requires the production level-1 baseline, not personal DEVB overrides.");
        Pass("GEAR v0.1 CONFIG: fresh MainGame; material .60/1-2, gear .08 Uniform Sword/Bow level1 Attack4; controlled A off.");
        // Only the isolated runtime config is made deterministic; asset stays .60/.08.
        cfg.material.chance=cfg.equipment.chance=1;
        phase=2;var lives=new List<CombatManager.ActiveMonsterInfo>(Enemies);
        ui.SetPaused(false);yield return Until(()=>gm.HeroData.equipmentItems.Count>0,50,"natural production gear kill");ui.SetPaused(true);
        Unique();string id=gm.HeroData.equipmentItems[0].instanceId;int count=gm.HeroData.equipmentItems.Count,stock=EquipmentInventory.Count(gm.HeroData,EquipmentRefinement.MaterialId);
        bool duplicate=false;foreach(var dead in lives)if(dead.currentHP<=0){Call(combat,"HandleMonsterDeath",dead);duplicate=true;break;}
        Require(duplicate && count==gm.HeroData.equipmentItems.Count && stock==EquipmentInventory.Count(gm.HeroData,EquipmentRefinement.MaterialId),"Duplicate death changed rewards.");
        phase=1;yield return OpenInventory();Select(id);int baseline=WeaponBasicAttack.DamageSource(gm.HeroData);
        Press("InventoryEquip");yield return null;
        var item=EquipmentRuntime.Find(gm.HeroData,id);int equipped=WeaponBasicAttack.DamageSource(gm.HeroData);
        Require(item.isEquipped && equipped==baseline+4 && hero.attackMode==WeaponBasicAttack.Resolve(gm.HeroData),"Inventory handler did not apply exact weapon stats/mode.");
        Press("EquipmentSlot0");Press("InventoryUnequip");yield return null;
        Require(!item.isEquipped && WeaponBasicAttack.DamageSource(gm.HeroData)==baseline,"Unequip left stale stats.");
        Select(id);Press("InventoryEquip");yield return null;
        Pass("GEAR v0.1 UI HANDLERS: awarded unique "+item.templateId+"; source "+baseline+" -> "+equipped+"; equip/unequip via real Inventory hit-test/click routing (not native input).");Status(1,"PASS");

        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationProgressionV02")>=0) {
            string beforeFailure=GearCore(gm.HeroData);int beforeStock=EquipmentInventory.Count(gm.HeroData,EquipmentRefinement.MaterialId);
            Require(beforeStock<EquipmentRefinement.SpecialCost,"Expected insufficient first-kill materials.");
            Press("InventoryForge");yield return null;Press("Forge_Up2");yield return null;
            Require(GearCore(gm.HeroData)==beforeFailure && EquipmentInventory.Count(gm.HeroData,EquipmentRefinement.MaterialId)==beforeStock &&
                ((string)Get(inventory,"lastForgeMessage")).Contains("Thiếu"),"Failed Forge changed data or reported success.");
            Press("Forge_Close");yield return null;
            Pass("V02 P4: insufficient-material Special Forge reports failure and preserves gear/stock.");
        }
        yield return CloseInventory();int damageEvents=gm.DamageHistory.Length;
        ui.SetPaused(false);yield return Until(()=>{
            var history=gm.DamageHistory;for(int i=damageEvents;i<history.Length;i++)if(history[i].Contains("; tổng: "+equipped+";"))return true;return false;
        },50,"new natural combat trace using equipped source");ui.SetPaused(true);
        Pass("GEAR v0.1 COMBAT: subsequent natural attack trace consumes equipped source "+equipped+" in mode "+hero.attackMode+"; duplicate death did not change gear/material.");Status(2,"PASS");
        // Earn the complete Forge budget from more natural death rewards.
        ui.SetPaused(false);yield return Until(()=>EquipmentInventory.Count(gm.HeroData,EquipmentRefinement.MaterialId)>=11,180,"natural material rewards for Normal+Special Forge");ui.SetPaused(true);Unique();
        phase=3;yield return OpenInventory();Select(id);Press("InventoryForge");yield return null;
        Require(inventory.IsForgeOpen,"Forge handler did not open.");stock=EquipmentInventory.Count(gm.HeroData,EquipmentRefinement.MaterialId);
        int beforeForge=WeaponBasicAttack.DamageSource(gm.HeroData);Press("Forge_Up1");yield return null;
        Require(item.refineLevel==1 && Mathf.Abs(EquipmentRefinement.PowerMultiplier(item)-1.02f)<.0001f &&
            EquipmentInventory.Count(gm.HeroData,EquipmentRefinement.MaterialId)==stock-1,"Normal Forge cost/power.");
        Press("Forge_Up2");yield return null;
        Require(item.rarity==EquipmentRarity.Hoang && item.specialForgeCount==1 && item.forgeHistory.Count==1 &&
            Mathf.Abs(EquipmentRefinement.PowerMultiplier(item)-1.16f)<.0001f && EquipmentInventory.Count(gm.HeroData,EquipmentRefinement.MaterialId)==stock-11 &&
            WeaponBasicAttack.DamageSource(gm.HeroData)==beforeForge+1,"Special Forge cost/power/history.");
        Pass("GEAR v0.1 FORGE: only natural reward material; stock "+stock+" -> "+(stock-11)+"; power 1 -> 1.02 -> 1.16, source "+beforeForge+" -> "+WeaponBasicAttack.DamageSource(gm.HeroData)+".");
        var expected=CanonicalExpected(gm.HeroData);AssertIsolated();Require(gm.TryManualSave(SaveSlot.ManualSave1),"Gear cycle Save failed.");
        byte[] bytes=File.ReadAllBytes(Path.Combine(SavePath,"ManualSave1.json"));
        Require(gm.LoadAndContinue(SaveSlot.ManualSave1),"Gear cycle Load failed.");yield return Ready();Matches(expected,"Gear v0.1 Load");
        Require(Convert.ToBase64String(bytes)==Convert.ToBase64String(File.ReadAllBytes(Path.Combine(SavePath,"ManualSave1.json"))),"Load rewrote save.");
        Pass("GEAR v0.1 SAVE/LOAD: same instance IDs, slots, main stats, Forge history, resource, character and combat state; owned save unchanged by Load.");Status(3,"PASS");
        phase=4;damageEvents=gm.DamageHistory.Length;int loadedSource=WeaponBasicAttack.DamageSource(gm.HeroData);
        ui.SetPaused(false);yield return Until(()=>{
            var history=gm.DamageHistory;for(int i=damageEvents;i<history.Length;i++)if(history[i].Contains("; tổng: "+loadedSource+";"))return true;return false;
        },50,"natural combat using restored forged source");ui.SetPaused(true);AssertIsolated();
        Pass("GEAR v0.1 POST LOAD: real Combat.Update uses restored forged source "+loadedSource+"; deterministic chances were runtime-only overrides, no dev grant.");Status(4,"PASS");
    }
    IEnumerator PersistenceAndFinal(Snapshot first,string itemId)
    {
        phase=3;
        byte[] firstBytes=File.ReadAllBytes(Path.Combine(SavePath,"ManualSave1.json"));
        Require(gm.LoadAndContinue(SaveSlot.ManualSave1),"Load1 failed while Forge open.");yield return Ready();
        Require(!inventory.IsOpen && !inventory.IsForgeOpen,"Load left stale Inventory/Forge modal open.");yield return Resize(800,450);Matches(first,"ManualSave1 from Forge");
        yield return OpenInventory();Select(itemId);Require(EquipmentRuntime.Find(gm.HeroData,itemId).refineLevel>=1,"Loaded forge data missing from current inventory.");
        yield return Capture("phase3-loaded-inventory");yield return CloseInventory();
        Press("StatsButton");yield return null;Require(((TMPro.TextMeshProUGUI)Get(ui,"damageText")).text.Contains(gm.HeroData.entityName),"Character panel did not bind loaded Hero.");Press("DamageClose");yield return null;
        Pass("ManualSave1 + Load while Forge open restore authoritative progress/gear/stock/history/stats and canonical legacy metadata.");

        // Allow the actual autosave coroutine to run, rather than calling its body.
        string auto=Path.Combine(SavePath,"AutoSave.json");DateTime stamp=File.Exists(auto)?File.GetLastWriteTimeUtc(auto):DateTime.MinValue;
        ui.SetPaused(false);yield return Until(()=>File.Exists(auto) && File.GetLastWriteTimeUtc(auto)>stamp,20,"actual autosave coroutine");ui.SetPaused(true);AssertIsolated();
        var autoHero=ScriptableObject.CreateInstance<EntityDataSO>();
        Require(saves.LoadGame(autoHero,SaveSlot.AutoSave),"Actual autosave file could not load.");
        var autoIds=new HashSet<string>();foreach(var gear in autoHero.equipmentItems)Require(autoIds.Add(gear.instanceId),"Autosave duplicated gear.");
        UnityEngine.Object.Destroy(autoHero);Pass("Actual ten-second AutoSave coroutine wrote a loadable isolated snapshot.");
        // A second slot captures later real progression/items, then slot 1 rolls back.
        yield return OpenInventory();Snapshot second=CanonicalExpected(gm.HeroData);AssertIsolated();Require(gm.TryManualSave(SaveSlot.ManualSave2),"ManualSave2 failed.");
        Require(gm.LoadAndContinue(SaveSlot.ManualSave2),"Load2 failed.");yield return Ready();yield return Resize(800,450);Matches(second,"ManualSave2");
        Require(gm.LoadAndContinue(SaveSlot.ManualSave1),"Rollback Load1 failed.");yield return Ready();yield return Resize(800,450);Matches(first,"Rollback ManualSave1");
        byte[] secondBytes=File.ReadAllBytes(Path.Combine(SavePath,"ManualSave2.json"));
        Require(gm.ReturnToMainMenu(),"ReturnToMainMenu autosave failed.");yield return null;AssertIsolated();
        Require(gm.LoadAndContinue(SaveSlot.AutoSave),"Load Auto failed.");yield return Ready();yield return Resize(800,450);Matches(first,"AutoSave after ReturnTitle");
        gm.OnNewGameClicked();Require(gm.HasPendingConfirmation,"New game must ask for reset confirmation.");AssertIsolated();gm.OnConfirmResetClicked();yield return Ready();
        Require(gm.HeroData.equipmentItems.Count==0,"New run did not reset owned auto profile.");
        Require(Convert.ToBase64String(firstBytes)==Convert.ToBase64String(File.ReadAllBytes(Path.Combine(SavePath,"ManualSave1.json"))) &&
            Convert.ToBase64String(secondBytes)==Convert.ToBase64String(File.ReadAllBytes(Path.Combine(SavePath,"ManualSave2.json"))),"New game changed a manual slot.");
        Require(gm.LoadAndContinue(SaveSlot.ManualSave1),"Manual slot lost after new game.");yield return Ready();yield return Resize(800,450);Matches(first,"ManualSave1 after New reset");
        Status(3,"PASS");Pass("Manual slots/rollback, ReturnTitle AutoSave/load, New reset of owned AutoSave while manual files stay byte-identical.");
        phase=4;Unique();yield return OpenInventory();Select(itemId);yield return Capture("phase4-final-restored-loop");yield return CloseInventory();
        int expected=WeaponBasicAttack.DamageSource(gm.HeroData);string token="; tổng: "+expected+";";
        Set(gm,"controlledLootTest",false);int gearBefore=gm.HeroData.equipmentItems.Count,levelBefore=gm.HeroData.currentLevel;float expBefore=gm.HeroData.currentExp;
        ui.SetPaused(false);yield return Until(()=>gm.HeroData.currentLevel!=levelBefore || gm.HeroData.currentExp!=expBefore,35,"real post-load kill with NoDrop profile");ui.SetPaused(true);
        Require(gm.HeroData.equipmentItems.Count==gearBefore,"NoDrop profile awarded gear.");
        Require(Array.Exists(gm.DamageHistory,x=>x.Contains(token)),"Restored equipment stats did not reach natural combat trace.");
        Require(combat.CurrentHeroCombatState.Data==gm.HeroData,"Final combat data binding mismatch.");AssertIsolated();Status(4,"PASS");
        Pass("Integrated MainGame loop complete, including natural combat after loaded forged equipment and a real NoDrop kill. Handler/hit-test UI, not physical mouse or Windows Player.");
    }
}
#endif
