using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class TransparentWindow : MonoBehaviour
{
    [Header("UI Interaction Elements (Thứ tự ưu tiên Z-Index)")]
    [Tooltip("Kéo các Popup/Bảng thông báo (Save/Load/Exit) vào đây. Ưu tiên cao nhất.")]
    public RectTransform[] modalUI;     
    
    [Tooltip("Kéo các nút bấm, kỹ năng, menu hệ thống vào đây. Ưu tiên thứ hai.")]
    public RectTransform[] clickableUI; 
    
    [Tooltip("Kéo vùng nền (Ground) dùng để kéo thả cửa sổ vào đây. Ưu tiên thấp nhất.")]
    public RectTransform[] draggableUI; 

    private bool isCurrentlyClickable = false;

    // --- IMPORT WinAPI ---
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();
    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);
    [DllImport("user32.dll")]
    private static extern int SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int cx, int cy, int uFlags);
    [DllImport("Dwmapi.dll")]
    private static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);
    [DllImport("user32.dll")]
    private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);
    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();
    [DllImport("user32.dll")]
    private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

    const int WM_NCLBUTTONDOWN = 0xA1;
    const int HTCAPTION = 0x2;

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X; public int Y; }
    private struct MARGINS { public int cxLeftWidth; public int cxRightWidth; public int cyTopHeight; public int cyBottomHeight; }

    const int GWL_EXSTYLE = -20;
    const uint WS_EX_LAYERED = 0x00080000;
    const uint WS_EX_TRANSPARENT = 0x00000020;
    const int GWL_STYLE = -16;
    const uint WS_POPUP = 0x80000000;
    const uint WS_VISIBLE = 0x10000000;
    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    
    private IntPtr hWnd;

    void Start()
    {
#if !UNITY_EDITOR
        hWnd = GetActiveWindow();
        SetWindowLong(hWnd, GWL_STYLE, WS_POPUP | WS_VISIBLE);
        
        MARGINS margins = new MARGINS { cxLeftWidth = -1 };
        DwmExtendFrameIntoClientArea(hWnd, ref margins);
        
        SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TRANSPARENT);
        SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, 0x0001 | 0x0002);
#endif
    }

    void Update()
    {
#if !UNITY_EDITOR
        if (Mouse.current == null) return;

        bool isOverClickable = false;
        bool isOverDraggable = false;
        
        CheckHitboxUI(out isOverClickable, out isOverDraggable);

        bool isHoveringOverAnyUI = isOverClickable || isOverDraggable;

        // Bật/tắt xuyên thấu
        if (isHoveringOverAnyUI && !isCurrentlyClickable)
        {
            ToggleClickThrough(false); 
        }
        else if (!isHoveringOverAnyUI && isCurrentlyClickable)
        {
            ToggleClickThrough(true);  
        }

        // Kéo thả (chỉ chạy khi chắc chắn không click đè lên Modal/Clickable)
        if (isOverDraggable && Mouse.current.leftButton.wasPressedThisFrame)
        {
            ReleaseCapture();
            SendMessage(hWnd, WM_NCLBUTTONDOWN, HTCAPTION, 0);
        }
#endif
    }

    void CheckHitboxUI(out bool overClickable, out bool overDraggable)
    {
        overClickable = false;
        overDraggable = false;

#if !UNITY_EDITOR
        POINT p;
        GetCursorPos(out p);
        ScreenToClient(hWnd, ref p);
        Vector2 mousePos = new Vector2(p.X, Screen.height - p.Y);
#else
        if (Mouse.current == null) return;
        Vector2 mousePos = Mouse.current.position.ReadValue();
#endif

        // 1. Tầng Modal/Popup (Ưu tiên tuyệt đối)
        if (modalUI != null)
        {
            for (int i = 0; i < modalUI.Length; i++)
            {
                if (modalUI[i] != null && modalUI[i].gameObject.activeInHierarchy && 
                    RectTransformUtility.RectangleContainsScreenPoint(modalUI[i], mousePos))
                {
                    overClickable = true;
                    return; // BREAK EARLY: Chặn tia Raycast, bảo vệ click cho Popup
                }
            }
        }

        // 2. Tầng UI Clickable thông thường (Nút bấm, Menu)
        if (clickableUI != null)
        {
            for (int i = 0; i < clickableUI.Length; i++)
            {
                if (clickableUI[i] != null && clickableUI[i].gameObject.activeInHierarchy && 
                    RectTransformUtility.RectangleContainsScreenPoint(clickableUI[i], mousePos))
                {
                    overClickable = true;
                    return; // BREAK EARLY: Chặn kéo thả cửa sổ khi đang đè lên nút
                }
            }
        }

        // 3. Tầng Draggable (Thấp nhất, chỉ xét khi 2 tầng trên bị xuyên thủng)
        if (draggableUI != null)
        {
            for (int i = 0; i < draggableUI.Length; i++)
            {
                if (draggableUI[i] != null && draggableUI[i].gameObject.activeInHierarchy && 
                    RectTransformUtility.RectangleContainsScreenPoint(draggableUI[i], mousePos))
                {
                    overDraggable = true;
                    break;
                }
            }
        }
    }

    // --- CÁC CỜ ÉP RENDER CỦA WINDOWS ---
    const int SWP_NOSIZE = 0x0001;
    const int SWP_NOMOVE = 0x0002;
    const int SWP_NOZORDER = 0x0004;
    const int SWP_FRAMECHANGED = 0x0020; 
    const int SWP_SHOWWINDOW = 0x0040;

    void ToggleClickThrough(bool isTransparent)
    {
        isCurrentlyClickable = !isTransparent;
        if (isTransparent)
            SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TRANSPARENT);
        else
            SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED);

        SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
        
        MARGINS margins = new MARGINS { cxLeftWidth = -1 };
        DwmExtendFrameIntoClientArea(hWnd, ref margins);
    }

    // --- KHỐI RESIZE BỌC THÉP TÁI THIẾT LẬP ---
    public void ResizeWindow(int width, int height)
    {
#if !UNITY_EDITOR
        if (hWnd != IntPtr.Zero)
        {
            StopAllCoroutines();
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
            StartCoroutine(ReapplyTransparencyDelay(width, height));
        }
#endif
    }

    private IEnumerator ReapplyTransparencyDelay(int width, int height)
    {
        yield return new WaitForSeconds(0.2f); 

#if !UNITY_EDITOR
        SetWindowLong(hWnd, GWL_STYLE, WS_POPUP | WS_VISIBLE);
        
        if (!isCurrentlyClickable)
            SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TRANSPARENT);
        else
            SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED);

        SetWindowPos(hWnd, IntPtr.Zero, 0, 0, width, height, SWP_NOMOVE | SWP_NOZORDER | SWP_SHOWWINDOW | SWP_FRAMECHANGED);
        
        MARGINS margins = new MARGINS { cxLeftWidth = -1 };
        DwmExtendFrameIntoClientArea(hWnd, ref margins);
#endif
    }
}