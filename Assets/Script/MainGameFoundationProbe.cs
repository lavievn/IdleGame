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
public sealed class MainGameFoundationProbe : MonoBehaviour
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
        public int level,wave,progress,damage,maxHP;
        public float exp;
        public long gold;
        public string map,gear,gearCore,inventory,legacyEquipped,weapon;
    }
    public string replayProfile;
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
        if(!string.IsNullOrEmpty(probe.replayProfile))
        {
            VerifyPath(probe.replayProfile);Require(File.ReadAllText(Path.Combine(probe.replayProfile,".owner"))==Marker,"Replay source is not owned.");
            Directory.CreateDirectory(Path.Combine(probe.profileRoot,"saves"));
            File.Copy(Path.Combine(probe.replayProfile,"saves/ManualSave1.json"),Path.Combine(probe.profileRoot,"saves/ManualSave1.json"),false);
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
            legacyEquipped=d.equippedItemId,weapon=WeaponBasicAttack.Identity(d)};
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

    IEnumerator Run()
    {
        yield return null;
        gm=saveOwner.GetComponent<GameManager>();combat=saveOwner.GetComponent<CombatManager>();ui=UIManager.Instance;
        space=(EnvironmentManager)Get(gm,"environmentManager");hero=(HeroController)Get(gm,"heroController");
        Require(ui!=null && space!=null && hero!=null,"MainGame references missing.");
        yield return Resize(800,450);Call(gm,"InitHeroData",false);Call(gm,"DeployHero",false);yield return Ready();
        inventory=(InventoryPanelUI)Get(ui,"inventoryUI");Require(inventory!=null,"No Inventory panel.");
        ui.inventoryLayoutDebug=false;ui.inventoryUseArtTheme=false;ui.ApplyHudFromInspector();
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
