using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.IO;

namespace FolderDiff.ViewModels;

public partial class FileResultItem : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    public string FilePath { get; init; } = string.Empty;
    public DateTime Modified { get; init; }

    public string FileName => Path.GetFileName(FilePath);
    public string ModifiedText => Modified.ToString("yyyy-MM-dd HH:mm");
}
