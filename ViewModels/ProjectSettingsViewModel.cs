using System;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class ProjectSettingsViewModel : ViewModelBase
{
    private readonly TestStepConfiguratorViewModel _testStepConfiguratorViewModel;
    private readonly IFileDialogService _fileDialogService;
    
    [ObservableProperty]
    private double _toleranceValue;
    
    [ObservableProperty]
    private bool _useSerialNumber;
    
    [ObservableProperty]
    private bool _saveTestResult;
    
    [ObservableProperty]
    private SaveTestResultOptions _saveTestResultOptions = SaveTestResultOptions.ALWAYS;
    
    [ObservableProperty]
    private string _saveTestResultFilePath = string.Empty;
    
    public bool CanSaveTestResult => UseSerialNumber && SaveTestResult;
    
    public event Action? ConfigurationChanged;
    
    public ProjectSettingsViewModel(TestStepConfiguratorViewModel testStepConfiguratorViewModel,
        IFileDialogService fileDialogService)
    {
        _testStepConfiguratorViewModel = testStepConfiguratorViewModel;
        _fileDialogService = fileDialogService;
        
        ResetToDefault();
    }
    
    public void ResetToDefault()
    {
        ToleranceValue = 10;
        UseSerialNumber = false;
        SaveTestResultFilePath = string.Empty;
        UseSerialNumber = false;
        SaveTestResult = false;
        SaveTestResultOptions = SaveTestResultOptions.ALWAYS;
    }
    
    partial void OnToleranceValueChanged(double value)
    {
        _testStepConfiguratorViewModel.Tolerance = value / 100.0;
        ConfigurationChanged?.Invoke();
    }

    partial void OnUseSerialNumberChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSaveTestResult));
        ConfigurationChanged?.Invoke();
    }
    partial void OnSaveTestResultChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSaveTestResult));
        ConfigurationChanged?.Invoke();
    }
    partial void OnSaveTestResultOptionsChanged(SaveTestResultOptions value) => ConfigurationChanged?.Invoke();
    partial void OnSaveTestResultFilePathChanged(string value) => ConfigurationChanged?.Invoke();

    [RelayCommand]
    private async Task SelectSaveTestResultFilePath()
    {
        var result = await _fileDialogService.OpenFolderAsync("Select folder to save test result");
        if (result == null) return;
        SaveTestResultFilePath = result.Path.LocalPath;
    }
}