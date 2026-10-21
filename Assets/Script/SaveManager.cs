using System;
using System.IO;
using System.Text.RegularExpressions;

using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string SAVE_EXTENSION = ".json";
    private const string TEMP_EXTENSION = ".tmp";
    
    private string saveDirectory;
    private bool customSaveDirectoryRequested;
    private bool awakeStarted;

    public string SaveDirectory => saveDirectory;

    // Call on an inactive GameObject, before Awake. Path validation only: no I/O.
    public void ConfigureSaveDirectoryBeforeAwake(string directory)
    {
        if (awakeStarted)
            throw new InvalidOperationException("The save directory is locked once Awake has started.");

        // An invalid explicit request must never fall back to the player directory,
        // even if the caller catches the validation exception and activates us.
        customSaveDirectoryRequested = true;
        saveDirectory = null;
        if (string.IsNullOrWhiteSpace(directory) || !Path.IsPathRooted(directory) ||
            directory.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            throw new ArgumentException("An absolute save-directory path is required.", nameof(directory));
        string absolute = Path.GetFullPath(directory);
        // Windows drive-relative paths (C:folder or \folder) are rooted but
        // still depend on the current drive/directory; do not accept them.
        if (!string.Equals(Path.GetPathRoot(directory), Path.GetPathRoot(absolute),
                StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("An absolute save-directory path is required.", nameof(directory));
        saveDirectory = absolute;
    }

    private void Awake()
    {
        awakeStarted = true;
        // Singleton pattern chuẩn
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        if (customSaveDirectoryRequested)
        {
            if (string.IsNullOrEmpty(saveDirectory))
                throw new InvalidOperationException("The explicitly configured save directory is invalid.");
        }
        else
        {
            // Preserve the existing gameplay default exactly.
            saveDirectory = Application.persistentDataPath + "/Saves/";
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Khởi tạo thư mục Save (custom or default, chosen before any I/O).
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
        string reason;
        if (!EquipmentFoundation.TryValidateSaveVersions(entityData, out reason))
        {
            Debug.LogWarning("[SaveManager] Refusing save: " + reason);
            return false;
        }
        
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
                string reason;
                if (!TryValidateEquipmentJson(jsonData, out reason))
                {
                    Debug.LogWarning("[SaveManager] Refusing load from " + slot + ": " + reason);
                    return false;
                }
                // JsonUtility.FromJsonOverwrite leaves template values intact for
                // missing fields. Clear 0.0.51 lists before loading 0.0.50 saves.
                // Old saves lack a stance field; do not inherit the
                // template asset's current Inspector stance by accident.
                entityData.savedAttackMode = TuTienCore.AttackMode.Melee;
                entityData.inventoryVersion = 0;
                entityData.inventory = new System.Collections.Generic.List<InventoryStack>();
                entityData.equippedItemId = "";
                entityData.testItemsGranted = false;
                // Never inherit a template's instances/recipes/professions when
                // loading 0.0.51 saves without these new serialized fields.
                entityData.equipmentVersion = 0;
                entityData.equipmentItems = new System.Collections.Generic.List<EquipmentInstance>();
                entityData.professionProgress = new System.Collections.Generic.List<ProfessionProgress>();
                entityData.learnedRecipes = new System.Collections.Generic.List<LearnedProfessionRecipe>();
                entityData.learnedSupportSkills = new System.Collections.Generic.List<LearnedSupportSkill>();
                entityData.gold = 0L;
                entityData.demoSwordGranted = false;
                entityData.combatStatsVersion = 0;
                entityData.characterTags = new System.Collections.Generic.List<string>();
                entityData.combatStatModifiers = new System.Collections.Generic.List<CombatStatModifier>();
                entityData.balanceVersion = 0;
                entityData.originHealth = 0;
                entityData.meleeGrowthBonus = 0;
                entityData.meleeGrowthThroughLevel = 0;
                entityData.difficulty = 0;
                entityData.mapNumber = 1;
                entityData.mapVisits = 0;
                entityData.completedWavesInMap = 0;
                entityData.mapProgressVersion = 0;
                // Clear the current ScriptableObject template values first. Legacy
                // JSON has none of these fields and must remain on legacy maps.
                entityData.worldMapVersion = 0;
                entityData.worldMapId = "";
                entityData.worldMapWaveProgress = 0;
                entityData.worldMapLoopWave = 1;
                entityData.worldMapVisited = new System.Collections.Generic.List<string>();
                entityData.worldMapCleared = new System.Collections.Generic.List<string>();
                entityData.regionIndex = -1;
                entityData.regionTheme = TuTienCore.RegionTheme.SonLam;
                entityData.monsterAnimal = "";
                entityData.mapName = "";
                entityData.mapTerrain = TuTienCore.TerrainType.DongBang;
                entityData.rootTiers = new System.Collections.Generic.List<int>();
                entityData.rootWeights = new System.Collections.Generic.List<float>();
                entityData.hybridSecondaryRace = TuTienCore.RaceType.YeuThu;
                JsonUtility.FromJsonOverwrite(jsonData, entityData);
                // A partial load of only inventory/equipment must NEVER count
                // as a successful progress rollback. Verify ALL present
                // authoritative progression fields before normalization.
                if (!MatchesSavedProgress(jsonData,entityData))
                {
                    Debug.LogError("[SaveManager] Incomplete progress restore from " +
                        slot + "; refusing to resume a mixed-era character.");
                    return false;
                }
                entityData.NormalizeRoots();
                if (entityData.worldMapVersion > 0)
                {
                    TuTienCore.WorldMapNode savedNode;
                    if (!TuTienCore.WorldMapCatalog.TryGet(entityData.worldMapId, out savedNode))
                    {
                        Debug.LogError("[SaveManager] Unknown world-map node in save: " + entityData.worldMapId);
                        return false; // No silent reroll/migration for corrupted world saves.
                    }
                }
                entityData.NormalizeMapProgress();
                entityData.NormalizeCombatStats();
                EquipmentInventory.Normalize(entityData);
                // Both systems own distinct prefixes so normalization never
                // replays gear onto base stats or wipes trait modifiers.
                EquipmentRuntime.Normalize(entityData);
                // Corrupt/future enum values never pass to a HeroController.
                if (entityData.savedAttackMode != TuTienCore.AttackMode.Melee &&
                    entityData.savedAttackMode != TuTienCore.AttackMode.RangedPhysical &&
                    entityData.savedAttackMode != TuTienCore.AttackMode.RangedMagic)
                {
                    entityData.savedAttackMode = TuTienCore.AttackMode.Melee;
                    entityData.isDirty = true;
                }
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

    // A slot preview uses values from the actual JSON file, not the running
    // Hero or an Inspector template. This helps distinguish an older manual
    // snapshot from the more recent autosave.
    public bool TryGetSlotProgress(SaveSlot slot, out int level, out int map)
    {
        level = 0; map = 0;
        string json = LoadGame(slot.ToString());
        if (string.IsNullOrEmpty(json) ||
            !TryReadInteger(json,"currentLevel",out level) || level < 1)
            return false;
        if (!TryReadInteger(json,"mapNumber",out map)) map = 1;
        return true;
    }

    private static bool TryReadInteger(string json, string field, out int value)
    {
        value = 0;
        // Exact JSON property match; never confuse currentLevel with a
        // modifier field. Unity serializes these progression fields as ints.
        var match = Regex.Match(json,"\\\"" + Regex.Escape(field) +
            "\\\"\\s*:\\s*(-?[0-9]+)");
        return match.Success && int.TryParse(match.Groups[1].Value,out value);
    }

    private static bool MatchesSavedProgress(string json, EntityDataSO data)
    {
        int value;
        if (!TryReadInteger(json,"currentLevel",out value) || value < 1 ||
            data.currentLevel != value) return false;
        return (!TryReadInteger(json,"currentExp",out value) || data.currentExp == value)
            && (!TryReadInteger(json,"statPoints",out value) || data.statPoints == value)
            && (!TryReadInteger(json,"addedDamage",out value) || data.addedDamage == value)
            && (!TryReadInteger(json,"addedHealth",out value) || data.addedHealth == value)
            && (!TryReadInteger(json,"mapNumber",out value) || data.mapNumber == value)
            && (!TryReadInteger(json,"completedWavesInMap",out value) ||
                data.completedWavesInMap == value);
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

    // New Game and Reset must NEVER touch the two manually controlled slots.
    // Remove only the old autosave (and its leftover temporary write) before
    // creating the fresh Hero. ManualSave1/2 remain byte-for-byte unchanged.
    public bool ResetAutoSave()
    {
        try
        {
            string finalPath = Path.Combine(saveDirectory, SaveSlot.AutoSave + SAVE_EXTENSION);
            string tempPath = Path.Combine(saveDirectory, SaveSlot.AutoSave + TEMP_EXTENSION);
            if (File.Exists(finalPath)) File.Delete(finalPath);
            if (File.Exists(tempPath)) File.Delete(tempPath);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("Không thể đặt lại ô lưu tự động: " + e.Message);
            return false;
        }
    }

    public void SaveGame(string slotName, string jsonData) { WriteToFile(slotName, jsonData); }

    // A separate plain object keeps both the caller and its template untouched
    // until every item version has passed. Missing fields remain legacy zero.
    // Reuse EquipmentInstance so nested affix/forge/checkpoint versions are read
    // by JsonUtility, not a regex or a second implementation of the JSON schema.
    [Serializable]
    private sealed class EquipmentVersionProbe
    {
        public int inventoryVersion = 0;
        public int equipmentVersion = 0;
        public System.Collections.Generic.List<EquipmentInstance> equipmentItems = null;
    }

    private static bool TryValidateEquipmentJson(string json, out string reason)
    {
        EquipmentVersionProbe probe;
        return TryReadEquipmentVersions(json, out probe, out reason) &&
            EquipmentFoundation.TryValidateSaveVersions(probe.inventoryVersion,
                probe.equipmentVersion, probe.equipmentItems, out reason);
    }

    private static bool TryReadEquipmentVersions(string json,
        out EquipmentVersionProbe probe, out string reason)
    {
        probe = null;
        if (string.IsNullOrEmpty(json) || !json.TrimStart().StartsWith("{"))
        { reason = "Expected a JSON object."; return false; }
        try
        {
            probe = new EquipmentVersionProbe();
            JsonUtility.FromJsonOverwrite(json, probe);
            reason = null;
            return true;
        }
        catch (Exception error)
        {
            reason = "Cannot read equipment versions: " + error.Message;
            return false;
        }
    }

    // Small JSON saves are serialized on the main thread. No queued old write
    // can recreate a deleted save after the player confirms New Game.
    private bool WriteToFile(string slotName, string data)
    {
        string reason;
        if (!TryValidateEquipmentJson(data, out reason))
        {
            Debug.LogWarning("[SaveManager] Refusing write to " + slotName + ": " + reason);
            return false;
        }
        string finalPath = Path.Combine(saveDirectory, slotName + SAVE_EXTENSION);
        string tempPath = Path.Combine(saveDirectory, slotName + TEMP_EXTENSION);
        try
        {
            // Even a supported running Hero must not overwrite a newer game's
            // existing slot (for example, after refusing to load that slot).
            // Unreadable-version/corrupt JSON is not a detected future schema;
            // preserve the existing ability to explicitly replace corrupt saves.
            EquipmentVersionProbe existing;
            if (File.Exists(finalPath) &&
                TryReadEquipmentVersions(File.ReadAllText(finalPath), out existing, out reason) &&
                !EquipmentFoundation.TryValidateSaveVersions(existing.inventoryVersion,
                    existing.equipmentVersion, existing.equipmentItems, out reason))
            {
                Debug.LogWarning("[SaveManager] Refusing overwrite of " + slotName + ": " + reason);
                return false;
            }
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
