using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class ProjectSettingsViewModel : ViewModelBase
{
    private readonly IFileDialogService _fileDialogService;

    public ProjectSettings Settings { get; }

    public bool CanSaveTestResult =>
        Settings.UseSerialNumber && Settings.SaveTestResult;

    public ProjectSettingsViewModel(
        IFileDialogService fileDialogService,
        ProjectSettings settings)
    {
        _fileDialogService = fileDialogService;
        Settings = settings;
        
        Settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(Settings.UseSerialNumber)
                or nameof(Settings.SaveTestResult))
            {
                OnPropertyChanged(nameof(CanSaveTestResult));
            }
        };
    }

    [RelayCommand]
    private async Task SelectSaveTestResultFilePath()
    {
        var result = await _fileDialogService.OpenFolderAsync("Select folder to save test result");
        if (result == null) return;

        Settings.SaveTestResultFilePath = result.Path.LocalPath;
    }

    [RelayCommand]
    private void ResetToDefault() => Settings.ResetToDefault();
}
