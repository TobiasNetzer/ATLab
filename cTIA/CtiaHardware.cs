using System;
using System.Collections.Generic;
using System.Threading;
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
    
    public byte UseExternalProbe { get; set; }
    
    private readonly SemaphoreSlim _ioLock = new(1, 1);

    public CtiaHardware(CtiaCommunication communication)
    {
        HardwareInfo = new HardwareInfo();
        StimChannelStates = [];
        ExtStimChannelStates = [];
        MeasChannelStates = [];
        
        ActiveMeasChannelH = 0;
        ActiveMeasChannelL = 0;
        UseExternalProbe = 0;
        
        _command = new CtiaCommand(communication);
    }

    public async Task<OperationResult> InitializeAsync()
    {
        await _ioLock.WaitAsync();
        try
        {
            var deviceIdResult = await _command.GetDeviceID();
            if (!deviceIdResult.IsSuccess)
                return OperationResult.Failure("Invalid response from device");

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

            var serialNumber = await _command.GetSerialNumber();
            if (!serialNumber.IsSuccess)
                return OperationResult.Failure(serialNumber.ErrorMessage);
            HardwareInfo.SerialNumber = serialNumber.Value ?? string.Empty;

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
            HardwareInfo.MeasChannelCount = measChannelCount.Value;

            var stimChannelCount = await _command.GetStimChannelCount();
            if (!stimChannelCount.IsSuccess)
                return OperationResult.Failure(stimChannelCount.ErrorMessage);
            HardwareInfo.StimChannelCount = stimChannelCount.Value;

            var extStimChannelCount = await _command.GetExtStimChannelCount();
            if (!extStimChannelCount.IsSuccess)
                return OperationResult.Failure(extStimChannelCount.ErrorMessage);
            HardwareInfo.ExtStimChannelCount = extStimChannelCount.Value;

            StimChannelStates = new bool[HardwareInfo.StimChannelCount];
            ExtStimChannelStates = new bool[HardwareInfo.ExtStimChannelCount];
            MeasChannelStates = new bool[HardwareInfo.MeasChannelCount];

            return OperationResult.Success();
        }
        finally
        {
            _ioLock.Release();
        }
    }

    public async Task<OperationResult> UpdateRelayStates()
    {
        await _ioLock.WaitAsync();
        try
        {
            var steps = new List<Func<Task<OperationResult>>>
            {
                () => SetStimChannels(),
                () => SetMeasChannelH(ActiveMeasChannelH),
                () => SetMeasChannelL(ActiveMeasChannelL),
                () => SetExternalProbe(UseExternalProbe),
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
        finally
        {
            _ioLock.Release();
        }
    }

    public async Task<OperationResult> ClearRelayStates()
    {
        await _ioLock.WaitAsync();
        try
        {
            var commandResponse = await _command.ClrAllRelayStates();
            return !commandResponse.IsSuccess
                ? OperationResult.Failure(commandResponse.ErrorMessage)
                : OperationResult.Success();
        }
        finally
        {
            _ioLock.Release();
        }
    }


    private async Task<OperationResult> SetStimChannels()
    {
        var commandResponse = await _command.SetStimChBitfield(StimChannelStates);
        
        return !commandResponse.IsSuccess ? OperationResult.Failure(commandResponse.ErrorMessage) : OperationResult.Success();
    }
    
    private async Task<OperationResult> SetExtStimChannels()
    {
        var commandResponse = await _command.SetExtStimChBitfield(ExtStimChannelStates);
        
        return !commandResponse.IsSuccess ? OperationResult.Failure(commandResponse.ErrorMessage) : OperationResult.Success();
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
    
    private async Task<OperationResult> SetExternalProbe(byte channel)
    {
        var commandResponse = await _command.SetExternalProbeIn(channel);
        
        return !commandResponse.IsSuccess ? OperationResult.Failure(commandResponse.ErrorMessage) : OperationResult.Success();
    }
}