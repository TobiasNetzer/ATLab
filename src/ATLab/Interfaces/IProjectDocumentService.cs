using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Interfaces;

/// <summary>File dialogs and unsaved-changes handling around opening and saving project files.</summary>
public interface IProjectDocumentService
{
    Task<AtlabFileDto?> OpenFileAsync();

    /// <summary>Asks for a project file; null if the dialog was canceled.</summary>
    Task<string?> PickProjectFileAsync();
    Task<AtlabFileDto?> OpenAsync(string path);
    Task<bool> SaveAsync(AtlabFileDto dto);
    Task<bool> SaveAsAsync(AtlabFileDto dto);
    Task<bool> NewProjectAsync();
    Task<bool> CloseProjectAsync();
    Task<bool> ConfirmAndContinueIfDirtyAsync();
}
