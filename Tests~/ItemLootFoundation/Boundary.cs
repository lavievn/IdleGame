#if ITEM_LOOT_FOUNDATION_TESTS
using System;
// CombatBalance's unrelated DevBalanceProfile signature needs this type. No
// tested Item/Loot/Inventory/Forge/stat/serialization code is replaced here.
// Crossing into environment presentation fails instead of simulating success.
public sealed class EnvironmentManager
{
    public float DefaultAttackRange(TuTienCore.AttackMode mode,bool hero)
    {
        throw new NotSupportedException("Environment presentation is outside data-only item tests.");
    }
}
#endif
