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

    public int statPoints = 0;
    public int addedHealth = 0;
    public int addedDamage = 0;
    public bool isDirty = false;

    // Old saves have no version field. Normalize once after every load.
    public int balanceVersion = 0;
    public int difficulty = 0; // 0: Bình thường, 1: Khó

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
