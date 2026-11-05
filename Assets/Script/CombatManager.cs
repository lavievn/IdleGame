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
    private LootDropPresentation lootPresentation;
    public IReadOnlyList<RectTransform> LootDropRoots => lootPresentation != null ? lootPresentation.Roots : null;
    private HeroCombatSfx heroSfx;
    private class Projectile
    {
        public ActiveMonsterInfo monster;
        public HeroCombatState hero; // Shooter if fromHero, target if monster-fired.
        public int heroLife, heroSpawn;
        public int monsterLife, monsterSpawn;
        public int battleVersion,stepTick;
        public bool fromHero, magic;
        public Vector2 start, end;
        public float elapsed, duration, arc;
        public string source;
        public DamageTrace trace;
        public AttackMode attackMode;
        public bool staffBasic;
        public BattleEffects.Bolt visual;
    }
    private readonly List<Projectile> projectiles = new List<Projectile>();
    // Visual-only tails hold no actor/data/target reference and can never hit.
    private struct MagicFlight
    {
        public Vector2 start,end;
        public float elapsed,duration,arc;
        public int stepTick;
        public BattleEffects.Bolt visual;
    }
    private readonly List<MagicFlight> magicFlights=new List<MagicFlight>();
    private int battleVersion,projectileTick;
    private const float MaxDetachedMagicSeconds=10f;
    public int PendingProjectileCount => projectiles.Count;
    public int DetachedMagicVisualCount => magicFlights.Count;
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
            if (primaryHeroState.CanFight) return primaryHeroState.Controller.EffectiveMoveSpeed();
            foreach (var state in heroStates.Values)
                if (state.CanFight) return state.Controller.EffectiveMoveSpeed();
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
        controller.BindBasicAttackData(data,true); // B2 test fallback, no persistent equipment.
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
        internal CombatLootRewards.KillKey lootKey;
        internal bool acceptedLethalDamage;
    }
    private bool resumeEncounterAfterEnable;
    private int monsterLifeSequence;
    private readonly CombatLootRewards lootRewards=new CombatLootRewards();
    private EntityDataSO LootRecipient => gameManager!=null
        ? (gameManager.isActiveAndEnabled?gameManager.HeroData:null):runtimeHeroData;

    private readonly List<ActiveMonsterInfo> activeMonsters = new List<ActiveMonsterInfo>();

    void Start()
    {
        heroSfx=HeroCombatSfx.Install(this);
        Instance = this;
        gameManager = GetComponent<GameManager>();
        heroController = Object.FindFirstObjectByType<HeroController>();
        if (heroController != null) heroStates[heroController] = primaryHeroState;
        if (monsterSpawner == null) monsterSpawner = Object.FindFirstObjectByType<MonsterSpawner>();
    }

    public void SetupHeroInfo(EntityDataSO hData)
    {
        if (hData == null) return;
        if (lootPresentation != null) lootPresentation.Clear();
        lootRewards.Invalidate();
        hData.NormalizeCombatStats();
        if(heroController!=null)heroController.BindBasicAttackData(hData,false);
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
        primaryHeroState.WeaponIdentity=WeaponBasicAttack.Identity(hData);
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
        ClearBattle(true,true); // Wave change retires old damage, not Magic visuals.
        lootRewards.BeginEncounter(LootRecipient);
        foreach (var m in monsters)
        {
            if (m == null) continue;
            var info = new ActiveMonsterInfo();
            info.lifeId = ++monsterLifeSequence;
            info.go = m;
            info.controller = m.GetComponent<MonsterController>();
            info.lootKey=lootRewards.Register(info.lifeId,info.controller!=null?info.controller.SpawnVersion:0);
            info.data = Instantiate(baseMonsterData);
            WorldMapNode worldNode = null;
            bool onWorldMap = WorldMapCatalog.Enabled(runtimeHeroData) &&
                WorldMapCatalog.TryGet(runtimeHeroData.worldMapId, out worldNode);
            if (onWorldMap)
            {
                // Per-map test band does not scale with the Hero's level.
                info.data.currentLevel = Mathf.Clamp(worldNode.SuggestedLevel +
                    Random.Range(-1, 2), 1, 999);
            }
            else
            {
                int spread = CombatBalance.LevelSpread(runtimeHeroData.currentLevel);
                info.data.currentLevel = Mathf.Clamp(runtimeHeroData.currentLevel +
                    Random.Range(-spread, spread + 1), 1, 999);
            }
            info.data.baseHealth = CombatBalance.MonsterHealth(info.data.currentLevel);
            info.data.baseDamage = CombatBalance.MonsterAttack(info.data.currentLevel);
            info.data.baseAttackSpeed = CombatBalance.MonsterSpeed(info.data.currentLevel);
            info.data.addedHealth = 0;
            info.data.addedDamage = 0;
            info.data.gender = Random.Range(0,2) == 0 ? GenderType.Nam : GenderType.Nu;
            info.data.entityName = NameDatabase.GetRandomName(info.data.gender);
            info.data.race = SynergyMath.GenerateMonsterRace(runtimeHeroData.mapTerrain);
            SynergyMath.GenerateRootProfile(info.data);
            if (onWorldMap)
            {
                // Draw one candidate from the current node's seven species or
                // one hostile human role. Bosses require separate future logic.
                int index = Random.Range(0, 8);
                if (index == 7)
                {
                    info.data.race = RaceType.NhanToc;
                    info.data.monsterAnimal = "";
                    info.data.monsterClass = MonsterClass.Nhan;
                    info.data.entityName = worldNode.HumanEnemy + " " + info.data.entityName;
                }
                else
                {
                    if (info.data.race == RaceType.NhanToc) info.data.race = RaceType.YeuThu;
                    info.data.monsterAnimal = worldNode.Animals[index];
                    info.data.monsterClass = WorldNames.ClassifyAnimal(info.data.monsterAnimal);
                    info.data.entityName = WorldNames.MonsterName(info.data, info.data.monsterAnimal);
                }
            }
            else info.data.entityName = WorldNames.RandomMonsterName(info.data, runtimeHeroData.mapTerrain);
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
        DiscardCombatProjectiles(true,true); ResetAllHeroAttacks();
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
    public void TickMonsterAwareness(EnvironmentManager space, float dt)
    {
        if (Time.timeScale == 0f || space == null || space.BattleArea == null) return;
        HeroController detected = null;
        foreach (var info in activeMonsters)
        {
            if (!CurrentMonsterLife(info)) continue;
            var seen = info.controller.TickAwareness(space, dt);
            if (detected == null && seen != null) detected = seen;
        }
        if (detected != null) AlertEncounter(detected, false);
    }
    private bool CurrentMonsterLife(ActiveMonsterInfo info)
    {
        return info.controller != null && info.controller.IsAlive && info.currentHP > 0 &&
            info.controller.SpawnVersion == info.lootKey.Spawn && lootRewards.IsCurrent(info.lootKey, LootRecipient);
    }
    private void AlertEncounter(HeroController hero, bool immediate)
    {
        foreach (var info in activeMonsters)
            if (CurrentMonsterLife(info)) info.controller.ReceiveAlert(hero, immediate);
    }
    public int MaxHeroHP => maxHeroHP;
    public void OnHeroAttackModeChanged()
    {
        OnRegisteredHeroModeChanged(heroController);
    }
    public void OnRegisteredHeroModeChanged(HeroController controller)
    {
        RefreshRegisteredHeroStats(controller,true);
    }

    public void RefreshWeaponAttack(HeroController controller)
    {
        var state=StateFor(controller);
        if(state==null || !controller.UsesWeaponBasicAttack)return;
        int hp=state.CurrentHP;
        controller.RefreshWeaponPresentation();
        state.WeaponIdentity=WeaponBasicAttack.Identity(state.Data);
        state.MaxHP=state.Data.GetCalculatedHealth(controller.attackMode);
        // A weapon change can lower the HP cap, but never heal the actor.
        state.CurrentHP=hp<=0?0:Mathf.Min(hp,state.MaxHP);
        state.WindupMode=controller.attackMode;
        ResetRegisteredAttack(state);
        float speed=CombatStats.Get(state.Data,CombatStatId.AttackSpeed,state.Data.baseAttackSpeed,
            ElementType.Vo,new[]{CombatStats.StanceTag(controller.attackMode)});
        state.AttackTimer=WeaponBasicAttack.Interval(controller.attackMode,speed);
        controller.UpdateHealthBar(state.CurrentHP,state.MaxHP);
        controller.UpdateStats(state.Data,state.CurrentHP,state.MaxHP);
    }

    public void RefreshRegisteredHeroStats(HeroController controller,bool resetAttack=false)
    {
        var state = StateFor(controller);
        if (state == null || state.Data == null || state.Controller == null) return;
        float ratio = state.MaxHP > 0 ? (float)state.CurrentHP/state.MaxHP : 1f;
        state.MaxHP = state.Data.GetCalculatedHealth(controller.attackMode);
        state.CurrentHP = state.CurrentHP <= 0 ? 0 :
            Mathf.Clamp(Mathf.RoundToInt(ratio*state.MaxHP),1,state.MaxHP);
        if (resetAttack) ResetRegisteredAttack(state);
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
        projectileTick++;
        var space = EnvironmentManager.Instance;
        float dt = Time.deltaTime;
        if (effects != null) effects.Advance(dt);
        if (lootPresentation != null) lootPresentation.Advance(dt, effects);
        if (space == null || space.BattleArea==null)
        {
            if (projectiles.Count>0 || magicFlights.Count>0) ClearProjectiles();
            return;
        }
        if(!AnyFightingHero())
        {
            DiscardCombatProjectiles(true,false);
            TickMagicFlights(dt);
            return;
        }
        if (effects == null && space.BattleArea != null) effects = BattleEffects.Create(space.BattleArea);
        // Older projectiles always advance before a new strike.
        TickProjectiles(space,dt);
        TickMagicFlights(dt);
        if (!AnyFightingHero()) return;
        foreach (var state in heroStates.Values)
            if (state.CanFight)
            {
                TickPassiveRegeneration(state,dt);
                TickRegisteredHeroAttack(state,space,dt);
            }
        if (!isBattling) return;
        for (int i=activeMonsters.Count-1;i>=0;i--)
        {
            if (i>=activeMonsters.Count) continue;
            var info=activeMonsters[i];
            if (info.controller==null || !info.controller.IsAlive) continue;
            if (!info.controller.IsAggressive)
            {
                info.attackTimer = 0f;
                info.controller.UpdateChargeBar(false, 0f);
                continue;
            }
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

    private void TickPassiveRegeneration(HeroCombatState state,float dt)
    {
        if (state.CurrentHP >= state.MaxHP || dt <= 0f)
        {
            state.HealthRegenRemainder = 0f;
            return;
        }
        float rate = CombatStats.Get(state.Data,CombatStatId.HealthRegeneration,0f,
            ElementType.Vo,new [] { CombatStats.StanceTag(state.SelectedMode) });
        if (rate <= 0f) { state.HealthRegenRemainder = 0f; return; }
        state.HealthRegenRemainder += Mathf.Min(100000f,rate) * dt;
        int heal = (int)System.Math.Min(state.MaxHP-state.CurrentHP,
            System.Math.Max(0f,state.HealthRegenRemainder));
        if (heal <= 0) return;
        state.HealthRegenRemainder -= heal;
        state.CurrentHP = Mathf.Min(state.MaxHP,state.CurrentHP+heal);
        state.Controller.UpdateHealthBar(state.CurrentHP,state.MaxHP);
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
        if(hero.UsesWeaponBasicAttack && (actor.WeaponIdentity!=WeaponBasicAttack.Identity(actor.Data) ||
            hero.attackMode!=WeaponBasicAttack.Resolve(actor.Data)))RefreshWeaponAttack(hero);
        hero.SyncWeaponMode();
        bool basic=hero.UsesWeaponBasicAttack;
        AttackMode mode=hero.attackMode;
        if (actor.WindupMode!=mode) { ResetRegisteredAttack(actor); actor.WindupMode=mode; }
        float speed=CombatStats.Get(actor.Data,CombatStatId.AttackSpeed,
            actor.Data.baseAttackSpeed,ElementType.Vo,new [] { CombatStats.StanceTag(mode) });
        float duration=basic?WeaponBasicAttack.Interval(mode,speed):AttackDuration(mode,speed);
        ActiveMonsterInfo target=NearestEligibleMonster(space,actor);
        if (!basic && mode==AttackMode.RangedMagic && (!isBattling || target==null))
        {
            actor.AttackTimer=0f;
            hero.UpdateChargeBar(false,0f);
            return;
        }
        bool ready=AdvanceCycle(ref actor.AttackTimer,basic?AttackMode.Melee:mode,duration,dt);
        hero.UpdateChargeBar(!basic && mode==AttackMode.RangedMagic,actor.AttackTimer/duration);
        if (!ready || !isBattling || target==null) return;
        actor.AttackTimer=!basic && mode==AttackMode.RangedMagic?0f:duration;
        hero.UpdateChargeBar(!basic && mode==AttackMode.RangedMagic,0f);
        hero.PlayAttackFeedback();
        var trace=basic?SynergyMath.EvaluateBasicDamage(actor.Data,target.data,mode,Random.Range(.85f,1f)):
            RollDamageTrace(actor.Data,target.data,actor.WindupMode);
        if (mode==AttackMode.Melee)
        {
            if(heroSfx!=null)heroSfx.TryPlay(actor,mode);
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
        if(heroSfx!=null)heroSfx.StopHero(actor.Controller);
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
        var damageMode=sourceMode??attacker.SelectedMode;
        int damage=trace.weaponBasic?CombatDamage.ResolveBasicSnapshot(trace.elementDamage,trace.basicBoost,
            target.data,damageMode,resistanceSettings):CombatDamage.ResolveLegacy(trace.elementDamage,attacker.Data,
            target.data,damageMode,resistanceSettings);
        if (gameManager!=null)
            gameManager.RecordDamage(trace.Describe(1f,1,trace.elementDamage) +
                (damage != trace.elementDamage ? "\n0.0.51 — Sau bổ trợ và phòng ngự: " + damage : ""));
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
            !target.controller.IsAlive || !IsRegistered(attacker) ||
            target.controller.SpawnVersion!=target.lootKey.Spawn ||
            !lootRewards.IsCurrent(target.lootKey,LootRecipient)) return;
        if (damage <= 0) return;
        bool surprise = !target.controller.HasDetectedHero;
        if (surprise) damage = (int)System.Math.Min(int.MaxValue, (long)damage * 2L);
        // Resolve at the accepted impact, after defense. A stale/missed shot
        // cannot consume surprise or wake this encounter; a lethal ambush still
        // alerts the other living members before the victim leaves the pool.
        AlertEncounter(attacker.Controller, true);
        target.currentHP=Mathf.Max(0,target.currentHP-damage);
        if (gameManager!=null)
        {
            gameManager.RecordDamageEvent(attacker.Data!=null?attacker.Data.entityName:"Hero",
                target.data!=null?target.data.entityName:"Quái",damage);
            gameManager.UpdateEventLog(IdentityDisplay.Describe(target.data)+" nhận "+damage+" sát thương.");
            if (surprise) gameManager.UpdateEventLog("Đánh bất ngờ: sát thương đòn đầu ×2.");
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
        if (target.currentHP==0) {
            target.acceptedLethalDamage=true;
            HandleMonsterDeathBy(target,attacker);
        }
    }

    // Keep the old one-argument reflection entry point in legacy tests.
    private void HandleMonsterDeath(ActiveMonsterInfo target)
    {
        HandleMonsterDeathBy(target,primaryHeroState);
    }
    private void HandleMonsterDeathBy(ActiveMonsterInfo target,HeroCombatState attacker)
    {
        if (target==null || !target.acceptedLethalDamage || target.currentHP!=0 ||
            target.data==null || target.controller==null || !target.controller.IsAlive ||
            target.controller.SpawnVersion!=target.lootKey.Spawn ||
            !IsRegistered(attacker) || attacker.Data==null || !activeMonsters.Contains(target) ||
            !lootRewards.TryClaim(target.lootKey,LootRecipient)) return;
        activeMonsters.Remove(target);
        if (target.controller!=null) target.controller.MarkDead();
        int exp=CombatBalance.KillExp(target.data.currentLevel)*
                (gameManager!=null && gameManager.IsHardMode?3:1);
        // Progress is credited to the attacker, not a global primary stat.
        attacker.Data.AddExp(exp);
        attacker.MaxHP=attacker.Data.GetCalculatedHealth(attacker.SelectedMode);
        int heal=attacker.SelectedMode==AttackMode.Melee
            ? CombatBalance.MeleeKillHeal(attacker.Data.currentLevel,Random.Range(3,6)) : 0;
        heal = Mathf.Max(0,heal + CombatStats.GetInt(attacker.Data,
            CombatStatId.KillHealBonus,0,ElementType.Vo,
            new [] { CombatStats.StanceTag(attacker.SelectedMode) }));
        attacker.CurrentHP=Mathf.Clamp(attacker.CurrentHP+heal,0,attacker.MaxHP);
        if (attacker.Controller!=null)
        {
            attacker.Controller.UpdateHealthBar(attacker.CurrentHP,attacker.MaxHP);
            attacker.Controller.UpdateStats(attacker.Data,attacker.CurrentHP,attacker.MaxHP);
        }
        if (gameManager!=null)
            gameManager.UpdateEventLog("Hạ "+IdentityDisplay.Describe(target.data)+". Nhận "+exp+" EXP!");
        string lootItem="",lootError;
        bool testProfile=gameManager!=null && gameManager.ControlledLootTestEnabled;
        CombatLootRewards.ProductionResult production=default(CombatLootRewards.ProductionResult);
        var lootOutcome=testProfile
            ?lootRewards.Complete(target.lootKey,LootRecipient,true,out lootItem,out lootError)
            :lootRewards.CompleteProduction(target.lootKey,LootRecipient,
                gameManager!=null?gameManager.ProductionLoot:null,out production,out lootError);
        if(!testProfile)lootItem=production.Equipment.instanceId;
        if((testProfile && lootOutcome==CombatLootRewards.Outcome.Awarded) ||
            (!testProfile && production.EquipmentOutcome==CombatLootRewards.Outcome.Awarded))
        {
            // A cosmetic failure must not abort despawn/wave progression after
            // the authoritative reward has already committed to inventory.
            try { ShowAwardedLoot(target,lootItem); }
            catch(System.Exception ex) { Debug.LogWarning("[LOOT VISUAL] "+ex.Message); }
            if(gameManager!=null)gameManager.UpdateEventLog(testProfile
                ?"[LOOT TEST A] Đang bật · Nhận Thiết Kiếm (Common) vào Kho đồ."
                :"Nhận "+EquipmentRefinement.DisplayName(EquipmentRuntime.Find(LootRecipient,lootItem))+" vào Kho đồ.");
        }
        if(!testProfile && production.MaterialOutcome==CombatLootRewards.Outcome.Awarded && gameManager!=null)
            gameManager.UpdateEventLog("Nhận "+EquipmentRefinement.MaterialName+" x"+production.Material.quantity+" vào Kho đồ.");
        // Partial success must retain its feedback AND report the failed stream.
        if(!string.IsNullOrEmpty(lootError))
        {
            Debug.LogError(lootError);
            if(gameManager!=null)gameManager.UpdateEventLog(testProfile?"[LOOT TEST A] Lỗi cấp đồ; đã dừng thưởng test. Xem Console.":"Lỗi cấp thưởng; xem Console.");
        }
        monsterSpawner.DespawnMonster(target.go);
        if (target.data!=null) Destroy(target.data);
        if (gameManager!=null) gameManager.OnMonsterDied(target.go);
        if (activeMonsters.Count==0) isBattling=false;
    }

    private void ShowAwardedLoot(ActiveMonsterInfo target,string itemId)
    {
        var space=EnvironmentManager.Instance;
        if(space==null || space.BattleArea==null || target.controller==null || target.controller.Rect==null)return;
        // Capture before despawn/OnMonsterDied: the last victim can end a wave
        // synchronously and the monster GameObject may be reused by the pool.
        Vector2 foot=space.Position(target.controller.Rect);
        if(effects==null)effects=BattleEffects.Create(space.BattleArea);
        if(lootPresentation==null)lootPresentation=LootDropPresentation.Install(this);
        var item=EquipmentRuntime.Find(LootRecipient,itemId);
        if(item!=null)lootPresentation.Show(space.BattleArea,foot,item.templateId,itemId,effects);
    }

    public void PanLoot(float groundAmount)
    {
        if(lootPresentation!=null)lootPresentation.Pan(groundAmount);
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
        int previousPipelineDamage = trace == null ? damage :
            Mathf.Max(1,Mathf.RoundToInt(trace.elementDamage*mapScale*hardScale));
        damage=Mathf.Max(1,Mathf.RoundToInt(damage*mapScale*hardScale));
        if (gameManager!=null && trace!=null)
            gameManager.RecordDamage(trace.Describe(mapScale,hardScale,previousPipelineDamage) +
                (damage != previousPipelineDamage ? "\n0.0.51 — Sau bổ trợ và phòng ngự: " + damage : ""));
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
        ClearBattle(false,true); // Game Over cancels combat; existing Magic finishes visually.
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
        bool staffBasic=fromHero && trace.weaponBasic && mode==AttackMode.RangedMagic;
        bool magic=mode==AttackMode.RangedMagic && !staffBasic;
        Vector2 start=BodyPosition(fromHero?actor.Controller.heroRect:monster.controller.Rect,space);
        Vector2 end=BodyPosition(fromHero?monster.controller.Rect:actor.Controller.heroRect,space);
        var shot=new Projectile {
            monster=monster,monsterLife=monster.lifeId,
            battleVersion=battleVersion,stepTick=projectileTick,
            monsterSpawn=monster.controller.SpawnVersion,
            hero=actor,heroLife=actor.LifeVersion,
            heroSpawn=actor.Controller.SpawnVersion,
            fromHero=fromHero,magic=magic,staffBasic=staffBasic,start=start,end=end,trace=trace,
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
        // Shoot/Magic belong to the actual release, never drawing, sprite
        // frame changes or projectile impact. Monster launches stay unchanged.
        if(fromHero && heroSfx!=null)heroSfx.TryPlay(actor,mode);
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
            if (shot.battleVersion!=battleVersion || !validHero ||
                ((!shot.fromHero || !shot.magic) && !validMonster))
            { RetireProjectile(i); continue; }
            shot.stepTick=projectileTick;
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
            if(!shot.staffBasic)Impact(end,shot.magic);
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
            if (!isBattling) { DiscardCombatProjectiles(true,false); return; }
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
    private void RetireProjectile(int index)
    {
        var shot=projectiles[index];
        if(!shot.magic) {RemoveProjectile(index);return;}
        float duration=float.IsNaN(shot.duration)||float.IsInfinity(shot.duration)
            ?MaxDetachedMagicSeconds:Mathf.Clamp(shot.duration,.001f,MaxDetachedMagicSeconds);
        magicFlights.Add(new MagicFlight {start=shot.start,end=shot.end,arc=shot.arc,
            elapsed=float.IsNaN(shot.elapsed)||float.IsInfinity(shot.elapsed)?0f:Mathf.Max(0f,shot.elapsed),
            duration=duration,stepTick=shot.stepTick,visual=shot.visual});
        projectiles.RemoveAt(index); // Transfer bolt ownership; do not remove its graphic.
    }
    private void TickMagicFlights(float dt)
    {
        for(int i=magicFlights.Count-1;i>=0;i--)
        {
            var flight=magicFlights[i];
            // A callback may retire a flight already stepped this Update.
            // Use the combat tick, not Time.frameCount (headless slices can share it).
            if(flight.stepTick!=projectileTick)flight.elapsed+=Mathf.Max(0f,dt);
            flight.stepTick=projectileTick;
            float t=Mathf.Clamp01(flight.elapsed/flight.duration);
            var position=new Vector2(flight.start.x+(flight.end.x-flight.start.x)*t,
                flight.start.y+(flight.end.y-flight.start.y)*t+4f*flight.arc*t*(1f-t));
            if(flight.visual!=null)
            {
                flight.visual.direction=position-flight.visual.position;flight.visual.position=position;
            }
            if(t>=1f)
            {
                if(effects!=null)effects.RemoveBolt(flight.visual);
                magicFlights.RemoveAt(i);Impact(flight.end,true);
            }
            else magicFlights[i]=flight;
        }
    }
    private void DiscardCombatProjectiles(bool keepMagicVisuals,bool clearParticles)
    {
        for(int i=projectiles.Count-1;i>=0;i--)
            if(keepMagicVisuals)RetireProjectile(i);else RemoveProjectile(i);
        if(clearParticles && effects!=null)effects.ClearParticles();
    }
    private void ClearProjectiles(bool clearParticles = true)
    {
        for (int i = projectiles.Count - 1; i >= 0; i--) RemoveProjectile(i);
        if(effects!=null)foreach(var flight in magicFlights)effects.RemoveBolt(flight.visual);
        magicFlights.Clear();
        if (clearParticles && effects != null) effects.Clear();
    }
    public void PanEffects(float amount)
    {
        foreach (var shot in projectiles) { shot.start.x += amount; shot.end.x += amount; }
        for(int i=0;i<magicFlights.Count;i++)
        {
            var flight=magicFlights[i];flight.start.x+=amount;flight.end.x+=amount;magicFlights[i]=flight;
        }
        if (effects != null) effects.Pan(amount);
    }
    internal void SuspendLootEncounter()
    {
        if(lootPresentation!=null)lootPresentation.Clear();
        resumeEncounterAfterEnable=activeMonsters.Count>0;
        lootRewards.Invalidate();battleVersion++;
    }
    void OnDisable()
    {
        SuspendLootEncounter();
        if(heroSfx!=null)heroSfx.StopAll();ClearProjectiles();ResetAllHeroAttacks();
    }
    void OnEnable() {ResumeLootEncounter();}
    internal void ResumeLootEncounter()
    {
        if(!resumeEncounterAfterEnable)return;
        resumeEncounterAfterEnable=false;
        // Resume combat only after explicitly validating every surviving life.
        // Re-enabled actors do not inherit any old receipt/projectile context.
        if(!lootRewards.MatchesOwner(LootRecipient) ||
            !ReferenceEquals(runtimeHeroData,LootRecipient))return;
        foreach(var info in activeMonsters)
            if(info.data==null || info.currentHP<=0 || info.controller==null ||
                !info.controller.IsAlive || info.controller.SpawnVersion!=info.lootKey.Spawn)return;
        battleVersion++; // Also retire any volley emitted while only the owner was disabled.
        lootRewards.BeginEncounter(LootRecipient);
        foreach(var info in activeMonsters)
        {
            info.lootKey=lootRewards.Register(info.lifeId,info.controller.SpawnVersion);
            info.acceptedLethalDamage=false;
        }
    }
    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (lootPresentation != null) lootPresentation.Clear();
        if (effects != null) Destroy(effects.gameObject);
    }

    public void ForceClearAllMonsters() { ClearBattle(false); }
    public void ClearWaveKeepingMagicVisuals() {ClearBattle(false,true);}
    private void ClearBattle(bool preserveHeroAttack,bool keepMagicVisuals=false)
    {
        if(!keepMagicVisuals && lootPresentation!=null)lootPresentation.Clear();
        resumeEncounterAfterEnable=false;
        lootRewards.Invalidate();
        battleVersion++;
        isBattling = false;
        if (!preserveHeroAttack) ResetAllHeroAttacks();
        if(keepMagicVisuals)DiscardCombatProjectiles(true,true);
        else ClearProjectiles(); // Load/Retry/Travel/New Game/menu/disable are hard boundaries.
        foreach (var info in activeMonsters)
        {
            if (info.controller != null) info.controller.MarkDead();
            if (info.go != null) monsterSpawner.DespawnMonster(info.go);
            if (info.data != null) Destroy(info.data);
        }
        activeMonsters.Clear();
    }
}
