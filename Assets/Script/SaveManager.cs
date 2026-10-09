using System;
using System.IO;

using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string SAVE_EXTENSION = ".json";
    private const string TEMP_EXTENSION = ".tmp";
    
    private string saveDirectory;

    private void Awake()
    {
        // Singleton pattern chuẩn
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Khởi tạo thư mục Save
        saveDirectory = Application.persistentDataPath + "/Saves/";
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }
    }

    // =========================================================
    // CÁC HÀM ADAPTER ĐỂ TƯƠNG THÍCH HOÀN TOÀN VỚI GAMEMANAGER CŨ
    // =========================================================

    public bool HasSave(SaveSlot slot)
    {
        string finalPath = Path.Combine(saveDirectory, slot.ToString() + SAVE_EXTENSION);
        return File.Exists(finalPath);
    }

    public void DeleteSave(SaveSlot slot)
    {
        string finalPath = Path.Combine(saveDirectory, slot.ToString() + SAVE_EXTENSION);
        if (File.Exists(finalPath))
        {
            File.Delete(finalPath);
            Debug.Log($"[SaveManager] Đã xóa file save của slot: {slot}");
        }
    }

    // Đổi thành bool để tương thích với GameManager hiện tại
    public bool SaveGame(EntityDataSO entityData, SaveSlot slot)
    {
        if (entityData == null) return false;
        
        // Serialize ScriptableObject thành JSON string ở Main Thread
        string jsonData = JsonUtility.ToJson(entityData, true);
        
        bool saved = WriteToFile(slot.ToString(), jsonData);
        if (saved) entityData.isDirty = false;
        return saved;
    }

    // Đổi thành bool để tương thích với GameManager dòng 54
    public bool LoadGame(EntityDataSO entityData, SaveSlot slot)
    {
        if (entityData == null) return false;

        string jsonData = LoadGame(slot.ToString());
        if (!string.IsNullOrEmpty(jsonData))
        {
            try
            {
                if (!jsonData.TrimStart().StartsWith("{") || !jsonData.Contains("\"currentLevel\"")) return false;
                entityData.balanceVersion = 0;
                entityData.difficulty = 0;
                entityData.mapNumber = 1;
                entityData.completedWavesInMap = 0;
                entityData.mapProgressVersion = 0;
                entityData.regionIndex = -1;
                entityData.regionTheme = TuTienCore.RegionTheme.SonLam;
                entityData.monsterAnimal = "";
                entityData.mapName = "";
                entityData.mapTerrain = TuTienCore.TerrainType.DongBang;
                entityData.rootTiers = new System.Collections.Generic.List<int>();
                entityData.rootWeights = new System.Collections.Generic.List<float>();
                entityData.hybridSecondaryRace = TuTienCore.RaceType.YeuThu;
                JsonUtility.FromJsonOverwrite(jsonData, entityData);
                entityData.NormalizeRoots();
                entityData.NormalizeMapProgress();
                return entityData.currentLevel >= 1;
            }
            catch (Exception e) { Debug.LogError("Không đọc được bản lưu: " + e.Message); return false; }
        }
        else
        {
            Debug.LogWarning($"[SaveManager] Không thể load hoặc file trống ở slot {slot}");
            return false;
        }
    }

    // =========================================================
    // LƯU ĐỒNG BỘ VÀ THAY TỆP QUA TỆP TẠM
    // =========================================================

    public bool TryGetLatestSlot(out SaveSlot slot)
    {
        slot = SaveSlot.AutoSave;
        DateTime latest = DateTime.MinValue;
        bool found = false;
        foreach (SaveSlot candidate in Enum.GetValues(typeof(SaveSlot)))
        {
            string path = Path.Combine(saveDirectory, candidate + SAVE_EXTENSION);
            if (!File.Exists(path)) continue;
            DateTime date = File.GetLastWriteTimeUtc(path);
            if (!found || date > latest) { latest = date; slot = candidate; found = true; }
        }
        return found;
    }

    public bool DeleteAllSaves()
    {
        try
        {
            foreach (SaveSlot slot in Enum.GetValues(typeof(SaveSlot)))
            {
                DeleteSave(slot);
                string temp = Path.Combine(saveDirectory, slot + TEMP_EXTENSION);
                if (File.Exists(temp)) File.Delete(temp);
            }
            return true;
        }
        catch (Exception e) { Debug.LogError("Không xóa được bản lưu: " + e.Message); return false; }
    }

    public void SaveGame(string slotName, string jsonData) { WriteToFile(slotName, jsonData); }

    // Small JSON saves are serialized on the main thread. No queued old write
    // can recreate a deleted save after the player confirms New Game.
    private bool WriteToFile(string slotName, string data)
    {
        string finalPath = Path.Combine(saveDirectory, slotName + SAVE_EXTENSION);
        string tempPath = Path.Combine(saveDirectory, slotName + TEMP_EXTENSION);
        try
        {
            File.WriteAllText(tempPath, data);
            if (File.Exists(finalPath)) File.Replace(tempPath, finalPath, null);
            else File.Move(tempPath, finalPath);
            return true;
        }
        catch (Exception e) { Debug.LogError("Không ghi được bản lưu: " + e.Message); return false; }
    }

    public string LoadGame(string slotName)
    {
        string finalPath = Path.Combine(saveDirectory, slotName + SAVE_EXTENSION);

        if (!File.Exists(finalPath))
        {
            return null;
        }

        try
        {
            return File.ReadAllText(finalPath);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Lỗi đọc file save: {e.Message}");
            return null;
        }
    }
}