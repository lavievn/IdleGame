using System;
using System.Collections.Generic;
using TuTienCore;

// 0.0.51: shared, serializable stat/condition contract.
// The game still has three fighting stances; "class" is NOT a C# class per buff.
public enum CombatStatId
{
    MaxHealth, Attack, Armor, MagicResistance, ElementResistance,
    PhysicalDamagePercent, MagicalDamagePercent, ElementDamagePercent,
    AttackSpeed, MovementSpeed, CastSpeed, CooldownReductionPercent,
    HealthRegeneration, ManaRegeneration, KillHealBonus
}

[Serializable]
public sealed class CombatStatModifier
{
    // Durable reference to the source (origin, event, item, learned passive).
    public string sourceId = "";
    public CombatStatId stat;
    // Raw points. For *Percent stats* this is a ratio: .15 = +15%.
    public float flat;
    // Proportional adjustment of the stat's BASE value: .15 = +15% of base.
    public float percent;
    public ElementType element = ElementType.Vo;
    public bool anyElement;
    // Conditions are all required. Examples: "stance:bow", "terrain:forest".
    public List<string> requiredTags = new List<string>();
    // Any one blocked condition disables the modifier.
    public List<string> blockedTags = new List<string>();
}

public static class CombatStats
{
    public const int SchemaVersion = 1;

    public static string StanceTag(AttackMode mode)
    {
        return mode == AttackMode.Melee ? "stance:melee" :
            mode == AttackMode.RangedPhysical ? "stance:bow" : "stance:magic";
    }

    public static string ElementTag(ElementType element)
    {
        return "element:" + element.ToString().ToLowerInvariant();
    }

    public static float Get(EntityDataSO entity, CombatStatId stat, float baseValue = 0f,
        ElementType element = ElementType.Vo, IEnumerable<string> context = null)
    {
        if (entity == null) return baseValue;
        float flat = 0f, percentage = 0f;
        var modifiers = entity.combatStatModifiers;
        if (modifiers == null || modifiers.Count == 0) return baseValue;
        HashSet<string> active = BuildTags(entity, context);
        foreach (var mod in modifiers)
        {
            if (mod == null || mod.stat != stat) continue;
            if ((stat == CombatStatId.ElementResistance || stat == CombatStatId.ElementDamagePercent) &&
                !mod.anyElement && mod.element != element) continue;
            if (!Matches(mod, active)) continue;
            if (Finite(mod.flat)) flat += mod.flat;
            if (Finite(mod.percent)) percentage += mod.percent;
        }
        double result = ((double)baseValue + flat) + baseValue * (double)percentage;
        if (double.IsNaN(result)) return baseValue;
        return (float)Math.Max(-1000000000d, Math.Min(1000000000d, result));
    }

    public static int GetInt(EntityDataSO entity, CombatStatId stat, int baseValue = 0,
        ElementType element = ElementType.Vo, IEnumerable<string> context = null)
    {
        return (int)Math.Max(int.MinValue, Math.Min(int.MaxValue,
            Math.Round(Get(entity,stat,baseValue,element,context),MidpointRounding.AwayFromZero)));
    }

    public static IEnumerable<string> AttackTags(AttackMode mode, DamageSchool school,
        ElementType element)
    {
        yield return StanceTag(mode);
        yield return school == DamageSchool.Physical ? "damage:physical" : "damage:magical";
        yield return ElementTag(element);
    }

    private static HashSet<string> BuildTags(EntityDataSO entity, IEnumerable<string> context)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (entity.characterTags != null)
            foreach (string tag in entity.characterTags)
                if (!string.IsNullOrEmpty(tag)) result.Add(tag.Trim());
        if (context != null)
            foreach (string tag in context)
                if (!string.IsNullOrEmpty(tag)) result.Add(tag.Trim());
        return result;
    }

    private static bool Matches(CombatStatModifier modifier, HashSet<string> active)
    {
        if (modifier.requiredTags != null)
            foreach (var tag in modifier.requiredTags)
                if (!string.IsNullOrEmpty(tag) && !active.Contains(tag.Trim())) return false;
        if (modifier.blockedTags != null)
            foreach (var tag in modifier.blockedTags)
                if (!string.IsNullOrEmpty(tag) && active.Contains(tag.Trim())) return false;
        return true;
    }

    private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
}
