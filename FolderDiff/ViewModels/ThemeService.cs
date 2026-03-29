using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FolderDiff.ViewModels;

/// <summary>
/// Manages the application light/dark theme and persists the preference.
/// </summary>
public class ThemeService : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static readonly ThemeService Instance = new();

    private bool _isDark;

    private ThemeService()
    {
        _isDark = SettingsService.LoadTheme() != "light";
        Apply();
    }

    /// <summary>
    /// Gets whether the dark theme is currently active.
    /// </summary>
    public bool IsDark => _isDark;

    /// <summary>
    /// Gets the icon label for the theme toggle button.
    /// </summary>
    public string Label => _isDark ? "☀" : "🌙";

    /// <summary>
    /// Toggles between light and dark theme and persists the preference.
    /// </summary>
    public void Toggle()
    {
        _isDark = !_isDark;
        SettingsService.SaveTheme(_isDark ? "dark" : "light");
        Apply();
        OnPropertyChanged(nameof(IsDark));
        OnPropertyChanged(nameof(Label));
    }

    /// <summary>
    /// Applies the current theme variant to the application and all open windows.
    /// </summary>
    public void Apply()
    {
        if (Application.Current is not { } app)
        {
            return;
        }

        var variant = _isDark ? ThemeVariant.Dark : ThemeVariant.Light;
        app.RequestedThemeVariant = variant;

        if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            foreach (var window in desktop.Windows)
            {
                window.RequestedThemeVariant = variant;
            }
        }
    }
}
