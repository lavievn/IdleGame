using UnityEngine;
using System.Collections.Generic;
using TuTienCore; 

[CreateAssetMenu(fileName = "NewEntityData", menuName = "TuTienData/Entity Base")]
public class EntityDataSO : ScriptableObject
{
    public string entityName = "Vô Danh";
    public int currentLevel = 1;
    public int currentExp = 0;
    public int expToNextLevel = 50; 
    public int baseHealth = 100;
    public int baseDamage = 10;
    public float baseAttackSpeed = 1.0f;
    public RaceType race = RaceType.NhanToc;
    public GenderType gender;
    public List<ElementType> spiritRoots = new List<ElementType>();

    public int statPoints = 0;
    public int addedHealth = 0;
    public int addedDamage = 0;
    public bool isDirty = false; 

    public bool AddExp(int amount)
    {
        bool leveledUp = false;
        currentExp += amount;
        while (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel;
            currentLevel++;
            expToNextLevel = currentLevel * 50; 
            baseHealth += 20; baseDamage += 5; statPoints += 2;
            leveledUp = true; 
        }
        isDirty = true; return leveledUp; 
    }

    public void AllocateHealth() { if (statPoints > 0) { statPoints--; addedHealth += 10; isDirty = true; } }
    public void AllocateDamage() { if (statPoints > 0) { statPoints--; addedDamage += 2; isDirty = true; } }
    public int GetCalculatedHealth() => baseHealth + addedHealth;
    public int GetCalculatedDamage() => baseDamage + addedDamage;
}