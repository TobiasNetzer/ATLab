using System;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class SerialNumberEntryWindowViewModel : ViewModelBase
{
    private readonly ProjectSettings _settings;
    
    [ObservableProperty]
    private string _serialNumber = string.Empty;
    
    [ObservableProperty]
    private bool _isOkEnabled;

    public event Action<bool>? RequestClose;
    
    public SerialNumberEntryWindowViewModel(ProjectSettings settings)
    { 
        _settings = settings;
    }

    partial void OnSerialNumberChanged(string value)
    {
        IsOkEnabled = ValidateSerialNumber(value);
    }

    [RelayCommand]
    private void Ok() => RequestClose?.Invoke(true);

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(false);
    
    private bool ValidateSerialNumber(string serial)
    {
        if (string.IsNullOrEmpty(serial))
            return false;
        
        if (!_settings.IsEnableSerialNumberValidation)
            return true;
        
        // Length check
        if (_settings.SerialNumberValidationLength > 0 &&
            serial.Length != _settings.SerialNumberValidationLength)
            return false;

        // StartsWith check
        if (!string.IsNullOrEmpty(_settings.SerialNumberValidationStartsWith) &&
            !serial.StartsWith(_settings.SerialNumberValidationStartsWith))
            return false;

        // EndsWith check
        if (!string.IsNullOrEmpty(_settings.SerialNumberValidationEndsWith) &&
            !serial.EndsWith(_settings.SerialNumberValidationEndsWith))
            return false;

        // Contains check
        if (!string.IsNullOrEmpty(_settings.SerialNumberValidationContains) &&
            !serial.Contains(_settings.SerialNumberValidationContains))
            return false;

        return true;
    }
}