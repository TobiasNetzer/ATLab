using ATLab.Interfaces;

namespace ATLab.ViewModels;

/// <summary>
/// Information reported by the connected adapter: firmware, serial number, channel counts and interfaces.
/// </summary>
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
    
    public TestHardwareInfoViewModel(IHardwareAccessor hardwareAccessor)
    {
        FirmwareVersion = hardwareAccessor.Hardware.HardwareInfo.FirmwareVersion;
        DeviceName = hardwareAccessor.Hardware.HardwareInfo.DeviceName;
        SerialNumber = hardwareAccessor.Hardware.HardwareInfo.SerialNumber;
        BuildDate = hardwareAccessor.Hardware.HardwareInfo.BuildDate;
        BuildTime = hardwareAccessor.Hardware.HardwareInfo.BuildTime;

        MeasChannelCount = hardwareAccessor.Hardware.HardwareInfo.MeasChannelCount;
        StimChannelCount = hardwareAccessor.Hardware.HardwareInfo.StimChannelCount;
        ExtStimChannelCount = hardwareAccessor.Hardware.HardwareInfo.ExtStimChannelCount;
        
        InterfaceAvailableI2C = hardwareAccessor.Hardware.HardwareInfo.InterfaceAvailableI2C;
        InterfaceAvailableUART = hardwareAccessor.Hardware.HardwareInfo.InterfaceAvailableUART;
        InterfaceAvailableRS485 = hardwareAccessor.Hardware.HardwareInfo.InterfaceAvailableRS485;
    }
}