using System.Threading.Tasks;

namespace ATLab.Interfaces;

/// <summary>High-level project commands of the toolbar: new, open, save and save as.</summary>
public interface IProjectController
{
    Task<bool> NewProjectAsync();
    Task SaveFileAsync();
    Task SaveFileAsAsync();
    Task LoadFileWithDialogAsync();
    Task LoadFileAsync(string path);
}