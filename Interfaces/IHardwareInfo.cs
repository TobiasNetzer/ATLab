namespace ATLab.Interfaces;
public interface IHardwareInfo
{
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