using System;
using TuTienCore;

// Shared by runtime and headless balance simulations.
public static class CombatBalance
{
    public const int Version = 1;
    public static int Level(int level) { return Math.Max(1, Math.Min(999, level)); }
    public static int RequiredExp(int level)
    {
        // Integer storage safety at extremely high levels, never wrap negative.
        return (int)Math.Min(2000000000d, Math.Ceiling(100d * Math.Pow(1.15d, Level(level) - 1)));
    }
    public static int HeroHealth(int level) { return 400 + 16 * (Level(level) - 1); }
    public static int HeroAttack(int level) { return 10 + Level(level) - 1; }
    public static float HeroSpeed(int level) { return 1f + Math.Min(0.25f, 0.005f * (Level(level) - 1)); }
    public static int MonsterHealth(int level) { return 55 + 6 * (Level(level) - 1); }
    public static int MonsterAttack(int level) { return 6 + (int)Math.Ceiling(0.6d * (Level(level) - 1)); }
    public static int LevelSpread(int level) { return Math.Max(2, (int)Math.Ceiling(Level(level) * 0.1d)); }
    public static int KillExp(int monsterLevel) { return 12 + 2 * Level(monsterLevel); }
    public static float AttackInterval(AttackMode mode, float speed)
    {
        float seconds = mode == AttackMode.Melee ? 1.4f : mode == AttackMode.RangedPhysical ? 0.7f : 2f;
        // A corrupted/legacy speed must not restore exponential attack growth.
        return seconds / Math.Max(0.1f, Math.Min(1.25f, speed));
    }
    public static int Damage(int attack, AttackMode mode, float roll)
    {
        float multiplier = mode == AttackMode.RangedMagic ? 1.8f : 1f;
        return Math.Max(1, (int)Math.Round(Math.Max(1, attack) * multiplier * Math.Max(0.85f, Math.Min(1f, roll))));
    }
}
