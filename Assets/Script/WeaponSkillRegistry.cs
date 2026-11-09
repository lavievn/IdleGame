using System;
using System.Collections.Generic;

internal enum CombatSkillSource { Learned, EquipmentGranted }
internal enum CombatResource { Mana }
internal enum SkillTargeting { ForwardRectangle, RandomForwardInRange, NearestInRange }
internal enum SkillEffect { PiercingThrust, ArrowVolley, LegacyFireball }

internal sealed class WeaponSkill
{
    public readonly string Id, Name;
    public readonly CombatSkillSource Source;
    public readonly IReadOnlyList<WeaponFamily> Families;
    public readonly float CastTime, Cooldown;
    public readonly CombatResource Resource;
    public readonly float Cost;
    public readonly SkillTargeting Targeting;
    public readonly SkillEffect Effect;
    public readonly float Width, Height;
    public readonly int ProjectileCount;

    internal WeaponSkill(string id, string name, CombatSkillSource source, WeaponFamily family,
        float castTime, float cooldown, CombatResource resource, float cost,
        SkillTargeting targeting, SkillEffect effect, float width = 0, float height = 0, int projectiles = 0)
    {
        Id = id;
        Name = name;
        Source = source;
        Families = Array.AsReadOnly(new[] { family });
        CastTime = castTime;
        Cooldown = cooldown;
        Resource = resource;
        Cost = cost;
        Targeting = targeting;
        Effect = effect;
        Width = width;
        Height = height;
        ProjectileCount = projectiles;
    }

    internal bool IsAvailable(EntityDataSO hero, EquipmentInstance weapon)
    {
        if (hero == null || !Supports(WeaponBasicAttack.Family(weapon))) return false;
        if (Source == CombatSkillSource.Learned) return hero.combatSkills.learned.Contains(Id);
        return weapon != null && weapon.rarity != EquipmentRarity.Common &&
            EquipmentFoundation.FindTemplate(weapon.templateId)?.grantedSkillId == Id;
    }

    bool Supports(WeaponFamily family)
    {
        foreach (var allowed in Families)
            if (allowed == family) return true;
        return false;
    }
}

internal static class WeaponSkillRegistry
{
    internal const string Thrust = "combat:kiem-dam";
    internal const string Volley = "combat:lien-hoan-tien";
    internal const string Fireball = "combat:hoa-cau-thuat";
    // Design pixels refer to the 800px gameplay view on MainGame's 1920-unit
    // BattleArea. Store the converted dimensions so window size cannot affect hits.
    const float UnitsPerDesignPixel = 1920f / 800f;
    internal static readonly IReadOnlyList<WeaponSkill> All = Array.AsReadOnly(new[]
    {
        new WeaponSkill(Thrust, "Đâm kiếm", CombatSkillSource.EquipmentGranted, WeaponFamily.Sword,
            .5f, 5f, CombatResource.Mana, 0, SkillTargeting.ForwardRectangle, SkillEffect.PiercingThrust,
            100 * UnitsPerDesignPixel, 35 * UnitsPerDesignPixel),
        new WeaponSkill(Volley, "Liên Hoàn Tiễn", CombatSkillSource.EquipmentGranted, WeaponFamily.Bow,
            .5f, 5f, CombatResource.Mana, 0, SkillTargeting.RandomForwardInRange, SkillEffect.ArrowVolley,
            projectiles: 3),
        new WeaponSkill(Fireball, "Hỏa Cầu Thuật", CombatSkillSource.EquipmentGranted, WeaponFamily.Staff,
            1f, 5f, CombatResource.Mana, 5, SkillTargeting.NearestInRange, SkillEffect.LegacyFireball)
    });

    internal static WeaponSkill Find(string id)
    {
        foreach (var skill in All)
            if (skill.Id == id) return skill;
        return null;
    }
}
