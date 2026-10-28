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

    // Legacy serialized compatibility only. Primary basic attacks resolve from
    // equipped template family; this cached mode is never load/combat authority.
    public AttackMode savedAttackMode = AttackMode.Melee;
    public void SetSavedAttackMode(AttackMode mode)
    {
        if (mode != AttackMode.Melee && mode != AttackMode.RangedPhysical &&
            mode != AttackMode.RangedMagic) return;
        if (savedAttackMode == mode) return;
        savedAttackMode = mode;
        isDirty = true;
    }

    [Header("0.0.51 - THUOC TINH / DAC TINH")]
    public int combatStatsVersion = 1;
    public List<string> characterTags = new List<string>();
    public List<CombatStatModifier> combatStatModifiers = new List<CombatStatModifier>();

    [Header("0.0.52 - KHO DO / TRANG BI")]
    public int inventoryVersion = 1;
    // Shared bag for the current run; equipped item leaves the bag.
    public List<InventoryStack> inventory = new List<InventoryStack>();
    public string equippedItemId = "";
    // Old saves can claim one set of sample items from the inventory panel.
    public bool testItemsGranted;

    [Header("0.0.52 - TRANG BI CA THE / NGHE PHU")]
    // Serialized with the same Hero snapshot as level/EXP/map. These are
    // canonical item instances: equipped flag/slot is stored on each item.
    public int equipmentVersion = 0;
    public List<EquipmentInstance> equipmentItems = new List<EquipmentInstance>();
    public long gold;
    public List<ProfessionProgress> professionProgress = new List<ProfessionProgress>();
    public List<LearnedProfessionRecipe> learnedRecipes = new List<LearnedProfessionRecipe>();
    public List<LearnedSupportSkill> learnedSupportSkills = new List<LearnedSupportSkill>();
    public bool demoSwordGranted;

    // Used by save migration and test-created/old characters. Does NOT reroll.
    public void NormalizeCombatStats()
    {
        if (characterTags == null) { characterTags = new List<string>(); isDirty = true; }
        if (combatStatModifiers == null) { combatStatModifiers = new List<CombatStatModifier>(); isDirty = true; }
        if (combatStatsVersion < CombatStats.SchemaVersion)
        { combatStatsVersion = CombatStats.SchemaVersion; isDirty = true; }
    }

    public bool AddCharacterTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return false;
        tag = tag.Trim();
        NormalizeCombatStats();
        if (characterTags.Exists(x => string.Equals(x,tag,System.StringComparison.OrdinalIgnoreCase)))
            return false;
        characterTags.Add(tag);
        isDirty = true;
        return true;
    }

    public bool RemoveCharacterTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return false;
        NormalizeCombatStats();
        int changed = characterTags.RemoveAll(x =>
            string.Equals(x,tag.Trim(),System.StringComparison.OrdinalIgnoreCase));
        if (changed > 0) isDirty = true;
        return changed > 0;
    }

    public void AddCombatModifier(CombatStatModifier modifier)
    {
        if (modifier == null) return;
        NormalizeCombatStats();
        combatStatModifiers.Add(modifier);
        isDirty = true;
    }

    public int RemoveModifiersBySource(string sourceId)
    {
        if (string.IsNullOrEmpty(sourceId)) return 0;
        NormalizeCombatStats();
        int count = combatStatModifiers.RemoveAll(mod => mod != null &&
            string.Equals(mod.sourceId,sourceId,System.StringComparison.Ordinal));
        if (count > 0) isDirty = true;
        return count;
    }
    // Independent per-character base; default/legacy characters start with test HP 400.
    public int originHealth = 0;
    // Persist the growth pool separately from the active stance for save/load stability.
    public int meleeGrowthThroughLevel = 0;
    public int meleeGrowthBonus = 0;
    public bool isDirty = false;

    // Old saves have no version field. Normalize once after every load.
    public int balanceVersion = 0;
    public int mapNumber = 1;
    public int mapVisits = 0; // Total entries; separate from the retry checkpoint.
    public int MapVisits => System.Math.Max(mapNumber, System.Math.Max(1, mapVisits));
    public int completedWavesInMap = 0;
    public int mapProgressVersion = 0;
    public int difficulty = 0; // 0: Bình thường, 1: Khó

    // World Map .54: 0 = pre-.54 legacy sequential run (keeps old saves untouched).
    // Do not infer a world node from the legacy mapNumber.
    public int worldMapVersion = 0;
    public string worldMapId = "";
    public int worldMapWaveProgress = 0; // 0..5 waves credited for first clear
    public int worldMapLoopWave = 1; // current round 1..5 (repeats indefinitely)
    public List<string> worldMapVisited = new List<string>();
    public List<string> worldMapCleared = new List<string>();

    public void NormalizeMapProgress()
    {
        if (WorldMapCatalog.Enabled(this))
        {
            WorldMapCatalog.Normalize(this);
            return;
        }
        if (mapProgressVersion == 0) { mapNumber = 1; completedWavesInMap = 0; mapProgressVersion = 1; isDirty = true; }
        mapNumber = System.Math.Max(1, mapNumber);
        mapVisits = MapVisits;
        completedWavesInMap = System.Math.Max(0, System.Math.Min(4, completedWavesInMap));
        difficulty = mapNumber % 6 == 0 ? 1 : 0;
        if (!System.Enum.IsDefined(typeof(TerrainType), mapTerrain)) { mapTerrain = TerrainType.DongBang; mapName = ""; }
        WorldNames.EnsureRegion(this, !string.IsNullOrEmpty(mapName));
        if (string.IsNullOrEmpty(mapName) || !System.Enum.IsDefined(typeof(TerrainType), mapTerrain)) WorldNames.AssignMap(this);
    }
    public bool CompleteWave()
    {
        if (WorldMapCatalog.Enabled(this)) return WorldMapCatalog.CompleteWave(this);
        NormalizeMapProgress();
        completedWavesInMap++;
        bool changed = completedWavesInMap == 5;
        if (changed) { completedWavesInMap = 0; if (mapNumber < int.MaxValue) mapNumber++; if (mapVisits < int.MaxValue) mapVisits++; WorldNames.AssignMap(this); }
        difficulty = mapNumber % 6 == 0 ? 1 : 0;
        isDirty = true;
        return changed;
    }

    public void RestartRegionAfterDefeat()
    {
        if (WorldMapCatalog.Enabled(this)) { WorldMapCatalog.RestartAfterDefeat(this); return; }
        NormalizeMapProgress();
        mapNumber = ((mapNumber - 1) / 5) * 5 + 1;
        completedWavesInMap = 0;
        if (mapVisits < int.MaxValue) mapVisits++;
        // Keep the region theme; generate a terrain/name in that same theme.
        WorldNames.AssignMap(this);
        difficulty = mapNumber % 6 == 0 ? 1 : 0;
        isDirty = true;
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
        if (originHealth <= 0)
        {
            // Old levelled saves stored a derived baseHealth, not their creation HP.
            originHealth = currentLevel == 1 && baseHealth > 0
                ? baseHealth : CombatBalance.HeroHealth(1);
            isDirty = true;
        }
        if (meleeGrowthThroughLevel <= 0)
        {
            // Previous versions never rolled class growth. Migrate existing levels once
            // with 75 HP per level (no random reroll every load).
            meleeGrowthBonus = System.Math.Max(0,currentLevel-1) * 75;
            meleeGrowthThroughLevel = currentLevel;
            isDirty = true;
        }
        if (meleeGrowthThroughLevel < currentLevel)
        {
            meleeGrowthBonus += (currentLevel-meleeGrowthThroughLevel)*75;
            meleeGrowthThroughLevel = currentLevel;
            isDirty = true;
        }
        if (meleeGrowthThroughLevel > currentLevel) meleeGrowthThroughLevel = currentLevel;
        RebuildHeroStats();
    }

    private void RebuildHeroStats()
    {
        baseHealth = CombatBalance.HeroDev != null ? CombatBalance.HeroHealth(currentLevel)
            : System.Math.Max(1,originHealth + CombatBalance.HeroHealth(currentLevel) - CombatBalance.HeroHealth(1));
        baseDamage = CombatBalance.HeroAttack(currentLevel);
        baseAttackSpeed = CombatBalance.HeroSpeed(currentLevel);
        AddAttackSpeed = baseAttackSpeed - (CombatBalance.HeroDev != null ? CombatBalance.HeroDev.attackSpeed : 1f);
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
            // Exactly one permanent roll per level, independent of selected stance.
            meleeGrowthBonus += Random.Range(50,101);
            meleeGrowthThroughLevel = currentLevel;
            RebuildHeroStats();
            leveledUp = true;
        }
        currentExp = (int)System.Math.Min(total, currentLevel == 999 ? expToNextLevel - 1L : int.MaxValue);
        isDirty = true;
        return leveledUp;
    }

    public void AllocateHealth() { if (statPoints > 0) { statPoints--; addedHealth += 6; isDirty = true; } }
    public void AllocateDamage() { if (statPoints > 0) { statPoints--; addedDamage += 1; isDirty = true; } }
    public int GetCalculatedHealth() => System.Math.Max(1,CombatStats.GetInt(
        this,CombatStatId.MaxHealth,System.Math.Max(1,baseHealth + addedHealth)));
    public int GetCalculatedHealth(AttackMode mode)
    {
        // Stance multiplier applies to original base HP, not bonus points/gear.
        long baseByMode = System.Math.Max(1,Mathf.RoundToInt(baseHealth * CombatBalance.HeroHealthMultiplier(mode)));
        long hp = baseByMode + addedHealth + (mode == AttackMode.Melee ? System.Math.Max(0,meleeGrowthBonus) : 0);
        hp = System.Math.Max(1,System.Math.Min(int.MaxValue,hp));
        return System.Math.Max(1,CombatStats.GetInt(this,CombatStatId.MaxHealth,(int)hp,
            ElementType.Vo,new [] { CombatStats.StanceTag(mode) }));
    }
    public int GetCalculatedDamage() => System.Math.Max(1,
        CombatStats.GetInt(this,CombatStatId.Attack,baseDamage + addedDamage));
    public int GetCalculatedDamage(AttackMode mode) => System.Math.Max(1,
        CombatStats.GetInt(this,CombatStatId.Attack,baseDamage + addedDamage,
            ElementType.Vo,new [] { CombatStats.StanceTag(mode) }));
}
