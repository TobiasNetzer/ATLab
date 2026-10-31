namespace ATLab.Enums;

/// <summary>Line terminator appended to commands sent over a serial connection.</summary>
public enum SerialTerminationMode
{
    NONE,
    LF,
    CR,
    CRLF
}