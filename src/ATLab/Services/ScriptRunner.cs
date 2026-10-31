using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Helpers;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

/// <summary>
/// Runs a script on a device: applies script and runtime variables to its commands, checks the responses of
/// commands with a response check and returns the answer of the command marked for evaluation.
/// </summary>
public class ScriptRunner : IScriptRunner
{
    private readonly IScriptRepository _scriptRepository;
    private readonly ICommandExecutor _commandExecutor;

    public ScriptRunner(
        IScriptRepository scriptRepository,
        ICommandExecutor commandExecutor)
    {
        _scriptRepository = scriptRepository;
        _commandExecutor = commandExecutor;
    }

    public async Task<OperationResult> ExecuteAsync(
        string scriptId,
        string deviceId,
        IEnumerable<CustomVariable> scriptVariables,
        CancellationToken token,
        List<CustomVariable>? runtimeVariables = null)
    {
        var result = await RunCoreAsync(scriptId, deviceId, scriptVariables, token, runtimeVariables);
        return result.IsSuccess
            ? OperationResult.Success()
            : OperationResult.Failure(result.ErrorMessage);
    }

    public async Task<OperationResult<T>> ExecuteAsync<T>(
        string scriptId,
        string deviceId,
        IEnumerable<CustomVariable> scriptVariables,
        CancellationToken token,
        List<CustomVariable>? runtimeVariables = null,
        ResponseMask? mask = null,
        List<ScriptCommandResult>? commandResults = null)
    {
        var result = await RunCoreAsync(scriptId, deviceId, scriptVariables, token, runtimeVariables, commandResults);
        
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

    private async Task<OperationResult<byte[]>> RunCoreAsync(
        string scriptId,
        string deviceName,
        IEnumerable<CustomVariable> scriptVariables,
        CancellationToken token,
        List<CustomVariable>? runtimeVariables = null,
        List<ScriptCommandResult>? commandResults = null)
    {
        if (string.IsNullOrEmpty(scriptId) || string.IsNullOrEmpty(deviceName))
            return OperationResult<byte[]>.Failure("Script and Device are required.");

        try
        {
            var script = await _scriptRepository.LoadAsync(scriptId, token);
            if (script == null)
            {
                return OperationResult<byte[]>.Failure($"Script with ID {scriptId} not found.");
            }

            return await ExecuteScriptAsync(script, deviceName, scriptVariables, token, runtimeVariables, commandResults);
        }
        catch (OperationCanceledException)
        {
            return OperationResult<byte[]>.Failure(string.Empty);
        }
        catch (Exception ex)
        {
            return OperationResult<byte[]>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult<byte[]>> ExecuteScriptAsync(
        CustomScript script,
        string deviceId,
        IEnumerable<CustomVariable> scriptVariables,
        CancellationToken token,
        List<CustomVariable>? runtimeVariables = null,
        List<ScriptCommandResult>? commandResults = null)
    {
        if (string.IsNullOrEmpty(deviceId))
            return OperationResult<byte[]>.Failure("Device is required.");

        try
        {
            // Clone and apply variables to commands
            var preparedCommands = script.Commands
                .Select(c => ApplyVariables(c, scriptVariables))
                .ToList();

            return await _commandExecutor.ExecuteAsync(preparedCommands, deviceId, token, runtimeVariables, commandResults);
        }
        catch (OperationCanceledException)
        {
            return OperationResult<byte[]>.Failure(string.Empty);
        }
        catch (Exception ex)
        {
            return OperationResult<byte[]>.Failure(ex.Message);
        }
    }

    private static ScriptCommand ApplyVariables(
        ScriptCommand original,
        IEnumerable<CustomVariable> variables)
    {
        var clone = new ScriptCommand(original);

        var commandText = clone.Command;
        if (!string.IsNullOrWhiteSpace(commandText))
        {
            foreach (var v in variables)
            {
                if (!string.IsNullOrEmpty(v.Name))
                {
                    commandText = commandText.Replace($"{{{v.Name}}}", v.Value);
                }
            }

            clone.Command = commandText;
        }

        return clone;
    }
}