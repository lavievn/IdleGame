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
    [Min(0f)] public float physicalHitRadius = 12f;
    [Min(0f)] public float magicImpactRadius = 100f; // Enemy radius unchanged.
    [Min(0f)] public float heroMagicImpactRadius = 250f; // Ground-local hero explosion.
    [Min(0.1f)] public float magicProjectileScale = 4f;
    [SerializeField] private MonsterSpawner monsterSpawner;
    private BattleEffects effects;
    private class Projectile
    {
        public ActiveMonsterInfo monster;
        public bool fromHero, magic;
        public Vector2 start, end;
        public float elapsed, duration, arc;
        public string source;
        public DamageTrace trace;
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

    public class ActiveMonsterInfo
    {
        public GameObject go;
        public MonsterController controller;
        public EntityDataSO data;
        public int currentHP;
        public int maxHP;
        public float attackTimer;
        public AttackMode cycleMode;
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
        if (CombatBalance.HeroDev != null && heroController != null) heroController.moveSpeed = CombatBalance.HeroDev.movementSpeed;
        maxHeroHP = runtimeHeroData.GetCalculatedHealth(heroController != null ? heroController.attackMode : AttackMode.Melee);
        currentHeroHP = maxHeroHP;
        ResetHeroAttack();
        if (heroController != null)
        {
            heroController.UpdateHealthBar(currentHeroHP, maxHeroHP);
            heroController.UpdateStats(runtimeHeroData, currentHeroHP, maxHeroHP);
        }
    }

    public void StartBattle(List<GameObject> monsters, EntityDataSO baseMonsterData)
    {
        ClearBattle(true);
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
            info.data.baseAttackSpeed = CombatBalance.MonsterSpeed(info.data.currentLevel);
            info.data.addedHealth = 0;
            info.data.addedDamage = 0;
            info.data.gender = Random.Range(0,2) == 0 ? GenderType.Nam : GenderType.Nu;
            info.data.entityName = NameDatabase.GetRandomName(info.data.gender);
            info.data.race = SynergyMath.GenerateMonsterRace(runtimeHeroData.mapTerrain);
            SynergyMath.GenerateRootProfile(info.data);
            info.data.entityName = WorldNames.RandomMonsterName(info.data, runtimeHeroData.mapTerrain);
            info.maxHP = info.data.GetCalculatedHealth();
            info.currentHP = info.maxHP;
            if (info.controller != null) { info.controller.UpdateHealthBar(info.currentHP, info.maxHP); info.controller.SetIdentityVisual(info.data); }
            if (info.controller != null && CombatBalance.MonsterDev != null) info.controller.moveSpeed = CombatBalance.MonsterDev.movementSpeed;
            info.cycleMode = info.controller != null ? info.controller.attackMode : AttackMode.Melee;
            activeMonsters.Add(info);
        }
        isBattling = activeMonsters.Count > 0;
    }

    public void ApplyDevBalance(bool monster)
    {
        // Changing balance cancels in-flight old power/cooldowns, not the wave or identity.
        ClearProjectiles(); ResetHeroAttack();
        if (!monster && runtimeHeroData != null)
        {
            float healthRatio = maxHeroHP > 0 ? (float)currentHeroHP / maxHeroHP : 1f;
            runtimeHeroData.ApplyHeroBalance(); maxHeroHP = runtimeHeroData.GetCalculatedHealth(heroController != null ? heroController.attackMode : AttackMode.Melee);
            currentHeroHP = currentHeroHP <= 0 ? 0 : Mathf.Clamp(Mathf.RoundToInt(healthRatio * maxHeroHP),1,maxHeroHP);
            if (heroController != null) {
                if (CombatBalance.HeroDev != null) heroController.moveSpeed = CombatBalance.HeroDev.movementSpeed;
                heroController.UpdateHealthBar(currentHeroHP,maxHeroHP); heroController.UpdateStats(runtimeHeroData,currentHeroHP,maxHeroHP);
            }
        }
        foreach (var info in activeMonsters)
        {
            info.attackTimer = 0f;
            if (info.controller != null) info.controller.UpdateChargeBar(false,0);
            if (!monster) continue;
            float healthRatio = info.maxHP > 0 ? (float)info.currentHP / info.maxHP : 1f;
            info.data.baseDamage = CombatBalance.MonsterAttack(info.data.currentLevel);
            info.data.baseHealth = CombatBalance.MonsterHealth(info.data.currentLevel);
            info.data.baseAttackSpeed = CombatBalance.MonsterSpeed(info.data.currentLevel);
            info.maxHP = info.data.GetCalculatedHealth();
            info.currentHP = info.currentHP <= 0 ? 0 : Mathf.Clamp(Mathf.RoundToInt(healthRatio * info.maxHP),1,info.maxHP);
            if (info.controller != null) {
                if (CombatBalance.MonsterDev != null) info.controller.moveSpeed = CombatBalance.MonsterDev.movementSpeed;
                info.controller.UpdateHealthBar(info.currentHP,info.maxHP);
            }
        }
    }

    public int CurrentHeroHP => currentHeroHP;
    public int MaxHeroHP => maxHeroHP;
    public void OnHeroAttackModeChanged()
    {
        if (runtimeHeroData == null || heroController == null) return;
        float ratio = maxHeroHP > 0 ? (float)currentHeroHP/maxHeroHP : 1f;
        maxHeroHP = runtimeHeroData.GetCalculatedHealth(heroController.attackMode);
        currentHeroHP = currentHeroHP <= 0 ? 0 : Mathf.Clamp(Mathf.RoundToInt(ratio*maxHeroHP),1,maxHeroHP);
        ResetHeroAttack();
        heroController.UpdateHealthBar(currentHeroHP,maxHeroHP);
        heroController.UpdateStats(runtimeHeroData,currentHeroHP,maxHeroHP);
    }
    void LateUpdate()
    {
        if (runtimeHeroData != null && heroController != null) heroController.UpdateStats(runtimeHeroData, currentHeroHP, maxHeroHP);
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
        var space = EnvironmentManager.Instance;
        float dt = Time.deltaTime;
        if (effects != null) effects.Advance(dt);
        if (currentHeroHP <= 0 || heroController == null || !heroController.IsDeployed || space == null)
        { if (projectiles.Count > 0) ClearProjectiles(); return; }
        if (effects == null && space.BattleArea != null) effects = BattleEffects.Create(space.BattleArea);
        // Existing shots advance first; new shots cannot consume pre-launch frame time.
        TickProjectiles(space, dt);
        if (currentHeroHP <= 0 || !heroController.IsDeployed) return;
        TickHeroAttack(space, dt); // Charge/cooldown also advances between waves.
        if (!isBattling) return;
        for (int i = activeMonsters.Count - 1; i >= 0; i--)
        {
            var info = activeMonsters[i];
            if (info.controller == null || !info.controller.IsAlive) continue;
            AttackMode mode = info.controller.attackMode;
            if (info.cycleMode != mode) { info.cycleMode = mode; info.attackTimer = 0f; }
            float duration = AttackDuration(mode, info.data.baseAttackSpeed, true);
            bool eligible = info.controller.CanAttack(heroController, space);
            if (mode == AttackMode.RangedMagic && !eligible)
            {
                info.attackTimer = 0f;
                info.controller.UpdateChargeBar(false,0f);
                continue;
            }
            bool ready = AdvanceCycle(ref info.attackTimer, mode, duration, dt);
            info.controller.UpdateChargeBar(mode == AttackMode.RangedMagic, info.attackTimer / duration);
            if (!ready || !eligible) continue;
            info.attackTimer = mode == AttackMode.RangedMagic ? 0f : duration;
            info.controller.UpdateChargeBar(mode == AttackMode.RangedMagic, 0f);
            var trace = RollDamageTrace(info.data, runtimeHeroData, mode);
            info.controller.PlayAttackFeedback();
            if (mode == AttackMode.Melee)
            {
                Impact(BodyPosition(heroController.heroRect, space), false);
                ApplyIncomingDamage(trace.elementDamage, IdentityDisplay.Describe(info.data), trace);
            }
            else Launch(info, false, mode, trace, duration, space);
            if (!isBattling) return;
        }
    }

    // Magic counts up before every cast; other attacks count down after a strike.
    private static bool AdvanceCycle(ref float timer, AttackMode mode, float duration, float dt)
    {
        timer = mode == AttackMode.RangedMagic ? Mathf.Min(duration, timer + dt) : Mathf.Max(0f, timer - dt);
        return mode == AttackMode.RangedMagic ? timer >= duration : timer <= 0f;
    }

    private ActiveMonsterInfo NearestEligibleMonster(EnvironmentManager space)
    {
        ActiveMonsterInfo target = null; float nearest = float.MaxValue;
        foreach (var info in activeMonsters)
        {
            if (info.controller == null || !heroController.CanAttack(info.controller,space)) continue;
            float distance = Mathf.Abs(space.Position(info.controller.Rect).x-space.Position(heroController.heroRect).x);
            if (distance < nearest) { nearest = distance; target = info; }
        }
        return target;
    }
    private void TickHeroAttack(EnvironmentManager space, float dt)
    {
        AttackMode mode = heroController.attackMode;
        if (heroWindupMode != mode) { ResetHeroAttack(); heroWindupMode = mode; }
        float duration = AttackDuration(mode,runtimeHeroData.baseAttackSpeed);
        ActiveMonsterInfo target = NearestEligibleMonster(space);
        if (mode == AttackMode.RangedMagic && (!isBattling || target == null))
        {
            heroAttackTimer = 0f;
            heroController.UpdateChargeBar(false,0f);
            return;
        }
        bool ready = AdvanceCycle(ref heroAttackTimer,mode,duration,dt);
        heroController.UpdateChargeBar(mode == AttackMode.RangedMagic,heroAttackTimer/duration);
        if (!ready || !isBattling || target == null) return;
        heroAttackTimer = mode == AttackMode.RangedMagic ? 0f : duration;
        heroController.UpdateChargeBar(mode == AttackMode.RangedMagic,0f);
        heroController.PlayAttackFeedback();
        var trace = RollHeroDamageTrace(target.data);
        if (mode == AttackMode.Melee)
        {
            Impact(BodyPosition(target.controller.Rect,space),false);
            DealDamageToMonsterWithTrace(target,trace);
        }
        else Launch(target,true,mode,trace,duration,space);
    }

    private static DamageTrace RollDamageTrace(EntityDataSO attacker, EntityDataSO defender, AttackMode mode)
    {
        float physicalFactor = mode == AttackMode.RangedPhysical ? .7f : 1f;
        return SynergyMath.EvaluateDamage(attacker, defender, mode, Random.Range(.85f, 1f), physicalFactor);
    }

    private static float AttackDuration(AttackMode mode, float speed, bool monster = false)
    {
        return CombatBalance.AttackInterval(mode, speed, (monster ? CombatBalance.MonsterDev : CombatBalance.HeroDev) != null, monster);
    }

    private void ResetHeroAttack()
    {
        heroAttackTimer = 0f;
        if (heroController != null) heroController.UpdateChargeBar(false, 0f);
    }

    private DamageTrace RollHeroDamageTrace(EntityDataSO defender)
    {
        return RollDamageTrace(runtimeHeroData, defender, heroWindupMode);
    }

    private void DealDamageToMonsterWithTrace(ActiveMonsterInfo target, DamageTrace trace)
    {
        if (gameManager != null) gameManager.RecordDamage(trace.Describe(1f, 1, trace.elementDamage));
        DealDamageToMonster(target, trace.elementDamage);
    }
    private void DealDamageToMonster(ActiveMonsterInfo target, int finalDmg)
    {
        target.currentHP = Mathf.Max(0, target.currentHP - finalDmg);
        if (gameManager != null)
        {
            gameManager.RecordDamageEvent(runtimeHeroData!=null?runtimeHeroData.entityName:"Hero",target.data!=null?target.data.entityName:"Quái",finalDmg);
            gameManager.UpdateEventLog(IdentityDisplay.Describe(target.data) + " nhận " + finalDmg + " sát thương.");
        }
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
        // Levelling increases max HP without filling the newly gained capacity.
        maxHeroHP = runtimeHeroData.GetCalculatedHealth(heroController != null ? heroController.attackMode : AttackMode.Melee);
        int heal = heroController != null && heroController.attackMode == AttackMode.Melee
            ? CombatBalance.MeleeKillHeal(runtimeHeroData.currentLevel,Random.Range(3,6)) : 0;
        currentHeroHP = Mathf.Clamp(currentHeroHP + heal,0,maxHeroHP);
        heroController.UpdateHealthBar(currentHeroHP, maxHeroHP);
        heroController.UpdateStats(runtimeHeroData, currentHeroHP, maxHeroHP);
        if (gameManager != null) gameManager.UpdateEventLog($"Hạ {IdentityDisplay.Describe(target.data)}. Nhận {exp} EXP!");
        monsterSpawner.DespawnMonster(target.go);
        if (target.data != null) Destroy(target.data);
        if (gameManager != null) gameManager.OnMonsterDied(target.go);
        if (activeMonsters.Count == 0) isBattling = false;
    }

    private void DealDamageToHero(int damage) { ApplyIncomingDamage(damage, "Quái"); }
    private void ApplyIncomingDamage(int damage, string source, DamageTrace trace = null)
    {
        float mapScale = WorldNames.MonsterDamageScale(runtimeHeroData.MapVisits);
        int hardScale = gameManager != null && gameManager.IsHardMode ? 2 : 1;
        damage = Mathf.Max(1, Mathf.RoundToInt(damage * mapScale * hardScale));
        if (gameManager != null && trace != null) gameManager.RecordDamage(trace.Describe(mapScale, hardScale, damage));
        currentHeroHP = Mathf.Max(0, currentHeroHP - damage);
        if (gameManager != null)
        {
            gameManager.RecordDamageEvent(source,runtimeHeroData!=null?runtimeHeroData.entityName:"Hero",damage);
            gameManager.UpdateEventLog(source + " gây " + damage + " sát thương cho " + runtimeHeroData.entityName + (currentHeroHP == 0 ? ": đã tử vong." : "."));
        }
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
        DamageTrace trace, float interval, EnvironmentManager space)
    {
        bool magic = mode == AttackMode.RangedMagic;
        Vector2 start = BodyPosition(fromHero ? heroController.heroRect : monster.controller.Rect, space);
        var shot = new Projectile { monster = monster, fromHero = fromHero, magic = magic,
            start = start, end = BodyPosition(fromHero ? monster.controller.Rect : heroController.heroRect, space), trace = trace, source = IdentityDisplay.Describe(fromHero ? runtimeHeroData : monster.data),
            duration = BattleMotion.FlightDuration(magic ? magicFlightTime : physicalFlightTime, interval),
            arc = magic ? Mathf.Max(0f, magicArcHeight) : 0f };
        if (effects != null)
        {
            shot.visual = effects.AddBolt(start, magic, (magic ? 5f * Mathf.Max(.1f, magicProjectileScale) : 2f) * Mathf.Max(0.1f, effectsScale));
            shot.visual.direction = shot.end - start;
        }
        projectiles.Add(shot);
    }

    private static bool WithinImpact(Vector2 a, Vector2 b, float radius)
    {
        float x = a.x - b.x, y = a.y - b.y;
        radius = Mathf.Max(0f, radius);
        return x * x + y * y <= radius * radius + .01f;
    }

    private void TickProjectiles(EnvironmentManager space, float dt)
    {
        for (int i = projectiles.Count - 1; i >= 0; i--)
        {
            var shot = projectiles[i];
            // Physical targets and enemy shooters use life identity, never pooled object identity.
            // Hero magic continues to its fixed endpoint even if its original target dies.
            if (!heroController.IsDeployed || ((!shot.fromHero || !shot.magic) &&
                (!activeMonsters.Contains(shot.monster) || shot.monster.controller == null || !shot.monster.controller.IsAlive)))
            { RemoveProjectile(i); continue; }
            shot.elapsed += dt;
            float t = shot.duration > 0f ? Mathf.Clamp01(shot.elapsed / shot.duration) : 1f;
            Vector2 end = shot.end;
            Vector2 position = new Vector2(shot.start.x + (end.x - shot.start.x) * t,
                shot.start.y + (end.y - shot.start.y) * t + 4f * shot.arc * t * (1f - t));
            if (shot.visual != null)
            {
                shot.visual.direction = position - shot.visual.position;
                shot.visual.position = position;
            }
            if (t < 1f) continue;
            RemoveProjectile(i); // Callbacks may clear the entire battle.
            Impact(end, shot.magic);
            bool hit = false;
            if (shot.fromHero && shot.magic)
            {
                // Snapshot the victims: death callbacks can modify the live list or spawn a wave.
                var victims = new List<ActiveMonsterInfo>(activeMonsters);
                foreach (var victim in victims)
                {
                    if (!activeMonsters.Contains(victim) || victim.controller == null || !victim.controller.IsAlive ||
                        !WithinImpact(BodyPosition(victim.controller.Rect, space), end, heroMagicImpactRadius)) continue;
                    hit = true;
                    DealDamageToMonsterWithTrace(victim, SynergyMath.EvaluateSnapshot(shot.trace, victim.data));
                }
            }
            else if (shot.fromHero)
            {
                hit = WithinImpact(BodyPosition(shot.monster.controller.Rect, space), end, physicalHitRadius);
                if (hit) DealDamageToMonsterWithTrace(shot.monster, SynergyMath.EvaluateSnapshot(shot.trace, shot.monster.data));
            }
            else
            {
                hit = WithinImpact(BodyPosition(heroController.heroRect, space), end, shot.magic ? magicImpactRadius : physicalHitRadius);
                if (hit) { var trace = SynergyMath.EvaluateSnapshot(shot.trace, runtimeHeroData); ApplyIncomingDamage(trace.elementDamage, shot.source, trace); }
            }
            if (!hit && gameManager != null)
            {
                string miss = shot.source + (shot.magic ? ": vùng nổ không có mục tiêu." : ": đòn cung hụt, sát thương 0.");
                gameManager.UpdateEventLog(miss); gameManager.RecordDamage(miss);
            }
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
        foreach (var shot in projectiles) { shot.start.x += amount; shot.end.x += amount; }
        if (effects != null) effects.Pan(amount);
    }
    void OnDisable() { ClearProjectiles(); ResetHeroAttack(); }
    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (effects != null) Destroy(effects.gameObject);
    }

    public void ForceClearAllMonsters() { ClearBattle(false); }
    private void ClearBattle(bool preserveHeroAttack)
    {
        isBattling = false;
        if (!preserveHeroAttack) ResetHeroAttack();
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
