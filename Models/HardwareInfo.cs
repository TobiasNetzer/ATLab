using ATLab.Interfaces;

namespace ATLab.Models;

public class HardwareInfo : IHardwareInfo
{
    public string DeviceName { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string FirmwareVersion { get; set; } = string.Empty;
    public string BuildDate { get; set; } = string.Empty;
    public string BuildTime { get; set; } = string.Empty;
    public int MeasChannelCount { get; set; }
    public int StimChannelCount { get; set; }
    public int ExtStimChannelCount { get; set; }
    public bool InterfaceAvailableI2C { get; set; }
    public bool InterfaceAvailableUART { get; set; }
    public bool InterfaceAvailableRS485 { get; set; }
}