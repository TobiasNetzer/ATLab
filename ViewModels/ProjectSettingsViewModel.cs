using System;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class ProjectSettingsViewModel : ViewModelBase
{
    private readonly IFileDialogService _fileDialogService;
    private readonly IProjectSettings _projectSettings;
    
    [ObservableProperty]
    private double _toleranceValue;

    [ObservableProperty]
    private int _resultPrecision;
    
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
    
    public ProjectSettingsViewModel(IFileDialogService fileDialogService,
        IProjectSettings projectSettings)
    {
        _fileDialogService = fileDialogService;
        _projectSettings = projectSettings;
        
        _projectSettings.SettingsChanged += UpdateFromService;
        
        ResetToDefault();
    }

    private void UpdateFromService()
    {
        ToleranceValue = _projectSettings.ToleranceValue;
        ResultPrecision = _projectSettings.ResultPrecision;
        UseSerialNumber = _projectSettings.UseSerialNumber;
        SaveTestResult = _projectSettings.SaveTestResult;
        SaveTestResultOptions = _projectSettings.SaveTestResultOptions;
        SaveTestResultFilePath = _projectSettings.SaveTestResultFilePath;
    }
    
    public void ResetToDefault()
    {
        _projectSettings.ResetToDefault();
    }
    
    partial void OnResultPrecisionChanged(int value)
    {
        _projectSettings.ResultPrecision = value;
        ConfigurationChanged?.Invoke();
    }
    
    partial void OnToleranceValueChanged(double value)
    {
        _projectSettings.ToleranceValue = value;
        ConfigurationChanged?.Invoke();
    }

    partial void OnUseSerialNumberChanged(bool value)
    {
        _projectSettings.UseSerialNumber = value;
        OnPropertyChanged(nameof(CanSaveTestResult));
        ConfigurationChanged?.Invoke();
    }
    partial void OnSaveTestResultChanged(bool value)
    {
        _projectSettings.SaveTestResult = value;
        OnPropertyChanged(nameof(CanSaveTestResult));
        ConfigurationChanged?.Invoke();
    }
    partial void OnSaveTestResultOptionsChanged(SaveTestResultOptions value)
    {
        _projectSettings.SaveTestResultOptions = value;
        ConfigurationChanged?.Invoke();
    }
    partial void OnSaveTestResultFilePathChanged(string value)
    {
        _projectSettings.SaveTestResultFilePath = value;
        ConfigurationChanged?.Invoke();
    }

    [RelayCommand]
    private async Task SelectSaveTestResultFilePath()
    {
        var result = await _fileDialogService.OpenFolderAsync("Select folder to save test result");
        if (result == null) return;
        SaveTestResultFilePath = result.Path.LocalPath;
    }
}