using ATLab.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

public partial class ShellCommand : ObservableObject
{
    [ObservableProperty]
    private string _command = string.Empty;

    [ObservableProperty]
    private ShellCommandOptions _option;
    
    [ObservableProperty]
    private bool _isDirectLaunch;
    
    public ShellCommand()
    {

    }
    
    public ShellCommand(ShellCommand other)
    {
        Command = other.Command;
        Option = other.Option;
        IsDirectLaunch = other.IsDirectLaunch;
    }
}