using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

/// <summary>Command line of an External Program step and whether its output is evaluated.</summary>
public partial class ExternalProgram : ObservableObject
{
    [ObservableProperty]
    private string _command = string.Empty;

    [ObservableProperty]
    private bool _evaluateOutput;

    public ExternalProgram()
    {

    }

    public ExternalProgram(ExternalProgram other)
    {
        Command = other.Command;
        EvaluateOutput = other.EvaluateOutput;
    }
}