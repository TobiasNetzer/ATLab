using System.Globalization;
using ATLab.Helpers;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

/// <summary>One command of a script, with the settings of its response check.</summary>
public partial class ScriptCommandViewModel : ViewModelBase
{
    private readonly ScriptCommand _model;
    private readonly ScriptViewModel _parent;

    [ObservableProperty]
    private string _command;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCheckResponse), nameof(IsResponseChecked))]
    private bool _isExpectResponse;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCheckResponse), nameof(IsResponseChecked))]
    private bool _isEvaluate;

    [ObservableProperty]
    private string _delayMs;

    [ObservableProperty]
    private string _timeoutMs;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsResponseChecked))]
    private bool _isCheckResponse;

    [ObservableProperty]
    private string _checkMask;

    [ObservableProperty]
    private string _checkSkip;

    [ObservableProperty]
    private string _checkLength;

    [ObservableProperty]
    private string _checkLowerLimit;

    [ObservableProperty]
    private string _checkUpperLimit;

    // The evaluated response is processed by the step, only the other responses can be checked in the script
    public bool CanCheckResponse => IsExpectResponse && !IsEvaluate;

    public bool IsResponseChecked => CanCheckResponse && IsCheckResponse;

    public ScriptCommandViewModel(ScriptCommand model, ScriptViewModel parent)
    {
        _model = model;
        _parent = parent;

        Command = model.Command;
        IsExpectResponse = model.IsExpectResponse;
        IsEvaluate = model.IsEvaluate;
        DelayMs = model.DelayMs.ToString();
        TimeoutMs = model.TimeoutMs.ToString();
        IsCheckResponse = model.IsCheckResponse;
        CheckMask = model.CheckMask ?? string.Empty;
        CheckSkip = model.CheckSkip.ToString();
        CheckLength = model.CheckLength.ToString();
        CheckLowerLimit = FormatLimit(model.CheckLowerLimit);
        CheckUpperLimit = FormatLimit(model.CheckUpperLimit);
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

    partial void OnIsCheckResponseChanged(bool value) => _model.IsCheckResponse = value;

    partial void OnCheckMaskChanged(string value) =>
        _model.CheckMask = string.IsNullOrWhiteSpace(value) ? null : value;

    partial void OnCheckSkipChanged(string value)
    {
        if (int.TryParse(value, out var result) && result >= 0)
        {
            _model.CheckSkip = result;
        }
        else
        {
            CheckSkip = "0";
            _model.CheckSkip = 0;
        }
    }

    partial void OnCheckLengthChanged(string value)
    {
        if (int.TryParse(value, out var result) && result >= 0)
        {
            _model.CheckLength = result;
        }
        else
        {
            CheckLength = "0";
            _model.CheckLength = 0;
        }
    }

    partial void OnCheckLowerLimitChanged(string value)
    {
        if (NumberHelper.TryParseDecimal(value, out var result))
            _model.CheckLowerLimit = result;
        else
            CheckLowerLimit = FormatLimit(_model.CheckLowerLimit);
    }

    partial void OnCheckUpperLimitChanged(string value)
    {
        if (NumberHelper.TryParseDecimal(value, out var result))
            _model.CheckUpperLimit = result;
        else
            CheckUpperLimit = FormatLimit(_model.CheckUpperLimit);
    }

    private static string FormatLimit(double value) => value.ToString("G15", CultureInfo.CurrentCulture);

    public ScriptCommand GetModel() => _model;
}
