using System.ComponentModel;

namespace ATLab.Enums;

public enum MessageFramingMode
{
    [Description("Chunk")]
    CHUNK,
    
    [Description("LF Terminated")]
    LF_TERMINATED,
    
    [Description("CRLF Terminated")]
    CR_LF_TERMINATED
}