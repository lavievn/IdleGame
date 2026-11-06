#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using TuTienCore;

// Explicit CLI verifier only; no startup hook and no player Save access.
public static class RealmIntegrationV06Verifier
{
    [Serializable] sealed class NativeResult
    {public string unity,profile,failure;public System.Collections.Generic.List<string> checks=new System.Collections.Generic.List<string>();}
    static void Require(bool ok,string message) {if(!ok)throw new InvalidOperationException(message);}
    static string StripRealm(string json,bool dirty=false)
    {
        string fields="realmComponentVersion|realmId|realmEntryLevel"+(dirty?"|isDirty":"");
        return System.Text.RegularExpressions.Regex.Replace(json,@"^\s*""("+fields+@")""\s*:[^\r\n]*\r?\n?","",System.Text.RegularExpressions.RegexOptions.Multiline);
    }
    public static void VerifyNativeSave()
    {
        var result=new NativeResult {unity=Application.unityVersion};GameObject owner=null;EntityDataSO source=null,loaded=null;
        string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.utmp/realm-v06-"+Guid.NewGuid().ToString("N")));
        try
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Refusing active Play session.");
            Require(System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(root),"^realm-v06-[0-9a-f]{32}$"),"Profile name");
            for(var d=new DirectoryInfo(root);d!=null;d=d.Parent)Require(!d.Exists || (d.Attributes&FileAttributes.ReparsePoint)==0,"No reparse path");
            Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,".owner"),"realm-v06-native");
            string directory=Path.Combine(root,"saves");Directory.CreateDirectory(directory);result.profile=root;
            owner=new GameObject("Owned inactive realm Save verifier");owner.SetActive(false);
            var saves=owner.AddComponent<SaveManager>();saves.ConfigureSaveDirectoryBeforeAwake(directory);
            Require(saves.SaveDirectory==directory && File.ReadAllText(Path.Combine(root,".owner"))=="realm-v06-native","Isolation before I/O");
            source=ScriptableObject.CreateInstance<EntityDataSO>();source.currentLevel=45;source.currentExp=73;source.statPoints=7;
            source.gender=GenderType.Nu;source.entityName="Realm fixture";source.spiritRoots=new System.Collections.Generic.List<ElementType>{ElementType.Kim};
            source.rootTiers=new System.Collections.Generic.List<int>{4};source.rootWeights=new System.Collections.Generic.List<float>{1};
            source.ApplyHeroBalance();EquipmentInventory.InitializeNewRun(source);EquipmentRuntime.InitializeNewRun(source);
            Require(EquipmentInventory.Add(source,EquipmentRefinement.MaterialId,37),"fixture material");
            var gear=EquipmentFoundation.CreateCommon(EquipmentFoundation.DemoSword,1,4);Require(EquipmentRuntime.AddInstance(source,gear),"fixture gear");
            Require(EquipmentRuntime.Equip(source,gear.instanceId,EquipmentSlot.Weapon),"Native fixture equipped gear with real modifiers");
            Require(WorldMapCatalog.StartNew(source),"fixture world");source.worldMapWaveProgress=3;source.worldMapLoopWave=4;source.completedWavesInMap=3;source.gold=9876543210L;
            source.NormalizeRoots();source.NormalizeMapProgress();source.NormalizeCombatStats();EquipmentInventory.Normalize(source);EquipmentRuntime.Normalize(source);
            LegacyRealmSnapshotCompatibility.Initialize(source);
            int sourceAttack=WeaponBasicAttack.Physical(source),sourceModifierCount=source.combatStatModifiers.Count;
            Require(sourceModifierCount>0 && sourceAttack==source.baseDamage+source.addedDamage+4,"Nonempty modifier fixture must add exactly four Physical");
            string current=JsonUtility.ToJson(source,true),legacy=StripRealm(current);string slot=Path.Combine(directory,"ManualSave1.json");
            Require(!legacy.Contains("\"realmComponentVersion\""),"Legacy fixture has no Realm fields");File.WriteAllText(slot,legacy);
            loaded=UnityEngine.Object.Instantiate(source);loaded.realmComponentVersion=99;loaded.realmId="template:wrong";loaded.realmEntryLevel=999;
            Require(saves.LoadGame(loaded,SaveSlot.ManualSave1),"Legacy native Load");
            File.WriteAllText(Path.Combine(root,"loaded-diagnostic.json"),JsonUtility.ToJson(loaded,true));
            File.WriteAllText(Path.Combine(root,"source-diagnostic.json"),current);
            Debug.Log("[REALM V06 NATIVE] Observed legacy state version="+loaded.realmComponentVersion+" id="+loaded.realmId+" entry="+loaded.realmEntryLevel+" level="+loaded.currentLevel+" exp="+loaded.currentExp);
            Require(loaded.realmComponentVersion==1 && loaded.realmId==LegacyRealmSnapshotCompatibility.LuyenKhiId && loaded.realmEntryLevel==1 && loaded.currentLevel==45 && loaded.currentExp==source.currentExp,"Conservative legacy migration, not template realm");
            Require(StripRealm(JsonUtility.ToJson(loaded,true),true)==StripRealm(current,true),"Legacy migration preserves all unrelated serialized fields");
            Require(File.ReadAllText(slot)==legacy,"Load does not rewrite legacy file");
            Require(saves.SaveGame(loaded,SaveSlot.ManualSave1),"Save migrated candidate");string migrated=File.ReadAllText(slot);
            Require(migrated.Contains("\"realmComponentVersion\": 1") && migrated.Contains("\"realmId\": \"realm:luyen-khi\"") && migrated.Contains("\"realmEntryLevel\": 1"),"Migrated component fields actually persisted in JSON");
            for(int i=0;i<3;i++)Require(saves.LoadGame(loaded,SaveSlot.ManualSave1) && loaded.realmEntryLevel==1 && loaded.realmId==LegacyRealmSnapshotCompatibility.LuyenKhiId && loaded.currentLevel==45 && loaded.currentExp==source.currentExp,"Repeated current Load idempotent");
            Require(StripRealm(JsonUtility.ToJson(loaded,true),true)==StripRealm(current,true),"Current roundtrip preserves all other fields/modifiers");
            Require(WeaponBasicAttack.Physical(loaded)==sourceAttack && loaded.combatStatModifiers.Count==sourceModifierCount,"Repeated native Load must not duplicate equipped modifiers");
            result.checks.Add("Legacy/current/native JsonUtility, contaminated template, repeated loads, no file rewrite on Load; all unrelated JSON fields preserved.");
            string slot2=Path.Combine(directory,"ManualSave2.json");
            foreach(string bad in new[]{current.Replace("\"realmComponentVersion\": 1","\"realmComponentVersion\": 2"),
                current.Replace(LegacyRealmSnapshotCompatibility.LuyenKhiId,"realm:unknown"),current.Replace("\"realmEntryLevel\": 1","\"realmEntryLevel\": 46"),
                current.Replace("\"realmEntryLevel\": 1","\"realmEntryLevel\": 0"),current.Replace("\"realmComponentVersion\": 1","\"realmComponentVersion\": -2147483648"),
                current.Replace("\"realmComponentVersion\": 1","\"realmComponentVersion\": -2147483648").Replace("\"realmEntryLevel\": 1","\"realmEntryLevel\": -2147483648").Replace("\"realmId\": \"realm:luyen-khi\"","\"realmId\": null"),
                System.Text.RegularExpressions.Regex.Replace(current,@"^\s*""realmEntryLevel""\s*:[^\r\n]*\r?\n?","",System.Text.RegularExpressions.RegexOptions.Multiline)})
            {
                Require(bad!=current,"Negative fixture must change input");File.WriteAllText(slot2,bad);string before=JsonUtility.ToJson(loaded,true);
                Require(!saves.LoadGame(loaded,SaveSlot.ManualSave2) && JsonUtility.ToJson(loaded,true)==before,"Invalid/future/partial Realm rejected before target mutation");
                Require(!saves.SaveGame(source,SaveSlot.ManualSave2) && File.ReadAllText(slot2)==bad,"Cannot overwrite invalid/future Realm snapshot");
            }
            result.checks.Add("Future version, unknown ID, entry0/after-Level, explicit int.Min version and partial component rejected before mutation; existing invalid/future snapshot preserved.");
            string validBefore=File.ReadAllText(slot);loaded.realmId="realm:invalid";
            Require(!saves.SaveGame(loaded,SaveSlot.ManualSave1) && File.ReadAllText(slot)==validBefore,"Typed invalid Realm save preserves valid file");
            loaded.realmId=LegacyRealmSnapshotCompatibility.LuyenKhiId;
            string temp=Path.Combine(directory,"ManualSave1.tmp");Directory.CreateDirectory(temp);
            Require(!saves.SaveGame(source,SaveSlot.ManualSave1) && File.ReadAllText(slot)==validBefore,"Write failure before commit preserves previous snapshot");Directory.Delete(temp);
            result.checks.Add("Invalid typed Save and injected .tmp-directory write failure leave previous .json bytes unchanged; existing atomic writer retained.");
            Debug.Log("[REALM V06 NATIVE] PASS "+root);
        }
        catch(Exception ex) {result.failure=ex.ToString();Debug.LogError("[REALM V06 NATIVE] FAIL "+ex);}
        finally
        {
            if(source!=null)UnityEngine.Object.DestroyImmediate(source);if(loaded!=null)UnityEngine.Object.DestroyImmediate(loaded);if(owner!=null)UnityEngine.Object.DestroyImmediate(owner);
            if(Directory.Exists(root))File.WriteAllText(Path.Combine(root,"result.json"),JsonUtility.ToJson(result,true));
        }
        EditorApplication.Exit(string.IsNullOrEmpty(result.failure)?0:2);
    }

    public static void VerifyCompilation()
    {
        foreach(var type in new[]{typeof(LegacyLevelRealmProgressionEvaluator),typeof(LegacyLevelRealmBreakthroughTransition),typeof(MartialArtsRegistry),typeof(BossEncounterCoordinator)})
        {
            if(type.Assembly!=typeof(EntityDataSO).Assembly)throw new InvalidOperationException("Unexpected assembly for "+type.FullName);
            Debug.Log("[REALM V06 BASELINE] Unity type loaded: "+type.FullName+" from "+type.Assembly.GetName().Name);
        }
        Debug.Log("[REALM V06 BASELINE] PASS Unity "+Application.unityVersion);
        EditorApplication.Exit(0);
    }
}
#endif
