using TuTienCore;

// B2: transient state for ONE combatant, independent from other Heroes.
// No shared static HP/timers. GameManager still saves only the primary
// Hero; B2's additional actors are registered at runtime, not serialized.
public sealed class HeroCombatState
{
    public HeroController Controller { get; set; }
    public EntityDataSO Data { get; private set; }
    int currentHP;
    bool persistHealth;
    public int CurrentHP
    {
        get => currentHP;
        set
        {
            currentHP = value;
            if (!persistHealth || Data.combatSkills.health == value) return;
            Data.combatSkills.health = value;
            Data.isDirty = true;
        }
    }
    public float CurrentMana => persistHealth ? Data.combatSkills.mana : 0;
    public float MaxMana => persistHealth ? System.Math.Max(0, CombatStats.Get(Data,
        CombatStatId.MaxMana, CombatSkillProgress.BaseMaxMana, ElementType.Vo,
        new[] { CombatStats.StanceTag(SelectedMode) })) : 0;
    public int MaxHP { get; set; }
    // Mutable field is intentional: legacy AdvanceCycle(ref timer) stays exact.
    public float AttackTimer;
    internal string WeaponIdentity;
    // Fractional health regeneration carries across frames, not respawns.
    public float HealthRegenRemainder;
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
    public void BeginLife(EntityDataSO data, int maxHP, bool persistent = false)
    {
        LifeVersion++;
        Data = data;
        MaxHP = maxHP;
        persistHealth = persistent && Data != null;
        if (persistHealth && Data.combatSkills == null) Data.combatSkills = new CombatSkillProgress();
        int storedHP = persistHealth ? Data.combatSkills.health : -1;
        CurrentHP = storedHP < 0 ? maxHP : System.Math.Min(storedHP, maxHP);
        ClampMana();
        AttackTimer = 0f;
        HealthRegenRemainder = 0f;
    }

    internal void ClampMana()
    {
        if (!persistHealth || Data.combatSkills.mana <= MaxMana) return;
        Data.combatSkills.mana = MaxMana;
        Data.isDirty = true;
    }

    public void ResetAttack() { AttackTimer = 0f; }
}
