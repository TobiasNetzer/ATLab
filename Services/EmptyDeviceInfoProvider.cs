using ATLab.Interfaces;

namespace ATLab.Services;

public class EmptyDeviceInfoProvider : IDeviceInfoProvider
{
    public string? FirmwareVersion => "N/A";
    public string? DeviceName => "No Device";
    public string? BuildDate => "N/A";
    public string? BuildTime => "N/A";
}
