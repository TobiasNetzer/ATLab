using System.Runtime.InteropServices.Marshalling;
using ATLab.Interfaces;

namespace ATLab.ViewModels;

public class TestHardwareInfoViewModel : ViewModelBase
{
    public string FirmwareVersion { get; }
    public string DeviceName { get; }
    public string SerialNumber { get; }
    public string BuildDate { get; }
    public string BuildTime { get; }

    public int MeasChannelCount { get; }
    public int StimChannelCount { get; }
    public int ExtStimChannelCount { get; }
    
    public bool InterfaceAvailableI2C { get; }
    public bool InterfaceAvailableUART { get; }
    public bool InterfaceAvailableRS485 { get; }
    
    public TestHardwareInfoViewModel(IHardwareInfo hardwareInfo)
    {
        FirmwareVersion = hardwareInfo.FirmwareVersion;
        DeviceName = hardwareInfo.DeviceName;
        SerialNumber = hardwareInfo.SerialNumber;
        BuildDate = hardwareInfo.BuildDate;
        BuildTime = hardwareInfo.BuildTime;

        MeasChannelCount = hardwareInfo.MeasChannelCount;
        StimChannelCount = hardwareInfo.StimChannelCount;
        ExtStimChannelCount = hardwareInfo.ExtStimChannelCount;

        InterfaceAvailableI2C = true;
        InterfaceAvailableUART = true;
        InterfaceAvailableRS485 = true;
    }
}