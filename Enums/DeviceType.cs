using System.ComponentModel;

namespace ATLab.Enums;

/// <summary>Transport used to reach a device from the Device Manager.</summary>
public enum DeviceType
{
    [Description("Serial Port")]
    SERIAL,

    [Description("TCP/IP")]
    TCP_IP,
    
    [Description("VISA")]
    VISA
}
