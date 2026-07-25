using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.Records;

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

    private I2CSpeedMode _speedMode = I2CSpeedMode.I2C_SPEED_UNDEFINED;

    public CtiaHardware(ICtiaCommunication communication)
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
            
            var availableI2CInterface = await _command.GetI2CInterface();
            if (!availableI2CInterface.IsSuccess)
                return OperationResult.Failure(availableI2CInterface.ErrorMessage);
            HardwareInfo.InterfaceAvailableI2C = Convert.ToBoolean(availableI2CInterface.Value);
            
            var availableUartInterface = await _command.GetUartInterface();
            if (!availableUartInterface.IsSuccess)
                return OperationResult.Failure(availableUartInterface.ErrorMessage);
            HardwareInfo.InterfaceAvailableI2C = Convert.ToBoolean(availableUartInterface.Value);
            
            var availableRs485Interface = await _command.GetRs485Interface();
            if (!availableRs485Interface.IsSuccess)
                return OperationResult.Failure(availableRs485Interface.ErrorMessage);
            HardwareInfo.InterfaceAvailableI2C = Convert.ToBoolean(availableRs485Interface.Value);

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
    
    public async Task<OperationResult> ConfigureI2CInterface(I2CSpeedMode speedMode)
    {
        if (_speedMode == speedMode)
            return OperationResult.Success();
        
        _speedMode = speedMode;
        
        await _ioLock.WaitAsync();
        try
        {
            return await _command.ConfI2CSettings(speedMode);
        }
        finally
        {
            _ioLock.Release();
        }
    }
    
    public async Task<OperationResult<TestHardwareDiagnostics>> ExecuteSelfTest()
    {
        await _ioLock.WaitAsync();
        try
        {
            return await _command.ExecuteSelfTest();
        }
        finally
        {
            _ioLock.Release();
        }
    }
    
    public async Task<OperationResult<I2CResponse>> ExecuteI2CTransmit(byte deviceAddr,byte[] data,  int timeoutMs = 1000)
    {
        await _ioLock.WaitAsync();
        try
        {
            return await _command.ExecuteI2CTransmit(deviceAddr, data, timeoutMs);
        }
        finally
        {
            _ioLock.Release();
        }
    }

    public async Task<OperationResult<I2CResponse>> ExecuteI2CReceive(byte deviceAddr, byte bytesToRead, int timeoutMs = 1000)
    {
        await _ioLock.WaitAsync();
        try
        {
            return await _command.ExecuteI2CReceive(deviceAddr, bytesToRead,  timeoutMs);
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
        
        return commandResponse.IsSuccess ? OperationResult.Success() : OperationResult.Failure(commandResponse.ErrorMessage);
    }
}