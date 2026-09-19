// Keyboard and mouse input (SendInput) for the self-test scripts.
// Compiled by Windows PowerShell 5.1 via Add-Type together with the other MspNative.*.cs files: C# 5 syntax only.
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

public static partial class MspNative
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT { public int dx, dy; public uint mouseData, dwFlags, time; public IntPtr dwExtraInfo; }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT { public ushort wVk, wScan; public uint dwFlags, time; public IntPtr dwExtraInfo; }

    [StructLayout(LayoutKind.Explicit)]
    public struct INPUTUNION
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT { public uint type; public INPUTUNION u; }

    private const uint INPUT_MOUSE = 0, INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x2, KEYEVENTF_UNICODE = 0x4;
    private const uint MOUSEEVENTF_MOVE = 0x1, MOUSEEVENTF_LEFTDOWN = 0x2, MOUSEEVENTF_LEFTUP = 0x4;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x20, MOUSEEVENTF_MIDDLEUP = 0x40, MOUSEEVENTF_WHEEL = 0x800;
    private const uint MOUSEEVENTF_VIRTUALDESK = 0x4000, MOUSEEVENTF_ABSOLUTE = 0x8000;

    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, INPUT[] inputs, int size);

    // vks: modifier keys first, e.g. { 0x11, 0x5A } for Ctrl+Z. Keys go to the foreground window, so it has to be ours.
    public static void KeyChord(IntPtr hwnd, ushort[] vks)
    {
        if (ForegroundWindowOf(hwnd) == IntPtr.Zero) throw new InvalidOperationException("App ist nicht im Vordergrund, Tasten werden nicht gesendet.");
        for (int i = 0; i < vks.Length; i++) Send(KeyInput(vks[i], 0, 0));
        for (int i = vks.Length - 1; i >= 0; i--) Send(KeyInput(vks[i], 0, KEYEVENTF_KEYUP));
        Thread.Sleep(120);
    }

    public static void KeyDown(IntPtr hwnd, ushort vk)
    {
        if (ForegroundWindowOf(hwnd) == IntPtr.Zero) throw new InvalidOperationException("App ist nicht im Vordergrund, Tasten werden nicht gesendet.");
        Send(KeyInput(vk, 0, 0));
    }

    public static void KeyUp(ushort vk) { Send(KeyInput(vk, 0, KEYEVENTF_KEYUP)); }

    public static void TypeText(IntPtr hwnd, string text)
    {
        if (ForegroundWindowOf(hwnd) == IntPtr.Zero) throw new InvalidOperationException("App ist nicht im Vordergrund, Text wird nicht gesendet.");
        foreach (char c in text)
        {
            Send(KeyInput(0, c, KEYEVENTF_UNICODE));
            Send(KeyInput(0, c, KEYEVENTF_UNICODE | KEYEVENTF_KEYUP));
        }
        Thread.Sleep(120);
    }

    public static void MouseWheel(IntPtr hwnd, double x, double y, int delta)
    {
        IntPtr previous = SetThreadDpiAwarenessContext(PerMonitorAwareV2);
        try
        {
            POINT at = ToScreen(hwnd, x, y);
            RequireAppAt(hwnd, at);
            MouseMove(at);
            Send(MouseInput(0, 0, (uint)delta, MOUSEEVENTF_WHEEL));
            Thread.Sleep(150);
        }
        finally { SetThreadDpiAwarenessContext(previous); }
    }

    // button: "left" or "middle"
    public static void MouseDrag(IntPtr hwnd, string button, double fromX, double fromY, double toX, double toY, int steps)
    {
        IntPtr previous = SetThreadDpiAwarenessContext(PerMonitorAwareV2);
        try
        {
            POINT from = ToScreen(hwnd, fromX, fromY), to = ToScreen(hwnd, toX, toY);
            RequireAppAt(hwnd, from);
            RequireAppAt(hwnd, to);
            bool middle = button == "middle";
            MouseMove(from);
            Send(MouseInput(0, 0, 0, middle ? MOUSEEVENTF_MIDDLEDOWN : MOUSEEVENTF_LEFTDOWN));
            for (int i = 1; i <= steps; i++)
            {
                POINT p = new POINT();
                p.X = from.X + (to.X - from.X) * i / steps;
                p.Y = from.Y + (to.Y - from.Y) * i / steps;
                MouseMove(p);
                Thread.Sleep(10);
            }
            Send(MouseInput(0, 0, 0, middle ? MOUSEEVENTF_MIDDLEUP : MOUSEEVENTF_LEFTUP));
            Thread.Sleep(150);
        }
        finally { SetThreadDpiAwarenessContext(previous); }
    }

    private static void MouseMove(POINT point)
    {
        int left = GetSystemMetrics(SM_XVIRTUALSCREEN), top = GetSystemMetrics(SM_YVIRTUALSCREEN);
        int width = GetSystemMetrics(SM_CXVIRTUALSCREEN), height = GetSystemMetrics(SM_CYVIRTUALSCREEN);
        int dx = (int)Math.Round((point.X - left) * 65535.0 / (width - 1)), dy = (int)Math.Round((point.Y - top) * 65535.0 / (height - 1));
        Send(MouseInput(dx, dy, 0, MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK));
    }

    private static INPUT MouseInput(int dx, int dy, uint data, uint flags)
    {
        INPUT input = new INPUT();
        input.type = INPUT_MOUSE;
        input.u.mi.dx = dx;
        input.u.mi.dy = dy;
        input.u.mi.mouseData = data;
        input.u.mi.dwFlags = flags;
        return input;
    }

    private static INPUT KeyInput(ushort vk, ushort scan, uint flags)
    {
        INPUT input = new INPUT();
        input.type = INPUT_KEYBOARD;
        input.u.ki.wVk = vk;
        input.u.ki.wScan = scan;
        input.u.ki.dwFlags = flags;
        return input;
    }

    private static void Send(INPUT input)
    {
        if (SendInput(1, new INPUT[] { input }, Marshal.SizeOf(typeof(INPUT))) != 1)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "SendInput");
        Thread.Sleep(15);
    }
}
