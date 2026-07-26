using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Interfaces;

public interface IProjectDocumentService
{
    Task<AtlabFileDto?> OpenFileAsync();
    Task<AtlabFileDto?> OpenAsync(string path);
    Task<bool> SaveAsync(AtlabFileDto dto);
    Task<bool> SaveAsAsync(AtlabFileDto dto);
    Task<bool> NewProjectAsync();
    Task<bool> ConfirmAndContinueIfDirtyAsync();
}
