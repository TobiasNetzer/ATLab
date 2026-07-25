using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Helpers;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

public class InterfaceCommandExecuter : IInterfaceCommandExecuter
{
    private readonly ITestHardware _testHardware;
    private readonly IErrorService  _errorService;

    public InterfaceCommandExecuter(ITestHardware testHardware,
        IErrorService  errorService)
    {
        _testHardware = testHardware;
        _errorService = errorService;
    }

    public async Task<OperationResult<double>> ExecuteAsync(
        TestInterfaceConfig config,
        List<CustomVariable> runtimeVariables,
        ResponseMask? mask)
    {
        switch (config.InterfaceType)
        {
            case CommunicationInterfaceType.I2C:
                return await ExecuteI2CAsync(config, runtimeVariables, mask);
            case CommunicationInterfaceType.UART:
                return await ExecuteUartAsync(config, runtimeVariables, mask);
            default:
                return OperationResult<double>.Failure("Interface not available");
        }
    }

    private async Task<OperationResult<double>> ExecuteI2CAsync(
        TestInterfaceConfig config,
        List<CustomVariable> runtimeVariables,
        ResponseMask? mask)
    {
        if (!_testHardware.HardwareInfo.InterfaceAvailableI2C)
            return OperationResult<double>.Failure("Interface not available");

        double result = 0;
        
        var configResp = await _testHardware.ConfigureI2CInterface(config.I2CSpeedMode);

        if (configResp.IsFailure)
            return OperationResult<double>.Failure(configResp.ErrorMessage);
        
        if (!string.IsNullOrEmpty(config.Command))
        {
            var compiled = CommandProcessor.CompileToBytes(config.Command, runtimeVariables);

            var status = await _testHardware.ExecuteI2CTransmit(
                Convert.ToByte(config.I2CAddress),
                compiled,
                config.TimeoutMs);

            if (status.IsTimeout)
                return OperationResult<double>.Timeout(status.ErrorMessage);

            if (status.IsFailure)
                return OperationResult<double>.Failure(status.ErrorMessage);
            
            if (status.Value != null && !status.Value.Success)
            {
                _errorService.AddError("I²C write failed: NACK received.");
            }
            
            result = Convert.ToDouble(status.Value != null && status.Value.Success);
        }
        
        if (config.BytesToRead > 0)
        {
            var status = await _testHardware.ExecuteI2CReceive(
                Convert.ToByte(config.I2CAddress),
                Convert.ToByte(config.BytesToRead),
                config.TimeoutMs);
            
            var processed = ResponseProcessor.Process(status.Value?.Data ?? Array.Empty<byte>(), mask);
            
            if (status.IsTimeout)
                return OperationResult<double>.Timeout(status.ErrorMessage);

            if (status.IsFailure)
                return OperationResult<double>.Failure(status.ErrorMessage);
        
            if (status.Value != null && !status.Value.Success)
            {
                _errorService.AddError("I²C read failed: NACK received.");
                return OperationResult<double>.Success(0);
            }

            try
            {
                var converted = (double)Convert.ChangeType(
                    processed,
                    typeof(double),
                    CultureInfo.InvariantCulture);

                return OperationResult<double>.Success(converted);
            }
            catch (Exception ex)
            {
                return OperationResult<double>.Failure(
                    $"Failed to convert result '{processed}': {ex.Message}");
            }
            
        }
        
        return OperationResult<double>.Success(result);
    }

    private async Task<OperationResult<double>> ExecuteUartAsync(
        TestInterfaceConfig config,
        List<CustomVariable> runtimeVariables,
        ResponseMask? mask)
    {
        if (!_testHardware.HardwareInfo.InterfaceAvailableUART)
            return OperationResult<double>.Failure("Interface not available");
        
        var configResp = await _testHardware.ConfigureUartInterface(config.BaudRate, config.DataBits, config.SerialParity, config.StopBits);

        if (configResp.IsFailure)
            return OperationResult<double>.Failure(configResp.ErrorMessage);
        
        var compiled = CommandProcessor.CompileToBytes(config.Command, runtimeVariables);
        
        var terminated = ApplyTermination(compiled, config.SerialTerminationMode);

        var status = await _testHardware.ExecuteUartTransceive(
            terminated,
            Convert.ToByte(config.BytesToRead),
            config.TimeoutMs);
            
        if (status.Value != null && status.Value.Length != config.BytesToRead)
        {
            _errorService.AddError("Not all bytes received. Expected: " + config.BytesToRead + ", Received: " + status.Value.Length + "");
        }
            
        var processed = ResponseProcessor.Process(status.Value ?? Array.Empty<byte>(), mask);
        
        if (status.IsTimeout)
            return OperationResult<double>.Timeout(status.ErrorMessage);

        if (status.IsFailure)
            return OperationResult<double>.Failure(status.ErrorMessage);
        
        if (config.BytesToRead == 0)
            return OperationResult<double>.Success(0);

        try
        {
            var converted = (double)Convert.ChangeType(
                processed,
                typeof(double),
                CultureInfo.InvariantCulture);

            return OperationResult<double>.Success(converted);
        }
        catch (Exception ex)
        {
            return OperationResult<double>.Failure(
                $"Failed to convert result '{processed}': {ex.Message}");
        }
    }
    
    private static byte[] ApplyTermination(byte[] data, SerialTerminationMode mode)
    {
        return mode switch
        {
            SerialTerminationMode.NONE => data,
            SerialTerminationMode.LF   => data.Concat(new byte[] { 0x0A }).ToArray(),
            SerialTerminationMode.CR   => data.Concat(new byte[] { 0x0D }).ToArray(),
            SerialTerminationMode.CRLF => data.Concat(new byte[] { 0x0D, 0x0A }).ToArray(),
            _ => data
        };
    }
}