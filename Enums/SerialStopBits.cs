using System.ComponentModel;

namespace ATLab.Enums;

/// <summary>Stop bits of the UART interface of the test hardware.</summary>
public enum SerialStopBits
{
    [Description("None")]
    NONE,
    [Description("1")]
    ONE,
    [Description("2")]
    TWO,
}