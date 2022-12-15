using System.Threading;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Drawing;

namespace Blossom;

public static class User32
{
    //Window Messages
    public const uint WM_NCPAINT = 0x85;
    public const uint WM_NCCALCSIZE = 0x83;
    public const uint WM_NCHITTEST = 0x84;

    //GetDCEx Flags
    public const int DCX_WINDOW = 0x00000001;
    public const int DCX_CACHE = 0x00000002;
    public const int DCX_PARENTCLIP = 0x00000020;
    public const int DCX_CLIPSIBLINGS = 0x00000010;
    public const int DCX_CLIPCHILDREN = 0x00000008;
    public const int DCX_NORESETATTRS = 0x00000004;
    public const int DCX_LOCKWINDOWUPDATE = 0x00000400;
    public const int DCX_EXCLUDERGN = 0x00000040;
    public const int DCX_INTERSECTRGN = 0x00000080;
    public const int DCX_INTERSECTUPDATE = 0x00000200;
    public const int DCX_VALIDATE = 0x00200000;

    //RECT Structure
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct RECT
    {
        public int left, top, right, bottom;
    }

    //WINDOWPOS Structure
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct WINDOWPOS
    {
        public IntPtr hwnd;
        public IntPtr hwndinsertafter;
        public int x, y, cx, cy;
        public int flags;
    }

    //NCCALCSIZE_PARAMS Structure
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct NCCALCSIZE_PARAMS
    {
        public RECT rgrc0, rgrc1, rgrc2;
        public WINDOWPOS lppos;
    }

    //SetWindowTheme UXtheme Function
    [System.Runtime.InteropServices.DllImport("uxtheme.dll", ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    public static extern int SetWindowTheme(
        IntPtr hWnd,
        String pszSubAppName,
        String pszSubIdList);

    //GetWindowRect User32 Function
    [System.Runtime.InteropServices.DllImport("user32.dll", ExactSpelling = true)]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    public static extern bool GetWindowRect(
        IntPtr hwnd,
        out RECT lpRect
        );

    //GetWindowDC User32 Function
    [System.Runtime.InteropServices.DllImport("user32.dll", ExactSpelling = true)]
    public static extern IntPtr GetWindowDC(
        IntPtr hWnd
        );

    //GetDCEx User32 Function
    [System.Runtime.InteropServices.DllImport("user32.dll", ExactSpelling = true)]
    public static extern IntPtr GetDCEx(
        IntPtr hWnd,
        IntPtr hrgnClip,
        int flags
        );

    [DllImport("user32.dll")]
    static extern bool AdjustWindowRect(ref RECT lpRect, uint dwStyle, bool bMenu);

    //WM_NCPAINT
    public static void WmNCPaint(IntPtr HDC)
    {
        //Store HDC
        Graphics gfx = null;

        //Graphics Object from HDC
        gfx = Graphics.FromHdc(HDC);

        //Exclude Client Area
        gfx.ExcludeClip(new Rectangle(0, 0, 100, 100));  //Exclude Client Area (GetWindowDC grabs the WHOLE window's graphics handle)
    }

    [DllImport("USER32.DLL")]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
    static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);

    [DllImport("user32.dll")]
    static extern IntPtr GetMenu(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern int GetMenuItemCount(IntPtr hMenu);

    [DllImport("user32.dll")]
    static extern bool DrawMenuBar(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool RemoveMenu(IntPtr hMenu, uint uPosition, uint uFlags);

    public static uint MF_BYPOSITION = 0x400;
    public static uint MF_REMOVE = 0x1000;

    public static void WindowsReStyle(IntPtr winHandle)
    {
        //get menu
        IntPtr HMENU = GetMenu(winHandle);
        //get item count
        int count = GetMenuItemCount(HMENU);
        //loop & remove
        for (int i = 0; i < count; i++)
            RemoveMenu(HMENU, 0, (MF_BYPOSITION | MF_REMOVE));

        //force a redraw
        DrawMenuBar(winHandle);
    }
}