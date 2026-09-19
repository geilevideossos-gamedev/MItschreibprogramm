// Win32 helpers for the self-test scripts. Compiled by Windows PowerShell 5.1 via Add-Type, so C# 5 syntax only.
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;

public static class MspNative
{
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINTER_INFO
    {
        public uint pointerType;
        public uint pointerId;
        public uint frameId;
        public uint pointerFlags;
        public IntPtr sourceDevice;
        public IntPtr hwndTarget;
        public POINT ptPixelLocation;
        public POINT ptHimetricLocation;
        public POINT ptPixelLocationRaw;
        public POINT ptHimetricLocationRaw;
        public uint dwTime;
        public uint historyCount;
        public int InputData;
        public uint dwKeyStates;
        public ulong PerformanceCount;
        public int ButtonChangeType;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINTER_PEN_INFO
    {
        public POINTER_INFO pointerInfo;
        public uint penFlags;
        public uint penMask;
        public uint pressure;
        public uint rotation;
        public int tiltX;
        public int tiltY;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINTER_TOUCH_INFO
    {
        public POINTER_INFO pointerInfo;
        public uint touchFlags;
        public uint touchMask;
        public RECT rcContact;
        public RECT rcContactRaw;
        public uint orientation;
        public uint pressure;
    }

    // Native union: the touch member only pads the struct to the size Windows expects (152 bytes on x64).
    [StructLayout(LayoutKind.Explicit)]
    public struct POINTER_TYPE_INFO
    {
        [FieldOffset(0)] public uint type;
        [FieldOffset(8)] public POINTER_PEN_INFO penInfo;
        [FieldOffset(8)] public POINTER_TOUCH_INFO touchInfo;
    }

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

    private const uint PT_PEN = 3;
    private const uint POINTER_FEEDBACK_DEFAULT = 1;
    private const uint POINTER_FLAG_INRANGE = 0x0002;
    private const uint POINTER_FLAG_INCONTACT = 0x0004;
    private const uint POINTER_FLAG_FIRSTBUTTON = 0x0010;
    private const uint POINTER_FLAG_SECONDBUTTON = 0x0020;
    private const uint POINTER_FLAG_DOWN = 0x00010000;
    private const uint POINTER_FLAG_UPDATE = 0x00020000;
    private const uint POINTER_FLAG_UP = 0x00040000;
    private const uint PEN_FLAG_BARREL = 0x1;
    private const uint PEN_FLAG_INVERTED = 0x2;
    private const uint PEN_FLAG_ERASER = 0x4;
    private const uint PEN_MASK_PRESSURE = 0x1;

    private const uint INPUT_MOUSE = 0, INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x2, KEYEVENTF_UNICODE = 0x4;
    private const uint MOUSEEVENTF_MOVE = 0x1, MOUSEEVENTF_LEFTDOWN = 0x2, MOUSEEVENTF_LEFTUP = 0x4;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x20, MOUSEEVENTF_MIDDLEUP = 0x40, MOUSEEVENTF_WHEEL = 0x800;
    private const uint MOUSEEVENTF_VIRTUALDESK = 0x4000, MOUSEEVENTF_ABSOLUTE = 0x8000;
    private const int SM_XVIRTUALSCREEN = 76, SM_YVIRTUALSCREEN = 77, SM_CXVIRTUALSCREEN = 78, SM_CYVIRTUALSCREEN = 79;
    private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
    private const uint PW_RENDERFULLCONTENT = 2;
    private const uint SWP_NOSIZE = 0x1, SWP_NOMOVE = 0x2, SWP_NOZORDER = 0x4, SWP_SHOWWINDOW = 0x40;
    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1), HWND_NOTOPMOST = new IntPtr(-2);
    private static readonly IntPtr PerMonitorAwareV2 = new IntPtr(-4);

    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr CreateSyntheticPointerDevice(uint pointerType, uint maxCount, uint mode);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool InjectSyntheticPointerInput(IntPtr device, [In] POINTER_TYPE_INFO[] pointerInfo, uint count);
    [DllImport("user32.dll")] private static extern void DestroySyntheticPointerDevice(IntPtr device);
    [DllImport("user32.dll")] private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
    [DllImport("user32.dll")] private static extern bool ClientToScreen(IntPtr hwnd, ref POINT point);
    [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr hwnd, out RECT rect);
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
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, INPUT[] inputs, int size);
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(IntPtr hwnd, int attribute, out RECT value, int size);

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

    public static string PenStroke(IntPtr hwnd, double fromX, double fromY, double toX, double toY,
        int pressureFrom, int pressureTo, int steps, int delayMs, bool barrel, bool inverted)
    {
        IntPtr previous = SetThreadDpiAwarenessContext(PerMonitorAwareV2);
        try
        {
            POINT from = ToScreen(hwnd, fromX, fromY), to = ToScreen(hwnd, toX, toY);
            RequireAppAt(hwnd, from);
            RequireAppAt(hwnd, to);
            IntPtr device = CreateSyntheticPointerDevice(PT_PEN, 1, POINTER_FEEDBACK_DEFAULT);
            if (device == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateSyntheticPointerDevice");
            try
            {
                uint hoverPen = (barrel ? PEN_FLAG_BARREL : 0) | (inverted ? PEN_FLAG_INVERTED : 0);
                uint contactPen = hoverPen | (inverted ? PEN_FLAG_ERASER : 0);
                uint contact = POINTER_FLAG_INRANGE | POINTER_FLAG_INCONTACT | (barrel ? POINTER_FLAG_SECONDBUTTON : POINTER_FLAG_FIRSTBUTTON);
                for (int i = 0; i < 6; i++) PenFrame(device, from.X, from.Y, POINTER_FLAG_INRANGE | POINTER_FLAG_UPDATE, hoverPen, 0, delayMs);
                PenFrame(device, from.X, from.Y, contact | POINTER_FLAG_DOWN, contactPen, (uint)pressureFrom, delayMs);
                for (int i = 1; i <= steps; i++)
                {
                    double t = (double)i / steps;
                    int x = (int)Math.Round(from.X + (to.X - from.X) * t), y = (int)Math.Round(from.Y + (to.Y - from.Y) * t);
                    uint pressure = (uint)Math.Round(pressureFrom + (pressureTo - pressureFrom) * t);
                    PenFrame(device, x, y, contact | POINTER_FLAG_UPDATE, contactPen, pressure, delayMs);
                }
                PenFrame(device, to.X, to.Y, POINTER_FLAG_INRANGE | POINTER_FLAG_UP, hoverPen, 0, delayMs);
                for (int i = 0; i < 6; i++) PenFrame(device, to.X, to.Y, POINTER_FLAG_INRANGE | POINTER_FLAG_UPDATE, hoverPen, 0, delayMs);
                PenFrame(device, to.X, to.Y, POINTER_FLAG_UPDATE, 0, 0, delayMs);
            }
            finally { DestroySyntheticPointerDevice(device); }
            return "screenFrom=" + from.X + "," + from.Y + " screenTo=" + to.X + "," + to.Y;
        }
        finally { SetThreadDpiAwarenessContext(previous); }
    }

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

    private static void PenFrame(IntPtr device, int x, int y, uint pointerFlags, uint penFlags, uint pressure, int delayMs)
    {
        POINTER_TYPE_INFO[] info = new POINTER_TYPE_INFO[1];
        info[0].type = PT_PEN;
        info[0].penInfo.pointerInfo.pointerType = PT_PEN;
        info[0].penInfo.pointerInfo.pointerFlags = pointerFlags;
        info[0].penInfo.pointerInfo.ptPixelLocation.X = x;
        info[0].penInfo.pointerInfo.ptPixelLocation.Y = y;
        info[0].penInfo.penFlags = penFlags;
        info[0].penInfo.penMask = PEN_MASK_PRESSURE;
        info[0].penInfo.pressure = pressure;
        if (!InjectSyntheticPointerInput(device, info, 1))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "InjectSyntheticPointerInput flags=0x" + pointerFlags.ToString("X"));
        Thread.Sleep(delayMs);
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
