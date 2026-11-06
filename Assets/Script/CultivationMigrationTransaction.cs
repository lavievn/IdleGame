using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using UnityEngine;
namespace TuTienCore
{
    public enum CultivationMigrationReason {None,NotLegacy,InvalidSource,SourceChanged,ConsentRequired,BackupMismatch,BackupFailure,UnsupportedReceipt,DowngradeGuard,WriteFailed,VerificationFailed,RollbackBlocked,AlreadyCurrent,UnsupportedConfiguration}
    [Serializable] class CultivationMigrationReceipt {public int version=1,configurationVersion;public string state,originalHash,upgradedHash,backupName,configurationId,slot;}
    [Serializable] sealed class CultivationConfigurationReceipt:CultivationMigrationReceipt
    {public string operation,sourceConfigurationId,previousReceiptHash,previousReceiptName;public int sourceConfigurationVersion;}
    public sealed class CultivationMigrationPlan
    {
        internal readonly byte[] Original,Upgraded,PreviousReceipt;internal readonly string Directory,ReceiptPath,ArchivePath;
        internal string ExpectedReceiptHash;internal CultivationMigrationReceipt Receipt;
        public readonly SaveSlot Slot;public readonly string OriginalHash,UpgradedHash,BackupPath;
        public readonly bool IsConfigurationUpgrade;public readonly string SourceConfigurationId,TargetConfigurationId;public readonly int SourceVersion,TargetVersion;
        internal CultivationMigrationPlan(string directory,SaveSlot slot,byte[] original,byte[] upgraded,CultivationPlaytestProfile source,CultivationPlaytestProfile target,CultivationMigrationReceipt history,byte[] previous)
        {
            Directory=directory;Slot=slot;Original=(byte[])original.Clone();Upgraded=(byte[])upgraded.Clone();OriginalHash=CultivationMigrationTransaction.Hash(original);UpgradedHash=CultivationMigrationTransaction.Hash(upgraded);
            IsConfigurationUpgrade=source!=null;SourceConfigurationId=source==null?"":source.Id;SourceVersion=source==null?0:source.Version;TargetConfigurationId=target.Id;TargetVersion=target.Version;
            string backup=slot+".cultivation-"+OriginalHash+(source==null?".legacy.bak":".snapshot.bak");BackupPath=Path.Combine(directory,backup);ReceiptPath=Path.Combine(directory,slot+".cultivation-migration.json");ExpectedReceiptHash=previous==null?null:CultivationMigrationTransaction.Hash(previous);
            var pending=history as CultivationConfigurationReceipt;
            if(pending!=null && pending.state=="Prepared" && pending.originalHash==OriginalHash && pending.configurationId==target.Id && pending.configurationVersion==target.Version && pending.sourceConfigurationId==SourceConfigurationId && pending.sourceConfigurationVersion==SourceVersion){Receipt=pending;Receipt.upgradedHash=UpgradedHash;}
            else if(source!=null || target.SaveVersion>=3){
                string prior=previous==null?"":CultivationMigrationTransaction.Hash(previous),name=previous==null?"":slot+".cultivation-history-"+prior+".receipt.json";
                PreviousReceipt=previous;ArchivePath=previous==null?null:Path.Combine(directory,name);
                Receipt=new CultivationConfigurationReceipt{version=2,operation=source==null?"legacy-upgrade":"configuration-upgrade",sourceConfigurationId=SourceConfigurationId,sourceConfigurationVersion=SourceVersion,previousReceiptHash=prior,previousReceiptName=name};
            }else Receipt=new CultivationMigrationReceipt();
            Receipt.state="Prepared";Receipt.originalHash=OriginalHash;Receipt.upgradedHash=UpgradedHash;Receipt.backupName=backup;Receipt.configurationId=target.Id;Receipt.configurationVersion=target.Version;Receipt.slot=slot.ToString();
        }
        public string UpgradedJson=>CultivationMigrationTransaction.Text(Upgraded);
    }
    // One transaction workflow for legacy initialization and supported config upgrades.
    public static class CultivationMigrationTransaction
    {
        public static string Hash(byte[] bytes){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        internal static string Text(byte[] bytes){using(var reader=new StreamReader(new MemoryStream(bytes),Encoding.UTF8,true))return reader.ReadToEnd();}
        [Serializable] sealed class Quoted {public string value;}
        static string Quote(string value){Dictionary<string,string> f;string why;CultivationSaveJson.TryObject(JsonUtility.ToJson(new Quoted{value=value}),out f,out why);return f["value"];}
        static string UpgradeJson(Dictionary<string,string> original,string component){var parts=new List<string>();foreach(var f in original)if(f.Key!=CultivationGameplayState.SaveField)parts.Add(Quote(f.Key)+":"+f.Value);parts.Add(Quote(CultivationGameplayState.SaveField)+":"+Quote(component));return "{"+string.Join(",",parts.ToArray())+"}";}
        static string SlotPath(string directory,SaveSlot slot){if(!Enum.IsDefined(typeof(SaveSlot),slot) || string.IsNullOrEmpty(directory) || !Path.IsPathRooted(directory))throw new ArgumentException("Explicit valid slot/directory required.");return Path.Combine(directory,slot+".json");}
        static bool Digest(string value){if(value==null || value.Length!=64)return false;foreach(char c in value)if(!(c>='0' && c<='9' || c>='a' && c<='f'))return false;return true;}
        static bool ReadReceipt(string path,out CultivationMigrationReceipt receipt,out byte[] bytes)
        {
            receipt=null;bytes=null;if(!File.Exists(path))return true;
            try{
                bytes=File.ReadAllBytes(path);Dictionary<string,string> fields;string why;int version;
                if(!CultivationSaveJson.TryObject(Text(bytes),out fields,out why) || !fields.ContainsKey("version") || !int.TryParse(fields["version"],out version) || version<1 || version>2)return false;
                CultivationMigrationReceipt r=version==1?new CultivationMigrationReceipt():new CultivationConfigurationReceipt();var members=r.GetType().GetFields();if(fields.Count!=members.Length)return false;
                foreach(var field in members){string token,text;int integer;if(!fields.TryGetValue(field.Name,out token) || field.FieldType==typeof(int) && !int.TryParse(token,out integer) || field.FieldType==typeof(string) && !CultivationSaveJson.TryString(token,out text))return false;}
                JsonUtility.FromJsonOverwrite(Text(bytes),r);SaveSlot slot;
                if(!Enum.TryParse(r.slot,out slot) || !Enum.IsDefined(typeof(SaveSlot),slot) || r.configurationVersion<1 || string.IsNullOrEmpty(r.configurationId) || (r.state!="Prepared" && r.state!="Committed" && r.state!="RollbackPrepared" && r.state!="RolledBack") || !Digest(r.originalHash) || !Digest(r.upgradedHash) || Path.GetFileName(r.backupName)!=r.backupName || !r.backupName.StartsWith(r.slot+".cultivation-"+r.originalHash,StringComparison.Ordinal) || !(r.backupName.EndsWith(".legacy.bak",StringComparison.Ordinal) || r.backupName.EndsWith(".snapshot.bak",StringComparison.Ordinal)))return false;
                if(version==2){var c=(CultivationConfigurationReceipt)r;if(c.operation!="legacy-upgrade" && c.operation!="configuration-upgrade" || c.operation=="configuration-upgrade" && (string.IsNullOrEmpty(c.sourceConfigurationId) || c.sourceConfigurationVersion<1) || c.operation=="legacy-upgrade" && (c.sourceConfigurationId!="" || c.sourceConfigurationVersion!=0) || c.previousReceiptHash==null || c.previousReceiptName==null)return false;if(c.previousReceiptHash!="" && (!Digest(c.previousReceiptHash) || c.previousReceiptName!=slot+".cultivation-history-"+c.previousReceiptHash+".receipt.json") || c.previousReceiptHash=="" && c.previousReceiptName!="")return false;}
                receipt=r;return true;
            }catch(Exception e) when(e is IOException || e is UnauthorizedAccessException || e is ArgumentException){return false;}
        }
        static bool KnownTarget(CultivationMigrationReceipt r,CultivationPlaytestProfile p){return p!=null && (r.configurationId==p.Id && r.configurationVersion==p.Version || CultivationProductionConfiguration.SupportedVersion(r.configurationId,r.configurationVersion)>0);}
        static bool HistoryValid(string directory,CultivationMigrationReceipt r){var c=r as CultivationConfigurationReceipt;return c==null || c.previousReceiptHash=="" || Same(Path.Combine(directory,c.previousReceiptName),c.previousReceiptHash);}
        public static bool CompleteInterruptedReceipt(string directory,SaveSlot slot,CultivationPlaytestProfile profile,out CultivationMigrationReason reason)
        {
            reason=CultivationMigrationReason.None;string receiptPath=Path.Combine(directory,slot+".cultivation-migration.json");CultivationMigrationReceipt r;byte[] bytes;
            if(!ReadReceipt(receiptPath,out r,out bytes) || r!=null && (!KnownTarget(r,profile) || r.slot!=slot.ToString())){reason=CultivationMigrationReason.UnsupportedReceipt;return false;}
            if(r==null)return true;
            try{
                if(!HistoryValid(directory,r)){reason=CultivationMigrationReason.BackupMismatch;return false;}
                if(r.state=="Committed" || r.state=="RolledBack")return true;
                string path=SlotPath(directory,slot);if(Same(path,r.originalHash) && Same(Path.Combine(directory,r.backupName),r.originalHash))return true;
                string component,error;
                if(!CultivationGameplayState.TryReadHeroJson(Text(File.ReadAllBytes(path)),profile,out component,out error) || string.IsNullOrEmpty(component))return true;
                CultivationPlaytestProfile resolved;if(!CultivationProductionConfiguration.TryResolve(component,profile,out resolved,out error) || r.configurationId!=resolved.Id || r.configurationVersion!=resolved.Version || !Same(path,r.upgradedHash) || !Same(Path.Combine(directory,r.backupName),r.originalHash)){reason=CultivationMigrationReason.SourceChanged;return false;}
                r.state="Committed";AtomicExpected(receiptPath,new UTF8Encoding(false).GetBytes(JsonUtility.ToJson(r,true)),Hash(bytes));return true;
            }catch(Exception e) when(e is IOException || e is UnauthorizedAccessException){reason=CultivationMigrationReason.VerificationFailed;return false;}
        }
        public static bool TryPrepare(string directory,SaveSlot slot,CultivationPlaytestProfile target,Func<string,bool> validateLegacy,Func<string,bool> validateCurrent,out CultivationMigrationPlan plan,out CultivationMigrationReason reason)
        {return Prepare(directory,slot,null,target,validateLegacy,validateCurrent,out plan,out reason);}
        public static bool TryPrepareConfiguration(string directory,SaveSlot slot,CultivationPlaytestProfile source,CultivationPlaytestProfile target,Func<string,bool> validateSource,Func<string,bool> validateTarget,out CultivationMigrationPlan plan,out CultivationMigrationReason reason)
        {return Prepare(directory,slot,source,target,validateSource,validateTarget,out plan,out reason);}
        static bool Prepare(string directory,SaveSlot slot,CultivationPlaytestProfile source,CultivationPlaytestProfile target,Func<string,bool> validateSource,Func<string,bool> validateTarget,out CultivationMigrationPlan plan,out CultivationMigrationReason reason)
        {
            plan=null;reason=CultivationMigrationReason.InvalidSource;
            try{
                if(target==null || !target.IsProduction || validateSource==null || validateTarget==null)return false;
                byte[] original=File.ReadAllBytes(SlotPath(directory,slot));string raw=Text(original),component,error;Dictionary<string,string> fields;
                if(!CultivationSaveJson.TryObject(raw,out fields,out error) || !CultivationGameplayState.TryReadHeroJson(raw,source??target,out component,out error))return false;
                if(source==null && !string.IsNullOrEmpty(component)){reason=CultivationMigrationReason.NotLegacy;return false;}
                if(source!=null && (string.IsNullOrEmpty(component) || source.Id==target.Id && source.Version==target.Version)){reason=CultivationMigrationReason.AlreadyCurrent;return false;}
                if(!validateSource(raw))return false;
                CultivationMigrationReceipt history;byte[] prior;
                if(!ReadReceipt(Path.Combine(directory,slot+".cultivation-migration.json"),out history,out prior) || history!=null && (!KnownTarget(history,target) || history.slot!=slot.ToString() || !HistoryValid(directory,history))){reason=CultivationMigrationReason.UnsupportedReceipt;return false;}
                if(source==null && history!=null && history.state=="Committed"){reason=CultivationMigrationReason.DowngradeGuard;return false;}
                string upgradedComponent;
                if(source==null)upgradedComponent=CultivationGameplayState.Capture(CultivationGameplayState.New(target),target);
                else if(!CultivationConfigurationUpgrade.TryConvert(component,source,target,out upgradedComponent,out error)){reason=CultivationMigrationReason.UnsupportedConfiguration;return false;}
                string upgraded=UpgradeJson(fields,upgradedComponent);if(!validateTarget(upgraded))return false;
                Dictionary<string,string> converted;if(!CultivationSaveJson.TryObject(upgraded,out converted,out error))return false;foreach(var field in fields)if(field.Key!=CultivationGameplayState.SaveField && (!converted.ContainsKey(field.Key) || converted[field.Key]!=field.Value))return false;
                plan=new CultivationMigrationPlan(directory,slot,original,new UTF8Encoding(false).GetBytes(upgraded),source,target,history,prior);reason=CultivationMigrationReason.None;return true;
            }catch(Exception e) when(e is IOException || e is UnauthorizedAccessException || e is ArgumentException){return false;}
        }
        static bool Same(string path,string expected){return File.Exists(path) && Hash(File.ReadAllBytes(path))==expected;}
        static void Stage(string path,byte[] bytes){using(var f=new FileStream(path,FileMode.Create,FileAccess.Write,FileShare.None)){f.Write(bytes,0,bytes.Length);f.Flush(true);}}
        static void AtomicExpected(string path,byte[] bytes,string expected){string temp=path+".tmp";Stage(temp,bytes);ReplaceExpected(temp,path,expected,Hash(bytes));}
        internal static void ReplaceExpected(string temp,string path,string expected,string desired)
        {
            for(int attempt=0;attempt<4;attempt++){
                string current=File.Exists(path)?Hash(File.ReadAllBytes(path)):null;if(attempt>0 && current==desired && (desired!=expected || !File.Exists(temp)))return;
                if(current!=expected || !Same(temp,desired))throw new IOException("Snapshot changed during atomic replace.");
                try{if(current==null)File.Move(temp,path);else File.Replace(temp,path,null);return;}catch(IOException){if(Same(path,desired) && (desired!=expected || !File.Exists(temp)))return;if(attempt==3)throw;System.Threading.Thread.Sleep(20);}
            }
        }
        static void Immutable(string path,byte[] bytes,string expected){if(!File.Exists(path))using(var f=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None)){f.Write(bytes,0,bytes.Length);f.Flush(true);}if(!Same(path,expected))throw new IOException("Immutable backup/history mismatch.");}
        static void Receipt(CultivationMigrationPlan p,string state){p.Receipt.state=state;byte[] bytes=new UTF8Encoding(false).GetBytes(JsonUtility.ToJson(p.Receipt,true));AtomicExpected(p.ReceiptPath,bytes,p.ExpectedReceiptHash);p.ExpectedReceiptHash=Hash(bytes);}
        static void Fault(Action<string> f,string point){if(f!=null)f(point);}
        public static string LastDiagnostic{get;private set;}
        public static bool Commit(CultivationMigrationPlan p,bool explicitConsent,Func<string,bool> validateCurrent,out CultivationMigrationReason reason,Action<string> ownedFault=null)
        {
            reason=CultivationMigrationReason.ConsentRequired;LastDiagnostic=null;if(p==null || !explicitConsent)return false;
            string path=SlotPath(p.Directory,p.Slot),temp=Path.Combine(p.Directory,p.Slot+".cultivation-upgrade.tmp");bool replaced=false;
            try{
                reason=CultivationMigrationReason.SourceChanged;if(!Same(path,p.OriginalHash) || (File.Exists(p.ReceiptPath)?Hash(File.ReadAllBytes(p.ReceiptPath)):null)!=p.ExpectedReceiptHash)return false;
                reason=CultivationMigrationReason.BackupFailure;Fault(ownedFault,"BeforeBackup");Immutable(p.BackupPath,p.Original,p.OriginalHash);
                reason=CultivationMigrationReason.BackupMismatch;if(!Same(p.BackupPath,p.OriginalHash))return false;Dictionary<string,string> f;string error;if(!CultivationSaveJson.TryObject(Text(File.ReadAllBytes(p.BackupPath)),out f,out error))return false;
                Fault(ownedFault,"BeforeReceiptArchive");if(p.ArchivePath!=null)Immutable(p.ArchivePath,p.PreviousReceipt,Hash(p.PreviousReceipt));Fault(ownedFault,"AfterReceiptArchive");
                Receipt(p,"Prepared");Fault(ownedFault,"AfterBackup");reason=CultivationMigrationReason.WriteFailed;Stage(temp,p.Upgraded);Fault(ownedFault,"AfterTemp");Fault(ownedFault,"BeforeReplace");
                if(!Same(path,p.OriginalHash)){reason=CultivationMigrationReason.SourceChanged;return false;}ReplaceExpected(temp,path,p.OriginalHash,p.UpgradedHash);replaced=true;Fault(ownedFault,"AfterReplace");reason=CultivationMigrationReason.VerificationFailed;Fault(ownedFault,"BeforeVerify");
                if(!Same(path,p.UpgradedHash) || !validateCurrent(Text(File.ReadAllBytes(path))))throw new IOException("Committed snapshot mismatch/invalid candidate.");Receipt(p,"Committed");reason=CultivationMigrationReason.None;return true;
            }catch(Exception e) when(e is IOException || e is UnauthorizedAccessException || e is ArgumentException){LastDiagnostic=e.Message;if(replaced){try{if(!Same(path,p.UpgradedHash)){reason=CultivationMigrationReason.RollbackBlocked;return false;}AtomicExpected(path,p.Original,p.UpgradedHash);if(!Same(path,p.OriginalHash))throw new IOException("Rollback mismatch");Receipt(p,"Prepared");}catch(Exception rollback) when(rollback is IOException || rollback is UnauthorizedAccessException){reason=CultivationMigrationReason.RollbackBlocked;}}return false;}
        }
        public static bool RestoreOriginal(CultivationMigrationPlan p,string expectedCurrentHash,out CultivationMigrationReason reason)
        {
            reason=CultivationMigrationReason.RollbackBlocked;if(p==null || expectedCurrentHash!=p.UpgradedHash)return false;
            try{string path=SlotPath(p.Directory,p.Slot);if(!Same(path,expectedCurrentHash) || !Same(p.BackupPath,p.OriginalHash))return false;Receipt(p,"RollbackPrepared");AtomicExpected(path,p.Original,p.UpgradedHash);if(!Same(path,p.OriginalHash))return false;Receipt(p,"RolledBack");reason=CultivationMigrationReason.None;return true;}catch(Exception e) when(e is IOException || e is UnauthorizedAccessException){return false;}
        }
    }
}
