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
            return settings;
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(settings, JsonFormat.Indented));
    }
}
