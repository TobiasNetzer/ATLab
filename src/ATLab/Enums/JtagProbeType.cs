using System.ComponentModel;

namespace ATLab.Enums;

/// <summary>Supported JTAG probes for boundary scan.</summary>
public enum JtagProbeType
{
    [Description("SEGGER J-Link")]
    JLINK,

    [Description("FTDI MPSSE (FT2232H / FT232H / FT4232H)")]
    FTDI_MPSSE
}