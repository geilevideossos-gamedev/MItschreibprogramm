using System.Windows.Media;
using Mitschreibprogramm.Models;

namespace Mitschreibprogramm.Rendering;

public static class Palette
{
    private const string BlackHex = "#000000";
    private const string BlueHex = "#1E50C8";
    private const string RedHex = "#D0252B";
    private const string GreenHex = "#1E8A3C";

    public static Color Pen(PenColor color) => (Color)ColorConverter.ConvertFromString(color switch
    {
        PenColor.Blue => BlueHex,
        PenColor.Red => RedHex,
        PenColor.Green => GreenHex,
        _ => BlackHex,
    });
}
