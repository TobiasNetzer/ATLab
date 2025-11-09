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
        Set = new SetCommands(new CTIACommunication(communicationInterface));
        Get = new GetCommands(new CTIACommunication(communicationInterface));
        
    }

    public async Task InitializeAsync()
    {
        var DeviceId = await Get.GetDeviceID();
        if (DeviceId != 0xA101)
        {
            throw new InvalidOperationException("Device ID invalid");
        }
        FirmwareVersion = await Get.GetFirmwareVersion();
        DeviceName = await Get.GetDeviceName();
        BuildDate = await Get.GetFirmwareBuildDate();
        BuildTime = await Get.GetFirmwareBuildTime();
        // Initialize relay states
        _Meas_H = new RelayState(32);
        _Meas_L = new RelayState(32);
        _Stim = new RelayState(16);
        _Ext_Stim = new RelayState(4);
    }
}