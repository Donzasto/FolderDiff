using System.Threading.Tasks;

namespace FolderDiff.ViewModels;

public interface IFolderPickerService
{
    Task<string?> PickFolderAsync();
}
