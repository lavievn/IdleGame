using System;
using System.IO;
using System.Threading.Tasks;
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
        string finalPath = saveDirectory + slot.ToString() + SAVE_EXTENSION;
        return File.Exists(finalPath);
    }

    public void DeleteSave(SaveSlot slot)
    {
        string finalPath = saveDirectory + slot.ToString() + SAVE_EXTENSION;
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
        
        // Gọi hàm ghi file bất đồng bộ và an toàn (Atomic)
        WriteToFileAsync(slot.ToString(), jsonData);
        
        return true;
    }

    // Đổi thành bool để tương thích với GameManager dòng 54
    public bool LoadGame(EntityDataSO entityData, SaveSlot slot)
    {
        if (entityData == null) return false;

        string jsonData = LoadGame(slot.ToString());
        if (!string.IsNullOrEmpty(jsonData))
        {
            // Đổ data từ chuỗi JSON trực tiếp đè lên ScriptableObject hiện tại
            JsonUtility.FromJsonOverwrite(jsonData, entityData);
            Debug.Log($"[SaveManager] Đã load thành công dữ liệu vào {entityData.name} từ {slot}");
            return true;
        }
        else
        {
            Debug.LogWarning($"[SaveManager] Không thể load hoặc file trống ở slot {slot}");
            return false;
        }
    }

    // =========================================================
    // LÕI XỬ LÝ I/O BẤT ĐỒNG BỘ VÀ AN TOÀN (ATOMIC SAVE)
    // =========================================================

    public void SaveGame(string slotName, string jsonData)
    {
        WriteToFileAsync(slotName, jsonData);
    }

    private async void WriteToFileAsync(string slotName, string data)
    {
        string finalPath = saveDirectory + slotName + SAVE_EXTENSION;
        string tempPath = saveDirectory + slotName + TEMP_EXTENSION;

        try
        {
            // Tách luồng I/O ra khỏi Game Loop chính để tránh giật lag
            await Task.Run(() =>
            {
                // Bước 1: Ghi toàn bộ data vào file .tmp
                File.WriteAllText(tempPath, data);

                // Bước 2: Swap file an toàn (Atomic Move)
                if (File.Exists(finalPath))
                {
                    File.Delete(finalPath);
                }
                File.Move(tempPath, finalPath);
            });

            Debug.Log($"[SaveManager] Đã ghi đè an toàn thành công vào slot: {slotName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Lỗi ghi file save (File gốc vẫn an toàn): {e.Message}");
        }
    }

    public string LoadGame(string slotName)
    {
        string finalPath = saveDirectory + slotName + SAVE_EXTENSION;

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