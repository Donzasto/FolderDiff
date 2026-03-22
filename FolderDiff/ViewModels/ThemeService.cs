using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FolderDiff.ViewModels;

public class ThemeService : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public static readonly ThemeService Instance = new();

    private bool _isDark;

    private ThemeService()
    {
        _isDark = SettingsService.LoadTheme() != "light";
        Apply();
    }

    public bool IsDark => _isDark;

    public string Label => _isDark ? "☀" : "🌙";

    public void Toggle()
    {
        _isDark = !_isDark;
        SettingsService.SaveTheme(_isDark ? "dark" : "light");
        Apply();
        OnPropertyChanged(nameof(IsDark));
        OnPropertyChanged(nameof(Label));
    }

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
