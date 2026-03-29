using System.Threading.Tasks;

namespace FolderDiff.ViewModels;

/// <summary>
/// Abstraction over the platform-specific folder picker dialog.
/// </summary>
public interface IFolderPickerService
{
    /// <summary>
    /// Opens a folder picker dialog and returns the selected path, or <see langword="null"/> if cancelled.
    /// </summary>
    /// <returns>The selected folder path, or <see langword="null"/> if the user cancelled.</returns>
    Task<string?> PickFolderAsync();
}
