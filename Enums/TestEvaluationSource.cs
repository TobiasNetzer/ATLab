using System.ComponentModel;

namespace ATLab.Enums;

public enum TestEvaluationSource
{
    [Description("None")]
    NONE,

    [Description("Script")]
    SCRIPT,
    
    [Description("Device Command")]
    COMMAND,
    
    [Description("Interface Command")]
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