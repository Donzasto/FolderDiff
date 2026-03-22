using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FolderDiff.ViewModels;
using Xunit;

namespace FolderDiff.Tests;

public class MainViewModelTests : IDisposable
{
    private readonly string _tempRoot;

    public MainViewModelTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "FolderDiffTests_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    private string MakeDir(string name)
    {
        var path = Path.Combine(_tempRoot, name);
        Directory.CreateDirectory(path);
        return path;
    }

    private static void WriteFile(string dir, string name, string content)
        => File.WriteAllText(Path.Combine(dir, name), content);

    [Fact]
    public void ComputeHash_SameContent_ReturnsSameHash()
    {
        var f1 = Path.Combine(_tempRoot, "a.txt");
        var f2 = Path.Combine(_tempRoot, "b.txt");
        File.WriteAllText(f1, "hello world");
        File.WriteAllText(f2, "hello world");

        Assert.Equal(MainViewModel.ComputeHash(f1), MainViewModel.ComputeHash(f2));
    }

    [Fact]
    public void ComputeHash_DifferentContent_ReturnsDifferentHash()
    {
        var f1 = Path.Combine(_tempRoot, "a.txt");
        var f2 = Path.Combine(_tempRoot, "b.txt");
        File.WriteAllText(f1, "hello");
        File.WriteAllText(f2, "world");

        Assert.NotEqual(MainViewModel.ComputeHash(f1), MainViewModel.ComputeHash(f2));
    }

    [Fact]
    public void ComputeHash_EmptyFile_ReturnsKnownHash()
    {
        var f = Path.Combine(_tempRoot, "empty.txt");
        File.WriteAllBytes(f, Array.Empty<byte>());

        Assert.Equal("d41d8cd98f00b204e9800998ecf8427e", MainViewModel.ComputeHash(f));
    }

    [Fact]
    public void GetFilesExcluding_NoExclusions_ReturnsAllFiles()
    {
        var dir = MakeDir("scan");
        WriteFile(dir, "a.txt", "a");
        WriteFile(dir, "b.txt", "b");
        var sub = Path.Combine(dir, "sub");
        Directory.CreateDirectory(sub);
        WriteFile(sub, "c.txt", "c");

        var files = MainViewModel.GetFilesExcluding(dir, []).ToList();
        Assert.Equal(3, files.Count);
    }

    [Fact]
    public void GetFilesExcluding_WithExcludedFolder_SkipsSubdir()
    {
        var dir = MakeDir("scan2");
        WriteFile(dir, "a.txt", "a");
        var excluded = Path.Combine(dir, "node_modules");
        Directory.CreateDirectory(excluded);
        WriteFile(excluded, "pkg.js", "x");

        var files = MainViewModel.GetFilesExcluding(dir, ["node_modules"]).ToList();
        Assert.Single(files);
        Assert.DoesNotContain(files, f => f.Contains("node_modules"));
    }

    [Fact]
    public void GetFilesExcluding_ExclusionIsCaseInsensitive()
    {
        var dir = MakeDir("scan3");
        WriteFile(dir, "root.txt", "r");
        var sub = Path.Combine(dir, "BIN");
        Directory.CreateDirectory(sub);
        WriteFile(sub, "out.dll", "d");

        var files = MainViewModel.GetFilesExcluding(dir, ["bin"]).ToList();
        Assert.Single(files);
    }

    [Fact]
    public void CompareFolders_IdenticalFolders_ReturnsNoGroups()
    {
        var f1 = MakeDir("cmp1_a");
        var f2 = MakeDir("cmp1_b");
        WriteFile(f1, "file.txt", "same");
        WriteFile(f2, "file.txt", "same");

        var groups = MainViewModel.CompareFolders(f1, f2, [], true, new Progress<double>(),
            "differs", "missing2", "missing1", "Folder not found: {0}");

        Assert.Empty(groups);
    }

    [Fact]
    public void CompareFolders_ByHash_DetectsDifferences()
    {
        var f1 = MakeDir("cmp2_a");
        var f2 = MakeDir("cmp2_b");
        WriteFile(f1, "file.txt", "version1");
        WriteFile(f2, "file.txt", "version2");

        var groups = MainViewModel.CompareFolders(f1, f2, [], true, new Progress<double>(),
            "differs", "missing2", "missing1", "Folder not found: {0}");

        Assert.Single(groups);
        Assert.Equal("differs", groups[0].Label);
    }

    [Fact]
    public void CompareFolders_ByName_DetectsMissingFiles()
    {
        var f1 = MakeDir("cmp3_a");
        var f2 = MakeDir("cmp3_b");
        WriteFile(f1, "only_in_1.txt", "x");
        WriteFile(f2, "only_in_2.txt", "y");

        var groups = MainViewModel.CompareFolders(f1, f2, [], false, new Progress<double>(),
            "differs", "missing2", "missing1", "Folder not found: {0}");

        Assert.Equal(2, groups.Count);
        var labels = groups.Select(g => g.Label).ToList();
        Assert.Contains("missing2", labels);
        Assert.Contains("missing1", labels);
    }

    [Fact]
    public void CompareFolders_MissingDirectory_ThrowsDirectoryNotFoundException()
    {
        var f1 = MakeDir("cmp4_a");
        var f2 = Path.Combine(_tempRoot, "nonexistent");

        Assert.Throws<DirectoryNotFoundException>(() =>
            MainViewModel.CompareFolders(f1, f2, [], true, new Progress<double>(),
                "d", "m2", "m1", "Folder not found: {0}"));
    }

    [Fact]
    public void FindDuplicatesInFolder_NoDuplicates_ReturnsNoGroups()
    {
        var dir = MakeDir("dup1");
        WriteFile(dir, "a.txt", "aaa");
        WriteFile(dir, "b.txt", "bbb");

        var groups = MainViewModel.FindDuplicatesInFolder(dir, [], true, new Progress<double>(),
            "Group #{0} hash:{1} ({2} files)", "Group #{0} name:{1} ({2} files)", "Folder not found: {0}");

        Assert.Empty(groups);
    }

    [Fact]
    public void FindDuplicatesInFolder_ByHash_GroupsDuplicates()
    {
        var dir = MakeDir("dup2");
        WriteFile(dir, "a.txt", "same content");
        WriteFile(dir, "b.txt", "same content");
        WriteFile(dir, "c.txt", "different");

        var groups = MainViewModel.FindDuplicatesInFolder(dir, [], true, new Progress<double>(),
            "Group #{0} hash:{1} ({2} files)", "Group #{0} name:{1} ({2} files)", "Folder not found: {0}");

        Assert.Single(groups);
        Assert.Equal(2, groups[0].Items.Count);
    }

    [Fact]
    public void FindDuplicatesInFolder_ByName_GroupsSameNameFiles()
    {
        var dir = MakeDir("dup3");
        var sub = Path.Combine(dir, "sub");
        Directory.CreateDirectory(sub);
        WriteFile(dir, "readme.txt", "content1");
        WriteFile(sub, "readme.txt", "content2");
        WriteFile(dir, "unique.txt", "x");

        var groups = MainViewModel.FindDuplicatesInFolder(dir, [], false, new Progress<double>(),
            "Group #{0} hash:{1} ({2} files)", "Group #{0} name:{1} ({2} files)", "Folder not found: {0}");

        Assert.Single(groups);
        Assert.Equal(2, groups[0].Items.Count);
    }

    [Fact]
    public void FindDuplicatesInFolder_MissingDirectory_ThrowsDirectoryNotFoundException()
    {
        var dir = Path.Combine(_tempRoot, "nonexistent");

        Assert.Throws<DirectoryNotFoundException>(() =>
            MainViewModel.FindDuplicatesInFolder(dir, [], true, new Progress<double>(),
                "g", "g", "Folder not found: {0}"));
    }

    [Fact]
    public void DeleteEmptyDirectoriesUp_RemovesEmptyDir()
    {
        var dir = MakeDir("del1");
        var sub = Path.Combine(dir, "empty_sub");
        Directory.CreateDirectory(sub);

        MainViewModel.DeleteEmptyDirectoriesUp(sub);

        Assert.False(Directory.Exists(sub));
    }

    [Fact]
    public void DeleteEmptyDirectoriesUp_StopsAtNonEmptyParent()
    {
        var dir = MakeDir("del2");
        WriteFile(dir, "keep.txt", "k");
        var sub = Path.Combine(dir, "empty_sub");
        Directory.CreateDirectory(sub);

        MainViewModel.DeleteEmptyDirectoriesUp(sub);

        Assert.False(Directory.Exists(sub));
        Assert.True(Directory.Exists(dir));
    }

    [Fact]
    public void DeleteEmptyDirectoriesUp_NullPath_DoesNotThrow()
    {
        var ex = Record.Exception(() => MainViewModel.DeleteEmptyDirectoriesUp(null));
        Assert.Null(ex);
    }

    [Fact]
    public void GetFilesExcluding_PreCancelledToken_ThrowsOperationCanceledException()
    {
        var dir = MakeDir("cancel1");
        WriteFile(dir, "a.txt", "a");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            MainViewModel.GetFilesExcluding(dir, [], cts.Token).ToList());
    }

    [Fact]
    public void FindDuplicatesInFolder_PreCancelledToken_ThrowsOperationCanceledException()
    {
        var dir = MakeDir("cancel2");
        WriteFile(dir, "a.txt", "a");
        WriteFile(dir, "b.txt", "a");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            MainViewModel.FindDuplicatesInFolder(dir, [], true, new Progress<double>(),
                "g", "g", "Folder not found: {0}", cts.Token));
    }

    [Fact]
    public void CompareFolders_PreCancelledToken_ThrowsOperationCanceledException()
    {
        var f1 = MakeDir("cancel3_a");
        var f2 = MakeDir("cancel3_b");
        WriteFile(f1, "a.txt", "a");
        WriteFile(f2, "a.txt", "a");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            MainViewModel.CompareFolders(f1, f2, [], true, new Progress<double>(),
                "d", "m2", "m1", "Folder not found: {0}", cts.Token));
    }

    [Fact]
    public async Task RunCommand_DuplicatesMode_EmptyFolder_SetsErrorStatus()
    {
        var vm = new MainViewModel();
        vm.IsCompareMode = false;
        vm.SingleFolder = string.Empty;

        await vm.RunCommand.ExecuteAsync(null);

        Assert.False(vm.IsRunning);
        Assert.StartsWith(vm.Loc.ErrorPrefix, vm.StatusMessage);
    }

    [Fact]
    public async Task RunCommand_CompareMode_EmptyFolders_SetsErrorStatus()
    {
        var vm = new MainViewModel();
        vm.IsCompareMode = true;
        vm.Folder1 = string.Empty;
        vm.Folder2 = string.Empty;

        await vm.RunCommand.ExecuteAsync(null);

        Assert.False(vm.IsRunning);
        Assert.StartsWith(vm.Loc.ErrorPrefix, vm.StatusMessage);
    }

    [Fact]
    public async Task RunCommand_WhenCancelled_SetsCancelledStatus()
    {
        var dir = MakeDir("cancel_vm");
        for (int i = 0; i < 200; i++)
        {
            WriteFile(dir, $"file{i}.txt", $"content_{i}");
        }

        var vm = new MainViewModel();
        vm.IsCompareMode = false;
        vm.SingleFolder = dir;
        vm.UseHashComparison = true;

        var runTask = vm.RunCommand.ExecuteAsync(null);
        vm.CancelRunCommand.Execute(null);
        await runTask;

        Assert.False(vm.IsRunning);
        Assert.Equal(vm.Loc.Cancelled, vm.StatusMessage);
    }
}
