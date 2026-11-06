using TuTienCore;
using UnityEngine;

partial class MotionRegression
{
    static void RealmSaveCompatibilityTests()
    {
        Run("v05 realm DTO roundtrip repeated load never grants a breakthrough",()=>{
            var e=RealmFixtureEvaluator();var original=new LegacyLevelRealmProgressionState("fixture:truc-co",45,46,97);
            var fixture=new RealmSaveCompatibilityFixture(e,original);string json=JsonUtility.ToJson(fixture.Capture(),true);
            Check(json.Contains("\"realmEntryLevel\":45") && json.Contains("fixture:truc-co"),"serialized DTO state");
            RealmSaveFixtureReason reason;
            for(int i=0;i<4;i++) {
                Check(fixture.TryLoadJson(json,out reason),"roundtrip/repeated load");var current=fixture.Current;
                Check(current.RealmId=="fixture:truc-co" && current.EntryLevel==45 && current.TotalLevel==46 && current.Experience==97,"no inferred realm or lost EXP");
                Check(e.Evaluate(current.RealmId,current.EntryLevel,current.TotalLevel).InternalLevel==2,"same derived state");
            }
        });
        Run("v05 realm DTO guards reject before candidate swap and do not infer high-level legacy realm",()=>{
            var fixture=new RealmSaveCompatibilityFixture(RealmFixtureEvaluator(),new LegacyLevelRealmProgressionState("fixture:luyen-khi",1,45,11));
            var original=fixture.Current;RealmSaveFixtureReason reason;
            Check(!fixture.TryLoad(new RealmSaveCompatibilityDto {totalLevel=45},out reason) && reason==RealmSaveFixtureReason.LegacyMigrationRequired,"no free realm for legacy45");
            foreach(int version in new[]{-1,0,2,int.MaxValue}) {
                var dto=fixture.Capture();dto.realmComponentVersion=version;
                Check(!fixture.TryLoad(dto,out reason) && reason==RealmSaveFixtureReason.UnsupportedVersion,"version guard");
                Check(fixture.Current==original,"version failure before mutation");
            }
            var bad=fixture.Capture();bad.realmId="unknown";
            Check(!fixture.TryLoad(bad,out reason) && reason==RealmSaveFixtureReason.InvalidState,"unknown ID");
            bad=fixture.Capture();bad.realmEntryLevel=46;Check(!fixture.TryLoad(bad,out reason),"entry after total");
            bad=fixture.Capture();bad.experience=-1;Check(!fixture.TryLoad(bad,out reason),"negative EXP");
            Check(!fixture.TryLoad(null,out reason) && reason==RealmSaveFixtureReason.MissingCandidate,"missing DTO");
            Check(fixture.Current==original,"all failures preserve live fixture reference");
        });
        Run("v05 realm missing fields never inherit previous state and DTO mutation cannot alter accepted snapshot",()=>{
            var fixture=new RealmSaveCompatibilityFixture(RealmFixtureEvaluator(),new LegacyLevelRealmProgressionState("fixture:truc-co",45,45,7));
            RealmSaveFixtureReason reason;var before=fixture.Current;
            Check(!fixture.TryLoadJson("{\"totalLevel\":45}",out reason) && reason==RealmSaveFixtureReason.LegacyMigrationRequired,"missing component blocked");
            Check(!fixture.TryLoadJson("{\"hasRealmComponent\":true,\"realmComponentVersion\":1,\"realmId\":\"fixture:luyen-khi\",\"totalLevel\":45}",out reason),"missing entry not inherited");
            Check(fixture.Current==before,"old state not changed");
            var dto=fixture.Capture();Check(fixture.TryLoad(dto,out reason),"valid snapshot");dto.realmId="unknown";dto.totalLevel=1;
            Check(fixture.Current.RealmId=="fixture:truc-co" && fixture.Current.TotalLevel==45,"candidate copy detached");
        });
    }
}
