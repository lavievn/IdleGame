using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
namespace TuTienCore
{
    [Serializable] public sealed class TribulationSaveReceipt
    {
        public int version,configurationVersion,heroLife,penaltyVersion,encounterVersion;public string encounterId,attemptId,ownerId,epoch,targetId,sourceRealm,targetRealm,configurationId,encounterConfiguration,encounterConfigHash,policyId,phase,outcome,appliedId,sourceCultivation,resultCultivation,sourceSnapshotHash;
        public double penalty,requestedPenalty;
    }
    public static class TribulationSaveTransaction
    {
        static bool Sha256(string value){if(value==null||value.Length!=64)return false;foreach(char ch in value)if((ch<'0'||ch>'9')&&(ch<'a'||ch>'f'))return false;return true;}
        static bool HasHash(string path,string hash){try{return File.Exists(path)&&CultivationMigrationTransaction.Hash(File.ReadAllBytes(path))==hash;}catch(Exception e) when(e is IOException||e is UnauthorizedAccessException){return false;}}
        public static string Hash(string text)=>CultivationMigrationTransaction.Hash(new UTF8Encoding(false).GetBytes(text));
        public static TribulationSaveReceipt Read(string json)
        {
            if(string.IsNullOrEmpty(json))return null;Dictionary<string,string> fields;string why;
            if(!CultivationSaveJson.TryObject(json,out fields,out why)||fields.Count!=typeof(TribulationSaveReceipt).GetFields().Length)throw new ArgumentException("Malformed trial receipt");
            foreach(var f in typeof(TribulationSaveReceipt).GetFields()){
                string token;if(!fields.TryGetValue(f.Name,out token))throw new ArgumentException("Missing trial receipt field");
                int i;double d;string s;if(f.FieldType==typeof(string)&&!CultivationSaveJson.TryString(token,out s)||f.FieldType==typeof(int)&&!int.TryParse(token,out i)||f.FieldType==typeof(double)&&(!double.TryParse(token,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out d)||!CultivationNumber.NonNegative(d)))throw new ArgumentException("Invalid trial receipt field");
            }
            var r=JsonUtility.FromJson<TribulationSaveReceipt>(json);r.penalty=double.Parse(fields["penalty"],System.Globalization.CultureInfo.InvariantCulture);r.requestedPenalty=double.Parse(fields["requestedPenalty"],System.Globalization.CultureInfo.InvariantCulture);return r;
        }
        public static bool Validate(string component,string currentCultivation,CultivationPlaytestProfile profile,HeavenlyGateConfiguration config,HeavenlyGatePenaltyPolicy policy,out string why)
        {
            why=null;if(string.IsNullOrEmpty(component))return true;
            try{
                var r=Read(component);Guid encounter,attempt,owner,epoch,target,applied;
                if(config==null||policy==null||!config.IsValid||profile==null||profile.IsProduction||profile.Id!=config.cultivationConfiguration||r.version!=1||r.configurationVersion!=profile.Version||r.configurationId!=profile.Id||r.encounterConfiguration!=config.id||r.encounterVersion!=config.version||r.encounterConfigHash!=Hash(JsonUtility.ToJson(config,false))||r.requestedPenalty!=policy.RequestedLoss||r.sourceRealm!=config.sourceRealm||r.targetRealm!=config.targetRealm||r.policyId!=policy.Id||r.penaltyVersion!=policy.Version||r.phase!="SealCombat"||r.heroLife<=0||!Sha256(r.sourceSnapshotHash)||!Guid.TryParseExact(r.encounterId,"N",out encounter)||!Guid.TryParseExact(r.attemptId,"N",out attempt)||!Guid.TryParseExact(r.ownerId,"N",out owner)||!Guid.TryParseExact(r.epoch,"N",out epoch)||!Guid.TryParseExact(r.targetId,"N",out target)||encounter==Guid.Empty||attempt==Guid.Empty||owner==Guid.Empty||epoch==Guid.Empty||target==Guid.Empty)throw new ArgumentException("Unsupported trial identity/policy");
                CultivationSession source,current;if(!CultivationGameplayState.TryRestore(r.sourceCultivation,profile,out source,out why)||!CultivationGameplayState.TryRestore(currentCultivation,profile,out current,out why)||source==null||current==null||source.OwnerId!=owner||current.OwnerId!=owner||source.Current.Foundation.ActualRealmId!=r.sourceRealm||source.CanAttemptBreakthrough()!=CultivationReason.None)throw new ArgumentException("Invalid trial source");
                if(r.outcome=="Active"){
                    if(r.appliedId!=""||r.resultCultivation!=""||r.penalty!=0||currentCultivation!=r.sourceCultivation)throw new ArgumentException("Incomplete active trial");return true;
                }
                if(!Guid.TryParseExact(r.appliedId,"N",out applied)||applied!=attempt)throw new ArgumentException("Invalid terminal application identity");
                CultivationReason result;if(r.outcome=="Success"){
                    if(r.penalty!=0)throw new ArgumentException("Success cannot charge penalty");result=new RealmBreakthroughTransition(source).ApplySuccess(owner,source.Epoch,attempt,source.RealmRevision,r.sourceRealm,r.targetRealm,0);
                }else if(r.outcome=="Failure"){
                    if(r.penalty!=policy.Loss(source.Realms,source.Current))throw new ArgumentException("Penalty policy mismatch");result=source.ApplyGatePenalty(owner,source.Epoch,attempt,source.RealmRevision,r.sourceRealm,policy);
                }else throw new ArgumentException("Unknown trial outcome");
                if(result!=CultivationReason.None||CultivationGameplayState.Capture(source,profile)!=r.resultCultivation||current.Current.Foundation.ActualRealmId!=source.Current.Foundation.ActualRealmId)throw new ArgumentException("Terminal trial proof mismatch");
                return true;
            }catch(Exception e) when(e is ArgumentException||e is FormatException||e is OverflowException){why=e.Message;return false;}
        }
        public static bool Commit(string directory,SaveSlot slot,string expected,string desired,string sourceHash,Action<string> fault,out string why)
        {
            why=null;if(!Sha256(expected)||!Sha256(sourceHash)){why="Invalid expected trial hash";return false;}
            string path=Path.Combine(directory,slot+".json"),temp=path+".tribulation.tmp",originalBackup=Path.Combine(directory,slot+".tribulation-"+sourceHash+".snapshot.bak");
            try{
                var bytes=new UTF8Encoding(false).GetBytes(desired);string wanted=CultivationMigrationTransaction.Hash(bytes);
                if(HasHash(path,wanted)){if(!HasHash(originalBackup,sourceHash))throw new IOException("Source provenance backup missing");return true;} // Retry an already verified complete result only.
                if(!File.Exists(path)||CultivationMigrationTransaction.Hash(File.ReadAllBytes(path))!=expected)throw new IOException("Newer snapshot preserved");
                byte[] original=File.ReadAllBytes(path);string backup=Path.Combine(directory,slot+".tribulation-"+expected+".snapshot.bak");
                if(!File.Exists(backup))using(var f=new FileStream(backup,FileMode.CreateNew,FileAccess.Write,FileShare.None)){f.Write(original,0,original.Length);f.Flush(true);}
                if(CultivationMigrationTransaction.Hash(File.ReadAllBytes(backup))!=expected)throw new IOException("Immutable trial backup mismatch");
                if(!HasHash(originalBackup,sourceHash))throw new IOException("Source provenance backup missing");
                fault?.Invoke("BeforeTemp");using(var f=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)){f.Write(bytes,0,bytes.Length);f.Flush(true);}fault?.Invoke("BeforeReplace");
                CultivationMigrationTransaction.ReplaceExpected(temp,path,expected,wanted);fault?.Invoke("AfterReplace");
                if(CultivationMigrationTransaction.Hash(File.ReadAllBytes(path))!=wanted)throw new IOException("Result verification mismatch");return true;
            }catch(Exception e) when(e is IOException||e is UnauthorizedAccessException){if(HasHash(path,Hash(desired))&&HasHash(originalBackup,sourceHash)){why=null;return true;}why=e.Message;return false;}
        }
    }
}
