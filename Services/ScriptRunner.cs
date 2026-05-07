using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

public class ScriptRunner : IScriptRunner
{
    private readonly IScriptRepository _scriptRepository;
    private readonly ICommandExecutor _commandExecutor;
    private readonly IResponseProcessor _responseProcessor;

    public ScriptRunner(
        IScriptRepository scriptRepository,
        ICommandExecutor commandExecutor,
        IResponseProcessor responseProcessor)
    {
        _scriptRepository = scriptRepository;
        _commandExecutor = commandExecutor;
        _responseProcessor = responseProcessor;
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
        ResponseMask? mask = null)
    {
        var result = await RunCoreAsync(scriptId, deviceId, scriptVariables, token, runtimeVariables);
        
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

    private async Task<OperationResult<string?>> RunCoreAsync(
        string scriptId,
        string deviceName,
        IEnumerable<CustomVariable> scriptVariables,
        CancellationToken token,
        List<CustomVariable>? runtimeVariables = null)
    {
        if (string.IsNullOrEmpty(scriptId) || string.IsNullOrEmpty(deviceName))
            return OperationResult<string?>.Failure("Script and Device are required.");

        try
        {
            var script = await _scriptRepository.LoadAsync(scriptId, token);
            if (script == null)
            {
                return OperationResult<string?>.Failure($"Script with ID {scriptId} not found.");
            }

            // Clone and apply variables to commands
            var preparedCommands = script.Commands
                .Select(c => ApplyVariables(c, scriptVariables))
                .ToList();

            return await _commandExecutor.ExecuteAsync(preparedCommands, deviceName, token, runtimeVariables);
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