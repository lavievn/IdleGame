using TuTienCore;

// B2: transient state for ONE combatant, independent from other Heroes.
// No shared static HP/timers. GameManager still saves only the primary
// Hero; B2's additional actors are registered at runtime, not serialized.
public sealed class HeroCombatState
{
    public HeroController Controller { get; set; }
    public EntityDataSO Data { get; private set; }
    public int CurrentHP { get; set; }
    public int MaxHP { get; set; }
    // Skills and mana are independent of the basic AttackMode and AttackTimer.
    // Unconfigured by default, so B2's current single-Hero gameplay is untouched.
    public HeroSkillRuntime Skills { get; } = new HeroSkillRuntime();
    public HitControlState HitControl { get; } = new HitControlState();
    // Zero by default. Later an equipped weapon/passive may supply this
    // independently of the Hero's normal AttackMode.
    public HitControlEffect BasicAttackControl { get; set; }
    public bool IsDamageImmune => Skills.IsDamageImmune;
    public bool IsControlImmune => Skills.IsControlImmune;
    // Mutable field is intentional: legacy AdvanceCycle(ref timer) stays exact.
    public float AttackTimer;
    public AttackMode WindupMode { get; set; }
    // Changes whenever this state starts a new life. Old projectiles cannot
    // hit a respawned actor just because the Controller object was reused.
    public int LifeVersion { get; private set; }
    public bool CanFight => Controller != null && Controller.IsDeployed &&
        Data != null && CurrentHP > 0;
    public AttackMode SelectedMode =>
        Controller != null ? Controller.attackMode : WindupMode;

    // Rebinding an actor to newly loaded/created hero data starts a fresh life.
    // The selected attack mode remains on HeroController, as in .54g.
    public void BeginLife(EntityDataSO data, int maxHP)
    {
        LifeVersion++;
        Skills.ResetTransientForNewLife(); // Cancel protection and cooldowns from the old life.
        HitControl.Reset();
        // Runtime weapon/passive effects are never copied into a new
        // character life implicitly; future equipment restore must reapply them.
        BasicAttackControl=default(HitControlEffect);
        Data = data;
        MaxHP = maxHP;
        CurrentHP = maxHP;
        AttackTimer = 0f;
    }

    public void ResetAttack() { AttackTimer = 0f; }
}
