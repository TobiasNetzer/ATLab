using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

public partial class ShellCommand : ObservableObject
{
    [ObservableProperty]
    private string _command = string.Empty;

    [ObservableProperty]
    private bool _evaluateOutput;

    public ShellCommand()
    {

    }

    public ShellCommand(ShellCommand other)
    {
        Command = other.Command;
        EvaluateOutput = other.EvaluateOutput;
    }
}