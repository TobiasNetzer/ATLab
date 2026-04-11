using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class ScriptCommandViewModel : ViewModelBase
{
    private readonly ScriptCommand _model;
    private readonly ScriptViewModel _parent;
    
    [ObservableProperty]
    private string _command;

    [ObservableProperty]
    private bool _isExpectResponse;
    
    [ObservableProperty]
    private bool _isEvaluate;

    [ObservableProperty]
    private string _delayMs;

    [ObservableProperty]
    private string _timeoutMs;

    public ScriptCommandViewModel(ScriptCommand model, ScriptViewModel parent)
    {
        _model = model;
        _parent = parent;
        
        Command = model.Command;
        IsExpectResponse = model.IsExpectResponse;
        IsEvaluate = model.IsEvaluate;
        DelayMs = model.DelayMs.ToString();
        TimeoutMs = model.TimeoutMs.ToString();
    }

    partial void OnCommandChanged(string value) => _model.Command = value;
    partial void OnIsExpectResponseChanged(bool value) => _model.IsExpectResponse = value;
    partial void OnIsEvaluateChanged(bool value)
    {
        _model.IsEvaluate = value;

        if (value)
            _parent?.ClearEvaluateExcept(this);
    }

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