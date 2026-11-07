using System;
using UnityEngine;
using UnityEngine.UI;
using TuTienCore;

public partial class CombatManager
{
    internal const float TrialGateFraming = .35f;
    internal const float TrialGateArtworkHeight = 288f * 3f;
    internal const float TrialGateApproachDistance = 800f;
    internal const float TrialGateArtworkOffset = -105f * (3f / 2.2f); // Receiver stays in the right side of the stone.

    private ActiveMonsterInfo gateTarget;
    private HeavenlyGateConfiguration gateConfig;
    private HeavenlyLightningSchedule gateSchedule;
    private Guid gateEncounter;
    private bool gateEngaged;
    private int gateHeroLife, gateTargetSpawn, gateBattleVersion;
    private EntityDataSO gateHeroData;
    private Func<bool> gateFighting;
    private Action<int,int,double> gateObserve;
    private Action gateInvalidated;

    [SerializeField] private float trialLightningWarningSeconds = .26f;
#if UNITY_5_3_OR_NEWER
    [SerializeField] private HeavenlyTrialView.SkyVariant trialSky;
    private HeavenlyTrialView trialView;
    private HeavenlyLightningVisual committedGateBreak;

    internal Vector2? CaptureGateBreak()
    {
        if (gateTarget == null || gateTarget.currentHP != 0 || trialView == null) return null;
        return trialView.SealBreakPosition;
    }

    internal void ShowCommittedGateBreak(Vector2 point, int sortingOrder)
    {
        var space = EnvironmentManager.Instance;
        if (space == null || space.BattleArea == null) return;
        ClearCommittedGateBreak();
        committedGateBreak = HeavenlyTrialView.CreateLightning(space.BattleArea, "Committed seal break");
        // The entry curtain is opaque during rebinding. Keep this brief flash
        // above that curtain while the trial environment is already gone.
        var canvas = committedGateBreak.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;
        committedGateBreak.BeginCommittedBreak(point);
    }

    private void ClearCommittedGateBreak()
    {
        if (committedGateBreak == null) return;
        committedGateBreak.gameObject.SetActive(false);
        Destroy(committedGateBreak.gameObject);
        committedGateBreak = null;
    }
    internal HeavenlyTrialView TrialView => trialView;
#endif

    public bool HeavenlyGateActive => gateTarget != null;
    internal bool HeavenlyGateApproaching => HeavenlyGateActive && !gateEngaged;
    public int HeavenlyGateHP => gateTarget == null ? 0 : gateTarget.currentHP;
    public int HeavenlyGateMaxHP => gateTarget == null ? 0 : gateTarget.maxHP;
    public string HeavenlyGateName => gateConfig?.title;
    public long HeroReleaseCount { get; private set; }
    public long AcceptedHeroHitCount { get; private set; }
    public long LastImpactRelease { get; private set; }
    private long impactRelease;

    internal long TrialStrikeCount { get; private set; }
    internal GateStrike LastTrialStrike { get; private set; }
    internal bool TrialWarning => gateSchedule != null && gateSchedule.IsWarning;
    private bool CanRunGateCombat => gateFighting != null && gateFighting();

    internal struct GateStrike
    {
        internal Guid Encounter;
        internal long Strike, TargetLife;
        internal int TargetSpawn, HeroLife, BattleVersion, HPBefore, HPAfter;
    }

    internal void BeginHeavenlyGate(HeavenlyGateConfiguration config, Guid encounter,
        Func<bool> fighting, Action<int,int,double> observe, Action invalidated)
    {
        if (config == null || !config.IsValid || encounter == Guid.Empty ||
            !primaryHeroState.CanFight || heroStates.Count != 1)
            throw new InvalidOperationException("One living canonical Hero and explicit gate required");

        ClearBattle(false);
        gateFighting = fighting;
        gateObserve = observe;
        gateInvalidated = invalidated;
        gateConfig = config.Copy();
        gateEncounter = encounter;
        gateHeroLife = primaryHeroState.LifeVersion;
        gateHeroData = primaryHeroState.Data;
        gateBattleVersion = battleVersion;
        gateSchedule = new HeavenlyLightningSchedule(config.strikeInterval, trialLightningWarningSeconds);

        var space = EnvironmentManager.Instance;
        var heroRect = primaryHeroState.Controller.heroRect;
        var foot = space.Position(heroRect);
        var area = space.BattleArea.rect;
        float receiverX = area.xMin + area.width * TrialGateFraming - TrialGateArtworkOffset;
        float reach = Mathf.Min(space.AttackRange(primaryHeroState.SelectedMode, true), (float)config.targetRange);
        foot.x = receiverX + reach + TrialGateApproachDistance;
        space.SetPosition(heroRect, foot);
        var go = new GameObject("Heavenly Gate target", typeof(RectTransform), typeof(Image));
        go.SetActive(false);
        go.transform.SetParent(space.BattleArea,false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(.5f, 0);
        rect.sizeDelta = new Vector2(116, 288);
        var image = go.GetComponent<Image>();
        image.color = new Color(0,0,0,0);
        image.raycastTarget = false;
        var controller = go.AddComponent<MonsterController>();
        controller.IsHeavenlyGate = true;
        controller.HeavenlyGateRange = (float)config.targetRange;
        space.SetPosition(rect, new Vector2(receiverX, foot.y));

        var data = ScriptableObject.CreateInstance<EntityDataSO>();
        data.entityName = config.title;
        data.baseHealth = config.hp;
        data.baseDamage = config.damagePerStrike;
        data.currentLevel = 1;
        data.combatStatModifiers = new System.Collections.Generic.List<CombatStatModifier>
        {
            new CombatStatModifier { sourceId=config.id, stat=CombatStatId.Armor, flat=(float)config.armor },
            new CombatStatModifier { sourceId=config.id, stat=CombatStatId.MagicResistance, flat=(float)config.magicResistance }
        };
        go.SetActive(true);
        controller.attackMode = AttackMode.Melee;
        gateTargetSpawn = controller.SpawnVersion;
        gateTarget = new ActiveMonsterInfo
        {
            go=go, controller=controller, data=data, maxHP=config.hp,
            currentHP=config.hp, lifeId=++monsterLifeSequence
        };
        activeMonsters.Add(gateTarget);
        isBattling = true;
        primaryHeroState.AttackTimer = WeaponBasicAttack.Interval(primaryHeroState.SelectedMode,
            primaryHeroState.Data.baseAttackSpeed);
#if UNITY_5_3_OR_NEWER
        trialView = HeavenlyTrialView.Install(space, foot.y, trialSky);
        trialView.AttachSeal(rect);
#endif
    }

    private bool ValidateGateLives()
    {
        if (primaryHeroState.Data == gateHeroData && primaryHeroState.LifeVersion == gateHeroLife &&
            primaryHeroState.Controller != null && gateTarget.controller != null &&
            gateTarget.controller.IsAlive && gateTarget.controller.SpawnVersion == gateTargetSpawn &&
            battleVersion == gateBattleVersion) return true;

        gateInvalidated?.Invoke();
        ClearBattle(false);
        return false;
    }

    private void TickHeavenlyGate(EnvironmentManager space, float dt)
    {
        if (!CanRunGateCombat || !ValidateGateLives()) return;
#if UNITY_5_3_OR_NEWER
        var lightning = trialView != null ? trialView.Lightning : null;
        if (lightning != null) lightning.Advance(dt);
#endif
        if (primaryHeroState.CanFight)
        {
            if (!gateEngaged)
                gateEngaged = primaryHeroState.Controller.CanAttack(gateTarget.controller, space);
        }
        if (gateEngaged && primaryHeroState.CanFight)
        {
            var action = gateSchedule.Advance(dt);
#if UNITY_5_3_OR_NEWER
            var heroRect = primaryHeroState.Controller.heroRect;
            if (lightning != null && gateSchedule.IsWarning && gateTarget.currentHP > 0)
                lightning.Warn(space.Position(heroRect));
#endif
            // Both sides resolve in one combat frame. An already due impact may
            // double KO the seal and Hero; the transaction resolves it as failure.
            if (action == HeavenlyLightningSchedule.Event.Impact)
            {
                var strike = new GateStrike
                {
                    Encounter=gateEncounter, Strike=gateSchedule.StrikeId, TargetLife=gateTarget.lifeId,
                    TargetSpawn=gateTargetSpawn, HeroLife=gateHeroLife, BattleVersion=gateBattleVersion,
                    HPBefore=primaryHeroState.CurrentHP
                };
#if UNITY_5_3_OR_NEWER
                if (lightning != null)
                    lightning.Strike(BodyPosition(heroRect,space),space.Position(heroRect),trialView.SkyTop,strike.Strike);
#endif
                var trace = SynergyMath.EvaluateDamage(gateTarget.data,primaryHeroState.Data,AttackMode.Melee,1f,1f);
                ApplyDamageToHeroState(primaryHeroState,trace.elementDamage,gateConfig.title,trace,null,gateTarget.data,AttackMode.Melee);
                strike.HPAfter = primaryHeroState.CurrentHP;
                LastTrialStrike = strike;
                TrialStrikeCount++;
            }
        }
        gateObserve?.Invoke(primaryHeroState.CurrentHP, gateTarget.currentHP, dt);
    }

    private void ShowGateHit(bool lethal)
    {
#if UNITY_5_3_OR_NEWER
        if (trialView == null) return;
        trialView.HitSeal(lethal);
        if (lethal && CaptureGateBreak() is Vector2 point)
            trialView.Lightning.BreakSeal(point);
#endif
    }

    private void ClearGateIdentity()
    {
#if UNITY_5_3_OR_NEWER
        if (trialView != null)
        {
            trialView.Release();
            Destroy(trialView.gameObject);
            trialView = null;
        }
#endif
        gateTarget = null;
        gateConfig = null;
        gateHeroData = null;
        gateSchedule = null;
        gateEngaged = false;
        gateEncounter = Guid.Empty;
        gateFighting = null;
        gateObserve = null;
        gateInvalidated = null;
    }
}
