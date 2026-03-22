using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System;
using System.Security.Cryptography;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FolderDiff.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IFolderPickerService? _folderPicker;
    private CancellationTokenSource? _cts;

    public MainViewModel()
    {
    }

    public MainViewModel(IFolderPickerService folderPicker)
    {
        _folderPicker = folderPicker;
    }

    public Localization Loc => Localization.Instance;
    public ThemeService Theme => ThemeService.Instance;

    [ObservableProperty]
    private bool _isCompareMode = true;

    [ObservableProperty]
    private bool _isDuplicatesMode = false;

    [ObservableProperty]
    private string _folder1 = string.Empty;

    [ObservableProperty]
    private string _folder2 = string.Empty;

    [ObservableProperty]
    private string _singleFolder = string.Empty;

    [ObservableProperty]
    private bool _isRunning = false;

    [ObservableProperty]
    private double _progress = 0;

    [ObservableProperty]
    private bool _hasResults = false;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _useHashComparison = true;

    [ObservableProperty]
    private bool _useNameComparison = false;

    [ObservableProperty]
    private bool _deleteEmptyFolders = true;

    [ObservableProperty]
    private string _newExcludePattern = string.Empty;

    public ObservableCollection<ResultGroup> ResultGroups { get; } = new();
    public ObservableCollection<string> ExcludedFolders { get; } = [];

    partial void OnIsCompareModeChanged(bool value)
    {
        if (IsDuplicatesMode != !value)
        {
            IsDuplicatesMode = !value;
        }
    }

    partial void OnIsDuplicatesModeChanged(bool value)
    {
        if (IsCompareMode != !value)
        {
            IsCompareMode = !value;
        }
    }

    partial void OnUseHashComparisonChanged(bool value) => UseNameComparison = !value;
    partial void OnUseNameComparisonChanged(bool value) => UseHashComparison = !value;

    [RelayCommand]
    private void SetLanguage(string lang) => Localization.Instance.SetLanguage(lang);

    [RelayCommand]
    private void ToggleLanguage() => Localization.Instance.ToggleLanguage();

    [RelayCommand]
    private void ToggleTheme() => ThemeService.Instance.Toggle();

    [RelayCommand]
    private void CancelRun() => _cts?.Cancel();

    [RelayCommand]
    public void AddExclude()
    {
        var pattern = NewExcludePattern.Trim();
        if (string.IsNullOrEmpty(pattern))
        {
            return;
        }
        if (ExcludedFolders.Contains(pattern, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }
        ExcludedFolders.Add(pattern);
        NewExcludePattern = string.Empty;
    }

    [RelayCommand]
    private void RemoveExclude(string pattern) => ExcludedFolders.Remove(pattern);

    [RelayCommand]
    private async Task BrowseFolder1()
    {
        var path = await _folderPicker!.PickFolderAsync();
        if (path != null)
        {
            Folder1 = path;
        }
    }

    [RelayCommand]
    private async Task BrowseFolder2()
    {
        var path = await _folderPicker!.PickFolderAsync();
        if (path != null)
        {
            Folder2 = path;
        }
    }

    [RelayCommand]
    private async Task BrowseSingleFolder()
    {
        var path = await _folderPicker!.PickFolderAsync();
        if (path != null)
        {
            SingleFolder = path;
        }
    }

    [RelayCommand]
    private async Task RunAsync()
    {
        var loc = Localization.Instance;
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        IsRunning = true;
        Progress = 0;
        StatusMessage = loc.Processing;
        ResultGroups.Clear();
        HasResults = false;

        var excluded      = ExcludedFolders.ToList();
        var useHash       = UseHashComparison;
        var reporter      = new Progress<double>(v => Progress = v);
        var differsLabel  = loc.DiffersLabel;
        var missing2Label = loc.Missing2Label;
        var missing1Label = loc.Missing1Label;
        var groupHashFmt  = loc.GroupHashFormat;
        var groupNameFmt  = loc.GroupNameFormat;
        var folderNotFmt  = loc.FolderNotExistsFormat;

        List<ResultGroup>? groups = null;
        string? error = null;
        bool cancelled = false;

        if (IsCompareMode)
        {
            if (!Directory.Exists(Folder1))
            {
                error = string.Format(folderNotFmt, Folder1);
            }
            else if (!Directory.Exists(Folder2))
            {
                error = string.Format(folderNotFmt, Folder2);
            }
        }
        else
        {
            if (!Directory.Exists(SingleFolder))
            {
                error = string.Format(folderNotFmt, SingleFolder);
            }
        }

        try
        {
            if (error == null)
            {
                if (IsCompareMode)
                {
                    var f1 = Folder1;
                    var f2 = Folder2;
                    groups = await Task.Run(() => CompareFolders(
                        f1, f2, excluded, useHash, reporter,
                        differsLabel, missing2Label, missing1Label, folderNotFmt, ct), ct);
                }
                else
                {
                    var root = SingleFolder;
                    groups = await Task.Run(() => FindDuplicatesInFolder(
                        root, excluded, useHash, reporter,
                        groupHashFmt, groupNameFmt, folderNotFmt, ct), ct);
                }
            }
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }
        finally
        {
            Progress = 100;
            IsRunning = false;
            _cts?.Dispose();
            _cts = null;
        }

        if (cancelled)
        {
            StatusMessage = loc.Cancelled;
        }
        else if (error != null)
        {
            StatusMessage = loc.ErrorPrefix + error;
        }
        else if (groups != null)
        {
            foreach (var g in groups)
            {
                ResultGroups.Add(g);
            }

            HasResults = ResultGroups.Count > 0;
            StatusMessage = HasResults
                ? (IsCompareMode
                    ? string.Format(loc.FoundDiffsFormat, ResultGroups.Sum(g => g.Items.Count))
                    : string.Format(loc.FoundGroupsFormat, ResultGroups.Count))
                : (IsCompareMode ? loc.FoldersIdentical : loc.NoDuplicates);
        }
    }

    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var item in ResultGroups.SelectMany(g => g.Items))
        {
            item.IsSelected = false;
        }
    }

    [RelayCommand]
    private void SelectOldFiles()
    {
        foreach (var group in ResultGroups)
        {
            if (group.Items.Count < 2)
            {
                continue;
            }

            var sorted = group.Items.OrderByDescending(i => i.Modified).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                sorted[i].IsSelected = i > 0;
            }
        }
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        var loc = Localization.Instance;

        var toDelete = ResultGroups
            .SelectMany(g => g.Items)
            .Where(i => i.IsSelected)
            .ToList();

        if (toDelete.Count == 0)
        {
            StatusMessage = loc.NoFilesSelected;
            return;
        }

        IsRunning = true;
        StatusMessage = string.Format(loc.DeletingFilesFormat, toDelete.Count);

        var deleted     = new HashSet<FileResultItem>();
        var errors      = new List<string>();
        var deleteEmpty = DeleteEmptyFolders;

        try
        {
            await Task.Run(() =>
            {
                foreach (var item in toDelete)
                {
                    try
                    {
                        File.Delete(item.FilePath);
                        deleted.Add(item);
                        if (deleteEmpty)
                        {
                            DeleteEmptyDirectoriesUp(Path.GetDirectoryName(item.FilePath));
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{item.FilePath}: {ex.Message}");
                    }
                }
            });

            foreach (var group in ResultGroups.ToList())
            {
                foreach (var item in group.Items.Where(i => deleted.Contains(i)).ToList())
                {
                    group.Items.Remove(item);
                }

                if (group.Items.Count == 0 || (IsDuplicatesMode && group.Items.Count < 2))
                {
                    ResultGroups.Remove(group);
                }
            }

            HasResults = ResultGroups.Count > 0;

            var status = string.Format(loc.DeletedFilesFormat, deleted.Count);
            if (errors.Count > 0)
            {
                status += $"  |  " + string.Format(loc.ErrorsFormat, errors.Count, string.Join("; ", errors.Take(3)));
            }
            StatusMessage = status;
        }
        finally
        {
            IsRunning = false;
        }
    }

    internal static List<ResultGroup> CompareFolders(
        string folder1, string folder2, List<string> excluded, bool useHash,
        IProgress<double> progress,
        string differsLabel, string missing2Label, string missing1Label, string folderNotFmt,
        CancellationToken ct = default)
    {
        if (!Directory.Exists(folder1))
        {
            throw new DirectoryNotFoundException(string.Format(folderNotFmt, folder1));
        }
        if (!Directory.Exists(folder2))
        {
            throw new DirectoryNotFoundException(string.Format(folderNotFmt, folder2));
        }

        var differs  = new ResultGroup { Label = differsLabel };
        var missing2 = new ResultGroup { Label = missing2Label };
        var missing1 = new ResultGroup { Label = missing1Label };

        if (useHash)
        {
            var allFiles1 = GetFilesExcluding(folder1, excluded, ct).ToList();
            var allFiles2 = GetFilesExcluding(folder2, excluded, ct).ToList();
            int total = allFiles1.Count + allFiles2.Count;
            int done  = 0;
            int last  = -1;

            void Tick()
            {
                ct.ThrowIfCancellationRequested();
                int pct = total == 0 ? 100 : (int)(++done * 100.0 / total);
                if (pct != last)
                {
                    progress.Report(pct);
                    last = pct;
                }
            }

            var hashes1 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in allFiles1)
            {
                hashes1[Path.GetRelativePath(folder1, f)] = ComputeHash(f, ct);
                Tick();
            }

            var hashes2 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in allFiles2)
            {
                hashes2[Path.GetRelativePath(folder2, f)] = ComputeHash(f, ct);
                Tick();
            }

            foreach (var file in hashes1)
            {
                var fullPath = Path.Combine(folder1, file.Key);
                if (hashes2.TryGetValue(file.Key, out string? hash2))
                {
                    if (file.Value != hash2)
                    {
                        differs.Items.Add(MakeItem(fullPath));
                    }
                }
                else
                {
                    missing2.Items.Add(MakeItem(fullPath));
                }
            }

            foreach (var file in hashes2)
            {
                if (!hashes1.ContainsKey(file.Key))
                {
                    missing1.Items.Add(MakeItem(Path.Combine(folder2, file.Key)));
                }
            }
        }
        else
        {
            var names1 = GetRelativePaths(folder1, excluded, ct);
            progress.Report(50);
            var names2 = GetRelativePaths(folder2, excluded, ct);
            progress.Report(100);

            foreach (var rel in names1)
            {
                if (!names2.Contains(rel))
                {
                    missing2.Items.Add(MakeItem(Path.Combine(folder1, rel)));
                }
            }

            foreach (var rel in names2)
            {
                if (!names1.Contains(rel))
                {
                    missing1.Items.Add(MakeItem(Path.Combine(folder2, rel)));
                }
            }
        }

        var groups = new List<ResultGroup>();
        if (differs.Items.Count > 0)
        {
            groups.Add(differs);
        }
        if (missing2.Items.Count > 0)
        {
            groups.Add(missing2);
        }
        if (missing1.Items.Count > 0)
        {
            groups.Add(missing1);
        }
        return groups;
    }

    internal static List<ResultGroup> FindDuplicatesInFolder(
        string rootPath, List<string> excluded, bool useHash,
        IProgress<double> progress,
        string groupHashFmt, string groupNameFmt, string folderNotFmt,
        CancellationToken ct = default)
    {
        if (!Directory.Exists(rootPath))
        {
            throw new DirectoryNotFoundException(string.Format(folderNotFmt, rootPath));
        }

        var allFiles = GetFilesExcluding(rootPath, excluded, ct).ToList();
        int total = allFiles.Count;
        int done  = 0;
        int last  = -1;

        var buckets = new Dictionary<string, List<string>>();

        foreach (var file in allFiles)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var key = useHash
                    ? ComputeHash(file, ct)
                    : Path.GetFileName(file).ToLowerInvariant();

                if (!buckets.TryGetValue(key, out var bucket))
                {
                    bucket = [];
                    buckets[key] = bucket;
                }
                bucket.Add(file);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
            }

            int pct = total == 0 ? 100 : (int)(++done * 100.0 / total);
            if (pct != last)
            {
                progress.Report(pct);
                last = pct;
            }
        }

        var groups = new List<ResultGroup>();
        int num = 1;

        foreach (var kvp in buckets.Where(g => g.Value.Count > 1))
        {
            var label = useHash
                ? string.Format(groupHashFmt, num++, kvp.Key[..16], kvp.Value.Count)
                : string.Format(groupNameFmt, num++, kvp.Key, kvp.Value.Count);

            var group = new ResultGroup { Label = label };
            foreach (var file in kvp.Value)
            {
                group.Items.Add(MakeItem(file));
            }
            groups.Add(group);
        }

        return groups;
    }

    private static FileResultItem MakeItem(string path) => new()
    {
        FilePath = path,
        Modified = File.Exists(path) ? File.GetLastWriteTime(path) : DateTime.MinValue
    };

    internal static HashSet<string> GetRelativePaths(string folderPath, List<string> excluded, CancellationToken ct = default)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in GetFilesExcluding(folderPath, excluded, ct))
        {
            paths.Add(Path.GetRelativePath(folderPath, file));
        }
        return paths;
    }

    internal static IEnumerable<string> GetFilesExcluding(string root, List<string> excluded, CancellationToken ct = default)
    {
        var dirs = new Stack<string>();
        dirs.Push(root);

        while (dirs.Count > 0)
        {
            ct.ThrowIfCancellationRequested();

            var dir = dirs.Pop();

            string[] files;
            try
            {
                files = Directory.GetFiles(dir);
            }
            catch
            {
                continue;
            }

            foreach (var f in files)
            {
                yield return f;
            }

            string[] subDirs;
            try
            {
                subDirs = Directory.GetDirectories(dir);
            }
            catch
            {
                continue;
            }

            foreach (var sub in subDirs)
            {
                var name = Path.GetFileName(sub);
                if (excluded.Count == 0 ||
                    !excluded.Any(ex => string.Equals(name, ex, StringComparison.OrdinalIgnoreCase)))
                {
                    dirs.Push(sub);
                }
            }
        }
    }

    internal static string ComputeHash(string filePath, CancellationToken ct = default)
    {
        using var md5    = MD5.Create();
        using var stream = File.OpenRead(filePath);
        var buffer = new byte[81920];
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            ct.ThrowIfCancellationRequested();
            md5.TransformBlock(buffer, 0, read, null, 0);
        }
        md5.TransformFinalBlock([], 0, 0);
        return BitConverter.ToString(md5.Hash!).Replace("-", "").ToLowerInvariant();
    }

    internal static void DeleteEmptyDirectoriesUp(string? dirPath)
    {
        while (!string.IsNullOrEmpty(dirPath) && Directory.Exists(dirPath))
        {
            if (Directory.GetFileSystemEntries(dirPath).Length > 0)
            {
                break;
            }

            Directory.Delete(dirPath);
            dirPath = Path.GetDirectoryName(dirPath);
        }
    }
}
