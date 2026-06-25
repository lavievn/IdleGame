using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;

public class TransparentWindow : MonoBehaviour
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    static extern int SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

    // API lấy trỏ chuột trực tiếp từ Windows
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X; public int Y; }
    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out POINT lpPoint);
    [DllImport("user32.dll")]
    public static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

    // API Kéo cửa sổ
    [DllImport("user32.dll")]
    public static extern bool ReleaseCapture();
    [DllImport("user32.dll")]
    public static extern bool SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);

    private const int GWL_EXSTYLE = -20;
    private const uint WS_EX_LAYERED = 0x00080000;
    private const uint WS_EX_TRANSPARENT = 0x00000020;
    private const uint LWA_COLORKEY = 0x00000001;
    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HT_CAPTION = 0x2;

    private IntPtr hWnd;

    [Header("GÁN CÁC NÚT BẤM (START, MENU...)")]
    public List<RectTransform> clickableUI = new List<RectTransform>();

    [Header("GÁN KHU VỰC KÉO CỬA SỔ (GROUND)")]
    public List<RectTransform> draggableUI = new List<RectTransform>();

    private void Start()
    {
#if !UNITY_EDITOR
        hWnd = GetActiveWindow();
        SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED);
        SetLayeredWindowAttributes(hWnd, 0, 0, LWA_COLORKEY);
        SetWindowPos(hWnd, new IntPtr(-1), 0, 0, 0, 0, 0x0001 | 0x0002);
#endif
    }

    private void Update()
    {
#if !UNITY_EDITOR
        bool isHoveringClickable = false;
        bool isHoveringDraggable = false;

        // 1. Dùng API Windows đo tọa độ thực tế, bỏ qua hệ thống Unity
        POINT p;
        GetCursorPos(out p);
        ScreenToClient(hWnd, ref p);
        
        // Convert tọa độ Win (Góc trái trên) sang tọa độ Unity (Góc trái dưới)
        Vector2 mousePos = new Vector2(p.X, Screen.height - p.Y);

        // 2. Quét mảng nút bấm
        foreach (var rect in clickableUI)
        {
            if (rect != null && rect.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(rect, mousePos, null))
            {
                isHoveringClickable = true; break;
            }
        }

        // 3. Quét mảng kéo rê (Chỉ quét nếu không trỏ vào nút)
        if (!isHoveringClickable)
        {
            foreach (var rect in draggableUI)
            {
                if (rect != null && rect.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(rect, mousePos, null))
                {
                    isHoveringDraggable = true; break;
                }
            }
        }

        // 4. Ra lệnh cho Windows
        if (isHoveringClickable || isHoveringDraggable)
        {
            // Tắt xuyên thấu, đóng rắn cửa sổ để bắt click
            SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED);

            // Xử lý kéo cửa sổ
            if (isHoveringDraggable && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                ReleaseCapture();
                SendMessage(hWnd, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
        else
        {
            // Bật xuyên thấu, cho click đâm qua Desktop
            SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TRANSPARENT);
        }
#endif
    }
	// --- MODULE ĐỔI KÍCH THƯỚC CỬA SỔ ---
    public void ResizeWindow(int width, int height)
    {
#if !UNITY_EDITOR
        if (hWnd != IntPtr.Zero)
        {
            // Cờ 0x0002 (SWP_NOMOVE): Giữ nguyên vị trí cửa sổ, chỉ đổi Width/Height
            // Cờ 0x0004 (SWP_NOZORDER): Giữ nguyên lớp Z (không đè lên các app khác)
            SetWindowPos(hWnd, IntPtr.Zero, 0, 0, width, height, 0x0002 | 0x0004);
        }
#else
        // Chạy bình thường nếu test trong Editor
        Screen.SetResolution(width, height, FullScreenMode.Windowed);
#endif
    }
}