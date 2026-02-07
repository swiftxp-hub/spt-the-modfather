using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace SwiftXP.SPT.TheModfather.Updater.UI;

[SupportedOSPlatform("windows")]
internal static class WindowHelper
{
    private static readonly IntPtr s_hWND_TOPMOST = new(-1);

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_SHOWWINDOW = 0x0040;

    public static void CenterAndTopMost()
    {
        nint consoleHandle = GetConsoleWindow();
        if (consoleHandle == IntPtr.Zero)
            return;

        GetWindowRect(consoleHandle, out RECT r);

        int winW = r.Right - r.Left;
        int winH = r.Bottom - r.Top;
        int scrW = GetSystemMetrics(SM_CXSCREEN);
        int scrH = GetSystemMetrics(SM_CYSCREEN);

        MoveWindow(consoleHandle, (scrW - winW) / 2, (scrH - winH) / 2, winW, winH, true);
        SetWindowPos(consoleHandle, s_hWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }
}