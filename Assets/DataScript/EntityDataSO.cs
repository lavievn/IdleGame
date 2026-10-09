using UnityEngine;
using System.Collections.Generic;
using TuTienCore;

[CreateAssetMenu(fileName = "NewEntityData", menuName = "TuTienData/Entity Base")]
public class EntityDataSO : ScriptableObject
{
    public string entityName = "Vô Danh";
    public int currentLevel = 1;
    public int currentExp = 0;
    public int expToNextLevel = 100;
    public int baseHealth = 400;
    public int baseDamage = 10;
    public float baseAttackSpeed = 1.0f;
	public float AddAttackSpeed = 0f;
    public RaceType race = RaceType.NhanToc;
    public GenderType gender;
    public List<ElementType> spiritRoots = new List<ElementType>();

    // Parallel serializable lists, indexed exactly like spiritRoots; old saves default to Tam trọng/equal shares.
    public List<int> rootTiers = new List<int>();
    public List<float> rootWeights = new List<float>();
    public RaceType hybridSecondaryRace = RaceType.YeuThu;
    public string mapName = "";
    public TerrainType mapTerrain = TerrainType.DongBang;

    public int regionIndex = -1;
    public RegionTheme regionTheme = RegionTheme.SonLam;
    public string monsterAnimal = "";
    public MonsterClass monsterClass = MonsterClass.Thu;

    public void NormalizeRoots()
    {
        if (spiritRoots == null) spiritRoots = new List<ElementType>();
        bool changed = false;
        // Preserve non-Vô roots and their matching tiers/weights from old invalid mixed saves.
        if (spiritRoots.Contains(ElementType.Vo) && (spiritRoots.Count > 1 || race == RaceType.ConLai)) {
            for (int i = spiritRoots.Count - 1; i >= 0; i--) if (spiritRoots[i] == ElementType.Vo) {
                if (rootTiers != null && rootTiers.Count == spiritRoots.Count) rootTiers.RemoveAt(i);
                if (rootWeights != null && rootWeights.Count == spiritRoots.Count) rootWeights.RemoveAt(i);
                spiritRoots.RemoveAt(i);
            }
            if (spiritRoots.Count == 0) { spiritRoots.Add(ElementType.Kim); rootTiers = new List<int> { 3 }; rootWeights = new List<float> { 1f }; }
            changed = true;
        }
        if (rootTiers == null || rootTiers.Count != spiritRoots.Count) {
            rootTiers = new List<int>(); foreach (var root in spiritRoots) rootTiers.Add(3); changed = true;
        }
        for (int i = 0; i < rootTiers.Count; i++) {
            int tier = Mathf.Clamp(rootTiers[i], 1, 5);
            if (tier != rootTiers[i]) { rootTiers[i] = tier; changed = true; }
        }
        bool valid = rootWeights != null && rootWeights.Count == spiritRoots.Count;
        float sum = 0f;
        if (valid) foreach (float weight in rootWeights) {
            if (float.IsNaN(weight) || float.IsInfinity(weight) || weight <= 0f) valid = false;
            sum += weight;
        }
        if (!valid || float.IsInfinity(sum) || (spiritRoots.Count > 0 && sum <= 0f)) {
            rootWeights = new List<float>();
            foreach (var root in spiritRoots) rootWeights.Add(1f / spiritRoots.Count);
            changed = true;
        } else if (sum > 0f && Mathf.Abs(sum - 1f) > .00001f) {
            for (int i = 0; i < rootWeights.Count; i++) rootWeights[i] /= sum;
            changed = true;
        }
        if (changed) isDirty = true;
    }

    public int statPoints = 0;
    public int addedHealth = 0;
    public int addedDamage = 0;
    public bool isDirty = false;

    // Old saves have no version field. Normalize once after every load.
    public int balanceVersion = 0;
    public int mapNumber = 1;
    public int completedWavesInMap = 0;
    public int mapProgressVersion = 0;
    public int difficulty = 0; // 0: Bình thường, 1: Khó

    public void NormalizeMapProgress()
    {
        if (mapProgressVersion == 0) { mapNumber = 1; completedWavesInMap = 0; mapProgressVersion = 1; isDirty = true; }
        mapNumber = System.Math.Max(1, mapNumber);
        completedWavesInMap = System.Math.Max(0, System.Math.Min(4, completedWavesInMap));
        difficulty = mapNumber % 6 == 0 ? 1 : 0;
        if (!System.Enum.IsDefined(typeof(TerrainType), mapTerrain)) { mapTerrain = TerrainType.DongBang; mapName = ""; }
        WorldNames.EnsureRegion(this, !string.IsNullOrEmpty(mapName));
        if (string.IsNullOrEmpty(mapName) || !System.Enum.IsDefined(typeof(TerrainType), mapTerrain)) WorldNames.AssignMap(this);
    }
    public bool CompleteWave()
    {
        NormalizeMapProgress();
        completedWavesInMap++;
        bool changed = completedWavesInMap == 5;
        if (changed) { completedWavesInMap = 0; if (mapNumber < int.MaxValue) mapNumber++; WorldNames.AssignMap(this); }
        difficulty = mapNumber % 6 == 0 ? 1 : 0;
        isDirty = true;
        return changed;
    }

    public void ApplyHeroBalance()
    {
        currentLevel = CombatBalance.Level(currentLevel);
        if (balanceVersion < CombatBalance.Version)
        {
            double progress = System.Math.Max(0d, System.Math.Min(0.999999d,
                (double)currentExp / System.Math.Max(1, expToNextLevel)));
            int healthPoints = System.Math.Max(0, addedHealth / 10);
            int damagePoints = System.Math.Max(0, addedDamage / 2);
            int budget = currentLevel - 1;
            long spent = (long)healthPoints + damagePoints;
            if (spent > budget)
            {
                healthPoints = (int)(budget * (double)healthPoints / spent);
                damagePoints = budget - healthPoints;
            }
            addedHealth = healthPoints * 6;
            addedDamage = damagePoints;
            statPoints = System.Math.Max(0, budget - healthPoints - damagePoints);
            currentExp = (int)(progress * CombatBalance.RequiredExp(currentLevel));
            balanceVersion = CombatBalance.Version;
            isDirty = true;
        }
        RebuildHeroStats();
    }

    private void RebuildHeroStats()
    {
        baseHealth = CombatBalance.HeroHealth(currentLevel);
        baseDamage = CombatBalance.HeroAttack(currentLevel);
        baseAttackSpeed = CombatBalance.HeroSpeed(currentLevel);
        AddAttackSpeed = baseAttackSpeed - 1f;
        expToNextLevel = CombatBalance.RequiredExp(currentLevel);
    }

    public bool AddExp(int amount)
    {
        ApplyHeroBalance();
        bool leveledUp = false;
        long total = (long)currentExp + System.Math.Max(0, amount);
        while (currentLevel < 999 && total >= expToNextLevel)
        {
            total -= expToNextLevel;
            currentLevel++;
            statPoints++;
            RebuildHeroStats();
            leveledUp = true;
        }
        currentExp = (int)System.Math.Min(total, currentLevel == 999 ? expToNextLevel - 1L : int.MaxValue);
        isDirty = true;
        return leveledUp;
    }

    public void AllocateHealth() { if (statPoints > 0) { statPoints--; addedHealth += 6; isDirty = true; } }
    public void AllocateDamage() { if (statPoints > 0) { statPoints--; addedDamage += 1; isDirty = true; } }
    public int GetCalculatedHealth() => baseHealth + addedHealth;
    public int GetCalculatedDamage() => baseDamage + addedDamage;
}
