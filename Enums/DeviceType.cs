using System.ComponentModel;

namespace ATLab.Enums;

public enum DeviceType
{
    [Description("Serial Port")]
    SERIAL,
    
    [Description("VISA")]
    VISA,

    [Description("TCP/IP")]
    TCP_IP
}
