using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class ScpiCommandViewModel : ViewModelBase
{
    private readonly ScpiCommand _model;

    public ScpiCommandViewModel(ScpiCommand model)
    {
        _model = model;
        _command = model.Command;
        _expectResponse = model.ExpectResponse;
        _delayMs = model.DelayMs.ToString();
        _timeoutMs = model.TimeoutMs.ToString();
    }

    [ObservableProperty]
    private string _command;

    [ObservableProperty]
    private bool _expectResponse;

    [ObservableProperty]
    private string _delayMs;

    [ObservableProperty]
    private string _timeoutMs;

    partial void OnCommandChanged(string value) => _model.Command = value;
    partial void OnExpectResponseChanged(bool value) => _model.ExpectResponse = value;

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

    public ScpiCommand GetModel() => _model;
    public int GetTimeoutMs() => _model.TimeoutMs;
    public int GetDelayMs() => _model.DelayMs;
}
