using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using ATLab.Helpers;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

public class InterfaceCommandExecuter : IInterfaceCommandExecuter
{
    private readonly ITestHardware _testHardware;

    public InterfaceCommandExecuter(ITestHardware testHardware)
    {
        _testHardware = testHardware;
    }

    public async Task<OperationResult<double>> ExecuteAsync(
        TestInterfaceConfig config,
        List<CustomVariable> runtimeVariables,
        ResponseMask? mask)
    {
        
        var configResp = await _testHardware.ConfigureI2CInterface(config.I2CSpeedMode);

        if (configResp.IsFailure)
            return OperationResult<double>.Failure(configResp.ErrorMessage);
        
        if (!string.IsNullOrEmpty(config.Command))
        {
            var compiled = CommandProcessor.CompileToBytes(config.Command, runtimeVariables);

            var status = await _testHardware.ExecuteI2CTransmit(
                Convert.ToByte(config.I2CAddress),
                compiled);

            if (status.IsTimeout)
                return OperationResult<double>.Timeout(status.ErrorMessage);

            if (status.IsFailure)
                return OperationResult<double>.Failure(status.ErrorMessage);
        }
        
        if (config.BytesToRead > 0)
        {
            var status = await _testHardware.ExecuteI2CReceive(
                Convert.ToByte(config.I2CAddress),
                Convert.ToByte(config.BytesToRead));

            if (status.IsTimeout)
                return OperationResult<double>.Timeout(status.ErrorMessage);

            if (status.IsFailure)
                return OperationResult<double>.Failure(status.ErrorMessage);

            if (status.Value == null || status.Value.Length == 0)
                return OperationResult<double>.Success(0);

            var processed = ResponseProcessor.Process(status.Value, mask);

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
        
        return OperationResult<double>.Success(0);
    }
}
