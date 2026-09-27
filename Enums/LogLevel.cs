using System.ComponentModel;

namespace ATLab.Enums;

public enum LogLevel
{
    [Description("Info")]
    INFO,
    [Description("Success")]
    SUCCESS,
    [Description("Warning")]
    WARNING,
    [Description("Error")]
    ERROR
}