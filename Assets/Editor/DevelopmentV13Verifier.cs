#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using TuTienCore;
using UnityEditor;
using UnityEngine;
public static class DevelopmentV13Verifier
{
 static void R(bool condition,string message,List<string> checks){if(!condition)throw new InvalidOperationException(message);checks.Add(message);}
 public static void RunNative()
 {
  int exit=0;var checks=new List<string>();string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.utmp/cultivation-playtest-"+Guid.NewGuid().ToString("N")));GameObject go=null;EntityDataSO hero=null,loaded=null;
  try{
   checks.AddRange(HeavenlyGateV13Fixtures.Run());for(var d=new DirectoryInfo(root);d!=null;d=d.Parent)if(d.Exists&&(d.Attributes&FileAttributes.ReparsePoint)!=0)throw new InvalidOperationException("No reparse path");Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,".owner"),"cultivation-playtest-v07");CultivationPlaytestBootstrap.VerifyOwnedProfile(root);
   go=new GameObject("Owned v13 SaveManager");go.SetActive(false);var saves=go.AddComponent<SaveManager>();saves.ConfigureSaveDirectoryBeforeAwake(root);var p=HeavenlyGateV13Fixtures.Profile();var c=HeavenlyGateV13Fixtures.Configuration();var policy=HeavenlyGateV13Fixtures.Penalty();saves.ConfigureCultivationPlaytestBeforeAwake(p,false);saves.ConfigureTribulationFixtureBeforeAwake(c,policy);
   hero=ScriptableObject.CreateInstance<EntityDataSO>();hero.currentLevel=1;hero.ApplyHeroBalance();LegacyRealmSnapshotCompatibility.Initialize(hero);WorldMapCatalog.StartNew(hero);EquipmentInventory.InitializeNewRun(hero);EquipmentRuntime.InitializeNewRun(hero);
   var source=HeavenlyGateV13Fixtures.Session(p);using(var binding=new CultivationGameplayBinding(hero,p,source)){R(binding.Advance(hero,0,.037,false,hero.worldMapId)==CultivationReason.None,"fractional clock captured",checks);}
   R(saves.SaveGame(hero,SaveSlot.AutoSave),"pretrial checkpoint committed",checks);string old=saves.TrialSnapshotHash(SaveSlot.AutoSave),path=Path.Combine(root,"AutoSave.json"),why;
   string id=Guid.NewGuid().ToString("N");var r=new TribulationSaveReceipt{version=1,configurationVersion=p.Version,heroLife=1,penaltyVersion=policy.Version,encounterId=Guid.NewGuid().ToString("N"),attemptId=id,ownerId=source.OwnerId.ToString("N"),epoch=source.Epoch.ToString("N"),targetId=Guid.NewGuid().ToString("N"),sourceRealm=c.sourceRealm,targetRealm=c.targetRealm,configurationId=p.Id,encounterConfiguration=c.id,encounterVersion=c.version,encounterConfigHash=TribulationSaveTransaction.Hash(JsonUtility.ToJson(c,false)),requestedPenalty=policy.RequestedLoss,policyId=policy.Id,phase=c.phase,outcome="Active",appliedId="",sourceCultivation=hero.cultivationJson,resultCultivation="",sourceSnapshotHash=old};
   hero.tribulationJson=JsonUtility.ToJson(r,false);R(saves.CommitTrialCandidate(hero,old,out why),"active receipt and complete Hero atomically written: "+why,checks);string active=saves.TrialSnapshotHash(SaveSlot.AutoSave),raw=File.ReadAllText(path);
   loaded=ScriptableObject.CreateInstance<EntityDataSO>();loaded.currentLevel=99;
   R(!saves.LoadGame(loaded,SaveSlot.AutoSave)&&loaded.currentLevel==99&&saves.TrialSnapshotHash(SaveSlot.AutoSave)==active,"interrupted active checkpoint blocked before candidate mutation",checks);
   R(!saves.SaveGame(hero,SaveSlot.ManualSave1)&&!saves.ResetAutoSave()&&saves.TrialSnapshotHash(SaveSlot.AutoSave)==active,"ordinary save/reset cannot erase active liability",checks);
   R(!saves.TryPrepareCultivationConfigurationUpgrade(SaveSlot.AutoSave,out _,out _,out _),"configuration upgrade unavailable for active fixture liability",checks);
   r.outcome="Failure";r.appliedId=id;r.penalty=policy.Loss(source.Realms,source.Current);R(HeavenlyGateV13Fixtures.ApplyFailureFixture(source,Guid.ParseExact(id,"N"),policy)==CultivationReason.None,"prepare loss only on candidate session",checks);r.resultCultivation=CultivationGameplayState.Capture(source,p);hero.cultivationJson=r.resultCultivation;hero.tribulationJson=JsonUtility.ToJson(r,false);
   foreach(string point in new[]{"BeforeTemp","BeforeReplace"}){saves.FailNextTrialWrite=point;R(!saves.CommitTrialCandidate(hero,active,out why)&&saves.TrialSnapshotHash(SaveSlot.AutoSave)==active,"failure preserves active file "+point,checks);}
   using(var locked=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read))R(!saves.CommitTrialCandidate(hero,active,out why)&&saves.TrialSnapshotHash(SaveSlot.AutoSave)==active,"actual locked Replace retains active file",checks);
   File.WriteAllText(path,raw+" ");string newer=saves.TrialSnapshotHash(SaveSlot.AutoSave);R(newer!=active&&!saves.CommitTrialCandidate(hero,active,out why)&&saves.TrialSnapshotHash(SaveSlot.AutoSave)==newer,"terminal write refuses competing newer snapshot",checks);File.WriteAllText(path,raw,new System.Text.UTF8Encoding(false));
   saves.FailNextTrialWrite="AfterReplace";R(saves.CommitTrialCandidate(hero,active,out why),"ambiguous post-Replace reconciles only exact complete result",checks);string final=saves.TrialSnapshotHash(SaveSlot.AutoSave);
   R(saves.CommitTrialCandidate(hero,active,out why)&&saves.TrialSnapshotHash(SaveSlot.AutoSave)==final,"exact terminal commit retry idempotent",checks);
   for(int i=0;i<3;i++)R(saves.LoadGame(loaded,SaveSlot.AutoSave)&&loaded.cultivationJson==r.resultCultivation&&loaded.tribulationJson==hero.tribulationJson,"terminal restore without replay "+i,checks);
   R(saves.TrialSnapshotHash(SaveSlot.AutoSave)==final&&File.Exists(Path.Combine(root,"AutoSave.tribulation-"+old+".snapshot.bak")),"immutable original backup and committed terminal file retained",checks);
   Debug.Log("[V13 NATIVE] PASS "+checks.Count+" "+root);
  }catch(Exception e){exit=2;Debug.LogError("[V13 NATIVE] FAIL "+root+" "+e);}
  finally{if(Directory.Exists(root))File.WriteAllLines(Path.Combine(root,"v13-native.txt"),checks);if(hero!=null)UnityEngine.Object.DestroyImmediate(hero);if(loaded!=null)UnityEngine.Object.DestroyImmediate(loaded);if(go!=null)UnityEngine.Object.DestroyImmediate(go);EditorApplication.Exit(exit);}
 }
}
#endif
