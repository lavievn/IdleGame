#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TuTienCore;

// Explicit CLI compilation/owned JSON fixture, never an automatic startup hook.
public static class DualProgressionV06Verifier
{
    [Serializable] sealed class Result {public string unity,profile,failure;public List<string> checks=new List<string>();}
    static void Require(bool ok,string message) {if(!ok)throw new InvalidOperationException(message);}
    public static void RunNative()
    {
        string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.utmp/dual-v06-"+Guid.NewGuid().ToString("N")));
        var result=new Result {unity=Application.unityVersion,profile=root};
        try
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Refusing active Play session.");
            Require(System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(root),"^dual-v06-[0-9a-f]{32}$"),"Profile name");
            for(var d=new DirectoryInfo(root);d!=null;d=d.Parent)Require(!d.Exists || (d.Attributes&FileAttributes.ReparsePoint)==0,"No reparse point");
            Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,".owner"),"dual-cultivation-v06");
            foreach(Type t in new[]{typeof(CultivationSession),typeof(CultivationRealmEvaluator),typeof(RealmBreakthroughTransition),typeof(BloodEssenceModifiers),typeof(InjuryTreatmentSession),typeof(CultivationCandidateSlot)})
                Require(t.Assembly==typeof(EntityDataSO).Assembly,"Unity compiled domain type "+t.FullName);
            result.checks.Add("Unity compiled all revised domain/transaction/transition/DTO types from Assembly-CSharp.");
            CultivationRealmEvaluator catalog;CultivationReason reason;
            Require(CultivationRealmEvaluator.TryCreate(new[]{new CultivationRealmDefinition("fixture:kim-dan",2,new[]{0d,300,600,1000},true,new[]{1,2,3,4},true)},out catalog,out reason),"Fixture catalog");
            var rules=new CultivationRules(60,1,1,0,1,100,0,1,0,.1,2,.001,.002,10,BloodCapacityOverflowPolicy.HoldExistingStopAccumulation);
            var owner=Guid.NewGuid();var oldEpoch=Guid.NewGuid();
            var source=new CultivationSession(owner,oldEpoch,catalog,rules,new CultivationSnapshot(new CultivationFoundation("fixture:kim-dan",1000),new UnrefinedQiReserve(123.5),new SpiritualEnergy(40),new BloodEssenceState(3,.25),new FoundationInjuryState(0)));
            var burn=Guid.NewGuid();double hp;Require(source.ActivateBlood(owner,oldEpoch,burn,20,100,out hp)==CultivationReason.None && hp==100,"Activation");
            Require(source.ExtendBlood(owner,oldEpoch,Guid.NewGuid(),burn)==CultivationReason.None,"Extension");
            var dto=CultivationComponentCodec.Capture(source,"fixture:native-v1");string json=JsonUtility.ToJson(dto,true);
            Require(!json.Contains("totalLevel") && !json.Contains("minorStage") && !json.Contains("effectiveCombatRealm") && !json.Contains("maximumSpiritual"),"No derived/Level fields saved");
            string path=Path.Combine(root,"component.json");File.WriteAllText(path,json);
            var parsed=new CultivationComponentDto();JsonUtility.FromJsonOverwrite(File.ReadAllText(path),parsed);
            var slot=new CultivationCandidateSlot(source);var newEpoch=Guid.NewGuid();
            Require(slot.TryLoad(parsed,catalog,rules,"fixture:native-v1",owner,newEpoch)==CultivationReason.None,"Native candidate load");
            var loaded=slot.Current;
            Require(loaded.Current.Reserve.Amount==123.5 && loaded.Current.Energy.Current==40 && loaded.Current.Blood.Charges==1 && loaded.Current.Blood.AccumulationProgress==.25,"Native primitive resource roundtrip");
            Require(loaded.Current.Burning.Status==BurningStatus.SettlementPending && loaded.Current.Burning.TotalDebt==200 && loaded.Current.Burning.ReferenceFoundation==1000 && loaded.Current.Burning.RemainingSeconds==60,"Native active debt roundtrip, no offline grant or discarded liability");
            Require(loaded.SettleBlood(owner,oldEpoch,burn)==CultivationReason.OwnerMismatch && loaded.SettleBlood(owner,newEpoch,burn)==CultivationReason.None && loaded.Current.Foundation.Amount==800,"Fresh epoch recovery, one payment");
            var paid=CultivationComponentCodec.Capture(loaded,"fixture:native-v1");var paidParsed=new CultivationComponentDto();JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(paid,true),paidParsed);
            Require(slot.TryLoad(paidParsed,catalog,rules,"fixture:native-v1",owner,Guid.NewGuid())==CultivationReason.None && slot.Current.SettleBlood(owner,slot.Current.Epoch,burn)==CultivationReason.AlreadySettled,"Native receipt prevents duplicate payment after reconstruction");
            result.checks.Add("Native JsonUtility/owned file preserves all resource values, locked burn reference/ratio/charges/time and settlement receipts; Load requires explicit settlement under fresh epoch.");
            var stable=slot.Current;
            foreach(string bad in new[]{json.Replace("\"version\": 1","\"version\": 2"),json.Replace("fixture:kim-dan","unknown:realm"),json.Replace("\"hasBurn\": true","\"hasBurn\": false"),"{}"}) {
                var invalid=new CultivationComponentDto();JsonUtility.FromJsonOverwrite(bad,invalid);
                Require(slot.TryLoad(invalid,catalog,rules,"fixture:native-v1",owner,Guid.NewGuid())!=CultivationReason.None && slot.Current==stable,"Future/unknown/hidden/missing component rejects before swap");
            }
            Require(!CultivationIntegrationGate.ProductionEnabled,"Production gate must stay closed");
            result.checks.Add("Future/unknown/missing/hidden-liability JSON rejected before swap; no player Save or incomplete production component used.");
            Debug.Log("[DUAL V06] PASS "+root);
        }
        catch(Exception ex) {result.failure=ex.ToString();Debug.LogError("[DUAL V06] FAIL "+ex);}
        if(Directory.Exists(root))File.WriteAllText(Path.Combine(root,"result.json"),JsonUtility.ToJson(result,true));
        EditorApplication.Exit(string.IsNullOrEmpty(result.failure)?0:2);
    }
}
#endif
