using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class ScriptCommandViewModel : ViewModelBase
{
    private readonly ScriptCommand _model;
    
    [ObservableProperty]
    private string _command;

    [ObservableProperty]
    private bool _expectResponse;
    
    [ObservableProperty]
    private bool _useForValidation;

    [ObservableProperty]
    private string _delayMs;

    [ObservableProperty]
    private string _timeoutMs;

    public ScriptCommandViewModel(ScriptCommand model)
    {
        _model = model;
        Command = model.Command;
        ExpectResponse = model.ExpectResponse;
        UseForValidation = model.UseForValidation;
        DelayMs = model.DelayMs.ToString();
        TimeoutMs = model.TimeoutMs.ToString();
    }

    partial void OnCommandChanged(string value) => _model.Command = value;
    partial void OnExpectResponseChanged(bool value) => _model.ExpectResponse = value;
    partial void OnUseForValidationChanged(bool value) => _model.UseForValidation = value;

    partial void OnDelayMsChanged(string value)
    {
        if (int.TryParse(value, out var result))
        {
            _model.DelayMs = result;
        }
        else
        {
            DelayMs = "0";
            _model.DelayMs = 0;
        }
    }

    partial void OnTimeoutMsChanged(string value)
    {
        if (int.TryParse(value, out var result))
        {
            _model.TimeoutMs = result;
        }
        else
        {
            TimeoutMs = "1000";
            _model.TimeoutMs = 1000;
        }
    }

    public ScriptCommand GetModel() => _model;
}
