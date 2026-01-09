using System.ComponentModel;

namespace ATLab.Enums;

public enum ShellCommandOptions
{
    [Description("Auto Close")]
    CLOSE_WHEN_DONE,
    
    [Description("Keep Open")]
    KEEP_OPEN
}