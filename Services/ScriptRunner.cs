using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Services;

public class ScriptRunner : IScriptRunner
{
    private readonly ISerialPortManager _portManager;
    private readonly IScriptRepository _scriptRepository;
    private readonly SerialDeviceManagerViewModel _deviceManager;

    public ScriptRunner(
        ISerialPortManager portManager,
        IScriptRepository scriptRepository,
        SerialDeviceManagerViewModel deviceManager)
    {
        _portManager = portManager;
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
            var converted = (T?)Convert.ChangeType(result.Value, typeof(T));
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

            var device = _deviceManager.SerialDevices.FirstOrDefault(d => d.Name == deviceName);
            if (device == null)
            {
                return OperationResult<string?>.Failure($"Device {deviceName} not found.");
            }

            var portName = device.SerialPort;
            _portManager.Open(portName);
            var transport = _portManager.GetPort(portName);
            var client = new ScriptClient(transport);

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

                if (command.ExpectResponse)
                {
                    lastResponse = await client.QueryAsync(commandText, command.TimeoutMs);
                }
                else
                {
                    await client.WriteAsync(commandText);
                }

                await Task.Delay(command.DelayMs, token);
            }

            return OperationResult<string?>.Success(lastResponse);
        }
        catch (OperationCanceledException)
        {
            return OperationResult<string?>.Failure("Cancelled");
        }
        catch (Exception ex)
        {
            return OperationResult<string?>.Failure(ex.Message);
        }
    }
}