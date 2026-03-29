using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.IO;

namespace FolderDiff.ViewModels;

/// <summary>
/// Represents a single file entry in a comparison or duplicate group.
/// </summary>
public partial class FileResultItem : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    /// <summary>
    /// Gets or sets whether this file is selected for deletion.
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Full absolute path to the file.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Last write time of the file.
    /// </summary>
    public DateTime Modified { get; init; }

    /// <summary>
    /// File name without the directory path.
    /// </summary>
    public string FileName => Path.GetFileName(FilePath);

    /// <summary>
    /// Last write time formatted as <c>yyyy-MM-dd HH:mm</c>.
    /// </summary>
    public string ModifiedText => Modified.ToString("yyyy-MM-dd HH:mm");
}
