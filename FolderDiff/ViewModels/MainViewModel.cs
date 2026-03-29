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

/// <summary>
/// Main view model — orchestrates folder comparison, duplicate search, and file deletion.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IFolderPickerService? _folderPicker;
    private CancellationTokenSource? _cts;

    /// <summary>
    /// Initializes a design-time instance with no services.
    /// </summary>
    public MainViewModel()
    {
    }

    /// <summary>
    /// Initializes a runtime instance with the given folder picker service.
    /// </summary>
    /// <param name="folderPicker">Platform-specific folder picker implementation.</param>
    public MainViewModel(IFolderPickerService folderPicker)
    {
        _folderPicker = folderPicker;
    }

    /// <summary>
    /// Gets the localization provider.
    /// </summary>
    public Localization Loc => Localization.Instance;

    /// <summary>
    /// Gets the theme service.
    /// </summary>
    public ThemeService Theme => ThemeService.Instance;

    /// <summary>
    /// Gets or sets whether the two-folder comparison mode is active.
    /// </summary>
    [ObservableProperty]
    private bool _isCompareMode = true;

    /// <summary>
    /// Gets or sets whether the find-duplicates mode is active.
    /// </summary>
    [ObservableProperty]
    private bool _isDuplicatesMode = false;

    /// <summary>
    /// Gets or sets the path to the first folder (compare mode).
    /// </summary>
    [ObservableProperty]
    private string _folder1 = string.Empty;

    /// <summary>
    /// Gets or sets the path to the second folder (compare mode).
    /// </summary>
    [ObservableProperty]
    private string _folder2 = string.Empty;

    /// <summary>
    /// Gets or sets the path to the folder to scan for duplicates.
    /// </summary>
    [ObservableProperty]
    private string _singleFolder = string.Empty;

    /// <summary>
    /// Gets or sets whether an operation is currently running.
    /// </summary>
    [ObservableProperty]
    private bool _isRunning = false;

    /// <summary>
    /// Gets or sets the current operation progress (0–100).
    /// </summary>
    [ObservableProperty]
    private double _progress = 0;

    /// <summary>
    /// Gets or sets whether there are any results to display.
    /// </summary>
    [ObservableProperty]
    private bool _hasResults = false;

    /// <summary>
    /// Gets or sets the status bar message.
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>
    /// Gets or sets whether files are compared by MD5 hash.
    /// </summary>
    [ObservableProperty]
    private bool _useHashComparison = true;

    /// <summary>
    /// Gets or sets whether files are compared by name only.
    /// </summary>
    [ObservableProperty]
    private bool _useNameComparison = false;

    /// <summary>
    /// Gets or sets whether empty directories are removed after file deletion.
    /// </summary>
    [ObservableProperty]
    private bool _deleteEmptyFolders = true;

    /// <summary>
    /// Gets or sets the folder name pattern being typed into the exclude input.
    /// </summary>
    [ObservableProperty]
    private string _newExcludePattern = string.Empty;

    /// <summary>
    /// Gets or sets whether the last delete operation produced errors.
    /// </summary>
    [ObservableProperty]
    private bool _hasDeleteErrors = false;

    /// <summary>
    /// Grouped comparison or duplicate results shown in the results list.
    /// </summary>
    public ObservableCollection<ResultGroup> ResultGroups { get; } = new();

    /// <summary>
    /// Folder name patterns excluded from scanning.
    /// </summary>
    public ObservableCollection<string> ExcludedFolders { get; } = [];

    /// <summary>
    /// Per-file error messages from the last delete operation.
    /// </summary>
    public ObservableCollection<string> DeleteErrors { get; } = new();

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

    /// <summary>
    /// Switches the UI language to <paramref name="lang"/>.
    /// </summary>
    /// <param name="lang">Language code: <c>"en"</c> or <c>"ru"</c>.</param>
    [RelayCommand]
    private void SetLanguage(string lang) => Localization.Instance.SetLanguage(lang);

    /// <summary>
    /// Toggles the UI language between English and Russian.
    /// </summary>
    [RelayCommand]
    private void ToggleLanguage() => Localization.Instance.ToggleLanguage();

    /// <summary>
    /// Toggles the application theme between light and dark.
    /// </summary>
    [RelayCommand]
    private void ToggleTheme() => ThemeService.Instance.Toggle();

    /// <summary>
    /// Cancels the currently running operation.
    /// </summary>
    [RelayCommand]
    private void CancelRun() => _cts?.Cancel();

    /// <summary>
    /// Adds <see cref="NewExcludePattern"/> to <see cref="ExcludedFolders"/> if non-empty and not already present.
    /// </summary>
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

    /// <summary>
    /// Removes the given pattern from <see cref="ExcludedFolders"/>.
    /// </summary>
    /// <param name="pattern">The pattern to remove.</param>
    [RelayCommand]
    private void RemoveExclude(string pattern) => ExcludedFolders.Remove(pattern);

    /// <summary>
    /// Opens a folder picker and sets <see cref="Folder1"/>.
    /// </summary>
    [RelayCommand]
    private async Task BrowseFolder1()
    {
        var path = await _folderPicker!.PickFolderAsync();
        if (path != null)
        {
            Folder1 = path;
        }
    }

    /// <summary>
    /// Opens a folder picker and sets <see cref="Folder2"/>.
    /// </summary>
    [RelayCommand]
    private async Task BrowseFolder2()
    {
        var path = await _folderPicker!.PickFolderAsync();
        if (path != null)
        {
            Folder2 = path;
        }
    }

    /// <summary>
    /// Opens a folder picker and sets <see cref="SingleFolder"/>.
    /// </summary>
    [RelayCommand]
    private async Task BrowseSingleFolder()
    {
        var path = await _folderPicker!.PickFolderAsync();
        if (path != null)
        {
            SingleFolder = path;
        }
    }

    /// <summary>
    /// Runs the comparison or duplicate search in the background and populates <see cref="ResultGroups"/>.
    /// </summary>
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

    /// <summary>
    /// Deselects all items in all result groups.
    /// </summary>
    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var item in ResultGroups.SelectMany(g => g.Items))
        {
            item.IsSelected = false;
        }
    }

    /// <summary>
    /// In each duplicate group, selects all files except the newest one.
    /// </summary>
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

    /// <summary>
    /// Deletes all selected files, removes their entries from the UI, and reports per-file errors in <see cref="DeleteErrors"/>.
    /// </summary>
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
        DeleteErrors.Clear();
        HasDeleteErrors = false;

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
                        var attrs = File.GetAttributes(item.FilePath);
                        if ((attrs & FileAttributes.ReadOnly) != 0)
                        {
                            File.SetAttributes(item.FilePath, attrs & ~FileAttributes.ReadOnly);
                        }
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
                status += $"  |  " + string.Format(loc.ErrorsFormat, errors.Count);
                foreach (var e in errors)
                {
                    DeleteErrors.Add(e);
                }
                HasDeleteErrors = true;
            }
            StatusMessage = status;
        }
        finally
        {
            IsRunning = false;
        }
    }

    /// <summary>
    /// Compares two folders and returns groups of differing and missing files.
    /// </summary>
    /// <param name="folder1">Path to the first folder.</param>
    /// <param name="folder2">Path to the second folder.</param>
    /// <param name="excluded">Folder names to skip during traversal.</param>
    /// <param name="useHash">When <see langword="true"/>, compares by MD5 hash; otherwise by relative path.</param>
    /// <param name="progress">Progress reporter (0–100).</param>
    /// <param name="differsLabel">Label for the "differs" group.</param>
    /// <param name="missing2Label">Label for the "missing in folder 2" group.</param>
    /// <param name="missing1Label">Label for the "missing in folder 1" group.</param>
    /// <param name="folderNotFmt">Format string for the folder-not-found error.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of result groups; empty groups are omitted.</returns>
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

    /// <summary>
    /// Scans a folder recursively and returns groups of files with identical content or name.
    /// </summary>
    /// <param name="rootPath">Root folder to scan.</param>
    /// <param name="excluded">Folder names to skip during traversal.</param>
    /// <param name="useHash">When <see langword="true"/>, groups by MD5 hash; otherwise by file name.</param>
    /// <param name="progress">Progress reporter (0–100).</param>
    /// <param name="groupHashFmt">Format string for hash-mode group labels.</param>
    /// <param name="groupNameFmt">Format string for name-mode group labels.</param>
    /// <param name="folderNotFmt">Format string for the folder-not-found error.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of groups containing two or more files with the same key.</returns>
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

    /// <summary>
    /// Returns the set of relative file paths under <paramref name="folderPath"/>, excluding specified sub-folders.
    /// </summary>
    /// <param name="folderPath">Root folder to scan.</param>
    /// <param name="excluded">Folder names to skip.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Case-insensitive set of relative paths.</returns>
    internal static HashSet<string> GetRelativePaths(string folderPath, List<string> excluded, CancellationToken ct = default)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in GetFilesExcluding(folderPath, excluded, ct))
        {
            paths.Add(Path.GetRelativePath(folderPath, file));
        }
        return paths;
    }

    /// <summary>
    /// Enumerates all files under <paramref name="root"/> recursively,
    /// skipping directories whose name matches any entry in <paramref name="excluded"/>.
    /// </summary>
    /// <param name="root">Root directory to traverse.</param>
    /// <param name="excluded">Folder names to skip.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Sequence of absolute file paths.</returns>
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

    /// <summary>
    /// Computes the MD5 hash of a file and returns it as a lowercase hex string.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <param name="ct">Cancellation token checked between read chunks.</param>
    /// <returns>Lowercase hex MD5 string, e.g. <c>"d41d8cd98f00b204e9800998ecf8427e"</c>.</returns>
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

    /// <summary>
    /// Deletes <paramref name="dirPath"/> and each of its ancestors while they are empty.
    /// </summary>
    /// <param name="dirPath">Starting directory path; may be <see langword="null"/>.</param>
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
