using System.Threading.Tasks;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Interfaces;

public interface IProjectController
{
    Task NewProjectAsync(TestingTabViewModel vm);
    Task SaveFileAsync(TestingTabViewModel vm);
    Task SaveFileAsAsync(TestingTabViewModel vm);
    Task LoadFileWithDialogAsync(TestingTabViewModel vm);
    Task LoadFileAsync(TestingTabViewModel vm, string path);
    void ApplyDto(TestingTabViewModel vm, AtlabFileDto dto);
    AtlabFileDto CaptureCurrentState(TestingTabViewModel vm);
    Task CheckForHardwareCompatibility(IHardwareInfo hardwareInfo, AtlabFileDto dto);
    void MarkDirty();
}