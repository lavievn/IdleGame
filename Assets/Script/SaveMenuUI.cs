using System.IO;
using UnityEngine;
using TMPro;

public class SaveMenuUI : MonoBehaviour
{
    [Header("REFERENCES")]
    public GameManager gameManager;
    // Bỏ SaveManager reference vì UI sẽ tự check file hoặc gọi thông qua GameManager để đảm bảo Decoupled (phân tách độc lập)

    [Header("CÁC BẢNG MENU CON (CẦN KÉO VÀO)")]
    public GameObject saveSubMenu;    // Bảng chứa 2 nút Save1, Save2
    public GameObject loadSubMenu;    // Bảng chứa 3 nút Load Auto, Load1, Load2
    public GameObject overwritePopup; // Bảng hỏi "Có muốn ghi đè?"

    [Header("TEXT HIỂN THỊ THÔNG TIN SLOT")]
    public TextMeshProUGUI autoSaveInfoText;
    public TextMeshProUGUI manual1InfoText;
    public TextMeshProUGUI manual2InfoText;

    private SaveSlot pendingSaveSlot; // Nhớ tạm slot đang định lưu đè
    private string saveDirectory;     // Cache lại đường dẫn gốc

    void Awake()
    {
        // Cache đường dẫn 1 lần duy nhất ở Awake để tránh tạo rác GC khi ghép chuỗi
        saveDirectory = Application.persistentDataPath + "/Saves/";
    }

    void OnEnable()
    {
        // Khi bật SystemMenu lên, giấu hết các bảng con đi
        if (saveSubMenu != null) saveSubMenu.SetActive(false);
        if (loadSubMenu != null) loadSubMenu.SetActive(false);
        if (overwritePopup != null) overwritePopup.SetActive(false);
        RefreshUI();
    }

    public void RefreshUI()
    {
        // Tối ưu Zero GC: Tránh dùng phép cộng chuỗi (+) khi cập nhật UI liên tục
        if (autoSaveInfoText != null) 
            autoSaveInfoText.text = HasSave(SaveSlot.AutoSave) ? "Auto: Đã có dữ liệu" : "Auto: Trống";
            
        if (manual1InfoText != null) 
            manual1InfoText.text = HasSave(SaveSlot.ManualSave1) ? "Slot 1: Đã có dữ liệu" : "Slot 1: Trống";
            
        if (manual2InfoText != null) 
            manual2InfoText.text = HasSave(SaveSlot.ManualSave2) ? "Slot 2: Đã có dữ liệu" : "Slot 2: Trống";
    }

    // Tự kiểm tra file có trên ổ cứng không thông qua System.IO
    private bool HasSave(SaveSlot slot)
    {
        return File.Exists(saveDirectory + slot.ToString() + ".json");
    }

    // --- CÁC NÚT ĐIỀU HƯỚNG CHÍNH ---
    public void OpenSaveSubMenu() 
    { 
        saveSubMenu.SetActive(true); 
        loadSubMenu.SetActive(false); 
        RefreshUI(); 
    }

    public void OpenLoadSubMenu() 
    { 
        loadSubMenu.SetActive(true); 
        saveSubMenu.SetActive(false); 
        RefreshUI(); 
    }

    // --- LOGIC LƯU GAME (CÓ HỎI GHI ĐÈ) ---
    // Gắn hàm này vào các nút Save1, Save2 và truyền Index tương ứng (1 hoặc 2)
    public void ClickSaveSlot(int slotIndex)
    {
        SaveSlot slot = (SaveSlot)slotIndex;
        
        // Nếu đã có dữ liệu -> Hiện bảng hỏi
        if (HasSave(slot))
        {
            pendingSaveSlot = slot;
            overwritePopup.SetActive(true);
        }
        else // Chưa có dữ liệu -> Lưu luôn
        {
            ExecuteSave(slot);
        }
    }

    public void ConfirmOverwrite()
    {
        ExecuteSave(pendingSaveSlot);
        overwritePopup.SetActive(false);
    }

    public void CancelOverwrite()
    {
        overwritePopup.SetActive(false);
    }

    private void ExecuteSave(SaveSlot slot)
    {
        if (gameManager != null)
        {
            gameManager.ForceManualSave((int)slot);
            RefreshUI(); // Update lại Text ngay lập tức sau khi có file
        }
        else
        {
            Debug.LogError("[SaveMenuUI] Thiếu tham chiếu tới GameManager. Hãy kéo thả vào Inspector!");
        }
    }

    // --- LOGIC TẢI GAME ---
    // Gắn hàm này vào các nút Load (Truyền Index 0, 1 hoặc 2)
    public void ClickLoadSlot(int slotIndex)
    {
        if (gameManager != null)
        {
            gameManager.ForceManualLoad(slotIndex);
            
            // Đóng toàn bộ System Menu lại sau khi Load thành công để người chơi vào game ngay
            gameObject.SetActive(false);
        }
    }
}