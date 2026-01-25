using System.ComponentModel;
using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Interfaces;

public interface IProjectService : INotifyPropertyChanged
{
    string? CurrentFilePath { get; set; }
    bool IsDirty { get; set; }
    
    Task<AtlabFileDto?> OpenFileAsync();
    Task<AtlabFileDto?> LoadAsync(string path);
    Task<bool> SaveAsync(AtlabFileDto dto);
    Task<bool> SaveAsAsync(AtlabFileDto dto);
    Task<bool> NewProjectAsync();
    Task<bool> ConfirmAndContinueIfDirtyAsync();
    
    void UpdateLastSavedState(AtlabFileDto dto);
}
