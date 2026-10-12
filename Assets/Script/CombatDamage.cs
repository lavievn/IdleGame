using System;
using System.Collections.Generic;
using TuTienCore;

public enum DamageSchool { Physical, Magical }

[Serializable]
public sealed class CombatDamagePart
{
    public DamageSchool school;
    public ElementType element = ElementType.Vo;
    public float attackMultiplier;
    public float fixedDamage;
    public List<string> additionalTags = new List<string>();
}

[Serializable]
public sealed class CombatResistanceSettings
{
    public float armorHalfReduction = 100f;
    public float magicResistanceHalfReduction = 100f;
    public float maximumSchoolReduction = .85f;
    public float maximumSchoolVulnerability = .5f;
    public float maximumElementReduction = .75f;
    public float minimumElementReduction = -.5f;
}

public sealed class CombatDamageResult
{
    public readonly List<float> afterDefense = new List<float>();
    public float rawDamage;
    public float afterModifiers;
    public float afterResistance;
    public int totalDamage;
}

public static class CombatDamage
{
    public static CombatDamageResult Resolve(EntityDataSO attacker, EntityDataSO defender,
        AttackMode mode, IEnumerable<CombatDamagePart> parts,
        CombatResistanceSettings settings = null)
    {
        var result = new CombatDamageResult();
        if (parts == null || defender == null) return result;
        if (settings == null) settings = new CombatResistanceSettings();
        float baseAttack = attacker == null ? 0f : attacker.GetCalculatedDamage(mode);
        foreach (var part in parts)
        {
            if (part == null) continue;
            double raw = Math.Max(0d, part.fixedDamage + baseAttack * part.attackMultiplier);
            if (raw <= 0d || double.IsNaN(raw) || double.IsInfinity(raw)) continue;
            result.rawDamage += (float)raw;
            var tags = new List<string>(CombatStats.AttackTags(mode,part.school,part.element));
            if (part.additionalTags != null) tags.AddRange(part.additionalTags);
            var stat = part.school == DamageSchool.Physical ?
                CombatStatId.PhysicalDamagePercent : CombatStatId.MagicalDamagePercent;
            float boost = CombatStats.Get(attacker,stat,0f,ElementType.Vo,tags) +
                CombatStats.Get(attacker,CombatStatId.ElementDamagePercent,0f,part.element,tags);
            double enhanced = raw * Math.Max(0d,1d + boost);
            result.afterModifiers += (float)enhanced;
            // Defender conditions concern incoming damage, never the
            // ATTACKER's combat stance.
            var incomingTags = new [] {
                part.school == DamageSchool.Physical ? "incoming:physical" : "incoming:magical",
                CombatStats.ElementTag(part.element)
            };
            float resistance = CombatStats.Get(defender,
                part.school == DamageSchool.Physical ? CombatStatId.Armor :
                CombatStatId.MagicResistance,0f,ElementType.Vo,incomingTags);
            float half = part.school == DamageSchool.Physical ?
                settings.armorHalfReduction : settings.magicResistanceHalfReduction;
            double reduction = resistance /
                (Math.Max(.001d,half) + Math.Abs(resistance));
            reduction = Math.Max(-Math.Max(0d,settings.maximumSchoolVulnerability),
                Math.Min(Math.Max(0d,settings.maximumSchoolReduction),reduction));
            float elemental = CombatStats.Get(defender,
                CombatStatId.ElementResistance,0f,part.element,incomingTags);
            double elementalReduction = Math.Max(settings.minimumElementReduction,
                Math.Min(settings.maximumElementReduction,elemental));
            double after = enhanced * (1d-reduction) * (1d-elementalReduction);
            after = Math.Max(0d,Math.Min(1000000000d,after));
            result.afterDefense.Add((float)after);
            result.afterResistance += (float)after;
        }
        result.totalDamage = result.rawDamage <= 0f ? 0 :
            (int)Math.Max(1d,Math.Min(int.MaxValue,
                Math.Round(result.afterResistance,MidpointRounding.AwayFromZero)));
        return result;
    }

    public static int ResolveLegacy(int alreadyRolled, EntityDataSO attacker,
        EntityDataSO defender, AttackMode mode, CombatResistanceSettings settings)
    {
        var part = new CombatDamagePart {
            school = mode == AttackMode.RangedMagic ? DamageSchool.Magical : DamageSchool.Physical,
            element = ElementType.Vo, fixedDamage = Math.Max(0,alreadyRolled)
        };
        return Resolve(attacker,defender,mode,new [] { part },settings).totalDamage;
    }
}
