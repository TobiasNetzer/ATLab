using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

public partial class ScriptCommand : ObservableObject
{
    [ObservableProperty]
    private string _command = string.Empty;

    [ObservableProperty]
    private bool _expectResponse;
    
    [ObservableProperty]
    private bool _useForValidation;

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
        ExpectResponse = other.ExpectResponse;
        UseForValidation = other.UseForValidation;
        DelayMs = other.DelayMs;
        TimeoutMs = other.TimeoutMs;
    }
}
