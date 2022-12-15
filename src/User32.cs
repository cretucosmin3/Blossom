using System;
using System.Runtime.InteropServices;
using System.Drawing;

namespace Blossom;

public static class User32
{
    const int GWL_STYLE = -16;
    const int WS_BORDER = 0x00800000;
    const int SWP_FRAMECHANGED = 0x0020;

    const int SWP_NOSIZE = 0x1;
    const int SWP_NOZORDER = 0x4;
    const int SWP_SHOWWINDOW = 0x0040;
    const int SWP_NOACTIVATE = 0x0010;

    const int WS_CAPTION = 0x00C00000;
    const int WS_THICKFRAME = 0x00040000;

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

    [DllImport("user32.dll")]
    static extern bool AdjustWindowRectEx(ref RECT lpRect, uint dwStyle, bool bMenu, uint dwExStyle);

    public static void RemoveTopbar(IntPtr hwnd)
    {
        var rect = new RECT();
        GetWindowRect(hwnd, ref rect);

        int style = GetWindowLong(hwnd, GWL_STYLE);
        style = WS_BORDER | WS_CAPTION;
        style = style & WS_BORDER;
        SetWindowLong(hwnd, GWL_STYLE, style);

        AdjustWindowRectEx(ref rect, WS_CAPTION | WS_THICKFRAME, false, 0);

        // Set the new position and dimensions of the window
        SetWindowPos(hwnd, IntPtr.Zero, rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top, SWP_NOZORDER | SWP_NOACTIVATE);
        // ------------------------------------------------------------------------
    }

    public static void SetCaptionHeight(IntPtr hWnd, int captionHeight)
    {
        // Use the SetWindowPos function to change the caption height
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}