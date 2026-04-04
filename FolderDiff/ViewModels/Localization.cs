using System.Globalization;
using System.Reflection;
using System.Resources;

namespace FolderDiff.ViewModels;

/// <summary>
/// Provides localized UI strings and handles language switching at runtime.
/// </summary>
public class Localization : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static readonly Localization Instance = new();

    private CultureInfo _culture = CultureInfo.GetCultureInfo("ru");

    private static readonly ResourceManager Rm =
        new("FolderDiff.Properties.Resources", typeof(Localization).Assembly);

    private static readonly string AppVersion =
        typeof(Localization).Assembly.GetName().Version?.ToString(3) ?? "??";

    private Localization()
    {
        var saved = SettingsService.LoadLanguage();
        _culture = CultureInfo.GetCultureInfo(saved == "en" ? "en" : "ru");
    }

    private string GetString(string key) => Rm.GetString(key, _culture) ?? key;

    /// <summary>
    /// Gets whether the current language is English.
    /// </summary>
    public bool IsEnglish => _culture.Name.StartsWith("en");

    /// <summary>
    /// Gets the short language label shown in the toggle button (<c>"EN"</c> or <c>"RU"</c>).
    /// </summary>
    public string CurrentLangLabel => IsEnglish ? "EN" : "RU";

    /// <summary>
    /// Switches the UI language to the given language code and persists the preference.
    /// </summary>
    /// <param name="lang">Language code: <c>"en"</c> or <c>"ru"</c>.</param>
    public void SetLanguage(string lang)
    {
        var code = lang == "en" ? "en" : "ru";
        _culture = CultureInfo.GetCultureInfo(code);
        SettingsService.SaveLanguage(code);
        OnPropertyChanged(string.Empty);
    }

    /// <summary>
    /// Toggles between English and Russian.
    /// </summary>
    public void ToggleLanguage() => SetLanguage(IsEnglish ? "ru" : "en");

    /// <summary>Gets the application window title with version number.</summary>
    public string AppTitle           => $"{GetString(nameof(AppTitle))} v{AppVersion}";
    /// <summary>Gets the label for the mode selector section.</summary>
    public string ModeLabel          => GetString(nameof(ModeLabel));
    /// <summary>Gets the label for the "Compare two folders" mode option.</summary>
    public string CompareTwoFolders  => GetString(nameof(CompareTwoFolders));
    /// <summary>Gets the label for the "Find duplicates" mode option.</summary>
    public string FindDuplicates     => GetString(nameof(FindDuplicates));
    /// <summary>Gets the label for the first folder input.</summary>
    public string Folder1Label       => GetString(nameof(Folder1Label));
    /// <summary>Gets the label for the second folder input.</summary>
    public string Folder2Label       => GetString(nameof(Folder2Label));
    /// <summary>Gets the label for the single folder input (duplicates mode).</summary>
    public string FolderLabel        => GetString(nameof(FolderLabel));
    /// <summary>Gets the label for the browse button.</summary>
    public string Browse             => GetString(nameof(Browse));
    /// <summary>Gets the watermark text for the first folder input.</summary>
    public string Folder1Watermark   => GetString(nameof(Folder1Watermark));
    /// <summary>Gets the watermark text for the second folder input.</summary>
    public string Folder2Watermark   => GetString(nameof(Folder2Watermark));
    /// <summary>Gets the watermark text for the single folder input.</summary>
    public string FolderWatermark    => GetString(nameof(FolderWatermark));
    /// <summary>Gets the label for the comparison method section.</summary>
    public string ComparisonMethod   => GetString(nameof(ComparisonMethod));
    /// <summary>Gets the label for the hash-based comparison option.</summary>
    public string ByHash             => GetString(nameof(ByHash));
    /// <summary>Gets the tooltip for the hash-based comparison option.</summary>
    public string ByHashTip          => GetString(nameof(ByHashTip));
    /// <summary>Gets the label for the name-based comparison option.</summary>
    public string ByName             => GetString(nameof(ByName));
    /// <summary>Gets the tooltip for the name-based comparison option.</summary>
    public string ByNameTip          => GetString(nameof(ByNameTip));
    /// <summary>Gets the label for the exclude folders section.</summary>
    public string ExcludeFolders     => GetString(nameof(ExcludeFolders));
    /// <summary>Gets the watermark text for the exclude pattern input.</summary>
    public string ExcludeWatermark   => GetString(nameof(ExcludeWatermark));
    /// <summary>Gets the label for the add exclude button.</summary>
    public string Add                => GetString(nameof(Add));
    /// <summary>Gets the label for the run button.</summary>
    public string Run                => GetString(nameof(Run));
    /// <summary>Gets the label for the "Select old files" button.</summary>
    public string SelectOldFiles     => GetString(nameof(SelectOldFiles));
    /// <summary>Gets the tooltip for the "Select old files" button.</summary>
    public string SelectOldFilesTip  => GetString(nameof(SelectOldFilesTip));
    /// <summary>Gets the label for the clear selection button.</summary>
    public string ClearSelection     => GetString(nameof(ClearSelection));
    /// <summary>Gets the label for the delete selected button.</summary>
    public string DeleteSelected     => GetString(nameof(DeleteSelected));
    /// <summary>Gets the label for the "delete empty folders" checkbox.</summary>
    public string DeleteEmptyFolders => GetString(nameof(DeleteEmptyFolders));
    /// <summary>Gets the label for the cancel button.</summary>
    public string Cancel                => GetString(nameof(Cancel));
    /// <summary>Gets the status message shown when the operation is cancelled.</summary>
    public string Cancelled             => GetString(nameof(Cancelled));
    /// <summary>Gets the status message shown while processing.</summary>
    public string Processing            => GetString(nameof(Processing));
    /// <summary>Gets the status message shown when the compared folders are identical.</summary>
    public string FoldersIdentical      => GetString(nameof(FoldersIdentical));
    /// <summary>Gets the status message shown when no duplicates are found.</summary>
    public string NoDuplicates          => GetString(nameof(NoDuplicates));
    /// <summary>Gets the format string for the "found N differences" status. Parameter: <c>{0}</c> = count.</summary>
    public string FoundDiffsFormat      => GetString(nameof(FoundDiffsFormat));
    /// <summary>Gets the format string for the "found N duplicate groups" status. Parameter: <c>{0}</c> = count.</summary>
    public string FoundGroupsFormat     => GetString(nameof(FoundGroupsFormat));
    /// <summary>Gets the prefix prepended to error messages in the status bar.</summary>
    public string ErrorPrefix           => GetString(nameof(ErrorPrefix));
    /// <summary>Gets the status message shown when no files are selected for deletion.</summary>
    public string NoFilesSelected       => GetString(nameof(NoFilesSelected));
    /// <summary>Gets the format string for the "deleting N files" status. Parameter: <c>{0}</c> = count.</summary>
    public string DeletingFilesFormat   => GetString(nameof(DeletingFilesFormat));
    /// <summary>Gets the format string for the "deleted N files" status. Parameter: <c>{0}</c> = count.</summary>
    public string DeletedFilesFormat    => GetString(nameof(DeletedFilesFormat));
    /// <summary>Gets the format string for the deletion error count in the status bar. Parameter: <c>{0}</c> = count.</summary>
    public string ErrorsFormat          => GetString(nameof(ErrorsFormat));
    /// <summary>Gets the format string shown when a folder does not exist. Parameter: <c>{0}</c> = path.</summary>
    public string FolderNotExistsFormat => GetString(nameof(FolderNotExistsFormat));
    /// <summary>Gets the group label for files that differ in content.</summary>
    public string DiffersLabel    => GetString(nameof(DiffersLabel));
    /// <summary>Gets the group label for files missing in the second folder.</summary>
    public string Missing2Label   => GetString(nameof(Missing2Label));
    /// <summary>Gets the group label for files missing in the first folder.</summary>
    public string Missing1Label   => GetString(nameof(Missing1Label));
    /// <summary>Gets the format string for duplicate group labels in hash mode. Parameters: <c>{0}</c> = index, <c>{1}</c> = hash prefix, <c>{2}</c> = count.</summary>
    public string GroupHashFormat => GetString(nameof(GroupHashFormat));
    /// <summary>Gets the format string for duplicate group labels in name mode. Parameters: <c>{0}</c> = index, <c>{1}</c> = file name, <c>{2}</c> = count.</summary>
    public string GroupNameFormat => GetString(nameof(GroupNameFormat));
}
