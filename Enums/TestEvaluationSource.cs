using System.ComponentModel;

namespace ATLab.Enums;

public enum TestEvaluationSource
{
    [Description("None")]
    NONE,

    [Description("Script")]
    SCRIPT,

    [Description("Console Command")]
    COMMAND
}
