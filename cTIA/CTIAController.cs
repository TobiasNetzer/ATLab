using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Services;
using ATLab.Models;

namespace ATLab.CTIA;

public class CTIAController : IUniversalTestHardwareInterface
{
    private readonly CtiaCommand _command;
    public IHardwareInfoProvider HardwareInfoProvider { get; }
    
    public bool[] StimChannelStates {get; set;}
    public bool[] ExtStimChannelStates { get; set; }
    public bool[] MeasChannelStatesH { get; set; }
    public bool[] MeasChannelStatesL { get; set; }

    public CTIAController(ICommunicationInterface communicationInterface)
    {
        HardwareInfoProvider = new EmptyHardwareInfoProvider();
        StimChannelStates = new bool[16];
        
        var communication = new CTIACommunication(communicationInterface);
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
        HardwareInfoProvider.FirmwareVersion = firmwareVersion.Value ?? string.Empty;

        var deviceName = await _command.GetDeviceName();
        if (!deviceName.IsSuccess)
            return OperationResult.Failure(deviceName.ErrorMessage);
        HardwareInfoProvider.DeviceName = deviceName.Value ?? string.Empty;

        var buildDate = await _command.GetFirmwareBuildDate();
        if (!buildDate.IsSuccess)
            return OperationResult.Failure(buildDate.ErrorMessage);
        HardwareInfoProvider.BuildDate = buildDate.Value ?? string.Empty;

        var buildTime = await _command.GetFirmwareBuildTime();
        if (!buildTime.IsSuccess)
            return OperationResult.Failure(buildTime.ErrorMessage);
        HardwareInfoProvider.BuildTime = buildTime.Value ?? string.Empty;

        HardwareInfoProvider.MeasChannelCount = 0; // Replace with GET_MEAS_CH Command
        HardwareInfoProvider.StimChannelCount = 0;
        HardwareInfoProvider.ExtStimChannelCount = 0;

        return OperationResult.Success();
    }

    public async Task<OperationResult> SetStimChannels()
    {
        await _command.SetStimChBiftield(StimChannelStates);
        return OperationResult.Success();
    }
}