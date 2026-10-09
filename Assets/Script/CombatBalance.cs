using System;
using TuTienCore;
using UnityEngine;
using System.IO;

// Shared by runtime and headless balance simulations.
public static class CombatBalance
{
    public const int Version = 1;
    public static DevBalanceProfile HeroDev { get; private set; }
    public static DevBalanceProfile MonsterDev { get; private set; }
    public static string DevSettingsPath => Path.Combine(Application.persistentDataPath, "dev_balance.jsonl");
    public static void ResetDevProfiles() { HeroDev = null; MonsterDev = null; }
    public static void SetDevProfile(bool monster, DevBalanceProfile profile)
    {
        if (monster) MonsterDev = profile; else HeroDev = profile;
    }
    public static bool SaveDevProfiles()
    {
        try {
            Directory.CreateDirectory(Application.persistentDataPath);
            string path = DevSettingsPath, temporary = path + ".tmp";
            File.WriteAllText(temporary, (HeroDev == null ? "none" : JsonUtility.ToJson(HeroDev, false)) + "\n" +
                (MonsterDev == null ? "none" : JsonUtility.ToJson(MonsterDev, false)));
            if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
            return true;
        } catch (Exception e) { Debug.LogWarning("Không lưu được DEVB: " + e.Message); return false; }
    }
    public static void LoadDevProfiles()
    {
        ResetDevProfiles();
        if (!File.Exists(DevSettingsPath)) return;
        try {
            string[] lines = File.ReadAllLines(DevSettingsPath);
            if (lines.Length != 2) throw new FormatException("Thiếu cấu hình DEVB");
            DevBalanceProfile[] profiles = new DevBalanceProfile[2];
            for (int i = 0; i < 2; i++) {
                if (lines[i] == "none") continue;
                profiles[i] = new DevBalanceProfile(); JsonUtility.FromJsonOverwrite(lines[i], profiles[i]);
                if (!profiles[i].IsValid()) throw new FormatException("Thông số DEVB không hợp lệ");
            }
            HeroDev = profiles[0]; MonsterDev = profiles[1];
        } catch (Exception e) { Debug.LogWarning("Bỏ qua cấu hình DEVB hỏng: " + e.Message); }
    }
    public static int Level(int level) { return Math.Max(1, Math.Min(999, level)); }
    public static int RequiredExp(int level)
    {
        // Integer storage safety at extremely high levels, never wrap negative.
        return (int)Math.Min(2000000000d, Math.Ceiling(100d * Math.Pow(1.15d, Level(level) - 1)));
    }
    public static int HeroHealth(int level) { return HeroDev != null ? HeroDev.HealthAt(level) : 400 + 16 * (Level(level) - 1); }
    public static int HeroAttack(int level) { return HeroDev != null ? HeroDev.AttackAt(level) : 10 + Level(level) - 1; }
    public static float HeroSpeed(int level) { return HeroDev != null ? HeroDev.SpeedAt(level) : 1f + Math.Min(0.25f, 0.005f * (Level(level) - 1)); }
    public static int MonsterHealth(int level) { return MonsterDev != null ? MonsterDev.HealthAt(level) : 55 + 6 * (Level(level) - 1); }
    public static int MonsterAttack(int level) { return MonsterDev != null ? MonsterDev.AttackAt(level) : 6 + (int)Math.Ceiling(0.6d * (Level(level) - 1)); }
    public static float MonsterSpeed(int level) { return MonsterDev != null ? MonsterDev.SpeedAt(level) : 1f + Math.Min(.25f, .005f * (Level(level) - 1)); }
    public static int LevelSpread(int level) { return Math.Max(2, (int)Math.Ceiling(Level(level) * 0.1d)); }
    public static int KillExp(int monsterLevel) { return 12 + 2 * Level(monsterLevel); }
    public static float AttackInterval(AttackMode mode, float speed, bool devOverride = false)
    {
        float seconds = mode == AttackMode.Melee ? 1.4f : mode == AttackMode.RangedPhysical ? 0.7f : 2f;
        // A corrupted/legacy speed must not restore exponential attack growth.
        return seconds / Math.Max(0.1f, Math.Min(devOverride ? 20f : 1.25f, speed));
    }
    public static float ModeMultiplier(AttackMode mode, float physicalFactor = .8f)
    {
        return mode == AttackMode.RangedMagic ? 2.5f : mode == AttackMode.RangedPhysical ? Math.Max(.55f, Math.Min(.8f, physicalFactor)) : 1f;
    }
    public static int Damage(int attack, AttackMode mode, float roll, float physicalFactor = .8f)
    {
        float multiplier = ModeMultiplier(mode, physicalFactor);
        return Math.Max(1, (int)Math.Round(Math.Max(1, attack) * multiplier * Math.Max(0.85f, Math.Min(1f, roll))));
    }
}


[Serializable]
public sealed class DevBalanceProfile
{
    public int version = 1;
    public float attack, attackPerLevel, attackSpeed, attackSpeedPerLevel;
    public float movementSpeed, health, healthPerLevel;
    public float meleeRange, physicalRange, magicRange;
    public DevBalanceProfile Copy() { return (DevBalanceProfile)MemberwiseClone(); }
    public int AttackAt(int level) { return Math.Max(1, (int)Math.Min(1000000d,
        Math.Ceiling(Math.Round(attack + attackPerLevel * (CombatBalance.Level(level)-1), 4)))); }
    public int HealthAt(int level) { return Math.Max(1, (int)Math.Min(10000000d,
        Math.Round(health + healthPerLevel * (CombatBalance.Level(level)-1)))); }
    public float SpeedAt(int level) { return Math.Max(.1f, Math.Min(20f, attackSpeed + attackSpeedPerLevel * (CombatBalance.Level(level)-1))); }
    public float Range(AttackMode mode) { return mode == AttackMode.Melee ? meleeRange : mode == AttackMode.RangedPhysical ? physicalRange : magicRange; }
    public void SetRange(AttackMode mode, float value) { if (mode == AttackMode.Melee) meleeRange = value; else if (mode == AttackMode.RangedPhysical) physicalRange = value; else magicRange = value; }
    public bool IsValid()
    {
        return version == 1 && Valid(attack,1,100000) && Valid(attackPerLevel,0,10000) && Valid(attackSpeed,.1f,20) &&
            Valid(attackSpeedPerLevel,0,2) && Valid(movementSpeed,0,20000) && Valid(health,1,1000000) && Valid(healthPerLevel,0,100000) &&
            Valid(meleeRange,1,20000) && Valid(physicalRange,1,20000) && Valid(magicRange,1,20000);
    }
    private static bool Valid(float value, float min, float max) { return !float.IsNaN(value) && !float.IsInfinity(value) && value >= min && value <= max; }
    public static DevBalanceProfile Defaults(bool monster, EnvironmentManager space, float movement)
    {
        return new DevBalanceProfile { attack = monster ? 6 : 10, attackPerLevel = monster ? .6f : 1,
            attackSpeed = 1, attackSpeedPerLevel = .005f, movementSpeed = movement, health = monster ? 55 : 400,
            healthPerLevel = monster ? 6 : 16, meleeRange = space != null ? space.DefaultAttackRange(AttackMode.Melee,!monster) : 35,
            physicalRange = space != null ? space.DefaultAttackRange(AttackMode.RangedPhysical,!monster) : monster ? 280 : 360,
            magicRange = space != null ? space.DefaultAttackRange(AttackMode.RangedMagic,!monster) : monster ? 360 : 420 };
    }
}
