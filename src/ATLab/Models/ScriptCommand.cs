using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

/// <summary>
/// One command of a script or a Device Command step. A script command that expects a response and is not the
/// evaluated one can check its response: it is processed with its own mask and compared with its own limits.
/// </summary>
public partial class ScriptCommand : ObservableObject
{
    [ObservableProperty]
    private string _command = string.Empty;

    [ObservableProperty]
    private bool _isExpectResponse;

    [ObservableProperty]
    private bool _isEvaluate;

    [ObservableProperty]
    private int _delayMs;

    [ObservableProperty]
    private int _timeoutMs = 1000;

    // The check settings are left out of the file while unused, so Device Command steps in projects stay unchanged
    [ObservableProperty]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    private bool _isCheckResponse;

    [ObservableProperty]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    private string? _checkMask;

    [ObservableProperty]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    private int _checkSkip;

    [ObservableProperty]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    private int _checkLength;

    [ObservableProperty]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    private double _checkLowerLimit;

    [ObservableProperty]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    private double _checkUpperLimit;

    /// <summary>True when the response of this command is checked during execution.</summary>
    [JsonIgnore]
    public bool IsResponseChecked => IsCheckResponse && IsExpectResponse && !IsEvaluate;

    public ScriptCommand()
    {

    }

    public ScriptCommand(ScriptCommand other)
    {
        Command = other.Command;
        IsExpectResponse = other.IsExpectResponse;
        IsEvaluate = other.IsEvaluate;
        DelayMs = other.DelayMs;
        TimeoutMs = other.TimeoutMs;
        IsCheckResponse = other.IsCheckResponse;
        CheckMask = other.CheckMask;
        CheckSkip = other.CheckSkip;
        CheckLength = other.CheckLength;
        CheckLowerLimit = other.CheckLowerLimit;
        CheckUpperLimit = other.CheckUpperLimit;
    }
}
