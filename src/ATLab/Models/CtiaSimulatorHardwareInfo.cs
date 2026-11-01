using ATLab.CTIA;
using ATLab.Interfaces;

namespace ATLab.Models;

/// <summary>
/// Hardware information of the cTIA simulator: a fully equipped cTIA. It reports the device ID of the real cTIA, so it
/// opens the same projects, and its own device name.
/// </summary>
public class CtiaSimulatorHardwareInfo : IHardwareInfo
{
    public ushort DeviceId { get; set; } = CtiaHardware.CtiaDeviceId;
    public string DeviceName { get; set; } = "cTIA Simulator";
    public string SerialNumber { get; set; } = "N/A";
    public string FirmwareVersion { get; set; } = "N/A";
    public string BuildDate { get; set; } = "N/A";
    public string BuildTime { get; set; } = "N/A";
    public int MeasChannelCount { get; set; } = 32;
    public int StimChannelCount { get; set; } = 16;
    public int ExtStimChannelCount { get; set; } = 4;
    public bool InterfaceAvailableI2C { get; set; } = true;
    public bool InterfaceAvailableUART { get; set; } = true;
    public bool InterfaceAvailableRS485 { get; set; } = true;
}
