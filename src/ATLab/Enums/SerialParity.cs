using System.ComponentModel;

namespace ATLab.Enums;

/// <summary>Parity of the UART interface of the test hardware.</summary>
public enum SerialParity
{
    [Description("None")]
    NONE,
    [Description("Odd")]
    ODD,
    [Description("Even")]
    EVEN,
}