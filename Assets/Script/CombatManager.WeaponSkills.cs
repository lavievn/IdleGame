using System.Collections.Generic;
using UnityEngine;
using TuTienCore;

public partial class CombatManager
{
    sealed class SkillCast
    {
        internal WeaponSkill Skill;
        internal string WeaponId;
        internal int Battle, Life, Spawn;
        internal float Remaining;
    }

    SkillCast weaponCast;
    readonly System.Random staffArcRandom = new System.Random();
    internal long WeaponSkillReleases { get; private set; }
    internal long VolleyProjectiles { get; private set; }
    internal string LastWeaponSkill { get; private set; }

    void TickCombatResources(HeroCombatState actor, float dt)
    {
        if (actor != primaryHeroState || gameManager == null || !gameManager.IsGameplayHUDVisible ||
            UIManager.Instance != null && UIManager.Instance.CombatResourcesMenuOpen)
            return;
        var progress = actor.Data.combatSkills;
        var tags = new[] { CombatStats.StanceTag(actor.SelectedMode) };
        float max = actor.MaxMana;
        float manaRate = progress.mana < max ? Mathf.Max(0, CombatStats.Get(actor.Data,
            CombatStatId.ManaRegeneration, CombatSkillProgress.BaseManaRegen, ElementType.Vo, tags)) : 0;
        if (progress.Advance(dt, manaRate, max)) actor.Data.isDirty = true;
    }

    // The existing basic clock is the decision boundary. A rejected skill never
    // consumes the ready basic attack or reserves a second cast slot.
    bool TryBeginWeaponCast(HeroCombatState actor, EnvironmentManager space)
    {
        if (actor != primaryHeroState || !isBattling) return false;
        var progress = actor.Data.combatSkills;
        if (progress.queue.Count == 0) return false;
        int first = Mathf.Max(0, progress.queue.IndexOf(progress.nextSkillId));
        var weapon = WeaponBasicAttack.Equipped(actor.Data);
        for (int step = 0; step < progress.queue.Count; step++)
        {
            var skill = WeaponSkillRegistry.Find(progress.queue[(first + step) % progress.queue.Count]);
            if (skill == null || !skill.IsAvailable(actor.Data, weapon) ||
                progress.Remaining(skill.Id) > 0 || !progress.CanPay(skill) ||
                PickSkillTarget(skill, actor, space, false) == null)
                continue;
            weaponCast = new SkillCast {
                Skill = skill, WeaponId = actor.WeaponIdentity, Battle = battleVersion,
                Life = actor.LifeVersion, Spawn = actor.Controller.SpawnVersion, Remaining = skill.CastTime
            };
            actor.Controller.UpdateChargeBar(true, 0);
            return true;
        }
        return false;
    }

    bool TickWeaponCast(HeroCombatState actor, EnvironmentManager space, float dt)
    {
        if (actor != primaryHeroState || weaponCast == null) return false;
        var cast = weaponCast;
        var skill = cast.Skill;
        var progress = actor.Data.combatSkills;
        if (!isBattling || cast.Battle != battleVersion || cast.Life != actor.LifeVersion ||
            cast.Spawn != actor.Controller.SpawnVersion || cast.WeaponId != actor.WeaponIdentity ||
            !progress.queue.Contains(skill.Id) || !skill.IsAvailable(actor.Data, WeaponBasicAttack.Equipped(actor.Data)) ||
            !progress.CanPay(skill))
        {
            CancelWeaponCast();
            return false;
        }
        cast.Remaining -= dt;
        actor.Controller.UpdateChargeBar(true, Mathf.Clamp01(1f - cast.Remaining / skill.CastTime));
        if (cast.Remaining > 0) return true;
        CancelWeaponCast();
        var target = PickSkillTarget(skill, actor, space, skill.Effect == SkillEffect.ArrowVolley);
        if (target == null) return false;

        progress.RecordRelease(skill);
        actor.Data.isDirty = true;
        WeaponSkillReleases++;
        LastWeaponSkill = skill.Id;
        gameManager?.UpdateEventLog("Thi triển " + skill.Name);
        actor.Controller.PlayAttackFeedback();
        switch (skill.Effect)
        {
            case SkillEffect.PiercingThrust:
                ReleaseThrust(skill, actor, space);
                break;
            case SkillEffect.ArrowVolley:
                ReleaseVolleyArrow(skill, target, actor, space, skill.ProjectileCount);
                break;
            case SkillEffect.LegacyFireball:
                impactRelease = ++HeroReleaseCount;
                LaunchProjectile(target, true, AttackMode.RangedMagic,
                    RollDamageTrace(actor.Data, target.data, AttackMode.RangedMagic),
                    skill.Cooldown, space, actor, skill);
                break;
        }
        return true;
    }

    void CancelWeaponCast()
    {
        if (weaponCast == null) return;
        weaponCast = null;
        primaryHeroState.Controller?.UpdateChargeBar(false, 0);
    }

    ActiveMonsterInfo PickSkillTarget(WeaponSkill skill, HeroCombatState actor,
        EnvironmentManager space, bool random)
    {
        ActiveMonsterInfo chosen = null;
        int seen = 0;
        float nearest = float.MaxValue;
        var origin = space.Position(actor.Controller.heroRect);
        foreach (var candidate in activeMonsters)
        {
            if (!CanTargetSkill(skill, actor, candidate, space, origin)) continue;
            if (random)
            {
                // Reservoir sampling chooses uniformly without changing basic targeting.
                if (Random.Range(0, ++seen) == 0) chosen = candidate;
            }
            else
            {
                float distance = Mathf.Abs(space.Position(candidate.controller.Rect).x - origin.x);
                if (distance < nearest) { nearest = distance; chosen = candidate; }
            }
        }
        return chosen;
    }

    bool CanTargetSkill(WeaponSkill skill, HeroCombatState actor, ActiveMonsterInfo target,
        EnvironmentManager space, Vector2 origin)
    {
        if (target == null || target.currentHP <= 0 || target.controller == null || !target.controller.IsAlive)
            return false;
        var delta = space.Position(target.controller.Rect) - origin;
        if (skill.Targeting == SkillTargeting.ForwardRectangle)
            return delta.x <= 0 && delta.x >= -skill.Width && Mathf.Abs(delta.y) <= skill.Height / 2 &&
                (target != gateTarget || actor.Controller.CanAttack(target.controller, space));
        if (skill.Targeting == SkillTargeting.RandomForwardInRange && delta.x > 0) return false;
        return actor.Controller.CanAttack(target.controller, space);
    }

    void ReleaseThrust(WeaponSkill skill, HeroCombatState actor, EnvironmentManager space)
    {
        var origin = space.Position(actor.Controller.heroRect);
        var targets = new List<ActiveMonsterInfo>(activeMonsters);
        int battle = battleVersion;
        impactRelease = ++HeroReleaseCount;
        heroSfx?.TryPlay(actor, AttackMode.Melee);
        if (effects != null)
        {
            var start = BodyPosition(actor.Controller.heroRect, space);
            var end = start - new Vector2(skill.Width, 0);
            magicFlights.Add(new MagicFlight { start = start, end = end, duration = .16f,
                visual = effects.AddBolt(start, false, 5f * Mathf.Max(.1f, effectsScale)),
                stepTick = projectileTick, silentEnd = true });
        }
        foreach (var target in targets)
        {
            if (battle != battleVersion || !actor.CanFight) break;
            if (!activeMonsters.Contains(target) || !CanTargetSkill(skill, actor, target, space, origin)) continue;
            Impact(BodyPosition(target.controller.Rect, space), false);
            DealDamageToMonsterFromHero(target, SynergyMath.EvaluateBasicDamage(actor.Data, target.data,
                AttackMode.Melee, Random.Range(.85f, 1f)), actor, origin.x, AttackMode.Melee);
        }
    }

    void ReleaseVolleyArrow(WeaponSkill skill, ActiveMonsterInfo target, HeroCombatState actor,
        EnvironmentManager space, int remaining)
    {
        if (target == null || remaining <= 0) return;
        impactRelease = ++HeroReleaseCount;
        VolleyProjectiles++;
        LaunchProjectile(target, true, AttackMode.RangedPhysical,
            SynergyMath.EvaluateBasicDamage(actor.Data, target.data, AttackMode.RangedPhysical,
                Random.Range(.85f, 1f)), skill.Cooldown, space, actor, skill, remaining - 1);
    }

    static void BindProjectileTarget(Projectile shot, ActiveMonsterInfo target)
    {
        shot.monster = target;
        shot.monsterLife = target.lifeId;
        shot.monsterSpawn = target.controller.SpawnVersion;
    }
}
