using System.ComponentModel;

namespace ATLab.Enums;

public enum TestEvaluationSource
{
    [Description("None")]
    NONE,

    [Description("Script")]
    SCRIPT,

    [Description("Shell Command")]
    SHELL_COMMAND
}
