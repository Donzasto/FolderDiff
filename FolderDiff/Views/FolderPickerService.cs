using Avalonia.Controls;
using Avalonia.Platform.Storage;
using FolderDiff.ViewModels;
using System.Threading.Tasks;

namespace FolderDiff.Views;

public class FolderPickerService : IFolderPickerService
{
    private readonly Window _window;

    public FolderPickerService(Window window) => _window = window;

    public async Task<string?> PickFolderAsync()
    {
        var folders = await _window.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { AllowMultiple = false });
        return folders.Count > 0 ? folders[0].Path.LocalPath : null;
    }
}
