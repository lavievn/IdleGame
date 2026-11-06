using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using UnityEngine;

namespace TuTienCore
{
    public enum CultivationMigrationReason { None, NotLegacy, InvalidSource, SourceChanged, ConsentRequired, BackupMismatch, BackupFailure, UnsupportedReceipt, DowngradeGuard, WriteFailed, VerificationFailed, RollbackBlocked }
    [Serializable] sealed class CultivationMigrationReceipt
    {
        public int version=1,configurationVersion;public string state,originalHash,upgradedHash,backupName,configurationId,slot;
    }
    public sealed class CultivationMigrationPlan
    {
        internal readonly byte[] Original,Upgraded;internal readonly string Directory,ReceiptPath;
        public readonly SaveSlot Slot;public readonly string OriginalHash,UpgradedHash,BackupPath;
        internal readonly CultivationMigrationReceipt Receipt;
        internal CultivationMigrationPlan(string directory,SaveSlot slot,byte[] original,byte[] upgraded,CultivationPlaytestProfile p)
        {
            Directory=directory;Slot=slot;Original=(byte[])original.Clone();Upgraded=(byte[])upgraded.Clone();
            OriginalHash=CultivationMigrationTransaction.Hash(Original);UpgradedHash=CultivationMigrationTransaction.Hash(Upgraded);
            string name=slot+".cultivation-"+OriginalHash+".legacy.bak";BackupPath=Path.Combine(directory,name);
            ReceiptPath=Path.Combine(directory,slot+".cultivation-migration.json");
            Receipt=new CultivationMigrationReceipt{state="Prepared",originalHash=OriginalHash,upgradedHash=UpgradedHash,backupName=name,configurationId=p.Id,configurationVersion=p.Version,slot=slot.ToString()};
        }
        public string UpgradedJson {get{return CultivationMigrationTransaction.Text(Upgraded);}}
    }
    // Compatibility transaction metadata records hashes/status only, never resources or an alternate progression timeline.
    // Original backup is immutable; normal manual slot rollback remains owned by SaveManager.
    public static class CultivationMigrationTransaction
    {
        public static string Hash(byte[] data){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(data)).Replace("-","").ToLowerInvariant();}
        internal static string Text(byte[] data){using(var s=new StreamReader(new MemoryStream(data),Encoding.UTF8,true))return s.ReadToEnd();}
        [Serializable] sealed class Quoted {public string value;}
        static string Quote(string value){Dictionary<string,string> fields;string reason;CultivationSaveJson.TryObject(JsonUtility.ToJson(new Quoted{value=value}),out fields,out reason);return fields["value"];}
        static string UpgradeJson(Dictionary<string,string> fields,string component)
        {
            var parts=new List<string>();foreach(var f in fields)if(f.Key!=CultivationGameplayState.SaveField)parts.Add(Quote(f.Key)+":"+f.Value);
            parts.Add(Quote(CultivationGameplayState.SaveField)+":"+Quote(component));return "{"+string.Join(",",parts.ToArray())+"}";
        }
        static string SlotPath(string directory,SaveSlot slot)
        {if(!Enum.IsDefined(typeof(SaveSlot),slot) || string.IsNullOrEmpty(directory) || !Path.IsPathRooted(directory))throw new ArgumentException("Explicit valid slot/directory required.");return Path.Combine(directory,slot+".json");}
        static bool ReadReceipt(string path,out CultivationMigrationReceipt receipt)
        {
            receipt=null;if(!File.Exists(path))return true;
            try {Dictionary<string,string> fields;string reason;string json=File.ReadAllText(path);
                if(!CultivationSaveJson.TryObject(json,out fields,out reason) || fields.Count!=8)return false;
                foreach(var f in typeof(CultivationMigrationReceipt).GetFields())if(!fields.ContainsKey(f.Name))return false;
                int integer;string text;
                foreach(var f in typeof(CultivationMigrationReceipt).GetFields()) {
                    if(f.FieldType==typeof(int) && !int.TryParse(fields[f.Name],out integer))return false;
                    if(f.FieldType==typeof(string) && !CultivationSaveJson.TryString(fields[f.Name],out text))return false;
                }
                var r=new CultivationMigrationReceipt();JsonUtility.FromJsonOverwrite(json,r);
                if(r.version!=1 || (r.state!="Prepared" && r.state!="Committed" && r.state!="RollbackPrepared" && r.state!="RolledBack") || r.originalHash==null || r.originalHash.Length!=64 || r.upgradedHash==null || r.upgradedHash.Length!=64 || string.IsNullOrEmpty(r.backupName) || Path.GetFileName(r.backupName)!=r.backupName)return false;
                receipt=r;return true;
            }catch(Exception e) when(e is IOException || e is UnauthorizedAccessException || e is ArgumentException){return false;}
        }
        public static bool CompleteInterruptedReceipt(string directory,SaveSlot slot,CultivationPlaytestProfile profile,out CultivationMigrationReason reason)
        {
            reason=CultivationMigrationReason.None;CultivationMigrationReceipt r;string receiptPath=Path.Combine(directory,slot+".cultivation-migration.json");
            if(!ReadReceipt(receiptPath,out r)){reason=CultivationMigrationReason.UnsupportedReceipt;return false;}
            if(r==null || r.state=="Committed" || r.state=="RolledBack")return true;
            try {
                string source=SlotPath(directory,slot);string component,error;
                if(!CultivationGameplayState.TryReadHeroJson(Text(File.ReadAllBytes(source)),profile,out component,out error) || string.IsNullOrEmpty(component))return true;
                if(r.slot!=slot.ToString() || r.configurationId!=profile.Id || r.configurationVersion!=profile.Version || !Same(source,r.upgradedHash) || !Same(Path.Combine(directory,r.backupName),r.originalHash))
                {reason=CultivationMigrationReason.SourceChanged;return false;}
                // Exact fully-prepared upgraded bytes prove Replace finished before the prior process stopped.
                // Only compatibility metadata is completed; the player snapshot is never rewritten on Load.
                r.state="Committed";Atomic(receiptPath,new UTF8Encoding(false).GetBytes(JsonUtility.ToJson(r,true)));return true;
            }catch(Exception e) when(e is IOException || e is UnauthorizedAccessException){reason=CultivationMigrationReason.VerificationFailed;return false;}
        }
        public static bool TryPrepare(string directory,SaveSlot slot,CultivationPlaytestProfile profile,Func<string,bool> validateLegacy,Func<string,bool> validateCurrent,out CultivationMigrationPlan plan,out CultivationMigrationReason reason)
        {
            plan=null;reason=CultivationMigrationReason.InvalidSource;
            try {
                if(profile==null || !profile.IsProduction || validateLegacy==null || validateCurrent==null)return false;
                string path=SlotPath(directory,slot);byte[] original=File.ReadAllBytes(path);string json=Text(original),component,error;
                Dictionary<string,string> fields;if(!CultivationSaveJson.TryObject(json,out fields,out error) || !CultivationGameplayState.TryReadHeroJson(json,profile,out component,out error))return false;
                if(!string.IsNullOrEmpty(component)){reason=CultivationMigrationReason.NotLegacy;return false;}
                if(!validateLegacy(json))return false;
                CultivationMigrationReceipt history;
                if(!ReadReceipt(Path.Combine(directory,slot+".cultivation-migration.json"),out history)){reason=CultivationMigrationReason.UnsupportedReceipt;return false;}
                if(history!=null) {
                    if(history.state=="Committed"){reason=CultivationMigrationReason.DowngradeGuard;return false;}
                    if(history.slot!=slot.ToString() || history.configurationId!=profile.Id || history.configurationVersion!=profile.Version || history.originalHash!=Hash(original)){reason=CultivationMigrationReason.SourceChanged;return false;}
                }
                string upgraded=UpgradeJson(fields,CultivationGameplayState.Capture(CultivationGameplayState.New(profile),profile));
                if(!validateCurrent(upgraded))return false;
                // Every unrelated root value is the EXACT original token, including unknown future-independent fields.
                Dictionary<string,string> result;if(!CultivationSaveJson.TryObject(upgraded,out result,out error))return false;
                foreach(var f in fields)if(f.Key!=CultivationGameplayState.SaveField && (!result.ContainsKey(f.Key) || result[f.Key]!=f.Value))return false;
                plan=new CultivationMigrationPlan(directory,slot,original,new UTF8Encoding(false).GetBytes(upgraded),profile);reason=CultivationMigrationReason.None;return true;
            }catch(Exception e) when(e is IOException || e is UnauthorizedAccessException || e is ArgumentException){return false;}
        }
        static void Stage(string path,byte[] bytes)
        {using(var f=new FileStream(path,FileMode.Create,FileAccess.Write,FileShare.None)){f.Write(bytes,0,bytes.Length);f.Flush(true);}}
        static void Atomic(string path,byte[] bytes)
        {string temp=path+".tmp";string previous=File.Exists(path)?Hash(File.ReadAllBytes(path)):null;Stage(temp,bytes);ReplaceExpected(temp,path,previous,Hash(bytes));}
        static bool Same(string path,string expected){return File.Exists(path) && Hash(File.ReadAllBytes(path))==expected;}
        internal static void ReplaceExpected(string temp,string path,string expected,string desired)
        {
            for(int attempt=0;attempt<4;attempt++) {
                string current=File.Exists(path)?Hash(File.ReadAllBytes(path)):null;
                if(attempt>0 && current==desired && (desired!=expected || !File.Exists(temp)))return;
                if(current!=expected || !Same(temp,desired))throw new IOException("Snapshot changed during atomic replace.");
                try {if(current==null)File.Move(temp,path);else File.Replace(temp,path,null);return;}
                catch(IOException) {
                    if(Same(path,desired) && (desired!=expected || !File.Exists(temp)))return;
                    if(attempt==3)throw;
                    System.Threading.Thread.Sleep(20);
                }
            }
        }
        static void Receipt(CultivationMigrationPlan p,string state)
        {
            p.Receipt.state=state;byte[] bytes=new UTF8Encoding(false).GetBytes(JsonUtility.ToJson(p.Receipt,true));
            string expected=File.Exists(p.ReceiptPath)?Hash(File.ReadAllBytes(p.ReceiptPath)):null,desired=Hash(bytes);
            string temp=p.ReceiptPath+".tmp";Stage(temp,bytes);
            // Windows can briefly refuse a replace immediately after creating this metadata file.
            // Bound retries to60ms, recheck both hashes, and accept an already-completed identical write.
            for(int attempt=0;attempt<4;attempt++) {
                string current=File.Exists(p.ReceiptPath)?Hash(File.ReadAllBytes(p.ReceiptPath)):null;
                if(current==desired)return;
                if(current!=expected || !Same(temp,desired))throw new IOException("Migration metadata changed during commit.");
                try {if(current!=null)File.Replace(temp,p.ReceiptPath,null);else File.Move(temp,p.ReceiptPath);return;}
                catch(IOException e){if(attempt==3)throw new IOException("Migration metadata atomic replace: "+e.Message,e);System.Threading.Thread.Sleep(20);}
            }
        }
        static void Fault(Action<string> fault,string point){if(fault!=null)fault(point);}
        public static string LastDiagnostic {get;private set;}
        public static bool Commit(CultivationMigrationPlan p,bool explicitConsent,Func<string,bool> validateCurrent,out CultivationMigrationReason reason,Action<string> ownedFault=null)
        {
            LastDiagnostic=null;reason=CultivationMigrationReason.ConsentRequired;if(p==null || !explicitConsent)return false;
            string path=SlotPath(p.Directory,p.Slot),temp=Path.Combine(p.Directory,p.Slot+".cultivation-upgrade.tmp");bool replaced=false;
            try {
                reason=CultivationMigrationReason.SourceChanged;if(!Same(path,p.OriginalHash))return false;
                reason=CultivationMigrationReason.BackupFailure;Fault(ownedFault,"BeforeBackup");
                if(!File.Exists(p.BackupPath))using(var b=new FileStream(p.BackupPath,FileMode.CreateNew,FileAccess.Write,FileShare.None)){b.Write(p.Original,0,p.Original.Length);b.Flush(true);}
                reason=CultivationMigrationReason.BackupMismatch;if(!Same(p.BackupPath,p.OriginalHash))return false;
                // Independent backup read/parse verifies recoverability before replacing the slot.
                Dictionary<string,string> originalFields;string error;if(!CultivationSaveJson.TryObject(Text(File.ReadAllBytes(p.BackupPath)),out originalFields,out error))return false;
                Receipt(p,"Prepared");Fault(ownedFault,"AfterBackup");reason=CultivationMigrationReason.WriteFailed;
                Stage(temp,p.Upgraded);Fault(ownedFault,"AfterTemp");Fault(ownedFault,"BeforeReplace");
                if(!Same(path,p.OriginalHash)){reason=CultivationMigrationReason.SourceChanged;return false;}
                ReplaceExpected(temp,path,p.OriginalHash,p.UpgradedHash);replaced=true;Fault(ownedFault,"AfterReplace");reason=CultivationMigrationReason.VerificationFailed;
                Fault(ownedFault,"BeforeVerify");if(!Same(path,p.UpgradedHash))throw new IOException("Committed bytes differ from prepared snapshot.");
                if(!validateCurrent(Text(File.ReadAllBytes(path))))throw new IOException("Committed snapshot validator rejected candidate.");
                Receipt(p,"Committed");reason=CultivationMigrationReason.None;return true;
            }catch(Exception e) when(e is IOException || e is UnauthorizedAccessException || e is ArgumentException) {
                LastDiagnostic=e.Message;
                if(replaced) {
                    // Only roll back the exact bytes this transaction committed; never overwrite a newer/different snapshot.
                    try {if(!Same(path,p.UpgradedHash)){reason=CultivationMigrationReason.RollbackBlocked;return false;}Atomic(path,p.Original);if(!Same(path,p.OriginalHash))throw new IOException("Rollback mismatch");Receipt(p,"Prepared");}
                    catch(Exception rollback) when(rollback is IOException || rollback is UnauthorizedAccessException){reason=CultivationMigrationReason.RollbackBlocked;}
                }
                return false;
            }
        }
        public static bool RestoreOriginal(CultivationMigrationPlan p,string expectedCurrentHash,out CultivationMigrationReason reason)
        {
            reason=CultivationMigrationReason.RollbackBlocked;if(p==null || expectedCurrentHash!=p.UpgradedHash)return false;
            try {
                string path=SlotPath(p.Directory,p.Slot);if(!Same(path,expectedCurrentHash) || !Same(p.BackupPath,p.OriginalHash))return false;
                Receipt(p,"RollbackPrepared");Atomic(path,p.Original);if(!Same(path,p.OriginalHash))return false;Receipt(p,"RolledBack");reason=CultivationMigrationReason.None;return true;
            }catch(Exception e) when(e is IOException || e is UnauthorizedAccessException){return false;}
        }
    }
}
