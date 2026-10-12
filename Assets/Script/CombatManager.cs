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
    [Header("0.0.51 - GIAP / KHANG PHEP / KHANG HE")]
    public CombatResistanceSettings resistanceSettings = new CombatResistanceSettings();
    [SerializeField] private MonsterSpawner monsterSpawner;
    private BattleEffects effects;
    private class Projectile
    {
        public ActiveMonsterInfo monster;
        public HeroCombatState hero; // Shooter if fromHero, target if monster-fired.
        public int heroLife, heroSpawn;
        public int monsterLife, monsterSpawn;
        public bool fromHero, magic;
        public Vector2 start, end;
        public float elapsed, duration, arc;
        public string source;
        public DamageTrace trace;
        public AttackMode attackMode;
        public BattleEffects.Bolt visual;
    }
    private readonly List<Projectile> projectiles = new List<Projectile>();
    public int PendingProjectileCount => projectiles.Count;
    private GameManager gameManager;
    // B1: ONE authoritative object owns this Hero's transient HP/data/timer.
    // These forwarding properties preserve the .54g combat call sites while
    // the next phase (B2) prepares target/damage routing for several Heroes.
    private readonly HeroCombatState primaryHeroState = new HeroCombatState();
    public HeroCombatState CurrentHeroCombatState => primaryHeroState;
    // Explicit B2 runtime registry. GameManager still owns ONE saved primary Hero.
    private readonly Dictionary<HeroController, HeroCombatState> heroStates =
        new Dictionary<HeroController, HeroCombatState>();
    public int RegisteredHeroCount => heroStates.Count;
    public bool HasLivingHero => AnyFightingHero();
    public float ActiveHeroMovementSpeed
    {
        get
        {
            if (primaryHeroState.CanFight) return primaryHeroState.Controller.moveSpeed;
            foreach (var state in heroStates.Values)
                if (state.CanFight) return state.Controller.moveSpeed;
            return 150f;
        }
    }
    public HeroCombatState StateFor(HeroController controller)
    {
        HeroCombatState state;
        return controller != null && heroStates.TryGetValue(controller, out state)
            ? state : null;
    }
    // Companion prototype API: does not spawn a Hero or persist a new save slot.
    public HeroCombatState RegisterAdditionalHero(HeroController controller, EntityDataSO data)
    {
        if (controller == null || data == null || controller == heroController)
            return null;
        HeroCombatState state;
        if (!heroStates.TryGetValue(controller, out state))
        {
            state = new HeroCombatState { Controller = controller };
            heroStates.Add(controller, state);
        }
        else
            state.Controller = controller;
        state.BeginLife(data, data.GetCalculatedHealth(controller.attackMode));
        controller.UpdateHealthBar(state.CurrentHP, state.MaxHP);
        controller.UpdateStats(data, state.CurrentHP, state.MaxHP);
        controller.UpdateChargeBar(false, 0f);
        return state;
    }
    public bool UnregisterAdditionalHero(HeroController controller)
    {
        if (controller == null || controller == heroController) return false;
        HeroCombatState state;
        if (!heroStates.TryGetValue(controller, out state)) return false;
        heroStates.Remove(controller);
        state.ResetAttack();
        // An unregistered but deployed Hero would still be targeted by
        // MonsterController and counted by the camera. Hide it as well.
        controller.HideHero();
        return true;
    }
    private bool IsRegistered(HeroCombatState state)
    {
        return state != null && state.Controller != null &&
            StateFor(state.Controller) == state;
    }
    private bool AnyFightingHero()
    {
        foreach (var state in heroStates.Values)
            if (state.CanFight) return true;
        return false;
    }
    private void ResetAllHeroAttacks()
    {
        foreach (var state in heroStates.Values) ResetRegisteredAttack(state);
    }
    private HeroController heroController
    {
        get { return primaryHeroState.Controller; }
        set { primaryHeroState.Controller = value; }
    }
    private EntityDataSO runtimeHeroData => primaryHeroState.Data;
    private int currentHeroHP
    {
        get { return primaryHeroState.CurrentHP; }
        set { primaryHeroState.CurrentHP = value; }
    }
    private int maxHeroHP
    {
        get { return primaryHeroState.MaxHP; }
        set { primaryHeroState.MaxHP = value; }
    }
    private float heroAttackTimer
    {
        get { return primaryHeroState.AttackTimer; }
        set { primaryHeroState.AttackTimer = value; }
    }
    private AttackMode heroWindupMode
    {
        get { return primaryHeroState.WindupMode; }
        set { primaryHeroState.WindupMode = value; }
    }
    private bool isBattling;

    public class ActiveMonsterInfo
    {
        public GameObject go;
        public MonsterController controller;
        public EntityDataSO data;
        public int currentHP;
        public int maxHP;
        public float attackTimer;
        public AttackMode cycleMode;
        public int lifeId;
    }
    private int monsterLifeSequence;

    private readonly List<ActiveMonsterInfo> activeMonsters = new List<ActiveMonsterInfo>();

    void Start()
    {
        Instance = this;
        gameManager = GetComponent<GameManager>();
        heroController = Object.FindFirstObjectByType<HeroController>();
        if (heroController != null) heroStates[heroController] = primaryHeroState;
        if (monsterSpawner == null) monsterSpawner = Object.FindFirstObjectByType<MonsterSpawner>();
    }

    public void SetupHeroInfo(EntityDataSO hData)
    {
        ClearProjectiles();
        if (CombatBalance.HeroDev != null && heroController != null) heroController.moveSpeed = CombatBalance.HeroDev.movementSpeed;
        int initialMaxHP = hData.GetCalculatedHealth(heroController != null ? heroController.attackMode : AttackMode.Melee);
        // Retry / new game begins with only the saved primary Hero.
        // Other runtime combatants must be registered for the new encounter.
        foreach (var oldState in heroStates.Values)
            if (oldState != primaryHeroState && oldState.Controller != null)
                oldState.Controller.HideHero();
        heroStates.Clear();
        primaryHeroState.BeginLife(hData, initialMaxHP);
        if (heroController != null) heroStates[heroController] = primaryHeroState;
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
            info.lifeId = ++monsterLifeSequence;
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
        ClearProjectiles(); ResetAllHeroAttacks();
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
        if (!monster)
        {
            foreach (var state in heroStates.Values)
            {
                if (state == primaryHeroState || state.Data == null) continue;
                float ratio = state.MaxHP > 0 ? (float)state.CurrentHP / state.MaxHP : 1f;
                state.Data.ApplyHeroBalance();
                state.MaxHP = state.Data.GetCalculatedHealth(state.SelectedMode);
                state.CurrentHP = state.CurrentHP <= 0 ? 0 :
                    Mathf.Clamp(Mathf.RoundToInt(ratio * state.MaxHP),1,state.MaxHP);
                if (CombatBalance.HeroDev != null && state.Controller != null)
                    state.Controller.moveSpeed = CombatBalance.HeroDev.movementSpeed;
                if (state.Controller != null)
                {
                    state.Controller.UpdateHealthBar(state.CurrentHP,state.MaxHP);
                    state.Controller.UpdateStats(state.Data,state.CurrentHP,state.MaxHP);
                }
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
        OnRegisteredHeroModeChanged(heroController);
    }
    public void OnRegisteredHeroModeChanged(HeroController controller)
    {
        var state = StateFor(controller);
        if (state == null || state.Data == null || state.Controller == null) return;
        float ratio = state.MaxHP > 0 ? (float)state.CurrentHP/state.MaxHP : 1f;
        state.MaxHP = state.Data.GetCalculatedHealth(controller.attackMode);
        state.CurrentHP = state.CurrentHP <= 0 ? 0 :
            Mathf.Clamp(Mathf.RoundToInt(ratio*state.MaxHP),1,state.MaxHP);
        ResetRegisteredAttack(state);
        controller.UpdateHealthBar(state.CurrentHP,state.MaxHP);
        controller.UpdateStats(state.Data,state.CurrentHP,state.MaxHP);
    }
    void LateUpdate()
    {
        foreach (var state in heroStates.Values)
            if (state.Data != null && state.Controller != null)
                state.Controller.UpdateStats(state.Data,state.CurrentHP,state.MaxHP);
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
        var space = EnvironmentManager.Instance;
        float dt = Time.deltaTime;
        if (effects != null) effects.Advance(dt);
        if (space == null || !AnyFightingHero())
        {
            if (projectiles.Count > 0) ClearProjectiles();
            return;
        }
        if (effects == null && space.BattleArea != null) effects = BattleEffects.Create(space.BattleArea);
        // Older projectiles always advance before a new strike.
        TickProjectiles(space,dt);
        if (!AnyFightingHero()) return;
        foreach (var state in heroStates.Values)
            if (state.CanFight) TickRegisteredHeroAttack(state,space,dt);
        if (!isBattling) return;
        for (int i=activeMonsters.Count-1;i>=0;i--)
        {
            if (i>=activeMonsters.Count) continue;
            var info=activeMonsters[i];
            if (info.controller==null || !info.controller.IsAlive) continue;
            AttackMode mode=info.controller.attackMode;
            if (info.cycleMode!=mode) { info.cycleMode=mode; info.attackTimer=0f; }
            float speed=CombatStats.Get(info.data,CombatStatId.AttackSpeed,
                info.data.baseAttackSpeed,ElementType.Vo,new [] { CombatStats.StanceTag(mode) });
            float duration=AttackDuration(mode,speed,true);
            var victim=ResolveMonsterTarget(info,space);
            bool eligible=victim!=null;
            if (mode==AttackMode.RangedMagic && !eligible)
            {
                info.attackTimer=0f;
                info.controller.UpdateChargeBar(false,0f);
                continue;
            }
            bool ready=AdvanceCycle(ref info.attackTimer,mode,duration,dt);
            info.controller.UpdateChargeBar(mode==AttackMode.RangedMagic,info.attackTimer/duration);
            if (!ready || !eligible) continue;
            info.attackTimer=mode==AttackMode.RangedMagic?0f:duration;
            info.controller.UpdateChargeBar(mode==AttackMode.RangedMagic,0f);
            var trace=RollDamageTrace(info.data,victim.Data,mode);
            info.controller.PlayAttackFeedback();
            if (mode==AttackMode.Melee)
            {
                Impact(BodyPosition(victim.Controller.heroRect,space),false);
                ApplyDamageToHeroState(victim,trace.elementDamage,
                    IdentityDisplay.Describe(info.data),trace,
                    BodyPosition(info.controller.Rect,space).x,info.data,mode);
            }
            else Launch(info,false,mode,trace,duration,space,victim);
            if (!isBattling) return;
        }
    }

    // Prefer the target selected by MonsterController's AI. When the target
    // has died or is not registered for combat, select the nearest registered
    // Hero IN RANGE; never silently damage the primary Hero instead.
    private HeroCombatState ResolveMonsterTarget(ActiveMonsterInfo info,EnvironmentManager space)
    {
        var intended=StateFor(info.controller.CurrentTarget);
        if (intended!=null && intended.CanFight &&
            info.controller.CanAttack(intended.Controller,space)) return intended;
        HeroCombatState nearest=null;
        float min=float.MaxValue;
        foreach (var state in heroStates.Values)
        {
            if (!state.CanFight || !info.controller.CanAttack(state.Controller,space)) continue;
            float d=Mathf.Abs(space.Position(state.Controller.heroRect).x-
                              space.Position(info.controller.Rect).x);
            if (d<min) { min=d; nearest=state; }
        }
        return nearest;
    }

    // Magic counts up before every cast; other attacks count down after a strike.
    private static bool AdvanceCycle(ref float timer, AttackMode mode, float duration, float dt)
    {
        timer = mode == AttackMode.RangedMagic ? Mathf.Min(duration, timer + dt) : Mathf.Max(0f, timer - dt);
        return mode == AttackMode.RangedMagic ? timer >= duration : timer <= 0f;
    }

    // Keep B1/private helper for regression clients; B2 passes the actor.
    private ActiveMonsterInfo NearestEligibleMonster(EnvironmentManager space)
    {
        return NearestEligibleMonster(space,primaryHeroState);
    }
    private ActiveMonsterInfo NearestEligibleMonster(EnvironmentManager space,HeroCombatState actor)
    {
        ActiveMonsterInfo target=null;
        float nearest=float.MaxValue;
        foreach (var info in activeMonsters)
        {
            if (info.controller==null || !info.controller.IsAlive ||
                !actor.Controller.CanAttack(info.controller,space)) continue;
            float distance=Mathf.Abs(space.Position(info.controller.Rect).x-
                                     space.Position(actor.Controller.heroRect).x);
            if (distance<nearest) { nearest=distance; target=info; }
        }
        return target;
    }
    private void TickHeroAttack(EnvironmentManager space,float dt)
    {
        TickRegisteredHeroAttack(primaryHeroState,space,dt);
    }
    private void TickRegisteredHeroAttack(HeroCombatState actor,EnvironmentManager space,float dt)
    {
        var hero=actor.Controller;
        AttackMode mode=hero.attackMode;
        if (actor.WindupMode!=mode) { ResetRegisteredAttack(actor); actor.WindupMode=mode; }
        float speed=CombatStats.Get(actor.Data,CombatStatId.AttackSpeed,
            actor.Data.baseAttackSpeed,ElementType.Vo,new [] { CombatStats.StanceTag(mode) });
        float duration=AttackDuration(mode,speed);
        ActiveMonsterInfo target=NearestEligibleMonster(space,actor);
        if (mode==AttackMode.RangedMagic && (!isBattling || target==null))
        {
            actor.AttackTimer=0f;
            hero.UpdateChargeBar(false,0f);
            return;
        }
        bool ready=AdvanceCycle(ref actor.AttackTimer,mode,duration,dt);
        hero.UpdateChargeBar(mode==AttackMode.RangedMagic,actor.AttackTimer/duration);
        if (!ready || !isBattling || target==null) return;
        actor.AttackTimer=mode==AttackMode.RangedMagic?0f:duration;
        hero.UpdateChargeBar(mode==AttackMode.RangedMagic,0f);
        hero.PlayAttackFeedback();
        var trace=RollDamageTrace(actor.Data,target.data,actor.WindupMode);
        if (mode==AttackMode.Melee)
        {
            Impact(BodyPosition(target.controller.Rect,space),false);
            DealDamageToMonsterFromHero(target,trace,actor,null,mode);
        }
        else Launch(target,true,mode,trace,duration,space,actor);
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

    private void ResetHeroAttack() { ResetRegisteredAttack(primaryHeroState); }
    private void ResetRegisteredAttack(HeroCombatState actor)
    {
        actor.ResetAttack();
        if (actor.Controller != null) actor.Controller.UpdateChargeBar(false,0f);
    }

    private DamageTrace RollHeroDamageTrace(EntityDataSO defender)
    {
        return RollDamageTrace(runtimeHeroData, defender, heroWindupMode);
    }

    // Retain B1 reflection-compatible entry points for the primary Hero.
    private void DealDamageToMonsterWithTrace(ActiveMonsterInfo target, DamageTrace trace, float? impactSourceX = null)
    {
        DealDamageToMonsterFromHero(target,trace,primaryHeroState,impactSourceX);
    }
    private void DealDamageToMonsterFromHero(ActiveMonsterInfo target,DamageTrace trace,
        HeroCombatState attacker,float? impactSourceX=null,AttackMode? sourceMode=null)
    {
        if (target == null || attacker == null || attacker.Data == null) return;
        int damage = CombatDamage.ResolveLegacy(trace.elementDamage,attacker.Data,target.data,
            sourceMode ?? attacker.SelectedMode,resistanceSettings);
        if (gameManager!=null) gameManager.RecordDamage(trace.Describe(1f,1,damage));
        DealDamageFromHero(target,damage,attacker,impactSourceX);
    }
    private void DealDamageToMonster(ActiveMonsterInfo target,int finalDmg,float? impactSourceX=null)
    {
        DealDamageFromHero(target,finalDmg,primaryHeroState,impactSourceX);
    }
    private void DealDamageFromHero(ActiveMonsterInfo target,int damage,
        HeroCombatState attacker,float? impactSourceX=null)
    {
        if (target==null || !activeMonsters.Contains(target) || target.controller==null ||
            !target.controller.IsAlive || !IsRegistered(attacker)) return;
        target.currentHP=Mathf.Max(0,target.currentHP-damage);
        if (gameManager!=null)
        {
            gameManager.RecordDamageEvent(attacker.Data!=null?attacker.Data.entityName:"Hero",
                target.data!=null?target.data.entityName:"Quái",damage);
            gameManager.UpdateEventLog(IdentityDisplay.Describe(target.data)+" nhận "+damage+" sát thương.");
        }
        target.controller.UpdateHealthBar(target.currentHP,target.maxHP);
        var space=EnvironmentManager.Instance;
        float away=-1f;
        if (space!=null && attacker.Controller!=null && attacker.Controller.heroRect!=null)
        {
            float fromX=impactSourceX.HasValue?impactSourceX.Value:
                space.Position(attacker.Controller.heroRect).x;
            away=DamagePopupMotion.AwaySign(space.Position(target.controller.Rect).x,fromX,-1f);
        }
        target.controller.ShowDamage(damage,away);
        if (target.currentHP==0) HandleMonsterDeathBy(target,attacker);
    }

    // Keep the old one-argument reflection entry point in legacy tests.
    private void HandleMonsterDeath(ActiveMonsterInfo target)
    {
        HandleMonsterDeathBy(target,primaryHeroState);
    }
    private void HandleMonsterDeathBy(ActiveMonsterInfo target,HeroCombatState attacker)
    {
        if (target==null || !activeMonsters.Contains(target)) return;
        activeMonsters.Remove(target);
        if (target.controller!=null) target.controller.MarkDead();
        int exp=CombatBalance.KillExp(target.data.currentLevel)*
                (gameManager!=null && gameManager.IsHardMode?3:1);
        // Progress is credited to the attacker, not a global primary stat.
        attacker.Data.AddExp(exp);
        attacker.MaxHP=attacker.Data.GetCalculatedHealth(attacker.SelectedMode);
        int heal=attacker.SelectedMode==AttackMode.Melee
            ? CombatBalance.MeleeKillHeal(attacker.Data.currentLevel,Random.Range(3,6)) : 0;
        attacker.CurrentHP=Mathf.Clamp(attacker.CurrentHP+heal,0,attacker.MaxHP);
        if (attacker.Controller!=null)
        {
            attacker.Controller.UpdateHealthBar(attacker.CurrentHP,attacker.MaxHP);
            attacker.Controller.UpdateStats(attacker.Data,attacker.CurrentHP,attacker.MaxHP);
        }
        if (gameManager!=null)
            gameManager.UpdateEventLog("Hạ "+IdentityDisplay.Describe(target.data)+". Nhận "+exp+" EXP!");
        monsterSpawner.DespawnMonster(target.go);
        if (target.data!=null) Destroy(target.data);
        if (gameManager!=null) gameManager.OnMonsterDied(target.go);
        if (activeMonsters.Count==0) isBattling=false;
    }

    private void DealDamageToHero(int damage) { ApplyIncomingDamage(damage,"Quái"); }
    private void ApplyIncomingDamage(int damage,string source,DamageTrace trace=null,float? impactSourceX=null)
    {
        ApplyDamageToHeroState(primaryHeroState,damage,source,trace,impactSourceX);
    }
    // Test/dev routing endpoint; rejects unregistered and dead Heroes.
    public bool ApplyDamageToRegisteredHero(HeroController target,int damage,string source)
    {
        var state=StateFor(target);
        if (state==null || !state.CanFight) return false;
        ApplyDamageToHeroState(state,damage,source);
        return true;
    }
    private void ApplyDamageToHeroState(HeroCombatState victim,int damage,string source,
        DamageTrace trace=null,float? impactSourceX=null,
        EntityDataSO attackerData=null,AttackMode sourceMode=AttackMode.Melee)
    {
        if (!IsRegistered(victim) || !victim.CanFight) return;
        // Direct test/console damage (trace null) remains unaffected.
        if (trace != null && attackerData != null)
            damage = CombatDamage.ResolveLegacy(damage,attackerData,victim.Data,
                sourceMode,resistanceSettings);
        float mapScale=WorldNames.MonsterDamageScale(victim.Data.MapVisits);
        int hardScale=gameManager!=null && gameManager.IsHardMode?2:1;
        damage=Mathf.Max(1,Mathf.RoundToInt(damage*mapScale*hardScale));
        if (gameManager!=null && trace!=null)
            gameManager.RecordDamage(trace.Describe(mapScale,hardScale,damage));
        victim.CurrentHP=Mathf.Max(0,victim.CurrentHP-damage);
        if (gameManager!=null)
        {
            gameManager.RecordDamageEvent(source,victim.Data.entityName,damage);
            gameManager.UpdateEventLog(source+" gây "+damage+" sát thương cho "+
                victim.Data.entityName+(victim.CurrentHP==0?": đã tử vong.":"."));
        }
        var hero=victim.Controller;
        hero.UpdateHealthBar(victim.CurrentHP,victim.MaxHP);
        var space=EnvironmentManager.Instance;
        float away=1f;
        if (space!=null && hero.heroRect!=null && impactSourceX.HasValue)
            away=DamagePopupMotion.AwaySign(space.Position(hero.heroRect).x,impactSourceX.Value,1f);
        hero.ShowDamage(damage,away);
        if (victim.CurrentHP>0) return;
        victim.ResetAttack();
        hero.Die();
        // Do not GameOver or clear projectiles when teammates remain alive.
        if (AnyFightingHero()) return;
        ForceClearAllMonsters();
        if (gameManager!=null) gameManager.OnHeroDied();
    }

    private Vector2 BodyPosition(RectTransform rect, EnvironmentManager space)
    {
        // Aim at the body rather than the feet pivot; height is Ground-local.
        return space.Position(rect) + new Vector2(0f, 24f * Mathf.Max(0.1f, effectsScale));
    }

    private void Launch(ActiveMonsterInfo monster,bool fromHero,AttackMode mode,
        DamageTrace trace,float interval,EnvironmentManager space,HeroCombatState actor)
    {
        if (actor==null || actor.Controller==null || actor.Controller.heroRect==null) return;
        bool magic=mode==AttackMode.RangedMagic;
        Vector2 start=BodyPosition(fromHero?actor.Controller.heroRect:monster.controller.Rect,space);
        Vector2 end=BodyPosition(fromHero?monster.controller.Rect:actor.Controller.heroRect,space);
        var shot=new Projectile {
            monster=monster,monsterLife=monster.lifeId,
            monsterSpawn=monster.controller.SpawnVersion,
            hero=actor,heroLife=actor.LifeVersion,
            heroSpawn=actor.Controller.SpawnVersion,
            fromHero=fromHero,magic=magic,start=start,end=end,trace=trace,
            attackMode=mode,
            source=IdentityDisplay.Describe(fromHero?actor.Data:monster.data),
            duration=BattleMotion.FlightDuration(magic?magicFlightTime:physicalFlightTime,interval),
            arc=magic?Mathf.Max(0f,magicArcHeight):0f
        };
        if (effects!=null)
        {
            shot.visual=effects.AddBolt(start,magic,
                (magic?5f*Mathf.Max(.1f,magicProjectileScale):2f)*Mathf.Max(.1f,effectsScale));
            shot.visual.direction=shot.end-shot.start;
        }
        projectiles.Add(shot);
    }

    private static bool WithinImpact(Vector2 a, Vector2 b, float radius)
    {
        float x = a.x - b.x, y = a.y - b.y;
        radius = Mathf.Max(0f, radius);
        return x * x + y * y <= radius * radius + .01f;
    }

    private void TickProjectiles(EnvironmentManager space,float dt)
    {
        for (int i=projectiles.Count-1;i>=0;i--)
        {
            var shot=projectiles[i];
            bool validHero=IsRegistered(shot.hero) &&
                           shot.hero.LifeVersion==shot.heroLife &&
                           shot.hero.Controller.SpawnVersion==shot.heroSpawn &&
                           shot.hero.CanFight;
            bool validMonster=shot.monster!=null &&
                              shot.monster.lifeId==shot.monsterLife &&
                              activeMonsters.Contains(shot.monster) &&
                              shot.monster.controller!=null &&
                              shot.monster.controller.SpawnVersion==shot.monsterSpawn &&
                              shot.monster.controller.IsAlive;
            // A resurrected Hero and a pooled monster are NEW lifetimes. Physical
            // shots require the original target; Hero magic still explodes at
            // its stored endpoint even if the original monster has died.
            if (!validHero || ((!shot.fromHero || !shot.magic) && !validMonster))
            { RemoveProjectile(i); continue; }
            shot.elapsed+=dt;
            float t=shot.duration>0f?Mathf.Clamp01(shot.elapsed/shot.duration):1f;
            Vector2 end=shot.end;
            Vector2 position=new Vector2(
                shot.start.x+(end.x-shot.start.x)*t,
                shot.start.y+(end.y-shot.start.y)*t+
                4f*shot.arc*t*(1f-t));
            if (shot.visual!=null)
            {
                shot.visual.direction=position-shot.visual.position;
                shot.visual.position=position;
            }
            if (t<1f) continue;
            RemoveProjectile(i); // Death callbacks may clear the remaining volley.
            Impact(end,shot.magic);
            bool hit=false;
            if (shot.fromHero && shot.magic)
            {
                var victims=new List<ActiveMonsterInfo>(activeMonsters);
                foreach (var victim in victims)
                {
                    if (!activeMonsters.Contains(victim) || victim.controller==null ||
                        !victim.controller.IsAlive ||
                        !WithinImpact(BodyPosition(victim.controller.Rect,space),
                            end,heroMagicImpactRadius)) continue;
                    hit=true;
                    DealDamageToMonsterFromHero(victim,
                        SynergyMath.EvaluateSnapshot(shot.trace,victim.data),shot.hero,
                        end.x,shot.attackMode);
                }
            }
            else if (shot.fromHero)
            {
                hit=WithinImpact(BodyPosition(shot.monster.controller.Rect,space),
                                 end,physicalHitRadius);
                if (hit) DealDamageToMonsterFromHero(shot.monster,
                    SynergyMath.EvaluateSnapshot(shot.trace,shot.monster.data),
                    shot.hero,shot.start.x,shot.attackMode);
            }
            else
            {
                var victim=shot.hero;
                hit=WithinImpact(BodyPosition(victim.Controller.heroRect,space),
                                 end,shot.magic?magicImpactRadius:physicalHitRadius);
                if (hit)
                {
                    var trace=SynergyMath.EvaluateSnapshot(shot.trace,victim.Data);
                    ApplyDamageToHeroState(victim,trace.elementDamage,shot.source,trace,
                        shot.magic?shot.end.x:shot.start.x,shot.monster.data,shot.attackMode);
                }
            }
            if (!hit && gameManager!=null)
            {
                string miss=shot.source+(shot.magic?": vùng nổ không có mục tiêu.":
                                          ": đòn cung hụt, sát thương 0.");
                gameManager.UpdateEventLog(miss);gameManager.RecordDamage(miss);
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
    void OnDisable() { ClearProjectiles(); ResetAllHeroAttacks(); }
    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (effects != null) Destroy(effects.gameObject);
    }

    public void ForceClearAllMonsters() { ClearBattle(false); }
    private void ClearBattle(bool preserveHeroAttack)
    {
        isBattling = false;
        if (!preserveHeroAttack) ResetAllHeroAttacks();
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
