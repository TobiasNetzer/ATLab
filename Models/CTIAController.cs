using System;
using System.Text;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Services;

namespace ATLab.Models;

public class CTIAController : IDeviceInfoProvider
{
    public GetCommands Get { get; private set; }
    public SetCommands Set { get; private set; }
    public ClrCommands Clr { get; private set; }

    public string FirmwareVersion { get; private set; } = string.Empty;
    public string DeviceName { get; private set; } = string.Empty;
    public string BuildDate { get; private set; } = string.Empty;
    public string BuildTime { get; private set; } = string.Empty;

    private RelayState? _Meas_H { get; set; }
    private RelayState? _Meas_L { get; set; }
    private RelayState? _Stim { get; set; }
    private RelayState? _Ext_Stim { get; set; }

    public CTIAController(ICommunicationInterface communicationInterface)
    {
        var communication = new CTIACommunication(communicationInterface);
        Set = new SetCommands(communication);
        Get = new GetCommands(communication);
        Clr = new ClrCommands(communication);
    }

    public async Task<OperationResult> InitializeAsync()
    {
        var deviceIdResult = await Get.GetDeviceID();
        if (!deviceIdResult.IsSuccess)
            return OperationResult.Failure(deviceIdResult.ErrorMessage);

        if (deviceIdResult.Value != 0xA101)
            return OperationResult.Failure("Device ID invalid");

        var firmwareVersion = await Get.GetFirmwareVersion();
        if (!firmwareVersion.IsSuccess)
            return OperationResult.Failure(firmwareVersion.ErrorMessage);
        FirmwareVersion = firmwareVersion.Value ?? string.Empty;

        var deviceName = await Get.GetDeviceName();
        if (!deviceName.IsSuccess)
            return OperationResult.Failure(deviceName.ErrorMessage);
        DeviceName = deviceName.Value ?? string.Empty;

        var buildDate = await Get.GetFirmwareBuildDate();
        if (!buildDate.IsSuccess)
            return OperationResult.Failure(buildDate.ErrorMessage);
        BuildDate = buildDate.Value ?? string.Empty;

        var buildTime = await Get.GetFirmwareBuildTime();
        if (!buildTime.IsSuccess)
            return OperationResult.Failure(buildTime.ErrorMessage);
        BuildTime = buildTime.Value ?? string.Empty;
        
        // Initialize relay states
        _Meas_H = new RelayState(32);
        _Meas_L = new RelayState(32);
        _Stim = new RelayState(16);
        _Ext_Stim = new RelayState(4);

        return OperationResult.Success();
    }
}