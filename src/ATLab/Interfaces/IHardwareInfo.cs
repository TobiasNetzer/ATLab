namespace ATLab.Interfaces;
/// <summary>Identification, channel counts and available interfaces of the test hardware.</summary>
public interface IHardwareInfo
{
    /// <summary>
    /// Identifies the kind of test hardware, reported by the hardware itself. Projects store the ID of the hardware
    /// they were developed for and only open on hardware (or a simulator) with the same ID.
    /// </summary>
    ushort DeviceId { get; set; }
    string FirmwareVersion { get; set; }
    string DeviceName { get; set; }
    string SerialNumber { get; set; }
    string BuildDate { get; set; }
    string BuildTime { get; set; }
    int MeasChannelCount { get; set; }
    int StimChannelCount { get; set; }
    int ExtStimChannelCount { get; set; }
    bool InterfaceAvailableI2C { get; set; }
    bool InterfaceAvailableUART { get; set; }
    bool InterfaceAvailableRS485 { get; set; }
}