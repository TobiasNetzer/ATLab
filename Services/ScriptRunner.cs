using System;
using System.Collections.Generic;
using System.Globalization;
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
    private readonly IScpiScriptRepository _scriptRepository;
    private readonly SerialDeviceManagerViewModel _deviceManager;
    private readonly IErrorService _errorService;

    public ScriptRunner(
        ISerialPortManager portManager,
        IScpiScriptRepository scriptRepository,
        SerialDeviceManagerViewModel deviceManager,
        IErrorService errorService)
    {
        _portManager = portManager;
        _scriptRepository = scriptRepository;
        _deviceManager = deviceManager;
        _errorService = errorService;
    }

    public async Task ExecuteAsync(string scriptId, string deviceName, IEnumerable<ScpiVariable> variables, CancellationToken token)
    {
        await RunCoreAsync(scriptId, deviceName, variables, token);
    }

    public async Task<T?> ExecuteAsync<T>(string scriptId, string deviceName, IEnumerable<ScpiVariable> variables, CancellationToken token)
    {
        var result = await RunCoreAsync(scriptId, deviceName, variables, token);
        if (result == null) return default;

        return (T?)Convert.ChangeType(result, typeof(T), CultureInfo.CurrentCulture);
    }

    private async Task<string?> RunCoreAsync(string scriptId, string deviceName, IEnumerable<ScpiVariable> variables, CancellationToken token)
    {
        if (string.IsNullOrEmpty(scriptId) || string.IsNullOrEmpty(deviceName)) return null;

        string? lastResponse = null;
        var script = await _scriptRepository.LoadAsync(scriptId, token);
        if (script == null)
        {
            throw new InvalidOperationException($"Script with ID {scriptId} not found.");
        }

        var device = _deviceManager.SerialDevices.FirstOrDefault(d => d.Name == deviceName);
        if (device == null)
        {
            throw new InvalidOperationException($"Device {deviceName} not found.");
        }

        var portName = device.SerialPort;
        _portManager.Open(portName);
        var transport = _portManager.GetPort(portName);
        var client = new ScpiClient(transport);

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
                    commandText = commandText.Replace($"{{{v.Name}}}", v.DefaultValue);
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

        return lastResponse;
    }
}