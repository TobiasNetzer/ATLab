using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class ExternalProgramEditorViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private ExternalProgram _externalProgram = new();

    [ObservableProperty]
    private bool _isExpanded;

    public ExternalProgramEditorViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;

        IsExpanded = settingsService.Settings.IsExternalProgramEditorExpanded;
    }

    partial void OnIsExpandedChanged(bool value)
    {
        _settingsService.Settings.IsExternalProgramEditorExpanded = value;
    }

    public void LoadTestStep(ExternalProgram externalProgram)
    {
        ExternalProgram = externalProgram;
    }

    [RelayCommand]
    private void ToggleExpanded() => IsExpanded = !IsExpanded;
}