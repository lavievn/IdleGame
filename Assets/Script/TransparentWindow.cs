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
    
    [Tooltip("Vùng kéo bổ sung. Nút, log và các bảng UI cũng hỗ trợ giữ-kéo.")]
    public RectTransform[] draggableUI; 

    [Header("Windows startup window")]
    public int startupWidth = 800;
    public int startupHeight = 450;
    private bool isCurrentlyClickable = false;
    private bool previousButtonDown;
    private WindowPointerGesture gesture = new WindowPointerGesture();
    private bool resizing;
    private CLIENTRECT resizeWorkArea;
    private bool dragging;
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
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFO info);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hwnd);
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFO { public int size; public CLIENTRECT monitor, work; public uint flags; }

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
        if (resizing) { previousButtonDown = NativeButtonDown(); return; }
        bool down = NativeButtonDown();
        bool pressed = ConsumePress(down, ref previousButtonDown);
        bool overClickable, overDraggable;
        CheckHitboxUI(out overClickable, out overDraggable);
        POINT cursor;
        if (!GetCursorPos(out cursor)) return;
        Vector2 nativePoint = new Vector2(cursor.X, cursor.Y);
        if (pressed && (overClickable || overDraggable))
        {
            CLIENTRECT rect; Vector2 gamePoint;
            if (GetWindowRect(hWnd, out rect) && TryGetPointerPosition(out gamePoint))
                gesture.Begin(new Vector2(rect.left, rect.top), nativePoint, gamePoint,
                    UIManager.Instance == null || !UIManager.Instance.IsTextInputAt(gamePoint));
        }
        Vector2 position;
        bool click = gesture.Step(down, nativePoint, out position);
        dragging = gesture.IsDragging;
        if (dragging && down)
            SetWindowPos(hWnd, IntPtr.Zero, Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y),
                0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_NOSENDCHANGING);
        if (click && UIManager.Instance != null)
        {
            Vector2 point;
            if (TryGetPointerPosition(out point))
            {
                if (UIManager.Instance.IsTextInputAt(point)) SetForegroundWindow(hWnd);
                UIManager.Instance.HandlePointerClick(gesture.PressGamePoint, point);
            }
        }
        bool wantsClicks = overClickable || overDraggable || gesture.IsPressed;
        if (wantsClicks != isCurrentlyClickable) ToggleClickThrough(!wantsClicks);

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
    const int SWP_NOSENDCHANGING = 0x0400;

    void ToggleClickThrough(bool isTransparent)
    {
        isCurrentlyClickable = !isTransparent;
        if (isTransparent)
            SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TRANSPARENT);
        else
            SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED);

        SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        
        MARGINS margins = new MARGINS { cxLeftWidth = -1 };
        DwmExtendFrameIntoClientArea(hWnd, ref margins);
    }

    // --- KHỐI RESIZE BỌC THÉP TÁI THIẾT LẬP ---
    public static Vector2 KeepInWorkArea(Vector2 position, float width, float height, Rect work)
    {
        return new Vector2(Mathf.Clamp(position.x, work.xMin, Mathf.Max(work.xMin, work.xMax - width)),
            Mathf.Clamp(position.y, work.yMin, Mathf.Max(work.yMin, work.yMin + work.height - height)));
    }
    private CLIENTRECT WorkArea()
    {
        var info = new MONITORINFO { size = Marshal.SizeOf(typeof(MONITORINFO)) };
        if (GetMonitorInfo(MonitorFromWindow(hWnd, 2), ref info)) return info.work;
        return new CLIENTRECT { right = GetSystemMetrics(0), bottom = GetSystemMetrics(1) };
    }
    public void ResizeWindow(int width, int height)
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (hWnd == IntPtr.Zero || resizing) return;
        CLIENTRECT rect;
        hasResizePosition = GetWindowRect(hWnd, out rect);
        if (hasResizePosition) resizePosition = new POINT { X = rect.left, Y = rect.top };
        resizeWorkArea = WorkArea();
        resizing = true; dragging = false; gesture.Cancel();
        Screen.SetResolution(width, height, FullScreenMode.Windowed);
        StartCoroutine(ReapplyTransparencyDelay(width, height));
#else
        Screen.SetResolution(width, height, FullScreenMode.Windowed);
#endif
    }

    private IEnumerator ReapplyTransparencyDelay(int width, int height, bool center = false)
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        resizing = true; gesture.Cancel();
        if (center) resizeWorkArea = WorkArea();
        // SetResolution completes asynchronously, at frame end. Wait for the
        // requested render size rather than racing it with a fixed 0.2s timer.
        yield return null; yield return null;
        for (int frame = 0; frame < 60 && (Screen.width != width || Screen.height != height); frame++) yield return null;
        IntPtr current = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
        if (current != IntPtr.Zero) hWnd = current;
        if (hWnd == IntPtr.Zero) { resizing = false; yield break; }
        SetWindowLong(hWnd, GWL_STYLE, WS_POPUP | WS_VISIBLE);
        SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED | (isCurrentlyClickable ? 0u : WS_EX_TRANSPARENT));
        var work = new Rect(resizeWorkArea.left, resizeWorkArea.top,
            resizeWorkArea.right - resizeWorkArea.left, resizeWorkArea.bottom - resizeWorkArea.top);
        Vector2 wanted = center || !hasResizePosition ? new Vector2(work.xMin + (work.width-width)/2,
            work.yMin + (work.height-height)/2) : new Vector2(resizePosition.X, resizePosition.Y);
        wanted = KeepInWorkArea(wanted, width, height, work);
        SetWindowPos(hWnd, HWND_TOPMOST, Mathf.RoundToInt(wanted.x), Mathf.RoundToInt(wanted.y), width, height,
            SWP_NOACTIVATE | SWP_SHOWWINDOW | SWP_FRAMECHANGED | SWP_NOSENDCHANGING);
        MARGINS margins = new MARGINS { cxLeftWidth = -1 };
        DwmExtendFrameIntoClientArea(hWnd, ref margins);
        // Repair any delayed Unity placement once, without constantly pinning a
        // user-dragged window. Pointer gestures are suspended only during settle.
        for (int frame = 0; frame < 8; frame++)
        {
            yield return null;
            CLIENTRECT actual;
            if (GetWindowRect(hWnd, out actual) && (actual.left != Mathf.RoundToInt(wanted.x) || actual.top != Mathf.RoundToInt(wanted.y)))
                SetWindowPos(hWnd, IntPtr.Zero, Mathf.RoundToInt(wanted.x), Mathf.RoundToInt(wanted.y), 0, 0,
                    SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_NOSENDCHANGING);
        }
        previousButtonDown = NativeButtonDown(); resizing = false;
#else
        yield break;
#endif
    }
}

// Button activation is a completed click, never the beginning of a drag.
public sealed class WindowPointerGesture
{
    public bool IsPressed { get; private set; }
    public bool IsDragging { get; private set; }
    public Vector2 PressGamePoint { get; private set; }
    private Vector2 startWindow, startCursor;
    private bool canDrag;
    public void Begin(Vector2 window, Vector2 cursor, Vector2 gamePoint, bool allowDrag)
    {
        IsPressed = true; IsDragging = false; startWindow = window; startCursor = cursor;
        PressGamePoint = gamePoint; canDrag = allowDrag;
    }
    public bool Step(bool down, Vector2 cursor, out Vector2 position)
    {
        position = startWindow;
        if (!IsPressed) return false;
        Vector2 delta = cursor - startCursor;
        if (canDrag && delta.x * delta.x + delta.y * delta.y >= 25f) IsDragging = true;
        if (IsDragging) position = TransparentWindow.DragPosition(startWindow, startCursor, cursor);
        if (down) return false;
        bool click = !IsDragging;
        IsPressed = false; IsDragging = false;
        return click;
    }
    public void Cancel() { IsPressed = false; IsDragging = false; }
}
