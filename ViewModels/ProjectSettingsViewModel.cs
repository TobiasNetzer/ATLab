using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class ProjectSettingsViewModel : ViewModelBase
{
    private readonly IFileDialogService _fileDialogService;

    public ProjectSettings Settings { get; }

    [ObservableProperty]
    private string _serialNumberValidationLengthString = string.Empty;

    public bool CanSaveTestResult =>
        Settings.IsUseSerialNumber && Settings.IsSaveTestResult;

    public ProjectSettingsViewModel(
        IFileDialogService fileDialogService,
        ProjectSettings settings)
    {
        _fileDialogService = fileDialogService;
        Settings = settings;
        
        Settings.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(Settings.IsUseSerialNumber)
                    or nameof(Settings.IsSaveTestResult):
                    
                    OnPropertyChanged(nameof(CanSaveTestResult));
                    break;
                
                case nameof(Settings.SerialNumberValidationLength):
                    
                    SerialNumberValidationLengthString = Settings.SerialNumberValidationLength == 0
                        ? string.Empty
                        : Settings.SerialNumberValidationLength.ToString();
                    
                    break;
            }
        };
    }
    
    partial void OnSerialNumberValidationLengthStringChanged(string value)
    {
        if (int.TryParse(value, out var parsed))
            Settings.SerialNumberValidationLength = parsed;
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
