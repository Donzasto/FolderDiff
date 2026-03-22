using System;
using System.IO;
using System.Text.Json;

namespace FolderDiff.ViewModels;

public static class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FolderDiff", "settings.json");

    private record AppSettings(string Language = "ru", string Theme = "dark");

    private static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return new AppSettings();
            }
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    private static void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings));
        }
        catch
        {
        }
    }

    public static string LoadLanguage() => Load().Language;
    public static string LoadTheme()    => Load().Theme;

    public static void SaveLanguage(string lang)  => Save(Load() with { Language = lang });
    public static void SaveTheme(string theme)    => Save(Load() with { Theme = theme });
}
