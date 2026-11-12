using System;
using System.Collections.Generic;

// Data contract only. No automatic award rate, level, cap or combat bonus is
// defined. The contract is not installed as a new player-save component yet.
[Serializable]
public sealed class MasteryRecord
{
    public string id;
    public long units;
}

[Serializable]
public sealed class MasterySnapshot
{
    public int version = MasteryFoundation.Version;
    public List<MasteryRecord> weapons = new List<MasteryRecord>();
    public List<MasteryRecord> skills = new List<MasteryRecord>();
}

public static class MasteryFoundation
{
    public const int Version = 1;
    // Explicit storage IDs: display names, attack modes and equipment instance
    // IDs never become mastery identities. Do not rename existing keys.
    public static string WeaponId(WeaponFamily family)
    {
        switch(family)
        {
            case WeaponFamily.Unarmed: return "weapon-family:unarmed";
            case WeaponFamily.Sword: return "weapon-family:sword";
            case WeaponFamily.Bow: return "weapon-family:bow";
            case WeaponFamily.Staff: return "weapon-family:staff";
            default: return null;
        }
    }

    public static bool IsValid(MasterySnapshot snapshot)
    {
        if(snapshot==null || snapshot.version!=Version || snapshot.weapons==null || snapshot.skills==null)return false;
        var ids=new HashSet<string>(StringComparer.Ordinal);
        foreach(var record in snapshot.weapons)
        {
            bool known=false;
            foreach(WeaponFamily family in Enum.GetValues(typeof(WeaponFamily)))
                if(record!=null && record.id==WeaponId(family))known=true;
            if(!known || record.units<0 || !ids.Add(record.id))return false;
        }
        ids.Clear();
        foreach(var record in snapshot.skills)
            if(record==null || WeaponSkillRegistry.Find(record.id)==null || record.units<0 || !ids.Add(record.id))return false;
        return true;
    }
}

internal enum MasteryExecutionKind { BasicWeapon, Skill }

// Transient execution evidence, not a saved reward or proof of hit/damage/kill.
// A future approved award policy must consume evidence separately.
internal readonly struct MasteryExecutionEvidence
{
    internal readonly Guid Epoch;
    internal readonly long Sequence;
    internal readonly int Life,Spawn,Battle;
    internal readonly MasteryExecutionKind Kind;
    internal readonly WeaponFamily Family;
    internal readonly string SkillId;
    internal MasteryExecutionEvidence(Guid epoch,long sequence,int life,int spawn,int battle,
        MasteryExecutionKind kind,WeaponFamily family,string skillId=null)
    {Epoch=epoch;Sequence=sequence;Life=life;Spawn=spawn;Battle=battle;Kind=kind;Family=family;SkillId=skillId;}
    internal string TargetId => Kind==MasteryExecutionKind.BasicWeapon?MasteryFoundation.WeaponId(Family):SkillId;
}

internal sealed class MasteryExecutionReceipts
{
    readonly EntityDataSO owner;
    internal readonly Guid Epoch;
    readonly int life,spawn,battle;
    long lastSequence;

    internal MasteryExecutionReceipts(EntityDataSO owner,Guid epoch,int life,int spawn,int battle)
    {
        if(owner==null || epoch==Guid.Empty || life<1 || spawn<1 || battle<0)throw new ArgumentException("Explicit live mastery context required.");
        this.owner=owner;Epoch=epoch;this.life=life;this.spawn=spawn;this.battle=battle;
    }

    internal bool Matches(EntityDataSO data,int currentLife,int currentSpawn,int currentBattle) =>
        ReferenceEquals(owner,data) && life==currentLife && spawn==currentSpawn && battle==currentBattle;

    internal bool TryAccept(EntityDataSO data,MasteryExecutionEvidence evidence,bool executionCommitted)
    {
        if(!executionCommitted || !Matches(data,evidence.Life,evidence.Spawn,evidence.Battle) || evidence.Epoch!=Epoch ||
            evidence.Sequence<=lastSequence || MasteryFoundation.WeaponId(evidence.Family)==null ||
            !Enum.IsDefined(typeof(MasteryExecutionKind),evidence.Kind))return false;
        // Do not infer a used weapon from attack mode or a future/current swap.
        var weapon=WeaponBasicAttack.Equipped(data);
        if(WeaponBasicAttack.Family(weapon)!=evidence.Family)return false;
        if(evidence.Kind==MasteryExecutionKind.Skill)
        {
            var skill=WeaponSkillRegistry.Find(evidence.SkillId);
            if(skill==null || !skill.IsAvailable(data,weapon))return false;
        }
        else if(!string.IsNullOrEmpty(evidence.SkillId))return false;
        lastSequence=evidence.Sequence;
        return true; // Acceptance does not mutate EXP, Mastery units, stats or cooldowns.
    }
}
