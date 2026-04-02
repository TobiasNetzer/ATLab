using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Services;

public class CommandExecutor : ICommandExecutor, IDisposable
{
    private readonly ICommunicationFactory _communicationFactory;
    private readonly DeviceManagerViewModel _deviceManager;
    private readonly IResponseProcessor _responseProcessor;

    private ICommunication? _deviceInterface;
    private DeviceConfiguration? _lastConfig;

    public CommandExecutor(
        ICommunicationFactory communicationFactory,
        DeviceManagerViewModel deviceManager,
        IResponseProcessor responseProcessor)
    {
        _communicationFactory = communicationFactory;
        _deviceManager = deviceManager;
        _responseProcessor = responseProcessor;
    }

    public Task<OperationResult<string?>> ExecuteAsync(
        ScriptCommand command,
        string deviceName,
        CancellationToken token)
    {
        return ExecuteAsync(new[] { command }, deviceName, token);
    }

    public async Task<OperationResult<string?>> ExecuteAsync(
        IEnumerable<ScriptCommand> commands,
        string deviceName,
        CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(deviceName))
            return OperationResult<string?>.Failure("Device is required.");

        string? queryResult = null;

        try
        {
            var device = _deviceManager.Devices.FirstOrDefault(d => d.Name == deviceName);
            if (device == null)
            {
                return OperationResult<string?>.Failure($"Device {deviceName} not found.");
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
                    _ => throw new NotSupportedException($"Device type {device.Type} is not supported.")
                };
                
                _lastConfig = device.Configuration.Clone();

                var connectResult = await _deviceInterface.ConnectAsync();
                if (!connectResult.IsSuccess)
                    return OperationResult<string?>.Failure(connectResult.ErrorMessage);
            }

            var client = new ScriptClient(_deviceInterface);

            foreach (var command in commands)
            {
                if (token.IsCancellationRequested)
                    break;

                var commandText = command.Command;
                if (string.IsNullOrWhiteSpace(commandText))
                    continue;

                if (command.DelayMs > 0)
                    await Task.Delay(command.DelayMs, token);

                switch (command.ExpectResponse)
                {
                    case true when command.Evaluate:
                        queryResult = await client.QueryAsync(commandText, command.TimeoutMs);
                        break;
                    case true:
                        await client.QueryAsync(commandText, command.TimeoutMs);
                        break;
                    default:
                        await client.WriteAsync(commandText);
                        break;
                }
            }

            return OperationResult<string?>.Success(queryResult);
        }
        catch (TimeoutException tex)
        {
            return OperationResult<string?>.Timeout(tex.Message);
        }
        catch (TaskCanceledException)
        {
            return token.IsCancellationRequested
                ? OperationResult<string?>.Failure("Operation cancelled by user")
                : OperationResult<string?>.Timeout("Operation timed out");
        }
        catch (OperationCanceledException)
        {
            return token.IsCancellationRequested
                ? OperationResult<string?>.Failure("Operation cancelled by user")
                : OperationResult<string?>.Timeout("Operation timed out");
        }
        catch (Exception ex)
        {
            return OperationResult<string?>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult<T>> ExecuteAsync<T>(
        ScriptCommand command,
        string deviceName,
        CancellationToken token,
        ResponseMask? mask = null)
    {
        var result = await ExecuteAsync(command, deviceName, token);

        if (result.IsTimeout)
            return OperationResult<T>.Timeout(result.ErrorMessage);

        if (result.IsFailure)
            return OperationResult<T>.Failure(result.ErrorMessage);

        if (result.Value == null)
            return OperationResult<T>.Success(default!);

        var processedValue = _responseProcessor.ApplyMask(result.Value, mask);

        try
        {
            var converted = (T?)Convert.ChangeType(processedValue, typeof(T), CultureInfo.InvariantCulture);
            return OperationResult<T>.Success(converted!);
        }
        catch (Exception ex)
        {
            return OperationResult<T>.Failure(
                $"Failed to convert result '{processedValue}' (original: '{result.Value}') to type {typeof(T).Name}: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_deviceInterface == null)
            return;

        _deviceInterface.DisconnectAsync().GetAwaiter().GetResult();
        (_deviceInterface as IDisposable)?.Dispose();
        _deviceInterface = null;
    }
}
