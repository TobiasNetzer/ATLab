using System.Threading.Tasks;

namespace ATLab.Interfaces;

public interface IProjectController
{
    Task<bool> NewProjectAsync();
    Task SaveFileAsync();
    Task SaveFileAsAsync();
    Task LoadFileWithDialogAsync();
    Task LoadFileAsync(string path);
}