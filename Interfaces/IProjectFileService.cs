using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Interfaces;

public interface IProjectFileService
{
    Task<AtlabFileDto?> OpenFileAsync();
    Task<AtlabFileDto?> LoadAsync(string path);
    Task<bool> SaveAsync(AtlabFileDto dto);
    Task<bool> SaveAsAsync(AtlabFileDto dto);
    Task<bool> NewProjectAsync();
    Task<bool> ConfirmAndContinueIfDirtyAsync();
}
