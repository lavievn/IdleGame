using TuTienCore;
using UnityEngine;

partial class MotionRegression
{
    static void RealmIntegrationTests()
    {
        Run("v06 canonical realm derives all stage boundaries without changing EXP growth or rewards",()=>{
            CombatBalance.ResetDevProfiles();var h=HeroAt(1);LegacyRealmSnapshotCompatibility.Initialize(h);
            var roots=h.spiritRoots;string id=h.realmId;int entry=h.realmEntryLevel;
            foreach(int target in new[]{1,9,10,19,20,29,30,45}) {
                while(h.currentLevel<target)h.AddExp(h.expToNextLevel-h.currentExp);
                RealmProgressionResult state;Check(LegacyRealmSnapshotCompatibility.TryEvaluateLegacyLevelSnapshot(h,out state),"valid canonical Realm");
                Check(state.InternalLevel==target && h.realmId==id && h.realmEntryLevel==entry,"no auto breakthrough "+target);
                Check(h.currentExp==0 && h.statPoints==target-1 && h.addedHealth==0 && h.addedDamage==0 && h.spiritRoots==roots,"only existing progression rewards "+target);
                Check(WeaponBasicAttack.Physical(h)==CombatBalance.HeroAttack(target) && WeaponBasicAttack.Magical(h)==0,"no Realm combat bonus");
                var expected=target>=30?RealmMinorStage.VienMan:target>=20?RealmMinorStage.HauKy:target>=10?RealmMinorStage.TrungKy:RealmMinorStage.SoKy;
                Check(state.MinorStage==expected && state.AtBottleneck==(target>=30),"derived stage "+target);
            }
            Check(LegacyRealmSnapshotCompatibility.Describe(h).Contains("Luyện Khí") && LegacyRealmSnapshotCompatibility.Describe(h).Contains(CultivationIntegrationGate.Status) && !LegacyRealmSnapshotCompatibility.Describe(h).Contains("Viên mãn"),"revised production presentation does not expose archived Level-derived stage");
        });
        Run("v06 canonical realm invalid state and cap guard have no silent normalization",()=>{
            var h=HeroAt(999);RealmProgressionResult state;
            Check(LegacyRealmSnapshotCompatibility.TryEvaluateLegacyLevelSnapshot(h,out state) && state.InternalLevel==999,"cap999");
            string before=JsonUtility.ToJson(h,true);h.realmId="realm:unknown";
            Check(!LegacyRealmSnapshotCompatibility.TryEvaluateLegacyLevelSnapshot(h,out state),"unknown ID rejected");h.realmId=LegacyRealmSnapshotCompatibility.LuyenKhiId;
            foreach(int entry in new[]{0,-1,1000,int.MaxValue}) {h.realmEntryLevel=entry;Check(!LegacyRealmSnapshotCompatibility.TryEvaluateLegacyLevelSnapshot(h,out state) && h.realmEntryLevel==entry,"invalid entry not clamped");}
            h.realmEntryLevel=1;h.realmComponentVersion=2;Check(!LegacyRealmSnapshotCompatibility.TryEvaluateLegacyLevelSnapshot(h,out state),"future version");h.realmComponentVersion=1;
            Check(JsonUtility.ToJson(h,true)==before,"validator did not mutate other state");
        });
    }
}
