using UnityEngine;
using TMPro;

public class SaveMenuUI : MonoBehaviour
{
    [Header("REFERENCES")]
    public GameManager gameManager;
    public SaveManager saveManager;

    [Header("CÁC BẢNG MENU CON (CẦN KÉO VÀO)")]
    public GameObject saveSubMenu;    // Bảng chứa 2 nút Save1, Save2
    public GameObject loadSubMenu;    // Bảng chứa 3 nút Load Auto, Load1, Load2
    public GameObject overwritePopup; // Bảng hỏi "Có muốn ghi đè?"

    [Header("TEXT HIỂN THỊ THÔNG TIN SLOT")]
    public TextMeshProUGUI autoSaveInfoText;
    public TextMeshProUGUI manual1InfoText;
    public TextMeshProUGUI manual2InfoText;

    private SaveSlot pendingSaveSlot; // Nhớ tạm slot đang định lưu đè

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
        if (saveManager == null) return;
        if (autoSaveInfoText != null) autoSaveInfoText.text = "Auto: " + saveManager.GetSaveDetails(SaveSlot.AutoSave);
        if (manual1InfoText != null) manual1InfoText.text = "Slot 1: " + saveManager.GetSaveDetails(SaveSlot.ManualSave1);
        if (manual2InfoText != null) manual2InfoText.text = "Slot 2: " + saveManager.GetSaveDetails(SaveSlot.ManualSave2);
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
        if (saveManager.HasSave(slot))
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
        gameManager.ForceManualSave((int)slot);
        RefreshUI(); // Update lại Text ngay lập tức
    }

    // --- LOGIC TẢI GAME ---
    // Gắn hàm này vào các nút Load (Truyền Index 0, 1 hoặc 2)
    public void ClickLoadSlot(int slotIndex)
    {
        gameManager.ForceManualLoad(slotIndex);
        
        // Đóng toàn bộ System Menu lại sau khi Load thành công để người chơi vào game ngay
        gameObject.SetActive(false);
    }
}