using ATLab.Interfaces;

namespace ATLab.Models;

public class DummyHardwareInfo : IHardwareInfo
{
    public string FirmwareVersion { get; set; } = "N/A";
    public string DeviceName { get; set; } = "No Device";
    public string BuildDate { get; set; } = "N/A";
    public string BuildTime { get; set; } = "N/A";
    public int MeasChannelCount { get; set; } = 32;
    public int StimChannelCount { get; set; } = 16;
    public int ExtStimChannelCount { get; set; } = 4;
}
