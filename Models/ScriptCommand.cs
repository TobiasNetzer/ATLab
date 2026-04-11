using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

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
    }
}
