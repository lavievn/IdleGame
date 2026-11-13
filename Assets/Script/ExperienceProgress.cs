using System;
using System.Globalization;
using System.Numerics;
using UnityEngine;
using TuTienCore;

[Serializable]
public sealed class ExperienceSnapshot
{
    public int version = 1;
    // Exact nonnegative integer text: neither floating-point rounding nor a
    // fixed-width overflow may discard earned EXP. This is not a balance cap.
    public string reserve = "0";
    public string lastReleaseAttempt = "";
    public string lastReleaseRealm = "";
}

public static class ExperienceProgress
{
    public const string SaveField = "experienceJson";

    public static bool TryRead(string component, out ExperienceSnapshot snapshot)
    {
        snapshot = null;
        if (component == null) return false;
        if (component.Length == 0) { snapshot = new ExperienceSnapshot(); return true; }
        if (!CultivationSaveJson.TryObject(component, out var fields, out _) || fields.Count != 4 ||
            !fields.TryGetValue("version", out var version) || version != "1" ||
            !fields.TryGetValue("reserve", out var amount) || !CultivationSaveJson.TryString(amount, out var reserve) ||
            !ValidInteger(reserve) ||
            !fields.TryGetValue("lastReleaseAttempt", out var attempt) || !CultivationSaveJson.TryString(attempt, out var id) ||
            !fields.TryGetValue("lastReleaseRealm", out var realm) || !CultivationSaveJson.TryString(realm, out var target) ||
            id == null || target == null ||
            (id.Length == 0 ? target.Length != 0 :
                !Guid.TryParseExact(id, "N", out var guid) || guid == Guid.Empty || string.IsNullOrWhiteSpace(target)))
            return false;
        snapshot = new ExperienceSnapshot { reserve = reserve, lastReleaseAttempt = id, lastReleaseRealm = target };
        return true;
    }

    static bool ValidInteger(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 1 && value[0] == '0') return false;
        foreach (char c in value) if (c < '0' || c > '9') return false;
        return true;
    }

    public static bool TryReadHeroJson(string root, out string component)
    {
        component = "";
        if (!CultivationSaveJson.TryObject(root, out var fields, out _)) return false;
        if (!fields.TryGetValue(SaveField, out var token)) return true; // Actual legacy absence only.
        return CultivationSaveJson.TryString(token, out component) && component != null && TryRead(component, out _);
    }

    internal static BigInteger Amount(ExperienceSnapshot snapshot) =>
        BigInteger.Parse(snapshot.reserve, CultureInfo.InvariantCulture);
    internal static string Capture(ExperienceSnapshot snapshot) => JsonUtility.ToJson(snapshot, false);

    // Structural path, not temporary actor readiness: injury, death or pause
    // cannot silently disable banking at an otherwise resolvable bottleneck.
    internal static bool HasBreakthroughPath(CultivationPlaytestProfile profile,
        HeavenlyGateConfiguration gate, string actualRealm)
    {
        CultivationRealmDefinition source, target;
        return profile != null && !profile.IsProduction && gate != null && gate.IsValid &&
            gate.cultivationConfiguration == profile.Id && gate.sourceRealm == actualRealm &&
            profile.ExplicitSuccessor(actualRealm) == gate.targetRealm &&
            profile.Catalog.TryGet(actualRealm, out source) && profile.Catalog.TryGet(gate.targetRealm, out target) &&
            target.CombatTier > source.CombatTier;
    }

    // Called only by the existing successful trial candidate transaction. The
    // domain transition validates revision/bottleneck/command; the source JSON
    // prevents a stale restored session from spending a newer Hero's reserve.
    internal static CultivationReason ApplyBreakthroughSuccess(EntityDataSO candidate, CultivationSession source,
        CultivationPlaytestProfile profile, HeavenlyGateConfiguration gate, Guid attempt, long expectedRevision)
    {
        if (candidate == null || source == null || attempt == Guid.Empty ||
            !TryRead(candidate.experienceJson, out var xp) || !candidate.CanApplyExperience())
            return CultivationReason.InvalidSnapshot;
        if (xp.lastReleaseAttempt == attempt.ToString("N")) return CultivationReason.DuplicateCommand;
        string actual = source.Current.Foundation.ActualRealmId;
        if (!HasBreakthroughPath(profile, gate, actual)) return CultivationReason.NoSuccessor;
        if (candidate.cultivationJson != CultivationGameplayState.Capture(source, profile)) return CultivationReason.StaleAttempt;
        var result = new RealmBreakthroughTransition(source).ApplySuccess(source.OwnerId, source.Epoch, attempt,
            expectedRevision, actual, gate.targetRealm, 0);
        if (result != CultivationReason.None) return result;
        BigInteger amount = Amount(xp);
        xp.reserve = "0";
        xp.lastReleaseAttempt = attempt.ToString("N");
        xp.lastReleaseRealm = gate.targetRealm;
        candidate.ApplyExperience(amount, xp);
        candidate.cultivationJson = CultivationGameplayState.Capture(source, profile);
        return CultivationReason.None;
    }
}
