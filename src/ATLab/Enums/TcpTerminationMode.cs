namespace ATLab.Enums;

/// <summary>Line terminator of a TCP device, used to send commands and to detect the end of a response.</summary>
public enum TcpTerminationMode
{
    NONE,
    LF,
    CR,
    CRLF
}