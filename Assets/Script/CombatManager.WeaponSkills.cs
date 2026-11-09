using System.Collections.Generic;
using UnityEngine;
using TuTienCore;

public partial class CombatManager
{
    // Both actors use the same windup and queue/cooldown contract.
    // Each adapter validates its own source and target life before advancing.
    internal sealed class SkillCast
    {
        internal WeaponSkill Skill;
        internal string WeaponId;
        internal int Battle, Life, Spawn, TargetLife, TargetSpawn;
        internal ActiveMonsterInfo MonsterTarget;
        internal HeroCombatState HeroTarget;
        internal float Remaining, Direction;
        internal float Progress => Mathf.Clamp01(1f - Remaining / Skill.CastTime);

        internal bool Advance(float dt)
        {
            Remaining = Mathf.Max(0f, Remaining - dt);
            return Remaining == 0f;
        }
    }

    SkillCast weaponCast;
    readonly System.Random staffArcRandom = new System.Random();
    internal long WeaponSkillReleases { get; private set; }
    internal long VolleyProjectiles { get; private set; }
    internal string LastWeaponSkill { get; private set; }
    internal long MonsterSkillReleases { get; private set; }
    internal string CastingWeaponSkill => weaponCast?.Skill.Id;
    internal float CastingWeaponProgress => weaponCast?.Progress ?? 0f;

    void TickCombatResources(HeroCombatState actor, float dt)
    {
        if (actor != primaryHeroState || gameManager == null || !gameManager.IsGameplayHUDVisible ||
            UIManager.Instance != null && UIManager.Instance.CombatResourcesMenuOpen)
            return;
        var progress = actor.Data.combatSkills;
        var tags = new[] { CombatStats.StanceTag(actor.SelectedMode) };
        float max = actor.MaxMana;
        float manaRate = progress.mana < max ? Mathf.Max(0, CombatStats.Get(actor.Data,
            CombatStatId.ManaRegeneration, CombatSkillProgress.ResourceBaseline(CombatStatId.ManaRegeneration), ElementType.Vo, tags)) : 0;
        if (progress.Advance(dt, manaRate, max)) actor.Data.isDirty = true;
    }

    // The existing basic clock is the decision boundary. A rejected skill never
    // consumes the ready basic attack or reserves a second cast slot.
    bool TryBeginWeaponCast(HeroCombatState actor, EnvironmentManager space)
    {
        if (actor != primaryHeroState || !HeroIsFighting(actor)) return false;
        var progress = actor.Data.combatSkills;
        if (progress.queue.Count == 0) return false;
        var weapon = WeaponBasicAttack.Equipped(actor.Data);
        float direction = ThrustDirection(actor, space);
        ActiveMonsterInfo target = null;
        var skill = SelectReadySkill(progress, candidate => candidate.IsAvailable(actor.Data, weapon) &&
            (target = PickSkillTarget(candidate, actor, space, false, direction)) != null);
        if (skill == null) return false;
        weaponCast = new SkillCast {
            Skill = skill, WeaponId = actor.WeaponIdentity, Battle = battleVersion,
            Life = actor.LifeVersion, Spawn = actor.Controller.SpawnVersion, Remaining = skill.CastTime,
            MonsterTarget = target, TargetLife = target.lifeId, TargetSpawn = target.controller.SpawnVersion,
            Direction = direction
        };
        BeginEngagement(actor,target);
        actor.Controller.UpdateChargeBar(true, 0f);
        ShowHeroCast(actor,space,weaponCast);
        heroSfx?.PlayHero(actor, actor.SelectedMode, CombatAudioEvent.Cast);
        return true;
    }

    static WeaponSkill SelectReadySkill(CombatSkillProgress progress, System.Predicate<WeaponSkill> eligible)
    {
        int first = Mathf.Max(0, progress.queue.IndexOf(progress.nextSkillId));
        for (int step = 0; step < progress.queue.Count; step++)
        {
            var skill = WeaponSkillRegistry.Find(progress.queue[(first + step) % progress.queue.Count]);
            if (skill != null && progress.Remaining(skill.Id) == 0f && progress.CanPay(skill) && eligible(skill))
                return skill;
        }
        return null;
    }

    bool HeroIsFighting(HeroCombatState actor) =>
        isBattling && actor.CanFight && actor.Controller.CurrentState == HeroState.Combat &&
        !actor.Controller.IsExplorationMovement;

    static float ThrustDirection(HeroCombatState actor, EnvironmentManager space)
    {
        var target = actor.Controller.CurrentTarget;
        return target != null && space.Position(target.Rect).x > space.Position(actor.Controller.heroRect).x ? 1f : -1f;
    }

    bool TickWeaponCast(HeroCombatState actor, EnvironmentManager space, float dt)
    {
        if (actor != primaryHeroState || weaponCast == null) return false;
        var cast = weaponCast;
        var skill = cast.Skill;
        var progress = actor.Data.combatSkills;
        var locked = cast.MonsterTarget;
        if (!HeroIsFighting(actor) || cast.Battle != battleVersion || cast.Life != actor.LifeVersion ||
            cast.Spawn != actor.Controller.SpawnVersion || cast.WeaponId != actor.WeaponIdentity ||
            !progress.queue.Contains(skill.Id) || !skill.IsAvailable(actor.Data, WeaponBasicAttack.Equipped(actor.Data)) ||
            !progress.CanPay(skill) || locked == null || !activeMonsters.Contains(locked) ||
            locked.lifeId != cast.TargetLife || locked.controller == null || locked.controller.SpawnVersion != cast.TargetSpawn ||
            !CanTargetSkill(skill, actor, locked, space, SkillTargetOrigin(skill,actor,space), cast.Direction))
        {
            CancelWeaponCast();
            return false;
        }
        if (!cast.Advance(dt))
        {
            actor.Controller.UpdateChargeBar(true, cast.Progress);
            ShowHeroCast(actor,space,cast);
            return true;
        }
        CancelWeaponCast();
        var target = skill.Effect == SkillEffect.ArrowVolley ? PickSkillTarget(skill, actor, space, true) : locked;
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
                ReleaseThrust(skill, actor, space, cast.Direction);
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
        var controller = primaryHeroState.Controller;
        if (controller == null) return;
        effects?.EndCast(controller.heroRect);
        heroSfx?.StopCast(controller.heroRect);
        controller.UpdateChargeBar(false, 0);
    }

    ActiveMonsterInfo PickSkillTarget(WeaponSkill skill, HeroCombatState actor,
        EnvironmentManager space, bool random, float direction = -1f)
    {
        ActiveMonsterInfo chosen = null;
        int seen = 0;
        float nearest = float.MaxValue;
        var origin = SkillTargetOrigin(skill,actor,space);
        foreach (var candidate in activeMonsters)
        {
            if (!CanTargetSkill(skill, actor, candidate, space, origin, direction)) continue;
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
        EnvironmentManager space, Vector2 origin, float direction)
    {
        if (target == null || target.currentHP <= 0 || target.controller == null || !target.controller.IsAlive)
            return false;
        if (skill.Targeting == SkillTargeting.ForwardRectangle)
        {
            var delta = ThrustBodyAnchor(target.controller.Rect,space) - origin;
            return delta.x * direction >= 0 && delta.x * direction <= skill.Width && Mathf.Abs(delta.y) <= skill.Height / 2 &&
                (target != gateTarget || actor.Controller.CanAttack(target.controller, space));
        }
        if (skill.Targeting == SkillTargeting.RandomForwardInRange && space.Position(target.controller.Rect).x > origin.x)
            return false;
        return actor.Controller.CanAttack(target.controller, space);
    }

    void ReleaseThrust(WeaponSkill skill, HeroCombatState actor, EnvironmentManager space, float direction)
    {
        var origin = SkillTargetOrigin(skill,actor,space);
        var targets = new List<ActiveMonsterInfo>(activeMonsters);
        int battle = battleVersion;
        impactRelease = ++HeroReleaseCount;
        heroSfx?.PlayHero(actor, AttackMode.Melee, CombatAudioEvent.Release);
        effects?.Thrust(origin, direction, skill.Width, skill.Height);
        foreach (var target in targets)
        {
            if (battle != battleVersion || !actor.CanFight) break;
            if (!activeMonsters.Contains(target) || !CanTargetSkill(skill, actor, target, space, origin, direction)) continue;
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

    // The thrust runs through the upper body of both actors. The same
    // fixed BattleArea height on each actor preserves relative lane bounds.
    private Vector2 ThrustBodyAnchor(RectTransform actor, EnvironmentManager space) =>
        BodyPosition(actor,space) + Vector2.up * 42f;

    private Vector2 SkillTargetOrigin(WeaponSkill skill, HeroCombatState actor, EnvironmentManager space) =>
        skill.Targeting == SkillTargeting.ForwardRectangle
            ? ThrustBodyAnchor(actor.Controller.heroRect,space) : space.Position(actor.Controller.heroRect);

    private Vector2 CastVisualAnchor(RectTransform actor, EnvironmentManager space) =>
        BodyPosition(actor,space) + space.ScreenPixelsToBattleUnits(new Vector2(-4f,10f));

    [SerializeField] private float bowCastingLiftPixels = 20f;
    [SerializeField] private float staffCastingLiftPixels = 20f;

    private void ShowHeroCast(HeroCombatState actor, EnvironmentManager space, SkillCast cast)
    {
        var rect = actor.Controller.heroRect;
        var family = WeaponBasicAttack.Family(WeaponBasicAttack.Equipped(actor.Data));
        float lift = family == WeaponFamily.Bow ? bowCastingLiftPixels :
            family == WeaponFamily.Staff ? staffCastingLiftPixels : 0f;
        effects?.SetCast(rect, CastVisualAnchor(rect,space) +
            space.ScreenPixelsToBattleUnits(Vector2.up * lift), cast.Progress);
    }

    void ShowCast(RectTransform actor, EnvironmentManager space, SkillCast cast)
    {
        effects?.SetCast(actor, CastVisualAnchor(actor,space), cast.Progress);
    }

    CombatSkillProgress MonsterSkills(ActiveMonsterInfo actor)
    {
        if (actor.skillProgress != null || actor.controller.SkillIds.Length == 0) return actor.skillProgress;
        var progress = new CombatSkillProgress { mana = actor.controller.SkillMana };
        foreach (string id in actor.controller.SkillIds)
            if (WeaponSkillRegistry.Find(id)?.Effect == SkillEffect.LegacyFireball) progress.Add(id);
        actor.skillProgress = progress;
        return progress;
    }

    bool TryBeginMonsterCast(ActiveMonsterInfo actor, HeroCombatState target, EnvironmentManager space)
    {
        var progress = actor.skillProgress;
        if (progress == null || target == null || actor.controller.attackMode != AttackMode.RangedMagic) return false;
        var skill = SelectReadySkill(progress, candidate => MonsterHasSkill(actor, candidate));
        if (skill == null) return false;
        actor.skillCast = new SkillCast {
            Skill = skill, Battle = battleVersion, Life = actor.lifeId, Spawn = actor.controller.SpawnVersion,
            HeroTarget = target, TargetLife = target.LifeVersion, TargetSpawn = target.Controller.SpawnVersion,
            Remaining = skill.CastTime
        };
        actor.controller.UpdateChargeBar(true, 0);
        ShowCast(actor.controller.Rect, space, actor.skillCast);
        heroSfx?.PlayMonster(actor.controller, actor.controller.attackMode, CombatAudioEvent.Cast);
        return true;
    }

    static bool MonsterHasSkill(ActiveMonsterInfo actor, WeaponSkill skill) =>
        skill.Effect == SkillEffect.LegacyFireball && System.Array.IndexOf(actor.controller.SkillIds, skill.Id) >= 0;

    bool TickMonsterCast(ActiveMonsterInfo actor, EnvironmentManager space, float dt)
    {
        var cast = actor.skillCast;
        if (cast == null) return false;
        var target = cast.HeroTarget;
        if (cast.Battle != battleVersion || cast.Life != actor.lifeId || cast.Spawn != actor.controller.SpawnVersion ||
            actor.controller.attackMode != AttackMode.RangedMagic || !actor.controller.IsAggressive ||
            !IsRegistered(target) || !target.CanFight || target.LifeVersion != cast.TargetLife ||
            target.Controller.SpawnVersion != cast.TargetSpawn ||
            !actor.controller.CanAttack(target.Controller, space) || !MonsterHasSkill(actor,cast.Skill) ||
            !actor.skillProgress.CanPay(cast.Skill))
        {
            CancelMonsterCast(actor);
            return false;
        }
        if (!cast.Advance(dt))
        {
            actor.controller.UpdateChargeBar(true, cast.Progress);
            ShowCast(actor.controller.Rect, space, cast);
            return true;
        }
        CancelMonsterCast(actor);
        actor.skillProgress.RecordRelease(cast.Skill);
        MonsterSkillReleases++;
        actor.controller.PlayAttackFeedback();
        LaunchProjectile(actor, false, AttackMode.RangedMagic,
            RollDamageTrace(actor.data, target.Data, AttackMode.RangedMagic),
            cast.Skill.Cooldown, space, target, cast.Skill);
        return true;
    }

    void CancelMonsterCast(ActiveMonsterInfo actor)
    {
        if (actor.skillCast == null) return;
        actor.skillCast = null;
        if (actor.controller == null) return;
        effects?.EndCast(actor.controller.Rect);
        heroSfx?.StopCast(actor.controller.Rect);
        actor.controller.UpdateChargeBar(false, 0);
    }

    internal void ForgetMonsterCast(MonsterController controller)
    {
        foreach (var actor in activeMonsters)
            if (actor.controller == controller) { CancelMonsterCast(actor); return; }
    }

    static void BindProjectileTarget(Projectile shot, ActiveMonsterInfo target)
    {
        shot.monster = target;
        shot.monsterLife = target.lifeId;
        shot.monsterSpawn = target.controller.SpawnVersion;
    }
}
