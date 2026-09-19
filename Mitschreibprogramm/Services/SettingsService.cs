using System.IO;
using System.Text.Json;
using Mitschreibprogramm.Models;

namespace Mitschreibprogramm.Services;

public sealed class SettingsService(string path)
{
    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mitschreibprogramm", "settings.json");

    public AppSettings Load()
    {
        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), JsonFormat.Indented) ?? new AppSettings();
            settings.StrokeWidth = Math.Clamp(settings.StrokeWidth, AppConstants.MinStrokeWidth, AppConstants.MaxStrokeWidth);
            var valid = Enum.IsDefined(settings.PenColor) && Enum.IsDefined(settings.PageStyle)
                && Enum.IsDefined(settings.LineColor) && Enum.IsDefined(settings.PageMode);
            return valid ? settings : new AppSettings();
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            return new AppSettings();
        }
    }

    // Runs while the window closes: settings that cannot be written must not turn into a crash on exit.
    public void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(settings, JsonFormat.Indented));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
        }
    }
}
