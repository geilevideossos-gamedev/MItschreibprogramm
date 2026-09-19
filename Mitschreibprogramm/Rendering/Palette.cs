using System.Windows.Media;
using Mitschreibprogramm.Models;

namespace Mitschreibprogramm.Rendering;

public static class Palette
{
    private const string BlackHex = "#000000";
    private const string BlueHex = "#1E50C8";
    private const string RedHex = "#D0252B";
    private const string GreenHex = "#1E8A3C";

    private const string PageHex = "#FFFFFF";
    private const byte RuleLineAlpha = 0x48;

    public static Color Pen(PenColor color) => Parse(color switch
    {
        PenColor.Blue => BlueHex,
        PenColor.Red => RedHex,
        PenColor.Green => GreenHex,
        _ => BlackHex,
    });

    public static Color Page() => Parse(PageHex);

    public static Color RuleLine(LineColor color)
    {
        var line = Parse(color == LineColor.Blue ? BlueHex : BlackHex);
        return Color.FromArgb(RuleLineAlpha, line.R, line.G, line.B);
    }

    private static Color Parse(string hex) => (Color)ColorConverter.ConvertFromString(hex);
}
