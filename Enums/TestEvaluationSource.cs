using System.ComponentModel;

namespace ATLab.Enums;

public enum TestEvaluationSource
{
    [Description("None")]
    NONE,

    [Description("Internal Script")]
    SCRIPT,

    [Description("Console Script")]
    COMMAND
}
