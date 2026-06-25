using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

// [CLASS KÈM THEO]: Gắn script này vào các cục UI muốn click thay thế cho Button của uGUI


public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI MENUS")]
    public GameObject systemMenu;
    public GameObject mapMenu;
    public Image groundImage;
    public TransparentWindow transparentWindow;

    // Danh sách các nút bấm đang hiển thị trên màn hình
    private List<CustomInteractable> interactables = new List<CustomInteractable>();

    void Awake() 
    { 
        Instance = this; 
    }
void Update()
    {
        // Xóa bỏ #if UNITY_EDITOR để bản Build cũng bắt được tín hiệu
        if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleMouseClick(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
        }
    }
	
	

    public void RegisterInteractable(CustomInteractable item) { if (!interactables.Contains(item)) interactables.Add(item); }
    public void UnregisterInteractable(CustomInteractable item) { interactables.Remove(item); }

    // Quét toán học xem chuột có đè lên bất kỳ nút nào không (Gọi từ TransparentWindow)
    public bool CheckInteractableHover(Vector2 mousePos)
    {
        foreach (var item in interactables)
        {
            if (item != null && item.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(item.GetRect(), mousePos, null))
                return true;
        }
        return false;
    }

// Kích hoạt Event Click (Gọi từ TransparentWindow)
    public void HandleMouseClick(Vector2 mousePos)
    {
        // 1. IN DEBUG TỌA ĐỘ
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (gm != null && gm.infoText != null) 
        {
            gm.infoText.text = $"WinAPI Tọa độ Click: {mousePos.x} , {mousePos.y}";
        }

        // 2. VÒNG LẶP XỬ LÝ CLICK CŨ
        foreach (var item in interactables)
        {
            if (item != null && item.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(item.GetRect(), mousePos, null))
            {
                item.onClickEvent?.Invoke();
			
                return; // Tránh click xuyên 2 nút nằm đè lên nhau
            }
        }
    }

    public void ToggleSystemMenu()
    {
        systemMenu.SetActive(!systemMenu.activeSelf);
        mapMenu.SetActive(false); 
    }

    public void ToggleMapMenu()
    {
        mapMenu.SetActive(!mapMenu.activeSelf);
        systemMenu.SetActive(false); 
    }

    public void ExitGame() { Application.Quit(); }

    public void SetWindowScale(int size)
    {
        if (transparentWindow != null) transparentWindow.ResizeWindow(size, size);
        systemMenu.SetActive(false); 
    }

    public void ChangeGroundColor(string hexColor)
    {
        if (ColorUtility.TryParseHtmlString(hexColor, out Color newColor))
        {
            groundImage.color = newColor;
        }
        mapMenu.SetActive(false); 
    }
}