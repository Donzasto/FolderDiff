using System.Collections.ObjectModel;

namespace FolderDiff.ViewModels;

public class ResultGroup
{
    public string Label { get; init; } = string.Empty;
    public ObservableCollection<FileResultItem> Items { get; } = new();
}
