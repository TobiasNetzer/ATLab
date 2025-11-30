namespace ATLab.Interfaces;
public interface IHardwareInfoProvider
{
    string FirmwareVersion { get; set; }
    string DeviceName { get; set; }
    string BuildDate { get; set; }
    string BuildTime { get; set; }
    int MeasChannelCount { get; set; }
    int StimChannelCount { get; set; }
    int ExtStimChannelCount { get; set; }
}