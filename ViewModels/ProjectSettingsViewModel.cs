using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class ProjectSettingsViewModel : ViewModelBase
{
    private readonly IFileDialogService _fileDialogService;

    public ProjectSettings Settings { get; }
    public List<LogLevel> LogLevels { get; } = new(Enum.GetValues<LogLevel>());
    
    [ObservableProperty]
    private LogLevel _selectedLogLevel = LogLevel.INFO;
    
    [ObservableProperty]
    private string _serialNumberValidationLengthString = string.Empty;
    
    [ObservableProperty]
    private string? _toleranceString = "10";
    
    [ObservableProperty]
    private string? _displayedDecimalPlacesString = "3";

    public bool CanSaveTestResult =>
        Settings.IsUseSerialNumber && Settings.IsSaveTestResult;

    public ProjectSettingsViewModel(
        IFileDialogService fileDialogService,
        ProjectModel projectModel)
    {
        _fileDialogService = fileDialogService;
        Settings = projectModel.Settings;
        
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
                
                case nameof(Settings.DisplayedDecimalPlaces):
                    
                    DisplayedDecimalPlacesString = Settings.DisplayedDecimalPlaces.ToString();
                    
                    break;
                
                case nameof(Settings.ToleranceValue):
                    
                    ToleranceString = Settings.ToleranceValue.ToString(CultureInfo.CurrentCulture);
                    
                    break;
                
                case nameof(Settings.ToastLogLevel):

                    SelectedLogLevel = Settings.ToastLogLevel;

                    break;
                
            }
        };
    }

    partial void OnToleranceStringChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;
        
        if (double.TryParse(value, CultureInfo.CurrentCulture, out var result) ||
            double.TryParse(value, CultureInfo.InvariantCulture, out result))
        {
            Settings.ToleranceValue = result;
        }
    }

    partial void OnDisplayedDecimalPlacesStringChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;
        
        Settings.DisplayedDecimalPlaces = int.Parse(value);
    }

    partial void OnSerialNumberValidationLengthStringChanged(string value)
    {
        if (int.TryParse(value, out var parsed))
            Settings.SerialNumberValidationLength = parsed;
    }
    
    partial void OnSelectedLogLevelChanged(LogLevel value)
    {
        Settings.ToastLogLevel = value;
    }

    [RelayCommand]
    private async Task SelectSaveTestResultFilePath()
    {
        var result = await _fileDialogService.OpenFolderAsync("Select folder for export");
        if (result == null) return;

        Settings.SaveTestResultFilePath = result.Path.LocalPath;
    }

    [RelayCommand]
    private void ResetToDefault() => Settings.ResetToDefault();
}