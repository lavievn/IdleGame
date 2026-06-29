using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TuTienCore;

public enum SaveSlot { AutoSave, ManualSave1, ManualSave2 }

[System.Serializable]
public class PlayerData
{
    public string entityName;
    public int currentLevel;
    public int currentExp;       
    public int expToNextLevel;   
    public int baseHealth;
    public int baseDamage;
    public float baseAttackSpeed;
    public RaceType race;
    public GenderType gender;
    public List<ElementType> spiritRoots;
    public int statPoints;
    public int addedHealth;
    public int addedDamage;
}

public class SaveManager : MonoBehaviour
{
    private string saveDirectory;

    void Awake()
    {
        saveDirectory = Path.Combine(Application.persistentDataPath, "SaveData");
    }

    private string GetFilePath(SaveSlot slot)
    {
        string fileName = "autosave.json";
        if (slot == SaveSlot.ManualSave1) fileName = "manualsave1.json";
        else if (slot == SaveSlot.ManualSave2) fileName = "manualsave2.json";
        return Path.Combine(saveDirectory, fileName);
    }

    public void SaveGame(EntityDataSO heroData, SaveSlot slot = SaveSlot.AutoSave)
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
        File.WriteAllText(GetFilePath(slot), json);
        
        heroData.isDirty = false; 
    }

    public bool LoadGame(EntityDataSO heroData, SaveSlot slot = SaveSlot.AutoSave)
    {
        string path = GetFilePath(slot);
        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
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

    public bool HasSave(SaveSlot slot)
    {
        return File.Exists(GetFilePath(slot));
    }

    // Tiện ích để UI hiển thị thông tin File Save
    public string GetSaveDetails(SaveSlot slot)
    {
        string path = GetFilePath(slot);
        if (!File.Exists(path)) return "Trống";
        try
        {
            string json = File.ReadAllText(path);
            PlayerData data = JsonUtility.FromJson<PlayerData>(json);
            return $"Lv.{data.currentLevel} - {data.entityName}";
        }
        catch { return "Lỗi Dữ Liệu"; }
    }

    public void DeleteSave(SaveSlot slot = SaveSlot.AutoSave)
    {
        string path = GetFilePath(slot);
        if (File.Exists(path)) File.Delete(path);
    }
}