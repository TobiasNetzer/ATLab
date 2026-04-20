using System.ComponentModel;

namespace ATLab.Enums;

public enum TestEvaluationSource
{
    [Description("None")]
    NONE,

    [Description("Script")]
    SCRIPT,
    
    [Description("Command")]
    COMMAND,

    [Description("Shell Command")]
    SHELL_COMMAND,
    
    [Description("Operator")]
    OPERATOR
}