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
        Data = data;
        MaxHP = maxHP;
        CurrentHP = maxHP;
        AttackTimer = 0f;
    }

    public void ResetAttack() { AttackTimer = 0f; }
}
