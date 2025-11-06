namespace ATLab.Interfaces;
public interface IDeviceInfoProvider
{
    string? FirmwareVersion { get; }
    string? DeviceName { get; }
    string? BuildDate { get; }
    string? BuildTime { get; }
}