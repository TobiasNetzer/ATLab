using System.Threading.Tasks;
using ATLab.ViewModels;

namespace ATLab.Interfaces;

public interface IProjectController
{
    Task NewProjectAsync(TestingTabViewModel vm);
    Task SaveFileAsync();
    Task SaveFileAsAsync();
    Task LoadFileWithDialogAsync(TestingTabViewModel vm);
    Task LoadFileAsync(TestingTabViewModel vm, string path);
}