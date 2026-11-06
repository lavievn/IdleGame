using System;
using TuTienCore;
using UnityEngine;

// Test-only DTO/candidate loader. Not a player Save schema or a migration policy.
[Serializable]
public sealed class RealmSaveCompatibilityDto
{
    public bool hasRealmComponent;
    public int realmComponentVersion;
    public string realmId;
    public int realmEntryLevel;
    public int totalLevel;
    public int experience;
}
public enum RealmSaveFixtureReason { None, MissingCandidate, LegacyMigrationRequired, UnsupportedVersion, InvalidState }
public sealed class RealmSaveCompatibilityFixture
{
    public const int FixtureVersion=1;
    readonly LegacyLevelRealmProgressionEvaluator evaluator;
    public LegacyLevelRealmProgressionState Current {get;private set;}
    public RealmSaveCompatibilityFixture(LegacyLevelRealmProgressionEvaluator evaluator,LegacyLevelRealmProgressionState current)
    {this.evaluator=evaluator;Current=current;}
    public bool TryLoad(RealmSaveCompatibilityDto candidate,out RealmSaveFixtureReason reason)
    {
        reason=RealmSaveFixtureReason.None;
        if(candidate==null)reason=RealmSaveFixtureReason.MissingCandidate;
        else if(!candidate.hasRealmComponent)reason=RealmSaveFixtureReason.LegacyMigrationRequired;
        else if(candidate.realmComponentVersion!=FixtureVersion)reason=RealmSaveFixtureReason.UnsupportedVersion;
        else if(candidate.experience<0 || !evaluator.Evaluate(candidate.realmId,candidate.realmEntryLevel,candidate.totalLevel).IsValid)
            reason=RealmSaveFixtureReason.InvalidState;
        if(reason!=RealmSaveFixtureReason.None)return false;
        // Swap only after all guards. Copy values, never retain the caller's DTO.
        Current=new LegacyLevelRealmProgressionState(candidate.realmId,candidate.realmEntryLevel,candidate.totalLevel,candidate.experience);
        return true;
    }
    public bool TryLoadJson(string json,out RealmSaveFixtureReason reason)
    {
        // Fresh DTO prevents absent fields inheriting a previous template realm.
        var candidate=new RealmSaveCompatibilityDto();
        JsonUtility.FromJsonOverwrite(json,candidate);
        return TryLoad(candidate,out reason);
    }
    public RealmSaveCompatibilityDto Capture()
    {return new RealmSaveCompatibilityDto {hasRealmComponent=true,realmComponentVersion=FixtureVersion,
        realmId=Current.RealmId,realmEntryLevel=Current.EntryLevel,totalLevel=Current.TotalLevel,experience=Current.Experience};}
}
