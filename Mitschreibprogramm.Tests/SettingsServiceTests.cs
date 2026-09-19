using System.IO;
using Mitschreibprogramm.Models;
using Mitschreibprogramm.Services;

namespace Mitschreibprogramm.Tests;

public sealed class SettingsServiceTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("msp-settings-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void SaveAndLoad_RoundTripsAllSettings()
    {
        var service = new SettingsService(Path.Combine(_folder, "nested", "settings.json"));
        var settings = new AppSettings
        {
            PenColor = PenColor.Green,
            StrokeWidth = 6.5,
            PressureEnabled = false,
            PageStyle = PageStyle.Squared,
            LineColor = LineColor.Black,
            PageMode = PageMode.Endless,
            DarkMode = true,
            WindowLeft = 10,
            WindowTop = 20,
            WindowWidth = 900,
            WindowHeight = 700,
            WindowMaximized = true,
            LastFolder = @"C:\Notizen",
        };

        service.Save(settings);
        var loaded = service.Load();

        Assert.Equal(PenColor.Green, loaded.PenColor);
        Assert.Equal(6.5, loaded.StrokeWidth);
        Assert.False(loaded.PressureEnabled);
        Assert.Equal(PageStyle.Squared, loaded.PageStyle);
        Assert.Equal(LineColor.Black, loaded.LineColor);
        Assert.Equal(PageMode.Endless, loaded.PageMode);
        Assert.True(loaded.DarkMode);
        Assert.Equal(10, loaded.WindowLeft);
        Assert.Equal(20, loaded.WindowTop);
        Assert.Equal(900, loaded.WindowWidth);
        Assert.Equal(700, loaded.WindowHeight);
        Assert.True(loaded.WindowMaximized);
        Assert.Equal(@"C:\Notizen", loaded.LastFolder);
    }

    [Fact]
    public void Load_ReturnsDefaultsWhenFileIsMissing()
    {
        var loaded = new SettingsService(Path.Combine(_folder, "missing.json")).Load();

        Assert.Equal(PenColor.Black, loaded.PenColor);
        Assert.Equal(AppConstants.MediumStrokeWidth, loaded.StrokeWidth);
        Assert.True(loaded.PressureEnabled);
        Assert.Equal(PageStyle.Lined, loaded.PageStyle);
        Assert.Null(loaded.WindowLeft);
        Assert.Null(loaded.LastFolder);
    }

    [Fact]
    public void Load_ReturnsDefaultsWhenFileIsCorrupt()
    {
        var path = Path.Combine(_folder, "corrupt.json");
        File.WriteAllText(path, "{ not json");

        Assert.Equal(PageMode.Pages, new SettingsService(path).Load().PageMode);
    }

    [Fact]
    public void Load_ReturnsDefaultsWhenAnEnumValueIsNotDefined()
    {
        var path = Path.Combine(_folder, "enum.json");
        File.WriteAllText(path, """{"penColor":"7","darkMode":true}""");

        var loaded = new SettingsService(path).Load();

        Assert.Equal(PenColor.Black, loaded.PenColor);
        Assert.False(loaded.DarkMode);
    }

    [Fact]
    public void Save_DoesNotThrowWhenTheFileCannotBeWritten()
    {
        var blocked = Path.Combine(_folder, "blocked.json");
        Directory.CreateDirectory(blocked);

        new SettingsService(blocked).Save(new AppSettings());

        Assert.True(Directory.Exists(blocked));
    }

    [Fact]
    public void Load_ReadsTheFormerDashedStyleAsSquared()
    {
        var path = Path.Combine(_folder, "legacy.json");
        File.WriteAllText(path, """{"pageStyle":"dashed","darkMode":true}""");

        var loaded = new SettingsService(path).Load();

        Assert.Equal(PageStyle.Squared, loaded.PageStyle);
        Assert.True(loaded.DarkMode);
    }

    [Fact]
    public void Load_ClampsStrokeWidthIntoTheAllowedRange()
    {
        var path = Path.Combine(_folder, "width.json");
        File.WriteAllText(path, """{"strokeWidth": 40}""");

        Assert.Equal(AppConstants.MaxStrokeWidth, new SettingsService(path).Load().StrokeWidth);
    }
}
