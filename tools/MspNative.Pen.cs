// Synthetic pen input (InjectSyntheticPointerInput) for the self-test scripts.
// Compiled by Windows PowerShell 5.1 via Add-Type together with the other MspNative.*.cs files: C# 5 syntax only.
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

public static partial class MspNative
{
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

    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr CreateSyntheticPointerDevice(uint pointerType, uint maxCount, uint mode);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool InjectSyntheticPointerInput(IntPtr device, [In] POINTER_TYPE_INFO[] pointerInfo, uint count);
    [DllImport("user32.dll")] private static extern void DestroySyntheticPointerDevice(IntPtr device);

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

    // A stroke along a polyline (client DIPs), about one frame per stepPixels on screen, constant pressure. holdMs keeps
    // the pen down and still on the last point before lifting (shape recognition by resting).
    public static string PenPath(IntPtr hwnd, double[] xs, double[] ys, int pressure, double stepPixels, int delayMs,
        bool barrel, bool inverted, int holdMs)
    {
        IntPtr previous = SetThreadDpiAwarenessContext(PerMonitorAwareV2);
        try
        {
            POINT[] points = new POINT[xs.Length];
            for (int i = 0; i < xs.Length; i++)
            {
                points[i] = ToScreen(hwnd, xs[i], ys[i]);
                RequireAppAt(hwnd, points[i]);
            }
            IntPtr device = CreateSyntheticPointerDevice(PT_PEN, 1, POINTER_FEEDBACK_DEFAULT);
            if (device == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateSyntheticPointerDevice");
            int frames = 0;
            try
            {
                uint hoverPen = (barrel ? PEN_FLAG_BARREL : 0) | (inverted ? PEN_FLAG_INVERTED : 0);
                uint contactPen = hoverPen | (inverted ? PEN_FLAG_ERASER : 0);
                uint contact = POINTER_FLAG_INRANGE | POINTER_FLAG_INCONTACT | (barrel ? POINTER_FLAG_SECONDBUTTON : POINTER_FLAG_FIRSTBUTTON);
                POINT first = points[0], last = points[points.Length - 1];
                for (int i = 0; i < 6; i++) PenFrame(device, first.X, first.Y, POINTER_FLAG_INRANGE | POINTER_FLAG_UPDATE, hoverPen, 0, delayMs);
                PenFrame(device, first.X, first.Y, contact | POINTER_FLAG_DOWN, contactPen, (uint)pressure, delayMs);
                for (int segment = 1; segment < points.Length; segment++)
                {
                    POINT from = points[segment - 1], to = points[segment];
                    double length = Math.Sqrt((double)(to.X - from.X) * (to.X - from.X) + (double)(to.Y - from.Y) * (to.Y - from.Y));
                    int steps = Math.Max(1, (int)Math.Ceiling(length / stepPixels));
                    for (int i = 1; i <= steps; i++)
                    {
                        double t = (double)i / steps;
                        PenFrame(device, (int)Math.Round(from.X + (to.X - from.X) * t), (int)Math.Round(from.Y + (to.Y - from.Y) * t),
                            contact | POINTER_FLAG_UPDATE, contactPen, (uint)pressure, delayMs);
                        frames++;
                    }
                }
                for (int waited = 0; waited < holdMs; waited += delayMs)
                    PenFrame(device, last.X, last.Y, contact | POINTER_FLAG_UPDATE, contactPen, (uint)pressure, delayMs);
                PenFrame(device, last.X, last.Y, POINTER_FLAG_INRANGE | POINTER_FLAG_UP, hoverPen, 0, delayMs);
                for (int i = 0; i < 6; i++) PenFrame(device, last.X, last.Y, POINTER_FLAG_INRANGE | POINTER_FLAG_UPDATE, hoverPen, 0, delayMs);
                PenFrame(device, last.X, last.Y, POINTER_FLAG_UPDATE, 0, 0, delayMs);
            }
            finally { DestroySyntheticPointerDevice(device); }
            return "points=" + points.Length + " frames=" + frames + " hold=" + holdMs;
        }
        finally { SetThreadDpiAwarenessContext(previous); }
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
}
