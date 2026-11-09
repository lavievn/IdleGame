using System;
using System.Collections.Generic;
using TuTienCore;

internal enum CombatSkillSource { Learned, EquipmentGranted }
internal enum CombatResource { Mana }
internal enum SkillTargeting { ForwardRectangle, RandomForwardInRange, NearestInRange }
internal enum SkillEffect { PiercingThrust, ArrowVolley, LegacyFireball }
internal enum SkillRequirementStage { Queue, Cast }

internal readonly struct SkillRequirement
{
    public readonly SkillRequirementStage Stage;
    public readonly string Label;
    public readonly bool Met;

    public SkillRequirement(SkillRequirementStage stage,string label,bool met)
    {
        Stage = stage;
        Label = label;
        Met = met;
    }
}

internal sealed class WeaponSkill
{
    public readonly string Id, Name, Description;
    public readonly AttackMode DamageMode;
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
        SkillTargeting targeting, SkillEffect effect, AttackMode damageMode, string description,
        float width = 0, float height = 0, int projectiles = 0)
    {
        Id = id;
        Name = name;
        Description = description;
        DamageMode = damageMode;
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

    internal bool IsAvailable(EntityDataSO hero, EquipmentInstance weapon) =>
        hero != null && Supports(WeaponBasicAttack.Family(weapon)) && HasSource(hero,weapon);

    private bool HasSource(EntityDataSO hero, EquipmentInstance weapon) =>
        Source == CombatSkillSource.Learned ? hero?.combatSkills?.learned?.Contains(Id) == true :
        EquipmentFoundation.GrantedSkillId(weapon) == Id;

    internal IEnumerable<SkillRequirement> Requirements(EntityDataSO hero, EquipmentInstance weapon)
    {
        var names = new List<string>();
        foreach (var family in Families)
            names.Add(family == WeaponFamily.Sword ? "Kiếm" : family == WeaponFamily.Bow ? "Cung" :
                family == WeaponFamily.Staff ? "Trượng" : "Tay không");
        yield return new SkillRequirement(SkillRequirementStage.Queue,
            "Trang bị " + string.Join(" / ", names) + " phù hợp", Supports(WeaponBasicAttack.Family(weapon)));
        yield return new SkillRequirement(SkillRequirementStage.Queue,
            Source == CombatSkillSource.Learned ? "Đã học kỹ năng" : "Vũ khí có cấp kỹ năng",
            HasSource(hero,weapon));
        if (Cost > 0)
            yield return new SkillRequirement(SkillRequirementStage.Cast,
                "Có " + Cost + " " + Resource, hero?.combatSkills?.CanPay(this) == true);
    }

    internal string DamageDescription()
    {
        bool magical = DamageMode == AttackMode.RangedMagic;
        string basis = magical ? "Công theo thế Trượng" : "Công vật lý";
        string factor = CombatBalance.ModeMultiplier(DamageMode,.7f).ToString("0.##",
            System.Globalization.CultureInfo.InvariantCulture).Replace('.',',');
        return (magical ? "Phép" : "Vật lý") + " · " + basis + " × " + factor +
            (ProjectileCount > 0 ? " mỗi mũi" : " mỗi mục tiêu") +
            "\nTrước biến thiên, linh căn, bổ trợ và phòng ngự.";
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
            AttackMode.Melee, "Đâm kiếm khí theo đường thẳng, trúng mỗi mục tiêu trong vùng phía trước một lần.",
            100 * UnitsPerDesignPixel, 35 * UnitsPerDesignPixel),
        new WeaponSkill(Volley, "Liên Hoàn Tiễn", CombatSkillSource.EquipmentGranted, WeaponFamily.Bow,
            .5f, 5f, CombatResource.Mana, 0, SkillTargeting.RandomForwardInRange, SkillEffect.ArrowVolley,
            AttackMode.RangedPhysical, "Bắn ba mũi liên tiếp. Mỗi mũi khóa một mục tiêu trong tầm phía trước.",
            projectiles: 3),
        new WeaponSkill(Fireball, "Hỏa Cầu Thuật", CombatSkillSource.EquipmentGranted, WeaponFamily.Staff,
            1f, 5f, CombatResource.Mana, 5, SkillTargeting.NearestInRange, SkillEffect.LegacyFireball,
            AttackMode.RangedMagic, "Phóng một hỏa cầu, gây sát thương phép trong vùng nổ quanh điểm chạm.")
    });

    internal static WeaponSkill Find(string id)
    {
        foreach (var skill in All)
            if (skill.Id == id) return skill;
        return null;
    }
}
