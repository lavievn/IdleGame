using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class TransparentWindow : MonoBehaviour
{
    [Header("UI Interaction Elements (Thứ tự ưu tiên Z-Index)")]
    [Tooltip("Kéo các Popup/Bảng thông báo (Save/Load/Exit) vào đây. Ưu tiên cao nhất.")]
    public RectTransform[] modalUI;     
    
    [Tooltip("Kéo các nút bấm, kỹ năng, menu hệ thống vào đây. Ưu tiên thứ hai.")]
    public RectTransform[] clickableUI; 
    
    [Tooltip("Kéo vùng nền (Ground) dùng để kéo thả cửa sổ vào đây. Ưu tiên thấp nhất.")]
    public RectTransform[] draggableUI; 

    [Header("Windows startup window")]
    public int startupWidth = 1000;
    public int startupHeight = 563;
    private bool isCurrentlyClickable = false;
    private bool previousButtonDown;
    private bool dragging;
    private POINT dragCursor;
    private POINT dragWindow;
    private POINT resizePosition;
    private bool hasResizePosition;

    // Edge detection must use the current high bit, not GetAsyncKeyState's
    // unreliable "pressed since last call" bit.
    public static bool ConsumePress(bool down, ref bool previous)
    {
        bool pressed = down && !previous;
        previous = down;
        return pressed;
    }

    public static Vector2 DragPosition(Vector2 window, Vector2 cursorAtPress, Vector2 cursorNow)
    {
        return window + cursorNow - cursorAtPress;
    }

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
    private static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out CLIENTRECT rect);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out CLIENTRECT rect);
    [StructLayout(LayoutKind.Sequential)]
    private struct CLIENTRECT { public int left, top, right, bottom; }


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
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        hWnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
        if (hWnd == IntPtr.Zero) hWnd = GetActiveWindow();
        previousButtonDown = NativeButtonDown();
        // Override saved Unity fullscreen/resolution preferences as well as
        // PlayerSettings. Transparency is reapplied after the resolution change.
        Screen.SetResolution(startupWidth, startupHeight, FullScreenMode.Windowed);
        StartCoroutine(ReapplyTransparencyDelay(startupWidth, startupHeight, true));
#endif
    }

    void Update()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (hWnd == IntPtr.Zero) return;
        bool down = NativeButtonDown();
        bool pressed = ConsumePress(down, ref previousButtonDown);
        bool overClickable, overDraggable;
        CheckHitboxUI(out overClickable, out overDraggable);
        if (!down) dragging = false;
        bool wantsClicks = overClickable || overDraggable || dragging;
        if (wantsClicks != isCurrentlyClickable) ToggleClickThrough(!wantsClicks);

        if (pressed && overClickable)
        {
            Vector2 point;
            if (UIManager.Instance != null && TryGetPointerPosition(out point))
                UIManager.Instance.HandleMouseClick(point);
        }
        else if (pressed && overDraggable)
        {
            CLIENTRECT rect;
            if (GetCursorPos(out dragCursor) && GetWindowRect(hWnd, out rect))
            {
                dragWindow = new POINT { X = rect.left, Y = rect.top };
                dragging = true;
            }
        }
        if (dragging && down)
        {
            POINT cursor;
            if (GetCursorPos(out cursor))
            {
                Vector2 position = DragPosition(new Vector2(dragWindow.X, dragWindow.Y),
                    new Vector2(dragCursor.X, dragCursor.Y), new Vector2(cursor.X, cursor.Y));
                // Move directly, preserving the grab offset. No caption drag,
                // Aero Snap, taskbar docking, or forced bottom coordinate.
                SetWindowPos(hWnd, IntPtr.Zero, Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y),
                    0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
            }
        }
#endif
    }

    private static bool NativeButtonDown()
    {
        // Respect Windows' swapped primary mouse button setting.
        return (GetAsyncKeyState(GetSystemMetrics(23) != 0 ? 0x02 : 0x01) & 0x8000) != 0;
    }

    void CheckHitboxUI(out bool overClickable, out bool overDraggable)
    {
        overClickable = false;
        overDraggable = false;

        Vector2 mousePos;
        if (!TryGetPointerPosition(out mousePos)) return;

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

        // Dynamic buttons and scene triggers omitted from clickableUI still
        // block click-through and dragging using their actual rendered rects.
        if (UIManager.Instance != null && UIManager.Instance.CheckInteractableHover(mousePos))
        { overClickable = true; return; }

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

    public void RegisterClickable(RectTransform rect)
    {
        if (rect == null) return;
        var list = new System.Collections.Generic.List<RectTransform>(clickableUI ?? new RectTransform[0]);
        if (!list.Contains(rect)) list.Add(rect);
        clickableUI = list.ToArray();
    }
    public static Vector2 ClientToGamePosition(float x, float y, float clientWidth, float clientHeight, float gameWidth, float gameHeight)
    {
        if (clientWidth <= 0f || clientHeight <= 0f) return Vector2.zero;
        return new Vector2(x * gameWidth / clientWidth, (clientHeight - y) * gameHeight / clientHeight);
    }
    public bool TryGetPointerPosition(out Vector2 point)
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        POINT cursor;
        CLIENTRECT client;
        if (hWnd == IntPtr.Zero || !GetCursorPos(out cursor) || !ScreenToClient(hWnd, ref cursor) || !GetClientRect(hWnd, out client))
        { point=Vector2.zero; return false; }
        point = ClientToGamePosition(cursor.X, cursor.Y, client.right-client.left, client.bottom-client.top, Screen.width, Screen.height);
        return true;
#else
        if (Mouse.current == null) { point=Vector2.zero; return false; }
        point = Mouse.current.position.ReadValue();
        return true;
#endif
    }

    // --- CÁC CỜ ÉP RENDER CỦA WINDOWS ---
    const int SWP_NOSIZE = 0x0001;
    const int SWP_NOMOVE = 0x0002;
    const int SWP_NOZORDER = 0x0004;
    const int SWP_NOACTIVATE = 0x0010;
    const int SWP_FRAMECHANGED = 0x0020; 
    const int SWP_SHOWWINDOW = 0x0040;

    void ToggleClickThrough(bool isTransparent)
    {
        isCurrentlyClickable = !isTransparent;
        if (isTransparent)
            SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TRANSPARENT);
        else
            SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED);

        SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_FRAMECHANGED);
        
        MARGINS margins = new MARGINS { cxLeftWidth = -1 };
        DwmExtendFrameIntoClientArea(hWnd, ref margins);
    }

    // --- KHỐI RESIZE BỌC THÉP TÁI THIẾT LẬP ---
    public void ResizeWindow(int width, int height)
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (hWnd != IntPtr.Zero)
        {
            CLIENTRECT rect;
            hasResizePosition = GetWindowRect(hWnd, out rect);
            if (hasResizePosition) resizePosition = new POINT { X = rect.left, Y = rect.top };
            dragging = false;
            StopAllCoroutines();
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
            StartCoroutine(ReapplyTransparencyDelay(width, height));
        }
#endif
    }

    private IEnumerator ReapplyTransparencyDelay(int width, int height, bool center = false)
    {
        yield return new WaitForSecondsRealtime(0.2f);

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        // Unity can recreate its native window while changing fullscreen mode.
        IntPtr current = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
        if (current != IntPtr.Zero) hWnd = current;
        if (hWnd == IntPtr.Zero) yield break;
        SetWindowLong(hWnd, GWL_STYLE, WS_POPUP | WS_VISIBLE);

        if (!isCurrentlyClickable)
            SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TRANSPARENT);
        else
            SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED);

        int x = center ? Math.Max(0, (GetSystemMetrics(0) - width) / 2) : resizePosition.X;
        int y = center ? Math.Max(0, (GetSystemMetrics(1) - height) / 2) : resizePosition.Y;
        SetWindowPos(hWnd, HWND_TOPMOST, x, y, width, height,
            (center || hasResizePosition ? 0 : SWP_NOMOVE) | SWP_NOACTIVATE | SWP_SHOWWINDOW | SWP_FRAMECHANGED);
        
        MARGINS margins = new MARGINS { cxLeftWidth = -1 };
        DwmExtendFrameIntoClientArea(hWnd, ref margins);
#endif
    }
}