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
using ATLab.ViewModels;

namespace ATLab.Services;

public class CommandExecutor : ICommandExecutor, IDisposable, IAsyncDisposable
{
    private readonly ICommunicationFactory _communicationFactory;
    private readonly DeviceManagerViewModel _deviceManager;

    private ICommunication? _deviceInterface;
    private DeviceConfiguration? _lastConfig;

    public CommandExecutor(
        ICommunicationFactory communicationFactory,
        DeviceManagerViewModel deviceManager)
    {
        _communicationFactory = communicationFactory;
        _deviceManager = deviceManager;
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
        List<CustomVariable>? runtimeVariables = null)
    {
        if (string.IsNullOrWhiteSpace(targetDeviceId))
            return OperationResult<byte[]>.Failure("Device is required.");

        byte[] queryResult = [];

        try
        {
            var device = _deviceManager.Devices.FirstOrDefault(d => d.Id == targetDeviceId);
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
                    (_deviceInterface as IDisposable)?.Dispose();
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

            foreach (var command in commands)
            {
                if (token.IsCancellationRequested)
                    break;

                var commandText = command.Command;
                if (string.IsNullOrWhiteSpace(commandText))
                    continue;
                
                var compiledCommand = CommandProcessor.CompileToBytes(commandText, runtimeVariables);

                if (command.DelayMs > 0)
                    await Task.Delay(command.DelayMs, token);

                switch (command.IsExpectResponse)
                {
                    case true when command.IsEvaluate:
                        queryResult = await client.QueryAsync(compiledCommand, command.TimeoutMs);
                        break;
                    case true:
                        await client.QueryAsync(compiledCommand, command.TimeoutMs);
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
                ? OperationResult<byte[]>.Failure("Operation cancelled by user")
                : OperationResult<byte[]>.Timeout("Operation timed out");
        }
        catch (OperationCanceledException)
        {
            return token.IsCancellationRequested
                ? OperationResult<byte[]>.Failure("Operation cancelled by user")
                : OperationResult<byte[]>.Timeout("Operation timed out");
        }
        catch (Exception ex)
        {
            return OperationResult<byte[]>.Failure(ex.Message);
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

        if (result.Value == null || result.Value.Length == 0)
            return OperationResult<T>.Success(default!);

        var processedValue = ResponseProcessor.Process(result.Value, mask);

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
    
    public async Task ReleaseDeviceAsync()
    {
        if (_deviceInterface != null)
        {
            await _deviceInterface.DisconnectAsync();
            (_deviceInterface as IDisposable)?.Dispose();
            _deviceInterface = null;
        }

        _lastConfig = null;
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_deviceInterface != null)
        {
            await _deviceInterface.DisconnectAsync();
            (_deviceInterface as IDisposable)?.Dispose();
            _deviceInterface = null;
        }
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}