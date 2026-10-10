using TuTienCore;

// B1: transient combat state owned by ONE Hero. This is not save data.
// Do not use static HP/timers or put shared mutable defaults here.
// GameManager still deploys one Hero; B2 will route per-actor target/damage.
public sealed class HeroCombatState
{
    public HeroController Controller { get; set; }
    public EntityDataSO Data { get; private set; }
    public int CurrentHP { get; set; }
    public int MaxHP { get; set; }
    // Mutable field is intentional: legacy AdvanceCycle(ref timer) stays exact.
    public float AttackTimer;
    public AttackMode WindupMode { get; set; }
    public AttackMode SelectedMode =>
        Controller != null ? Controller.attackMode : WindupMode;

    // Rebinding an actor to newly loaded/created hero data starts a fresh life.
    // The selected attack mode remains on HeroController, as in .54g.
    public void BeginLife(EntityDataSO data, int maxHP)
    {
        Data = data;
        MaxHP = maxHP;
        CurrentHP = maxHP;
        AttackTimer = 0f;
    }

    public void ResetAttack() { AttackTimer = 0f; }
}
