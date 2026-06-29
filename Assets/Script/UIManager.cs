using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{	
    [Header("Core References")]
    public static UIManager Instance;

    [Header("UI MENUS")]
    public GameObject systemMenu;
    public GameObject mapMenu;
    public Image groundImage;
    public TransparentWindow transparentWindow;
    
    [Header("POPUP THOÁT GAME")]
    public GameObject exitConfirmPopup; // Bảng hỏi "Bạn có muốn thoát..."

    private List<CustomInteractable> interactables = new List<CustomInteractable>();

    void Awake() 
    { 
        Instance = this; 
    }

    void Update()
    {
        if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleMouseClick(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
        }
    }

    public void RegisterInteractable(CustomInteractable item) { if (!interactables.Contains(item)) interactables.Add(item); }
    public void UnregisterInteractable(CustomInteractable item) { interactables.Remove(item); }

    public bool CheckInteractableHover(Vector2 mousePos)
    {
        foreach (var item in interactables)
        {
            if (item != null && item.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(item.GetRect(), mousePos, null))
                return true;
        }
        return false;
    }

    public void HandleMouseClick(Vector2 mousePos)
    {
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (gm != null && gm.infoText != null) 
        {
            gm.infoText.text = $"WinAPI Tọa độ Click: {mousePos.x:F1} , {mousePos.y:F1}";
        }

        foreach (var item in interactables)
        {
            if (item != null && item.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(item.GetRect(), mousePos, null))
            {
                item.onClickEvent?.Invoke();
                return; 
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

    // --- LOGIC THOÁT GAME ---
    public void ClickExitButton() 
    { 
        if (exitConfirmPopup != null) exitConfirmPopup.SetActive(true); 
    }

    public void ConfirmExit() 
    { 
        Application.Quit(); // Lệnh này chạy sẽ ngầm gọi Auto-save trong GameManager
    }

    public void CancelExit() 
    { 
        if (exitConfirmPopup != null) exitConfirmPopup.SetActive(false); 
    }
    // -------------------------

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

    private void ExecuteScale(int width)
    {
        if (transparentWindow == null) transparentWindow = FindObjectOfType<TransparentWindow>(); 
        if (transparentWindow != null)
        {
            int height = Mathf.RoundToInt(width * (9f / 16f));
            transparentWindow.ResizeWindow(width, height);
        }
    }

    public void Scale200() { ExecuteScale(200); }
    public void Scale500() { ExecuteScale(500); }
    public void Scale1000() { ExecuteScale(1000); }
}