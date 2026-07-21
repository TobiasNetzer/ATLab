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
    
    [Description("Interface")]
    INTERFACE,

    [Description("Shell Command")]
    SHELL_COMMAND,
    
    [Description("User Response")]
    USER_RESPONSE,
    
    [Description("Expression")]
    EXPRESSION,
    
    [Description("File Content")]
    FILE
}