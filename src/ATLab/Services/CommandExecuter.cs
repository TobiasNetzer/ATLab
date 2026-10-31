using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Helpers;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

/// <summary>
/// Sends commands to devices from the Device Manager. The connection to the last device is kept open between steps
/// and only recreated when the device or its settings change.
/// </summary>
public class CommandExecutor : ICommandExecutor
{
    private readonly ICommunicationFactory _communicationFactory;
    private readonly ProjectModel _projectModel;

    private ICommunication? _deviceInterface;
    private DeviceConfiguration? _lastConfig;

    // Test runs and the script runner of the Scripting tab share the connection, one command sequence at a time
    private readonly SemaphoreSlim _gate = new(1, 1);

    private bool _disposed;

    public CommandExecutor(
        ICommunicationFactory communicationFactory,
        ProjectModel projectModel)
    {
        _communicationFactory = communicationFactory;
        _projectModel = projectModel;
    }

    public Task<OperationResult<byte[]>> ExecuteAsync(
        ScriptCommand command,
        string targetDeviceId,
        CancellationToken token,
        List<CustomVariable>? runtimeVariables = null)
    {
        return ExecuteAsync(new[] { command }, targetDeviceId, token, runtimeVariables);
    }

    public async Task<OperationResult<byte[]>> ExecuteAsync(
        IEnumerable<ScriptCommand> commands,
        string targetDeviceId,
        CancellationToken token,
        List<CustomVariable>? runtimeVariables = null,
        List<ScriptCommandResult>? commandResults = null)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(CommandExecutor));

        if (string.IsNullOrWhiteSpace(targetDeviceId))
            return OperationResult<byte[]>.Failure("Device is required.");

        byte[] queryResult = [];

        try
        {
            await _gate.WaitAsync(token);
        }
        catch (OperationCanceledException)
        {
            return OperationResult<byte[]>.Failure("Operation canceled by user");
        }

        try
        {
            var device = _projectModel.Devices.FirstOrDefault(d => d.Id == targetDeviceId);
            if (device == null)
            {
                return OperationResult<byte[]>.Failure($"Target Device ID {targetDeviceId} not found.");
            }

            var resource = device.ResourceString;
            
            var configChanged = _lastConfig == null || !_lastConfig.Equals(device.Configuration);

            if (_deviceInterface == null ||
                !_deviceInterface.IsConnected ||
                _deviceInterface.Resource != resource ||
                configChanged)
            {
                if (_deviceInterface != null)
                {
                    await _deviceInterface.DisconnectAsync();
                    await _deviceInterface.DisposeAsync();
                }

                _deviceInterface = device.Type switch
                {
                    DeviceType.SERIAL => _communicationFactory.CreateSerial(device.ResourceString, device.Configuration),
                    DeviceType.VISA   => _communicationFactory.CreateVisa(device.ResourceString, device.Configuration),
                    DeviceType.TCP_IP    => _communicationFactory.CreateTcp(device.ResourceString, device.Configuration),
                    _ => throw new NotSupportedException($"Device type {device.Type} is not supported.")
                };
                
                _lastConfig = device.Configuration.Clone();

                var connectResult = await _deviceInterface.ConnectAsync();
                if (!connectResult.IsSuccess)
                    return OperationResult<byte[]>.Failure(connectResult.ErrorMessage);
            }

            var client = new TransportService(_deviceInterface);
            var commandNumber = 0;

            foreach (var command in commands)
            {
                commandNumber++;

                if (token.IsCancellationRequested)
                    break;

                var commandText = command.Command;
                if (string.IsNullOrWhiteSpace(commandText))
                    continue;
                
                var compiledCommand = CommandProcessor.CompileToBytes(commandText, runtimeVariables);

                var commandResult = new ScriptCommandResult
                {
                    Number = commandNumber,
                    Command = commandText,
                    IsExpectResponse = command.IsExpectResponse,
                    IsEvaluate = command.IsExpectResponse && command.IsEvaluate,
                    IsResponseChecked = command.IsResponseChecked
                };
                commandResults?.Add(commandResult);

                if (command.DelayMs > 0)
                    await Task.Delay(command.DelayMs, token);

                switch (command.IsExpectResponse)
                {
                    case true when command.IsEvaluate:
                        queryResult = await client.QueryAsync(compiledCommand, command.TimeoutMs);
                        commandResult.Response = queryResult;
                        break;
                    case true:
                        commandResult.Response = await client.QueryAsync(compiledCommand, command.TimeoutMs);

                        if (command.IsResponseChecked)
                            CheckResponse(command, commandResult);
                        break;
                    default:
                        await client.WriteAsync(compiledCommand);
                        break;
                }
            }

            return OperationResult<byte[]>.Success(queryResult);
        }
        catch (TimeoutException tex)
        {
            return OperationResult<byte[]>.Timeout(tex.Message);
        }
        catch (TaskCanceledException)
        {
            return token.IsCancellationRequested
                ? OperationResult<byte[]>.Failure("Operation canceled by user")
                : OperationResult<byte[]>.Timeout("Operation timed out");
        }
        catch (OperationCanceledException)
        {
            return token.IsCancellationRequested
                ? OperationResult<byte[]>.Failure("Operation canceled by user")
                : OperationResult<byte[]>.Timeout("Operation timed out");
        }
        catch (Exception ex)
        {
            return OperationResult<byte[]>.Failure(ex.Message);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationResult<T>> ExecuteAsync<T>(
        ScriptCommand command,
        string targetDeviceId,
        CancellationToken token,
        List<CustomVariable>? runtimeVariables = null,
        ResponseMask? mask = null)
    {
        var result = await ExecuteAsync(command, targetDeviceId, token, runtimeVariables);

        if (result.IsTimeout)
            return OperationResult<T>.Timeout(result.ErrorMessage);

        if (result.IsFailure)
            return OperationResult<T>.Failure(result.ErrorMessage);

        if (mask == null && (result.Value == null || result.Value.Length == 0))
            return OperationResult<T>.Success(default!);

        string processedValue;
        try
        {
            processedValue = ResponseProcessor.Process(result.Value ?? Array.Empty<byte>(), mask);
        }
        catch (ResponseProcessingException ex)
        {
            return OperationResult<T>.Failure(ex.Message);
        }

        // numbers via NumberHelper so that ',' is a decimal separator (Convert treats it as a thousands separator: 4,5 -> 45)
        if (typeof(T) == typeof(double))
        {
            return NumberHelper.TryParseDecimal(processedValue, out var number)
                ? OperationResult<T>.Success((T)(object)number)
                : OperationResult<T>.Failure($"Failed to convert result '{processedValue}' to a number.");
        }

        try
        {
            var converted = (T)Convert.ChangeType(processedValue, typeof(T), CultureInfo.InvariantCulture);
            return OperationResult<T>.Success(converted);
        }
        catch (Exception ex)
        {
            return OperationResult<T>.Failure(
                $"Failed to convert result '{processedValue}': {ex.Message}");
        }
    }
    
    /// <summary>
    /// Processes the response with the command's check mask and compares it with the check limits like a step
    /// value. Stores the processed value and the outcome in the command result.
    /// </summary>
    private static void CheckResponse(ScriptCommand command, ScriptCommandResult result)
    {
        var mask = new ResponseMask
        {
            Mask = command.CheckMask ?? string.Empty,
            Skip = command.CheckSkip,
            Length = command.CheckLength
        };

        result.IsCheckPassed = false;

        string processedValue;
        try
        {
            processedValue = ResponseProcessor.Process(result.Response ?? [], mask);
        }
        catch (ResponseProcessingException ex)
        {
            result.CheckMessage = ex.Message;
            return;
        }

        result.CheckValue = processedValue;
        var received = mask.OriginalResponse.Trim();

        if (!NumberHelper.TryParseDecimal(processedValue, out var value))
        {
            result.CheckMessage = $"received '{received}', result '{processedValue}' is not a number.";
            return;
        }

        var roundedValue = NumberHelper.RoundToSignificantDigits(value);
        if (roundedValue >= NumberHelper.RoundToSignificantDigits(command.CheckLowerLimit) &&
            roundedValue <= NumberHelper.RoundToSignificantDigits(command.CheckUpperLimit))
        {
            result.IsCheckPassed = true;
            return;
        }

        result.CheckMessage = string.Format(CultureInfo.CurrentCulture,
            "received '{0}', result {1} not within {2} … {3}.",
            received, value, command.CheckLowerLimit, command.CheckUpperLimit);
    }

    public async Task ReleaseDeviceAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_deviceInterface != null)
            {
                await _deviceInterface.DisconnectAsync();
                await _deviceInterface.DisposeAsync();
                _deviceInterface = null;
            }

            _lastConfig = null;
        }
        finally
        {
            _gate.Release();
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        
        await ReleaseDeviceAsync();
        
        _disposed = true;
    }
}