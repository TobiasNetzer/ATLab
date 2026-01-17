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

public class ScriptRunner : IScriptRunner
{
    private readonly ICommunicationFactory _communicationInterface;
    private readonly IScriptRepository _scriptRepository;
    private readonly DeviceManagerViewModel _deviceManager;

    private ICommunication? _testDeviceInterface;

    public ScriptRunner(
        ICommunicationFactory communicationInterface,
        IScriptRepository scriptRepository,
        DeviceManagerViewModel deviceManager)
    {
        _communicationInterface = communicationInterface;
        _scriptRepository = scriptRepository;
        _deviceManager = deviceManager;
    }

    public async Task<OperationResult> ExecuteAsync(string scriptId, string deviceName, IEnumerable<ScriptVariable> variables, CancellationToken token)
    {
        var result = await RunCoreAsync(scriptId, deviceName, variables, token);
        return result.IsSuccess ? OperationResult.Success() : OperationResult.Failure(result.ErrorMessage);
    }

    public async Task<OperationResult<T>> ExecuteAsync<T>(string scriptId, string deviceName, IEnumerable<ScriptVariable> variables, CancellationToken token)
    {
        var result = await RunCoreAsync(scriptId, deviceName, variables, token);
        if (!result.IsSuccess) return OperationResult<T>.Failure(result.ErrorMessage);
        if (result.Value == null) return OperationResult<T>.Success(default!);

        try
        {
            var converted = (T?)Convert.ChangeType(result.Value, typeof(T), CultureInfo.InvariantCulture);
            return OperationResult<T>.Success(converted!);
        }
        catch (Exception ex)
        {
            return OperationResult<T>.Failure($"Failed to convert result '{result.Value}' to type {typeof(T).Name}: {ex.Message}");
        }
    }

    private async Task<OperationResult<string?>> RunCoreAsync(string scriptId, string deviceName, IEnumerable<ScriptVariable> variables, CancellationToken token)
    {
        if (string.IsNullOrEmpty(scriptId) || string.IsNullOrEmpty(deviceName)) 
            return OperationResult<string?>.Failure("Script and Device are required.");

        string? lastResponse = null;
        try
        {
            var script = await _scriptRepository.LoadAsync(scriptId, token);
            if (script == null)
            {
                return OperationResult<string?>.Failure($"Script with ID {scriptId} not found.");
            }

            var device = _deviceManager.Devices.FirstOrDefault(d => d.Name == deviceName);
            if (device == null)
            {
                return OperationResult<string?>.Failure($"Device {deviceName} not found.");
            }

            var resource = device.ResourceString;
            
            if (_testDeviceInterface == null ||
                !_testDeviceInterface.IsConnected ||
                _testDeviceInterface.Resource != resource)
            {
                if (_testDeviceInterface != null)
                {
                    await _testDeviceInterface.DisconnectAsync();
                    (_testDeviceInterface as IDisposable)?.Dispose();
                }
                
                _testDeviceInterface = device.Type switch
                {
                    DeviceType.SERIAL => _communicationInterface.CreateSerial(device.ResourceString, device.Configuration),
                    DeviceType.VISA   => _communicationInterface.CreateVisa(device.ResourceString, device.Configuration),
                    _ => throw new NotSupportedException()
                };
                
                var result = await _testDeviceInterface.ConnectAsync();
                if (!result.IsSuccess)
                    return OperationResult<string?>.Failure(result.ErrorMessage);
            }
            
            var client = new ScriptClient(_testDeviceInterface);

            foreach (var command in script.Commands)
            {
                if (token.IsCancellationRequested) break;

                var commandText = command.Command;
                if (string.IsNullOrWhiteSpace(commandText)) continue;

                // Replace variables
                foreach (var v in variables)
                {
                    if (!string.IsNullOrEmpty(v.Name))
                    {
                        commandText = commandText.Replace($"{{{v.Name}}}", v.Value);
                    }
                }
                
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
}