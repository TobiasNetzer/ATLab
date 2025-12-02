using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Services;
using ATLab.Models;

namespace ATLab.CTIA;

public class CtiaHardware : ITestHardware
{
    private readonly CtiaCommand _command;
    public IHardwareInfo HardwareInfo { get; }
    
    public bool[] StimChannelStates {get; set;}
    public bool[] ExtStimChannelStates { get; set; }
    public bool[] MeasChannelStatesH { get; set; }
    public bool[] MeasChannelStatesL { get; set; }

    public CtiaHardware(ITestHardwareCommunication testHardwareCommunication)
    {
        HardwareInfo = new DummyHardwareInfo();
        
        MeasChannelStatesH = new  bool[HardwareInfo.MeasChannelCount];
        MeasChannelStatesL = new  bool[HardwareInfo.MeasChannelCount];
        StimChannelStates = new  bool[HardwareInfo.StimChannelCount];
        ExtStimChannelStates = new  bool[HardwareInfo.ExtStimChannelCount];
        
        var communication = new CtiaCommunication(testHardwareCommunication);
        _command = new CtiaCommand(communication);
    }

    public async Task<OperationResult> InitializeAsync()
    {
        var deviceIdResult = await _command.GetDeviceID();
        if (!deviceIdResult.IsSuccess)
            return OperationResult.Failure(deviceIdResult.ErrorMessage);

        if (deviceIdResult.Value != 0xA101)
            return OperationResult.Failure("Device ID invalid");

        var firmwareVersion = await _command.GetFirmwareVersion();
        if (!firmwareVersion.IsSuccess)
            return OperationResult.Failure(firmwareVersion.ErrorMessage);
        HardwareInfo.FirmwareVersion = firmwareVersion.Value ?? string.Empty;

        var deviceName = await _command.GetDeviceName();
        if (!deviceName.IsSuccess)
            return OperationResult.Failure(deviceName.ErrorMessage);
        HardwareInfo.DeviceName = deviceName.Value ?? string.Empty;

        var buildDate = await _command.GetFirmwareBuildDate();
        if (!buildDate.IsSuccess)
            return OperationResult.Failure(buildDate.ErrorMessage);
        HardwareInfo.BuildDate = buildDate.Value ?? string.Empty;

        var buildTime = await _command.GetFirmwareBuildTime();
        if (!buildTime.IsSuccess)
            return OperationResult.Failure(buildTime.ErrorMessage);
        HardwareInfo.BuildTime = buildTime.Value ?? string.Empty;

        HardwareInfo.MeasChannelCount = 0; // Replace with GET_MEAS_CH Command
        HardwareInfo.StimChannelCount = 16;
        HardwareInfo.ExtStimChannelCount = 4;
        
        MeasChannelStatesH = new  bool[HardwareInfo.MeasChannelCount];
        MeasChannelStatesL = new  bool[HardwareInfo.MeasChannelCount];
        StimChannelStates = new  bool[HardwareInfo.StimChannelCount];
        ExtStimChannelStates = new  bool[HardwareInfo.ExtStimChannelCount];
        
        return OperationResult.Success();
    }

    public async Task<OperationResult> SetStimChannels()
    {
        await _command.SetStimChBiftield(StimChannelStates);
        return OperationResult.Success();
    }
    
    public async Task<OperationResult> SetExtStimChannels()
    {
        await _command.SetExtStimChBiftield(ExtStimChannelStates);
        return OperationResult.Success();
    }
}