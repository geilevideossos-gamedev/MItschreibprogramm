// Mouse cursor of the screen for the self-test scripts: which cursor is shown, how big, in which colour.
// Compiled by Windows PowerShell 5.1 via Add-Type together with the other MspNative.*.cs files: C# 5 syntax only.
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public static partial class MspNative
{
    [StructLayout(LayoutKind.Sequential)]
    private struct CURSORINFO { public int cbSize; public int flags; public IntPtr hCursor; public POINT ptScreenPos; }

    [StructLayout(LayoutKind.Sequential)]
    private struct ICONINFO { public bool fIcon; public int xHotspot; public int yHotspot; public IntPtr hbmMask; public IntPtr hbmColor; }

    private const int DI_NORMAL = 3;
    private const int CursorCanvas = 256;
    private static readonly string[] StandardCursorNames = { "arrow", "ibeam", "cross", "sizeall", "hand" };
    private static readonly int[] StandardCursorIds = { 32512, 32513, 32515, 32646, 32649 };

    [DllImport("user32.dll")] private static extern bool GetCursorInfo(ref CURSORINFO info);
    [DllImport("user32.dll")] private static extern bool GetIconInfo(IntPtr icon, out ICONINFO info);
    [DllImport("user32.dll")] private static extern IntPtr LoadCursor(IntPtr instance, IntPtr name);
    [DllImport("user32.dll")] private static extern bool DrawIconEx(IntPtr hdc, int x, int y, IntPtr icon, int width, int height, int step, IntPtr brush, int flags);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr handle);

    // "flags=1 kind=custom outer=13 fill=10 center=0,0,0". kind names a standard cursor or is "custom". The cursor is
    // drawn on magenta: outer is the width of what is drawn in the hotspot row, fill counts the pixels there in the
    // hotspot colour, both physical pixels; center is "none" if the hotspot is transparent (a ring).
    // flags 1 = shown, 2 = suppressed (touch or pen input without a cursor).
    public static string CursorState()
    {
        IntPtr previous = SetThreadDpiAwarenessContext(PerMonitorAwareV2);
        try
        {
            CURSORINFO info = ReadCursor();
            string kind = "custom";
            for (int i = 0; i < StandardCursorIds.Length; i++)
            {
                if (info.hCursor == LoadCursor(IntPtr.Zero, new IntPtr(StandardCursorIds[i]))) kind = StandardCursorNames[i];
            }
            if (info.hCursor == IntPtr.Zero) return "flags=" + info.flags + " kind=none";
            Point hotspot = Hotspot(info.hCursor);
            using (Bitmap bitmap = new Bitmap(CursorCanvas, CursorCanvas, PixelFormat.Format24bppRgb))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.Magenta);
                    IntPtr hdc = g.GetHdc();
                    try { DrawIconEx(hdc, 0, 0, info.hCursor, 0, 0, 0, IntPtr.Zero, DI_NORMAL); }
                    finally { g.ReleaseHdc(hdc); }
                }
                Color center = bitmap.GetPixel(hotspot.X, hotspot.Y);
                bool hollow = IsMagenta(center);
                int first = -1, last = -1, fill = 0;
                for (int x = 0; x < CursorCanvas; x++)
                {
                    Color c = bitmap.GetPixel(x, hotspot.Y);
                    if (!IsMagenta(c))
                    {
                        if (first < 0) first = x;
                        last = x;
                    }
                    if (!hollow && Math.Abs(c.R - center.R) + Math.Abs(c.G - center.G) + Math.Abs(c.B - center.B) <= 24) fill++;
                }
                return "flags=" + info.flags + " kind=" + kind + " outer=" + (first < 0 ? 0 : last - first + 1) + " fill=" + fill +
                    " center=" + (hollow ? "none" : center.R + "," + center.G + "," + center.B);
            }
        }
        finally { SetThreadDpiAwarenessContext(previous); }
    }

    // Screenshots made with PrintWindow never contain the cursor; this draws it in at its screen position.
    private static void DrawCursor(IntPtr hdc, int windowLeft, int windowTop)
    {
        CURSORINFO info = ReadCursor();
        if (info.hCursor == IntPtr.Zero) return;
        Point hotspot = Hotspot(info.hCursor);
        DrawIconEx(hdc, info.ptScreenPos.X - windowLeft - hotspot.X, info.ptScreenPos.Y - windowTop - hotspot.Y, info.hCursor, 0, 0, 0, IntPtr.Zero, DI_NORMAL);
    }

    private static CURSORINFO ReadCursor()
    {
        CURSORINFO info = new CURSORINFO();
        info.cbSize = Marshal.SizeOf(typeof(CURSORINFO));
        GetCursorInfo(ref info);
        return info;
    }

    private static Point Hotspot(IntPtr cursor)
    {
        ICONINFO icon;
        if (!GetIconInfo(cursor, out icon)) return new Point(0, 0);
        if (icon.hbmMask != IntPtr.Zero) DeleteObject(icon.hbmMask);
        if (icon.hbmColor != IntPtr.Zero) DeleteObject(icon.hbmColor);
        return new Point(icon.xHotspot, icon.yHotspot);
    }

    private static bool IsMagenta(Color c) { return c.R > 240 && c.G < 15 && c.B > 240; }
}
