using System;
using ATLab.Models;
using ATLab.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;

namespace ATLab.ViewModels;

public partial class SerialNumberEntryDialogViewModel : ViewModelBase, IDisposable
{
    private readonly ProjectSettings _settings;
    private readonly ControlModuleService _controlModuleService;
    private readonly DialogManager _dialogManager;
    
    [ObservableProperty]
    private string _serialNumber = string.Empty;
    
    [ObservableProperty]
    private bool _isOkEnabled;

    public event Action<bool>? RequestClose;
    
    private event Action OkHandler;
    private event Action CancelHandler;
    
    public SerialNumberEntryDialogViewModel(ProjectModel projectModel,
        ControlModuleService controlModuleService,
        DialogManager dialogManager)
    { 
        _settings = projectModel.Settings;
        _controlModuleService = controlModuleService;
        _dialogManager = dialogManager;
        
        OkHandler += async () => 
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync( () =>
            {
                if (IsOkEnabled)
                    RequestClose?.Invoke(true);
                else
                    _controlModuleService.SetUserResponseMode(true);
            });
            
        CancelHandler += async () => 
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync( () =>
            {
                RequestClose?.Invoke(false);
            });
        
        _controlModuleService.PassPressed += OkHandler;
        _controlModuleService.FailPressed += CancelHandler;
    }

    partial void OnSerialNumberChanged(string value)
    {
        IsOkEnabled = ValidateSerialNumber(value);
    }

    [RelayCommand]
    private void Submit()
    {
        _dialogManager.Close(this, new CloseDialogOptions { Success = true });
    }

    [RelayCommand]
    private void Cancel()
    {
        _dialogManager.Close(this);
    }
    
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
    
    public void Dispose()
    {
        _controlModuleService.PassPressed -= OkHandler;
        _controlModuleService.FailPressed -= CancelHandler;
    }
}