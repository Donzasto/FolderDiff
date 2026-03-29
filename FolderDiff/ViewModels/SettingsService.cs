using System;
using System.IO;
using System.Text.Json;

namespace FolderDiff.ViewModels;

/// <summary>
/// Persists user preferences (language, theme) to a JSON file in <c>%AppData%\FolderDiff</c>.
/// </summary>
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

    /// <summary>
    /// Returns the saved UI language code.
    /// </summary>
    /// <returns><c>"ru"</c> or <c>"en"</c>.</returns>
    public static string LoadLanguage() => Load().Language;

    /// <summary>
    /// Returns the saved theme name.
    /// </summary>
    /// <returns><c>"dark"</c> or <c>"light"</c>.</returns>
    public static string LoadTheme() => Load().Theme;

    /// <summary>
    /// Saves the UI language code.
    /// </summary>
    /// <param name="lang">Language code to persist (<c>"ru"</c> or <c>"en"</c>).</param>
    public static void SaveLanguage(string lang) => Save(Load() with { Language = lang });

    /// <summary>
    /// Saves the theme name.
    /// </summary>
    /// <param name="theme">Theme name to persist (<c>"dark"</c> or <c>"light"</c>).</param>
    public static void SaveTheme(string theme) => Save(Load() with { Theme = theme });
}
