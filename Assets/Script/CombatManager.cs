using System.Collections.Generic;
using UnityEngine;
using TuTienCore;

// EnvironmentManager moves actors first; damage uses their final positions this frame.
[DefaultExecutionOrder(100)]
public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }
    [Header("HIỆU ỨNG ĐÒN ĐÁNH")]
    [Min(0.01f)] public float physicalFlightTime = 0.18f;
    [Min(0.01f)] public float magicFlightTime = 1.2f;
    [Min(0f)] public float magicArcHeight = 65f;
    [Min(0.1f)] public float effectsScale = 1f;
    [SerializeField] private MonsterSpawner monsterSpawner;
    private BattleEffects effects;
    private class Projectile
    {
        public ActiveMonsterInfo monster;
        public bool fromHero, magic;
        public Vector2 start;
        public float elapsed, duration, arc;
        public int damage;
        public string source;
        public BattleEffects.Bolt visual;
    }
    private readonly List<Projectile> projectiles = new List<Projectile>();
    public int PendingProjectileCount => projectiles.Count;
    private GameManager gameManager;
    private HeroController heroController;
    private EntityDataSO runtimeHeroData;
    private int currentHeroHP, maxHeroHP;
    private bool isBattling;
    private float heroAttackTimer;
    private AttackMode heroWindupMode;
    private ActiveMonsterInfo heroWindupTarget;

    public class ActiveMonsterInfo
    {
        public GameObject go;
        public MonsterController controller;
        public EntityDataSO data;
        public int currentHP;
        public int maxHP;
        public float attackTimer;
    }

    private readonly List<ActiveMonsterInfo> activeMonsters = new List<ActiveMonsterInfo>();

    void Start()
    {
        Instance = this;
        gameManager = GetComponent<GameManager>();
        heroController = Object.FindFirstObjectByType<HeroController>();
        if (monsterSpawner == null) monsterSpawner = Object.FindFirstObjectByType<MonsterSpawner>();
    }

    public void SetupHeroInfo(EntityDataSO hData)
    {
        ClearProjectiles();
        runtimeHeroData = hData;
        maxHeroHP = runtimeHeroData.GetCalculatedHealth();
        currentHeroHP = maxHeroHP;
        ResetHeroAttack();
        if (heroController != null)
        {
            heroController.UpdateHealthBar(currentHeroHP, maxHeroHP);
            heroController.UpdateAtkUI(runtimeHeroData.GetCalculatedDamage());
        }
    }

    public void StartBattle(List<GameObject> monsters, EntityDataSO baseMonsterData)
    {
        ForceClearAllMonsters();
        foreach (var m in monsters)
        {
            if (m == null) continue;
            var info = new ActiveMonsterInfo();
            info.go = m;
            info.controller = m.GetComponent<MonsterController>();
            info.data = Instantiate(baseMonsterData);
            int spread = CombatBalance.LevelSpread(runtimeHeroData.currentLevel);
            info.data.currentLevel = Mathf.Clamp(runtimeHeroData.currentLevel + Random.Range(-spread, spread + 1), 1, 999);
            info.data.baseHealth = CombatBalance.MonsterHealth(info.data.currentLevel);
            info.data.baseDamage = CombatBalance.MonsterAttack(info.data.currentLevel);
            info.data.baseAttackSpeed = CombatBalance.HeroSpeed(info.data.currentLevel);
            info.data.addedHealth = 0;
            info.data.addedDamage = 0;
            info.data.gender = Random.Range(0,2) == 0 ? GenderType.Nam : GenderType.Nu;
            info.data.entityName = NameDatabase.GetRandomName(info.data.gender);
            info.data.race = SynergyMath.GenerateRandomRace();
            SynergyMath.GenerateRootProfile(info.data);
            info.data.entityName = WorldNames.RandomMonsterName(info.data);
            info.maxHP = info.data.GetCalculatedHealth();
            info.currentHP = info.maxHP;
            if (info.controller != null) { info.controller.UpdateHealthBar(info.currentHP, info.maxHP); info.controller.SetIdentityVisual(info.data); }
            activeMonsters.Add(info);
        }
        isBattling = activeMonsters.Count > 0;
    }

    void Update()
    {
        var space = EnvironmentManager.Instance;
        if (effects != null) effects.Advance(Time.deltaTime);
        if (!isBattling || currentHeroHP <= 0 || heroController == null || !heroController.IsDeployed || space == null)
        { if (projectiles.Count > 0) ClearProjectiles(); return; }
        if (effects == null && space.BattleArea != null) effects = BattleEffects.Create(space.BattleArea);
        // Advance existing shots before launching new ones; a new shot must not
        // consume time from before its launch during this frame.
        TickProjectiles(space, Time.deltaTime);
        if (!isBattling) return;
        TickHeroAttack(space, Time.deltaTime);
        if (!isBattling) return;
        for (int i = activeMonsters.Count - 1; i >= 0; i--)
        {
            var info = activeMonsters[i];
            if (info.controller == null || !info.controller.CanAttack(heroController, space) ||
                info.controller.CurrentTarget != heroController)
            {
                info.attackTimer = 0f;
                continue;
            }
            info.attackTimer += Time.deltaTime;
            float duration = AttackDuration(info.controller.attackMode, info.data.baseAttackSpeed);
            if (info.attackTimer < duration) continue;
            info.attackTimer = 0f;
            // Revalidate immediately before damage; there are no suspended coroutines
            // that can wake up after a load/retry or a pooled object is reused.
            if (!info.controller.CanAttack(heroController, space)) continue;
            int finalDmg = SynergyMath.Damage(info.data, runtimeHeroData, info.controller.attackMode, Random.Range(0.85f, 1f));
            info.controller.PlayAttackFeedback();
            if (info.controller.attackMode == AttackMode.Melee)
            {
                Impact(BodyPosition(heroController.heroRect, space), false);
                ApplyIncomingDamage(finalDmg, IdentityDisplay.Describe(info.data));
            }
            else Launch(info, false, info.controller.attackMode, finalDmg, duration, space, 0);
            if (!isBattling) return;
        }
    }

    private void TickHeroAttack(EnvironmentManager space, float dt)
    {
        ActiveMonsterInfo target = null;
        foreach (var info in activeMonsters)
            if (info.controller == heroController.CurrentTarget) { target = info; break; }
        if (target == null || !heroController.CanAttack(target.controller, space))
        {
            ResetHeroAttack();
            return;
        }
        if (heroWindupTarget != target || heroWindupMode != heroController.attackMode)
        {
            ResetHeroAttack();
            heroWindupTarget = target;
            heroWindupMode = heroController.attackMode;
        }
        heroAttackTimer += dt;
        if (heroAttackTimer < AttackDuration(heroWindupMode, runtimeHeroData.baseAttackSpeed)) return;
        heroAttackTimer = 0f;
        heroController.PlayAttackFeedback();
        if (heroWindupMode == AttackMode.RangedMagic)
        {
            // Ground-local horizontal AoE, limited to the hero's actual attack range.
            // One fireball per in-range victim; each resolves its own impact.
            for (int i = activeMonsters.Count - 1; i >= 0; i--)
            {
                var victim = activeMonsters[i];
                if (heroController.CanAttack(victim.controller, space))
                    Launch(victim, true, heroWindupMode, RollHeroDamage(victim.data),
                        AttackDuration(heroWindupMode, runtimeHeroData.baseAttackSpeed), space, i);
            }
        }
        else if (heroController.CanAttack(target.controller, space))
        {
            if (heroWindupMode == AttackMode.Melee)
            {
                Impact(BodyPosition(target.controller.Rect, space), false);
                DealDamageToMonster(target, RollHeroDamage(target.data));
            }
            else Launch(target, true, heroWindupMode, RollHeroDamage(target.data),
                AttackDuration(heroWindupMode, runtimeHeroData.baseAttackSpeed), space, 0);
        }
    }

    private static float AttackDuration(AttackMode mode, float speed)
    {
        return CombatBalance.AttackInterval(mode, speed);
    }

    private void ResetHeroAttack() { heroAttackTimer = 0f; heroWindupTarget = null; }

    private int RollHeroDamage(EntityDataSO defender)
    {
        return SynergyMath.Damage(runtimeHeroData, defender, heroWindupMode, Random.Range(0.85f, 1f));
    }

    private void DealDamageToMonster(ActiveMonsterInfo target, int finalDmg)
    {
        target.currentHP = Mathf.Max(0, target.currentHP - finalDmg);
        if (gameManager != null) gameManager.UpdateEventLog(IdentityDisplay.Describe(target.data) + " nhận " + finalDmg + " sát thương.");
        if (target.controller != null)
        {
            target.controller.UpdateHealthBar(target.currentHP, target.maxHP);
            target.controller.ShowDamage(finalDmg);
        }
        if (target.currentHP == 0) HandleMonsterDeath(target);
    }

    private void HandleMonsterDeath(ActiveMonsterInfo target)
    {
        activeMonsters.Remove(target);
        if (target.controller != null) target.controller.MarkDead();
        int exp = CombatBalance.KillExp(target.data.currentLevel) * (gameManager != null && gameManager.IsHardMode ? 3 : 1);
        runtimeHeroData.AddExp(exp);
        // Raising max HP never fills current HP. The only in-run healing is a
        // small, explicit 5..10 HP reward per kill, capped at the new maximum.
        maxHeroHP = runtimeHeroData.GetCalculatedHealth();
        currentHeroHP = Mathf.Clamp(currentHeroHP + Random.Range(5, 11), 0, maxHeroHP);
        heroController.UpdateHealthBar(currentHeroHP, maxHeroHP);
        heroController.UpdateAtkUI(runtimeHeroData.GetCalculatedDamage());
        if (gameManager != null) gameManager.UpdateEventLog($"Hạ {IdentityDisplay.Describe(target.data)}. Nhận {exp} EXP!");
        monsterSpawner.DespawnMonster(target.go);
        if (target.data != null) Destroy(target.data);
        if (gameManager != null) gameManager.OnMonsterDied(target.go);
        if (activeMonsters.Count == 0) { isBattling = false; ResetHeroAttack(); }
    }

    private void DealDamageToHero(int damage) { ApplyIncomingDamage(damage, "Quái"); }
    private void ApplyIncomingDamage(int damage, string source)
    {
        float mapScale = WorldNames.MonsterDamageScale(runtimeHeroData.mapNumber);
        damage = Mathf.Max(1, Mathf.RoundToInt(damage * mapScale * (gameManager != null && gameManager.IsHardMode ? 2 : 1)));
        currentHeroHP = Mathf.Max(0, currentHeroHP - damage);
        if (gameManager != null) gameManager.UpdateEventLog(source + " gây " + damage + " sát thương cho " + runtimeHeroData.entityName + (currentHeroHP == 0 ? ": đã tử vong." : "."));
        heroController.UpdateHealthBar(currentHeroHP, maxHeroHP);
        heroController.ShowDamage(damage);
        if (currentHeroHP > 0) return;
        heroController.Die();
        ForceClearAllMonsters();
        if (gameManager != null) gameManager.OnHeroDied();
    }

    private Vector2 BodyPosition(RectTransform rect, EnvironmentManager space)
    {
        // Aim at the body rather than the feet pivot; height is Ground-local.
        return space.Position(rect) + new Vector2(0f, 24f * Mathf.Max(0.1f, effectsScale));
    }

    private void Launch(ActiveMonsterInfo monster, bool fromHero, AttackMode mode,
        int damage, float interval, EnvironmentManager space, int spread)
    {
        bool magic = mode == AttackMode.RangedMagic;
        Vector2 start = BodyPosition(fromHero ? heroController.heroRect : monster.controller.Rect, space);
        var shot = new Projectile { monster = monster, fromHero = fromHero, magic = magic,
            start = start, damage = damage, source = IdentityDisplay.Describe(fromHero ? runtimeHeroData : monster.data),
            duration = BattleMotion.FlightDuration(magic ? magicFlightTime : physicalFlightTime, interval),
            arc = magic ? Mathf.Max(0f, magicArcHeight) * (1f + (spread % 3) * 0.22f) : 0f };
        if (effects != null)
        {
            shot.visual = effects.AddBolt(start, magic, (magic ? 5f : 2f) * Mathf.Max(0.1f, effectsScale));
            shot.visual.direction = BodyPosition(fromHero ? monster.controller.Rect : heroController.heroRect, space) - start;
        }
        projectiles.Add(shot);
    }

    private void TickProjectiles(EnvironmentManager space, float dt)
    {
        for (int i = projectiles.Count - 1; i >= 0; i--)
        {
            var shot = projectiles[i];
            // Info identity, not just GameObject identity: pooling cannot redirect
            // an old projectile onto a new life of the same monster.
            if (!activeMonsters.Contains(shot.monster) || shot.monster.controller == null ||
                !shot.monster.controller.IsAlive || !heroController.IsDeployed)
            { RemoveProjectile(i); continue; }
            shot.elapsed += dt;
            float t = shot.duration > 0f ? Mathf.Clamp01(shot.elapsed / shot.duration) : 1f;
            Vector2 end = BodyPosition(shot.fromHero ? shot.monster.controller.Rect : heroController.heroRect, space);
            Vector2 position = new Vector2(shot.start.x + (end.x - shot.start.x) * t,
                shot.start.y + (end.y - shot.start.y) * t + 4f * shot.arc * t * (1f - t));
            if (shot.visual != null)
            {
                shot.visual.direction = position - shot.visual.position;
                shot.visual.position = position;
            }
            if (t < 1f) continue;
            RemoveProjectile(i); // remove BEFORE callbacks can clear the entire battle
            Impact(end, shot.magic);
            if (shot.fromHero) DealDamageToMonster(shot.monster, shot.damage);
            else ApplyIncomingDamage(shot.damage, shot.source);
            if (!isBattling) { ClearProjectiles(false); return; }
        }
    }

    private void Impact(Vector2 position, bool fire)
    {
        if (effects != null) effects.Burst(position, fire, Mathf.Max(0.1f, effectsScale));
    }
    private void RemoveProjectile(int index)
    {
        if (effects != null && projectiles[index].visual != null) effects.RemoveBolt(projectiles[index].visual);
        projectiles.RemoveAt(index);
    }
    private void ClearProjectiles(bool clearParticles = true)
    {
        for (int i = projectiles.Count - 1; i >= 0; i--) RemoveProjectile(i);
        if (clearParticles && effects != null) effects.Clear();
    }
    public void PanEffects(float amount)
    {
        foreach (var shot in projectiles) shot.start.x += amount;
        if (effects != null) effects.Pan(amount);
    }
    void OnDisable() { ClearProjectiles(); ResetHeroAttack(); }
    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (effects != null) Destroy(effects.gameObject);
    }

    public void ForceClearAllMonsters()
    {
        isBattling = false;
        ResetHeroAttack();
        ClearProjectiles();
        foreach (var info in activeMonsters)
        {
            if (info.controller != null) info.controller.MarkDead();
            if (info.go != null) monsterSpawner.DespawnMonster(info.go);
            if (info.data != null) Destroy(info.data);
        }
        activeMonsters.Clear();
    }
}
