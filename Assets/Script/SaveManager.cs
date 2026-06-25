using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TuTienCore;

[System.Serializable]
public class PlayerData
{
    public string entityName;
    public int currentLevel;
    public int currentExp;       // Thêm lưu trữ EXP
    public int expToNextLevel;   // Thêm lưu trữ Max EXP
    public int baseHealth;
    public int baseDamage;
    public float baseAttackSpeed;
    public RaceType race;
    public GenderType gender;
    public List<ElementType> spiritRoots;
    
    // Lưu trữ tiến trình cộng điểm
    public int statPoints;
    public int addedHealth;
    public int addedDamage;
}

public class SaveManager : MonoBehaviour
{
    private string saveDirectory;
    private string saveFilePath;

    void Awake()
    {
        // [FIX CHÍ MẠNG] Dùng persistentDataPath để game luôn có quyền Write/Read trên mọi phân vùng Windows
        saveDirectory = Path.Combine(Application.persistentDataPath, "SaveData");
        saveFilePath = Path.Combine(saveDirectory, "player_save.json");
    }

    public void SaveGame(EntityDataSO heroData)
    {
        if (!Directory.Exists(saveDirectory)) Directory.CreateDirectory(saveDirectory);

        PlayerData data = new PlayerData
        {
            entityName = heroData.entityName,
            currentLevel = heroData.currentLevel,
            currentExp = heroData.currentExp,
            expToNextLevel = heroData.expToNextLevel,
            baseHealth = heroData.baseHealth,
            baseDamage = heroData.baseDamage,
            baseAttackSpeed = heroData.baseAttackSpeed,
            race = heroData.race,
            gender = heroData.gender,
            spiritRoots = new List<ElementType>(heroData.spiritRoots),
            statPoints = heroData.statPoints,
            addedHealth = heroData.addedHealth,
            addedDamage = heroData.addedDamage
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(saveFilePath, json);
        
        heroData.isDirty = false; // Reset cờ sau khi lưu thành công
        Debug.Log($"[SAVE] Đã lưu tiến trình tại: {saveFilePath}");
    }

    public bool LoadGame(EntityDataSO heroData)
    {
        if (File.Exists(saveFilePath))
        {
            try
            {
                string json = File.ReadAllText(saveFilePath);
                PlayerData data = JsonUtility.FromJson<PlayerData>(json);

                heroData.entityName = data.entityName;
                heroData.currentLevel = data.currentLevel;
                heroData.currentExp = data.currentExp;
                heroData.expToNextLevel = data.expToNextLevel > 0 ? data.expToNextLevel : 100;
                heroData.baseHealth = data.baseHealth;
                heroData.baseDamage = data.baseDamage;
                heroData.baseAttackSpeed = data.baseAttackSpeed > 0 ? data.baseAttackSpeed : 1.0f;
                heroData.race = data.race;
                heroData.gender = data.gender;
                heroData.spiritRoots = new List<ElementType>(data.spiritRoots);
                
                heroData.statPoints = data.statPoints;
                heroData.addedHealth = data.addedHealth;
                heroData.addedDamage = data.addedDamage;
                
                heroData.isDirty = false;
                return true;
            }
            catch { return false; }
        }
        return false;
    }

    public void DeleteSave()
    {
        if (File.Exists(saveFilePath)) File.Delete(saveFilePath);
    }
}