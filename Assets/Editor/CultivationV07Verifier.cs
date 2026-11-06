#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using TuTienCore;
using UnityEditor;
using UnityEngine;

public static class CultivationV07Verifier
{
    [Serializable] sealed class Result {public string unity,profile,failure;public List<string> checks=new List<string>();}
    static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
    public static void RunNative()
    {
        string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.utmp/cultivation-playtest-"+Guid.NewGuid().ToString("N")));
        var result=new Result{unity=Application.unityVersion,profile=root};GameObject owner=null;EntityDataSO source=null,target=null;
        try {
            for(var d=new DirectoryInfo(root);d!=null;d=d.Parent)Require(!d.Exists || (d.Attributes&FileAttributes.ReparsePoint)==0,"No reparse paths");
            Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,".owner"),"cultivation-playtest-v07");CultivationPlaytestBootstrap.VerifyOwnedProfile(root);
            var profile=CultivationPlaytestBootstrap.ReadProfile(File.ReadAllText(CultivationPlaytestLauncher.ConfigurationPath));
            owner=new GameObject("Owned v07 Save fixture");owner.SetActive(false);var save=owner.AddComponent<SaveManager>();
            save.ConfigureSaveDirectoryBeforeAwake(Path.Combine(root,"saves"));save.ConfigureCultivationPlaytestBeforeAwake(profile,true);Directory.CreateDirectory(save.SaveDirectory); // Edit Mode does not run MonoBehaviour Awake; Play verifier checks the real path.
            source=ScriptableObject.CreateInstance<EntityDataSO>();LegacyRealmSnapshotCompatibility.Initialize(source);source.currentLevel=45;source.currentExp=17;source.statPoints=11;
            WorldMapCatalog.StartNew(source);source.ApplyHeroBalance();source.currentExp=17;source.statPoints=11;source.addedHealth=198; // Explicit current-balance snapshot (33 spent +11 unspent), not old balance migration input.
            EquipmentInventory.InitializeNewRun(source);EquipmentRuntime.InitializeNewRun(source);
            var item=EquipmentFoundation.CreateDemoBow();EquipmentRuntime.AddInstance(source,item);Require(EquipmentRuntime.Equip(source,item.instanceId,EquipmentSlot.Weapon),"Fixture equip");
            var binding=new CultivationGameplayBinding(source,profile,CultivationGameplayState.New(profile));
            for(int i=0;i<250;i++)binding.Advance(source,i,.1,false,"TV-20");
            double reserve=binding.Current.Reserve.Amount;Require(binding.Current.Foundation.Amount==20 && reserve>0,"Fixture reaches bottleneck");
            Require(save.SaveGame(source,SaveSlot.ManualSave1),"Native save");string path=Path.Combine(save.SaveDirectory,"ManualSave1.json");byte[] stable=File.ReadAllBytes(path);string json=File.ReadAllText(path);
            target=ScriptableObject.CreateInstance<EntityDataSO>();target.cultivationJson="contaminated template";
            for(int i=0;i<3;i++) {
                Require(save.LoadGame(target,SaveSlot.ManualSave1),"Native repeated load");CultivationSession loaded;string reason;
                Require(CultivationGameplayState.TryRestore(target.cultivationJson,profile,out loaded,out reason) && loaded.Current.Foundation.Amount==20 && loaded.Current.Reserve.Amount==reserve,"Resources roundtrip without payout "+reason);
                Require(target.currentLevel==45 && target.currentExp==17 && target.statPoints==11 && target.worldMapId==source.worldMapId && target.equipmentItems.Count==1 && target.equipmentItems[0].instanceId==item.instanceId && target.equipmentItems[0].isEquipped,"Unrelated Hero/world/gear preserved");
            }
            result.checks.Add("Native canonical component and repeated SaveManager Load preserve bottleneck/reserve/config/Level/EXP/points/world/equipment, ignoring contaminated template.");
            string invalidPath=Path.Combine(save.SaveDirectory,"ManualSave2.json");
            foreach(string invalid in new[]{source.cultivationJson.Replace("\"version\":1","\"version\":2"),source.cultivationJson.Replace(profile.Id,"unknown"),source.cultivationJson.Replace("\"configurationVersion\":1","\"configurationVersion\":2"),
                source.cultivationJson.Replace("realm:luyen-khi","unknown"),System.Text.RegularExpressions.Regex.Replace(source.cultivationJson,@"""foundation""\s*:\s*[^,}]+","\"foundation\":-1"),"{}"}) {
                Require(invalid!=source.cultivationJson,"Invalid fixture must change its input");string good=source.cultivationJson;source.cultivationJson=invalid;File.WriteAllText(invalidPath,JsonUtility.ToJson(source,true));source.cultivationJson=good;
                string before=JsonUtility.ToJson(target);Require(!save.LoadGame(target,SaveSlot.ManualSave2) && JsonUtility.ToJson(target)==before,"Invalid candidate must not mutate destination");
                Require(!save.SaveGame(source,SaveSlot.ManualSave2),"Future/invalid existing component cannot be overwritten");
            }
            string component=source.cultivationJson;source.cultivationJson="invalid";Require(!save.SaveGame(source,SaveSlot.ManualSave1) && Convert.ToBase64String(File.ReadAllBytes(path))==Convert.ToBase64String(stable),"Invalid running state cannot overwrite snapshot");source.cultivationJson=component;
            source.cultivationJson="";Require(!save.SaveGame(source,SaveSlot.AutoSave) && !save.HasSave(SaveSlot.AutoSave),"A playtest Hero missing its component cannot write an empty snapshot");source.cultivationJson=component;
            result.checks.Add("Future/config mismatch/unknown realm/invalid/partial candidate rejects before mutation; invalid source and incompatible existing slot preserve prior file.");
            foreach(var fault in new[]{SaveManager.OwnedWriteFault.BeforeWrite,SaveManager.OwnedWriteFault.AfterTempWrite,SaveManager.OwnedWriteFault.BeforeReplace}) {
                save.FailNextOwnedWrite=fault;Require(!save.SaveGame(source,SaveSlot.ManualSave1),"Expected injected write failure "+fault);
                Require(Convert.ToBase64String(File.ReadAllBytes(path))==Convert.ToBase64String(stable) && save.LoadGame(target,SaveSlot.ManualSave1),"Interrupted write kept committed snapshot "+fault);
            }
            Require(save.SaveGame(source,SaveSlot.ManualSave1),"Next write recovers from orphan temp");
            result.checks.Add("BeforeWrite/AfterTempWrite/BeforeReplace faults retain committed file; loader ignores orphan temp and next atomic write succeeds (process interruption, not power-cut proof).");
            Dictionary<string,string> initialRoot=null,currentRoot=null;string baselineReason;
            Require(CultivationSaveJson.TryObject(json,out initialRoot,out baselineReason) && CultivationSaveJson.TryObject(File.ReadAllText(path),out currentRoot,out baselineReason),"Committed snapshot baselines parse");
            Require(initialRoot["isDirty"]=="true" && currentRoot["isDirty"]=="false","Successful retry serialized the expected dirty-flag transition");
            Require(initialRoot.Count==currentRoot.Count,"Successful retry preserves the schema");
            foreach(var field in initialRoot)if(field.Key!="isDirty")Require(currentRoot.ContainsKey(field.Key) && currentRoot[field.Key]==field.Value,"Successful retry changed unrelated field: "+field.Key);
            stable=File.ReadAllBytes(path); // Compare each fault with the immediately preceding committed snapshot.
            string temp=Path.Combine(save.SaveDirectory,"ManualSave1.tmp");
            Require(!File.Exists(temp) && !Directory.Exists(temp),"Successful commit consumed temp");
            Directory.CreateDirectory(temp); // Owned exact empty directory intentionally prevents temp FileStream.Create.
            Require(!save.SaveGame(source,SaveSlot.ManualSave1) && Convert.ToBase64String(File.ReadAllBytes(path))==Convert.ToBase64String(stable),"Actual temp creation I/O failure retains final");
            Require(Directory.GetFileSystemEntries(temp).Length==0,"Only delete the owned empty blocker");Directory.Delete(temp);
            using(var locked=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)) {
                // Reads remain allowed for preflight, but Windows Replace requires delete sharing.
                Require(!save.SaveGame(source,SaveSlot.ManualSave1) && File.Exists(temp),"Actual destination lock rejects Replace after staging temp");
                Require(Convert.ToBase64String(File.ReadAllBytes(path))==Convert.ToBase64String(stable),"Failed Replace leaves prior snapshot byte-identical");
            }
            Require(save.LoadGame(target,SaveSlot.ManualSave1) && save.SaveGame(source,SaveSlot.ManualSave1),"Recover from actual temp/Replace faults");
            result.checks.Add("Actual temp creation failure and Windows destination-lock Replace failure retain final bytes; valid Load and next write recover.");

            var fields=new Dictionary<string,string>();string error;Require(CultivationSaveJson.TryObject(json,out fields,out error),"Root parse");
            fields.Remove("cultivationJson");var parts=new List<string>();foreach(var f in fields)parts.Add(JsonUtility.ToJson(new KeyName{key=f.Key}).Replace("{\"key\":","").TrimEnd('}')+":"+f.Value);
            string legacy="{"+string.Join(",",parts)+"}";File.WriteAllText(invalidPath,legacy);target.cultivationJson=source.cultivationJson;
            Require(save.LoadGame(target,SaveSlot.ManualSave2) && target.currentLevel==45 && target.currentExp==17 && target.equipmentItems[0].instanceId==item.instanceId,"Explicit owned legacy migration preserves progression");
            CultivationSession migrated;Require(CultivationGameplayState.TryRestore(target.cultivationJson,profile,out migrated,out error) && migrated.Current.Foundation.Amount==0 && migrated.Current.Reserve.Amount==0 && migrated.Current.Blood.Charges==0,"Legacy starts zero cultivation, no EXP inference");
            Require(File.ReadAllText(invalidPath)==legacy,"Load migration does not rewrite file");
            result.checks.Add("Explicit owned legacy initialization is zero-state, independent of Level45; preserves other data and leaves source file byte-identical. Production migration stays disabled.");
            Debug.Log("[CULTIVATION V07] PASS "+root);
        } catch(Exception e){result.failure=e.ToString();Debug.LogError("[CULTIVATION V07] FAIL "+e);}
        if(Directory.Exists(root))File.WriteAllText(Path.Combine(root,"result.json"),JsonUtility.ToJson(result,true));
        if(owner!=null)UnityEngine.Object.DestroyImmediate(owner);if(source!=null)UnityEngine.Object.DestroyImmediate(source);if(target!=null)UnityEngine.Object.DestroyImmediate(target);
        EditorApplication.Exit(string.IsNullOrEmpty(result.failure)?0:2);
    }
    [Serializable] sealed class KeyName {public string key;}
}
#endif
