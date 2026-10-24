using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

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