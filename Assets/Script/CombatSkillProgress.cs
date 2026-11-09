using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using TuTienCore;

[Serializable]
public sealed class CombatSkillCooldown
{
    public string id;
    public float seconds;
}

// Snapshot belongs to the saved primary Hero. Transient casts and bolts are
// deliberately excluded. A save made during windup has not spent the cost.
[Serializable]
public sealed class CombatSkillProgress
{
    public const int CurrentVersion = 1;
    public const float BaseMaxMana = 100f, BaseManaRegen = 2f, BaseExplorationHPRegen = 10f;
    internal static bool IsResourceStat(CombatStatId stat) =>
        stat==CombatStatId.MaxMana || stat==CombatStatId.ManaRegeneration || stat==CombatStatId.HealthRegeneration;

    internal static float ResourceBaseline(CombatStatId stat)
    {
        switch(stat)
        {
            case CombatStatId.MaxMana: return BaseMaxMana;
            case CombatStatId.ManaRegeneration: return BaseManaRegen;
            case CombatStatId.HealthRegeneration: return BaseExplorationHPRegen;
            default: return 0f;
        }
    }

    public int version = CurrentVersion;
    public float mana = BaseMaxMana;
    public int health = -1; // Absent in old saves, or intentional fresh life.
    public List<string> learned = new List<string>();
    public List<string> queue = new List<string>();
    public string nextSkillId = "";
    public List<CombatSkillCooldown> cooldowns = new List<CombatSkillCooldown>();

    internal float Remaining(string id)
    {
        foreach (var timer in cooldowns)
            if (timer.id == id) return timer.seconds;
        return 0;
    }

    internal bool CanPay(WeaponSkill skill) => skill.Cost == 0 || skill.Resource == CombatResource.Mana && mana >= skill.Cost;

    internal void RecordRelease(WeaponSkill skill)
    {
        if (skill.Resource == CombatResource.Mana) mana -= skill.Cost;
        cooldowns.Add(new CombatSkillCooldown { id = skill.Id, seconds = skill.Cooldown });
        int index = queue.IndexOf(skill.Id);
        nextSkillId = queue[(index + 1) % queue.Count];
    }

    internal bool Advance(float dt, float manaRate, float maxMana)
    {
        float updated = Mathf.Clamp(mana + manaRate * dt, 0, maxMana);
        bool changed = updated != mana || cooldowns.Count != 0;
        mana = updated;
        for (int i = cooldowns.Count - 1; i >= 0; i--)
        {
            cooldowns[i].seconds -= dt;
            if (cooldowns[i].seconds <= 0) cooldowns.RemoveAt(i);
        }
        return changed;
    }

    internal bool Add(string id)
    {
        if (queue.Contains(id)) return false;
        queue.Add(id);
        if (queue.Count == 1) nextSkillId = id;
        return true;
    }

    internal bool Remove(string id)
    {
        int index = queue.IndexOf(id);
        if (index < 0) return false;
        queue.RemoveAt(index);
        if (nextSkillId == id) nextSkillId = queue.Count == 0 ? "" : queue[index % queue.Count];
        return true;
    }

    internal bool Move(int index, int delta)
    {
        int destination = index + delta;
        if (index < 0 || index >= queue.Count || destination < 0 || destination >= queue.Count) return false;
        string entry = queue[index];
        queue.RemoveAt(index);
        queue.Insert(destination, entry);
        return true;
    }

    internal bool IsValid()
    {
        if (version != CurrentVersion || !FiniteNonnegative(mana) || health < -1 ||
            !ValidIds(queue) || !ValidIds(learned) || nextSkillId == null ||
            (nextSkillId.Length != 0 && !queue.Contains(nextSkillId)) || cooldowns == null)
            return false;
        var unique = new HashSet<string>(StringComparer.Ordinal);
        foreach (var timer in cooldowns)
            if (timer == null || !MartialArtsRegistry.ValidId(timer.id) ||
                !unique.Add(timer.id) || !FiniteNonnegative(timer.seconds) || timer.seconds <= 0)
                return false;
        return true;
    }

    static bool ValidIds(List<string> ids)
    {
        if (ids == null) return false;
        var unique = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in ids)
            if (!MartialArtsRegistry.ValidId(id) || !unique.Add(id)) return false;
        return true;
    }

    static bool FiniteNonnegative(float n) => n >= 0 && !float.IsInfinity(n);

    static bool ArrayField(Dictionary<string, string> fields, string name) =>
        fields.TryGetValue(name, out var token) && token.StartsWith("[", StringComparison.Ordinal);

    internal static bool TryRead(string root, out CombatSkillProgress progress)
    {
        progress = null;
        if (!CultivationSaveJson.TryObject(root, out var fields, out _)) return false;
        if (!fields.TryGetValue("combatSkills", out var component))
        {
            progress = new CombatSkillProgress();
            return true;
        }
        if (!CultivationSaveJson.TryObject(component, out var parts, out _) ||
            !parts.TryGetValue("version", out var versionToken) ||
            !int.TryParse(versionToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out int schema) ||
            schema != CurrentVersion ||
            !parts.TryGetValue("mana", out var manaToken) ||
            !float.TryParse(manaToken, NumberStyles.Float, CultureInfo.InvariantCulture, out float savedMana) ||
            !FiniteNonnegative(savedMana) ||
            !parts.TryGetValue("health", out var hpToken) ||
            !int.TryParse(hpToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out int savedHP) ||
            savedHP < -1 || !ArrayField(parts, "queue") || !ArrayField(parts, "learned") ||
            !parts.TryGetValue("nextSkillId", out var next) || !CultivationSaveJson.TryString(next, out _) ||
            !ArrayField(parts, "cooldowns"))
            return false;
        try
        {
            progress = JsonUtility.FromJson<CombatSkillProgress>(component);
            if (progress == null || progress.health != savedHP) return false;
            progress.mana = savedMana;
            return progress.IsValid();
        }
        catch (ArgumentException) { return false; }
    }
}
