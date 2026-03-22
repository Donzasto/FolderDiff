using System.Globalization;
using System.Resources;

namespace FolderDiff.ViewModels;

public class Localization : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public static readonly Localization Instance = new();

    private CultureInfo _culture = CultureInfo.GetCultureInfo("ru");

    private static readonly ResourceManager Rm =
        new("FolderDiff.Properties.Resources", typeof(Localization).Assembly);

    private Localization()
    {
        var saved = SettingsService.LoadLanguage();
        _culture = CultureInfo.GetCultureInfo(saved == "en" ? "en" : "ru");
    }

    private string GetString(string key) => Rm.GetString(key, _culture) ?? key;

    public bool IsEnglish => _culture.Name.StartsWith("en");
    public string CurrentLangLabel => IsEnglish ? "EN" : "RU";

    public void SetLanguage(string lang)
    {
        var code = lang == "en" ? "en" : "ru";
        _culture = CultureInfo.GetCultureInfo(code);
        SettingsService.SaveLanguage(code);
        OnPropertyChanged(string.Empty);
    }

    public void ToggleLanguage() => SetLanguage(IsEnglish ? "ru" : "en");

    public string AppTitle           => GetString(nameof(AppTitle));
    public string ModeLabel          => GetString(nameof(ModeLabel));
    public string CompareTwoFolders  => GetString(nameof(CompareTwoFolders));
    public string FindDuplicates     => GetString(nameof(FindDuplicates));
    public string Folder1Label       => GetString(nameof(Folder1Label));
    public string Folder2Label       => GetString(nameof(Folder2Label));
    public string FolderLabel        => GetString(nameof(FolderLabel));
    public string Browse             => GetString(nameof(Browse));
    public string Folder1Watermark   => GetString(nameof(Folder1Watermark));
    public string Folder2Watermark   => GetString(nameof(Folder2Watermark));
    public string FolderWatermark    => GetString(nameof(FolderWatermark));
    public string ComparisonMethod   => GetString(nameof(ComparisonMethod));
    public string ByHash             => GetString(nameof(ByHash));
    public string ByHashTip          => GetString(nameof(ByHashTip));
    public string ByName             => GetString(nameof(ByName));
    public string ByNameTip          => GetString(nameof(ByNameTip));
    public string ExcludeFolders     => GetString(nameof(ExcludeFolders));
    public string ExcludeWatermark   => GetString(nameof(ExcludeWatermark));
    public string Add                => GetString(nameof(Add));
    public string Run                => GetString(nameof(Run));
    public string SelectOldFiles     => GetString(nameof(SelectOldFiles));
    public string SelectOldFilesTip  => GetString(nameof(SelectOldFilesTip));
    public string ClearSelection     => GetString(nameof(ClearSelection));
    public string DeleteSelected     => GetString(nameof(DeleteSelected));
    public string DeleteEmptyFolders => GetString(nameof(DeleteEmptyFolders));

    public string Cancel                => GetString(nameof(Cancel));
    public string Cancelled             => GetString(nameof(Cancelled));
    public string Processing            => GetString(nameof(Processing));
    public string FoldersIdentical      => GetString(nameof(FoldersIdentical));
    public string NoDuplicates          => GetString(nameof(NoDuplicates));
    public string FoundDiffsFormat      => GetString(nameof(FoundDiffsFormat));
    public string FoundGroupsFormat     => GetString(nameof(FoundGroupsFormat));
    public string ErrorPrefix           => GetString(nameof(ErrorPrefix));
    public string NoFilesSelected       => GetString(nameof(NoFilesSelected));
    public string DeletingFilesFormat   => GetString(nameof(DeletingFilesFormat));
    public string DeletedFilesFormat    => GetString(nameof(DeletedFilesFormat));
    public string ErrorsFormat          => GetString(nameof(ErrorsFormat));
    public string FolderNotExistsFormat => GetString(nameof(FolderNotExistsFormat));

    public string DiffersLabel    => GetString(nameof(DiffersLabel));
    public string Missing2Label   => GetString(nameof(Missing2Label));
    public string Missing1Label   => GetString(nameof(Missing1Label));
    public string GroupHashFormat => GetString(nameof(GroupHashFormat));
    public string GroupNameFormat => GetString(nameof(GroupNameFormat));
}
