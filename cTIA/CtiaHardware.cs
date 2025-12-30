using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.CTIA;

public class CtiaHardware : ITestHardware
{
    private readonly CtiaCommand _command;
    public IHardwareInfo HardwareInfo { get; }
    
    public bool[] StimChannelStates {get; set;}
    public bool[] ExtStimChannelStates { get; set; }
    public bool[] MeasChannelStates { get; set; }
    public byte ActiveMeasChannelH { get; set; }
    public byte ActiveMeasChannelL { get; set; }

    public CtiaHardware(CtiaCommunication communication)
    {
        HardwareInfo = new DummyHardwareInfo();
        StimChannelStates = new  bool[HardwareInfo.StimChannelCount];
        ExtStimChannelStates = new  bool[HardwareInfo.ExtStimChannelCount];
        MeasChannelStates = new  bool[HardwareInfo.MeasChannelCount];
        
        ActiveMeasChannelH = 0;
        ActiveMeasChannelL = 0;
        
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

        var measChannelCount = await _command.GetMeasChannelCount();
        if (!measChannelCount.IsSuccess)
            return OperationResult.Failure(measChannelCount.ErrorMessage);
        HardwareInfo.MeasChannelCount =  measChannelCount.Value;
        
        var stimChannelCount = await _command.GetStimChannelCount();
        if (!stimChannelCount.IsSuccess)
            return OperationResult.Failure(stimChannelCount.ErrorMessage);
        HardwareInfo.StimChannelCount = stimChannelCount.Value;
        
        var extStimChannelCount = await _command.GetExtStimChannelCount();
        if (!extStimChannelCount.IsSuccess)
            return OperationResult.Failure(extStimChannelCount.ErrorMessage);
        HardwareInfo.ExtStimChannelCount =  extStimChannelCount.Value;
        
        StimChannelStates = new  bool[HardwareInfo.StimChannelCount];
        ExtStimChannelStates = new  bool[HardwareInfo.ExtStimChannelCount];
        MeasChannelStates = new  bool[HardwareInfo.MeasChannelCount];
        
        return OperationResult.Success();
    }

    public async Task<OperationResult> UpdateRelayStates()
    {
        var steps = new List<Func<Task<OperationResult>>>
        {
            () => SetStimChannels(),
            () => SetMeasChannelH(ActiveMeasChannelH),
            () => SetMeasChannelL(ActiveMeasChannelL),
            () => SetExtStimChannels()
        };

        foreach (var step in steps)
        {
            var result = await step();
            if (!result.IsSuccess)
                return result;
        }

        return OperationResult.Success();
    }

    public async Task<OperationResult> ClearRelayStates()
    {
        var commandResponse = await _command.ClrAllRelayStates();
        return !commandResponse.IsSuccess ? OperationResult.Failure(commandResponse.ErrorMessage) : OperationResult.Success();
    }

    private async Task<OperationResult> SetStimChannels()
    {
        var commandResponse = await _command.SetStimChBitfield(StimChannelStates);
        
        if (!commandResponse.IsSuccess)
            return OperationResult.Failure(commandResponse.ErrorMessage);
        
        return OperationResult.Success();
    }
    
    private async Task<OperationResult> SetExtStimChannels()
    {
        var commandResponse = await _command.SetExtStimChBitfield(ExtStimChannelStates);
        
        if (!commandResponse.IsSuccess)
            return OperationResult.Failure(commandResponse.ErrorMessage);
        
        return OperationResult.Success();
    }

    private async Task<OperationResult> SetMeasChannelH(byte channel)
    {
        if (channel == 0)
        {
            var commandResponse = await _command.ClrMeasH();
            if (!commandResponse.IsSuccess)
                return OperationResult.Failure(commandResponse.ErrorMessage);
        }
        else
        {
            var commandResponse = await _command.SetExclusiveMeasChH(channel);
            if (!commandResponse.IsSuccess)
                    return OperationResult.Failure(commandResponse.ErrorMessage);
        }
        
        return OperationResult.Success();
    }
    
    private async Task<OperationResult> SetMeasChannelL(byte channel)
    {
        if ( channel == 0)
        {
            var commandResponse = await _command.ClrMeasL();
            if (!commandResponse.IsSuccess)
                return OperationResult.Failure(commandResponse.ErrorMessage);
        }
        else
        {
           var commandResponse = await _command.SetExclusiveMeasChL(channel);
           if (!commandResponse.IsSuccess)
                   return OperationResult.Failure(commandResponse.ErrorMessage); 
        }
        
        return OperationResult.Success();
    }
}