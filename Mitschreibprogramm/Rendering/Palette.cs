using System.Windows.Media;
using Mitschreibprogramm.Models;

namespace Mitschreibprogramm.Rendering;

public static class Palette
{
    private const string BlackHex = "#000000";
    private const string BlueHex = "#1E50C8";
    private const string RedHex = "#D0252B";
    private const string GreenHex = "#1E8A3C";

    private const string WhiteHex = "#FFFFFF";
    private const string DarkPageHex = "#2B2B2B";
    private const string DarkRuleBlueHex = "#8FB0FF";
    private const byte RuleLineAlpha = 0x48;
    private const byte DarkRuleLineAlpha = 0x60;

    public static Color Pen(PenColor color) => Parse(color switch
    {
        PenColor.Blue => BlueHex,
        PenColor.Red => RedHex,
        PenColor.Green => GreenHex,
        _ => BlackHex,
    });

    // On the dark page the black pen is drawn white. Files and PDF export keep the logical colour.
    public static Color PenOnPage(PenColor color, bool dark) => dark && color == PenColor.Black ? Parse(WhiteHex) : Pen(color);

    public static PenColor LogicalPen(Color shown) =>
        Enum.GetValues<PenColor>().FirstOrDefault(color => Pen(color) == shown, PenColor.Black);

    public static Color Page(bool dark) => Parse(dark ? DarkPageHex : WhiteHex);

    public static Color RuleLine(LineColor color, bool dark)
    {
        var line = Parse(color == LineColor.Blue ? (dark ? DarkRuleBlueHex : BlueHex) : (dark ? WhiteHex : BlackHex));
        return Color.FromArgb(dark ? DarkRuleLineAlpha : RuleLineAlpha, line.R, line.G, line.B);
    }

    // PDF export: the translucent screen colour blended onto the white page, as one opaque colour.
    public static Color RuleLineOnPaper(LineColor color)
    {
        var line = RuleLine(color, dark: false);
        var paper = Page(dark: false);
        byte Blend(byte ink, byte background) => (byte)Math.Round(background + ((ink - background) * (line.A / 255.0)));
        return Color.FromRgb(Blend(line.R, paper.R), Blend(line.G, paper.G), Blend(line.B, paper.B));
    }

    private static Color Parse(string hex) => (Color)ColorConverter.ConvertFromString(hex);
}
