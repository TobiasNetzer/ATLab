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

    private ICommunication? _deviceInterface;

    public CommandExecutor(
        ICommunicationFactory communicationFactory,
        DeviceManagerViewModel deviceManager)
    {
        _communicationFactory = communicationFactory;
        _deviceManager = deviceManager;
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

        string? lastResponse = null;

        try
        {
            var device = _deviceManager.Devices.FirstOrDefault(d => d.Name == deviceName);
            if (device == null)
            {
                return OperationResult<string?>.Failure($"Device {deviceName} not found.");
            }

            var resource = device.ResourceString;

            // Ensure correct and connected interface
            if (_deviceInterface == null ||
                !_deviceInterface.IsConnected ||
                _deviceInterface.Resource != resource)
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

                if (command.ExpectResponse)
                {
                    lastResponse = await client.QueryAsync(commandText, command.TimeoutMs);
                }
                else
                {
                    await client.WriteAsync(commandText);
                }
            }

            return OperationResult<string?>.Success(lastResponse);
        }
        catch (OperationCanceledException)
        {
            return OperationResult<string?>.Failure(string.Empty);
        }
        catch (Exception ex)
        {
            return OperationResult<string?>.Failure(ex.Message);
        }
    }
    
    public async Task<OperationResult<T>> ExecuteAsync<T>(
        ScriptCommand command,
        string deviceName,
        CancellationToken token)
    {
        var result = await ExecuteAsync(command, deviceName, token);

        if (!result.IsSuccess)
            return OperationResult<T>.Failure(result.ErrorMessage);

        if (result.Value == null)
            return OperationResult<T>.Success(default!);

        try
        {
            var converted = (T?)Convert.ChangeType(result.Value, typeof(T), CultureInfo.InvariantCulture);
            return OperationResult<T>.Success(converted!);
        }
        catch (Exception ex)
        {
            return OperationResult<T>.Failure(
                $"Failed to convert result '{result.Value}' to type {typeof(T).Name}: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_deviceInterface != null)
        {
            _deviceInterface.DisconnectAsync().GetAwaiter().GetResult();
            (_deviceInterface as IDisposable)?.Dispose();
            _deviceInterface = null;
        }
    }
}