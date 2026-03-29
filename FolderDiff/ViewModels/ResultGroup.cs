using System.Collections.ObjectModel;

namespace FolderDiff.ViewModels;

/// <summary>
/// A labeled group of file results displayed in the results list.
/// </summary>
public class ResultGroup
{
    /// <summary>
    /// Display label for the group (e.g. "Differing files", "Missing in folder 2").
    /// </summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// Files belonging to this group.
    /// </summary>
    public ObservableCollection<FileResultItem> Items { get; } = new();
}
