// Window handling and screenshots for the self-test scripts.
// Compiled by Windows PowerShell 5.1 via Add-Type together with the other MspNative.*.cs files: C# 5 syntax only.
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;

public static partial class MspNative
{
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    private const int SM_XVIRTUALSCREEN = 76, SM_YVIRTUALSCREEN = 77, SM_CXVIRTUALSCREEN = 78, SM_CYVIRTUALSCREEN = 79;
    private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
    private const uint PW_RENDERFULLCONTENT = 2;
    private const uint SWP_NOSIZE = 0x1, SWP_NOMOVE = 0x2, SWP_NOZORDER = 0x4, SWP_SHOWWINDOW = 0x40;
    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1), HWND_NOTOPMOST = new IntPtr(-2);
    private static readonly IntPtr PerMonitorAwareV2 = new IntPtr(-4);

    [DllImport("user32.dll")] private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
    [DllImport("user32.dll")] private static extern bool ClientToScreen(IntPtr hwnd, ref POINT point);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(POINT point);
    [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool BringWindowToTop(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hwnd, int command);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
    [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint attach, uint attachTo, bool on);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(IntPtr hwnd, int attribute, out RECT value, int size);

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO { public uint cbSize; public uint dwTime; }

    [DllImport("user32.dll")] private static extern bool GetLastInputInfo(ref LASTINPUTINFO info);

    [DllImport("kernel32.dll")] private static extern uint SetThreadExecutionState(uint flags);

    private const uint ES_CONTINUOUS = 0x80000000, ES_SYSTEM_REQUIRED = 0x1, ES_DISPLAY_REQUIRED = 0x2;

    // Keeps the machine and the display awake until the calling process exits; a standby in the middle of a run stalls it.
    public static void KeepAwake() { SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED); }

    // Seconds since the last keyboard or mouse input of any kind (injected input counts as well).
    public static int IdleSeconds()
    {
        LASTINPUTINFO info = new LASTINPUTINFO();
        info.cbSize = (uint)Marshal.SizeOf(typeof(LASTINPUTINFO));
        GetLastInputInfo(ref info);
        return (int)(unchecked((uint)Environment.TickCount - info.dwTime) / 1000);
    }

    public static IntPtr FindAppWindow(string processName)
    {
        foreach (Process process in Process.GetProcessesByName(processName))
        {
            if (process.MainWindowHandle != IntPtr.Zero) return process.MainWindowHandle;
        }
        throw new InvalidOperationException("Kein Fenster von '" + processName + "' gefunden.");
    }

    // The window a modal dialog belongs to is disabled; input then has to go to the foreground dialog of the same process.
    public static IntPtr ForegroundWindowOf(IntPtr appWindow)
    {
        IntPtr foreground = GetForegroundWindow();
        return ProcessOf(foreground) == ProcessOf(appWindow) ? foreground : IntPtr.Zero;
    }

    public static bool Activate(IntPtr hwnd)
    {
        if (ForegroundWindowOf(hwnd) != IntPtr.Zero) return true;
        if (IsIconic(hwnd)) ShowWindow(hwnd, 9);
        SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        SetWindowPos(hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        for (int attempt = 0; attempt < 3 && ForegroundWindowOf(hwnd) == IntPtr.Zero; attempt++)
        {
            uint ignored;
            uint foregroundThread = GetWindowThreadProcessId(GetForegroundWindow(), out ignored);
            uint thisThread = GetCurrentThreadId();
            AttachThreadInput(thisThread, foregroundThread, true);
            SetForegroundWindow(hwnd);
            BringWindowToTop(hwnd);
            AttachThreadInput(thisThread, foregroundThread, false);
            Thread.Sleep(200);
        }
        return ForegroundWindowOf(hwnd) != IntPtr.Zero;
    }

    public static string Bounds(IntPtr hwnd)
    {
        IntPtr previous = SetThreadDpiAwarenessContext(PerMonitorAwareV2);
        try
        {
            RECT r;
            GetWindowRect(hwnd, out r);
            return r.Left + "," + r.Top + "," + (r.Right - r.Left) + "," + (r.Bottom - r.Top);
        }
        finally { SetThreadDpiAwarenessContext(previous); }
    }

    public static void Place(IntPtr hwnd, int x, int y, int width, int height)
    {
        IntPtr previous = SetThreadDpiAwarenessContext(PerMonitorAwareV2);
        try { SetWindowPos(hwnd, IntPtr.Zero, x, y, width, height, SWP_NOZORDER | SWP_SHOWWINDOW); }
        finally { SetThreadDpiAwarenessContext(previous); }
    }

    // PrintWindow renders the window itself, so the image is correct even if another window covers it.
    public static string Capture(IntPtr hwnd, string path)
    {
        IntPtr previous = SetThreadDpiAwarenessContext(PerMonitorAwareV2);
        try
        {
            RECT window, frame;
            GetWindowRect(hwnd, out window);
            if (DwmGetWindowAttribute(hwnd, DWMWA_EXTENDED_FRAME_BOUNDS, out frame, Marshal.SizeOf(typeof(RECT))) != 0) frame = window;
            using (Bitmap full = new Bitmap(window.Right - window.Left, window.Bottom - window.Top))
            {
                using (Graphics g = Graphics.FromImage(full))
                {
                    IntPtr hdc = g.GetHdc();
                    try { PrintWindow(hwnd, hdc, PW_RENDERFULLCONTENT); }
                    finally { g.ReleaseHdc(hdc); }
                }
                Rectangle visible = new Rectangle(frame.Left - window.Left, frame.Top - window.Top, frame.Right - frame.Left, frame.Bottom - frame.Top);
                using (Bitmap cropped = full.Clone(visible, full.PixelFormat)) cropped.Save(path, ImageFormat.Png);
            }
            return (frame.Right - frame.Left) + "x" + (frame.Bottom - frame.Top);
        }
        finally { SetThreadDpiAwarenessContext(previous); }
    }

    // Colour of the screen pixel at a client position, as "r,g,b". The window has to be visible there.
    public static string ClientPixel(IntPtr hwnd, double x, double y)
    {
        IntPtr previous = SetThreadDpiAwarenessContext(PerMonitorAwareV2);
        try
        {
            POINT at = ToScreen(hwnd, x, y);
            RequireAppAt(hwnd, at);
            using (Bitmap bitmap = new Bitmap(1, 1))
            {
                using (Graphics g = Graphics.FromImage(bitmap)) g.CopyFromScreen(at.X, at.Y, 0, 0, new Size(1, 1));
                Color c = bitmap.GetPixel(0, 0);
                return c.R + "," + c.G + "," + c.B;
            }
        }
        finally { SetThreadDpiAwarenessContext(previous); }
    }

    public static string CaptureScreen(string path)
    {
        IntPtr previous = SetThreadDpiAwarenessContext(PerMonitorAwareV2);
        try
        {
            int x = GetSystemMetrics(SM_XVIRTUALSCREEN), y = GetSystemMetrics(SM_YVIRTUALSCREEN);
            int width = GetSystemMetrics(SM_CXVIRTUALSCREEN), height = GetSystemMetrics(SM_CYVIRTUALSCREEN);
            using (Bitmap bitmap = new Bitmap(width, height))
            {
                using (Graphics g = Graphics.FromImage(bitmap)) g.CopyFromScreen(x, y, 0, 0, new Size(width, height));
                bitmap.Save(path, ImageFormat.Png);
            }
            return width + "x" + height;
        }
        finally { SetThreadDpiAwarenessContext(previous); }
    }

    public static int ProcessIdOf(IntPtr hwnd) { return (int)ProcessOf(hwnd); }

    private static uint ProcessOf(IntPtr hwnd)
    {
        uint processId;
        GetWindowThreadProcessId(hwnd, out processId);
        return processId;
    }

    private static POINT ToScreen(IntPtr hwnd, double x, double y)
    {
        POINT origin = new POINT();
        ClientToScreen(hwnd, ref origin);
        double scale = GetDpiForWindow(hwnd) / 96.0;
        POINT result = new POINT();
        result.X = origin.X + (int)Math.Round(x * scale);
        result.Y = origin.Y + (int)Math.Round(y * scale);
        return result;
    }

    // Never inject into a foreign window: the pixel has to belong to the app's process.
    private static void RequireAppAt(IntPtr hwnd, POINT point)
    {
        IntPtr root = GetAncestor(WindowFromPoint(point), 2);
        if (ProcessOf(root) != ProcessOf(hwnd))
            throw new InvalidOperationException("Abbruch: Bildschirmpunkt " + point.X + "," + point.Y + " gehoert nicht zum App-Fenster (verdeckt oder ausserhalb).");
    }
}
