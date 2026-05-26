using System.ComponentModel;

namespace ATLab.Enums;

public enum DeviceType
{
    [Description("Serial Port")]
    SERIAL,

    [Description("TCP/IP")]
    TCP_IP,
    
    [Description("VISA")]
    VISA
}
