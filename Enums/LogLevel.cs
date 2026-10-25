using System.ComponentModel;

namespace ATLab.Enums;

/// <summary>Severity of a log message, also the minimum level for toasts and the log file.</summary>
public enum LogLevel
{
    [Description("Info")]
    INFO,
    [Description("Warning")]
    WARNING,
    [Description("Error")]
    ERROR
}