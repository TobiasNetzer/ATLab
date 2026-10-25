using System.ComponentModel;

namespace ATLab.Enums;

/// <summary>How the end of a response is detected on a serial device.</summary>
public enum MessageFramingMode
{
    [Description("Chunk")]
    CHUNK,
    
    [Description("LF Terminated")]
    LF_TERMINATED,
    
    [Description("CRLF Terminated")]
    CR_LF_TERMINATED,
    
    [Description("Timeout Based")]
    TIMEOUT_BASED
}