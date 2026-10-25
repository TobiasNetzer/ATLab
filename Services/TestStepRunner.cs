using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Helpers;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Services;

/// <summary>
/// Executes one step: switches the relays, waits for the delay and acquires the value from the step's evaluation
/// source.
/// </summary>
public class TestStepRunner : ITestStepRunner
{
    
    private readonly ITestHardware _testHardware;
    private readonly IScriptRunner _scriptRunner;
    private readonly ICommandExecutor _commandExecutor;
    private readonly IExternalProgramRunner _externalProgramRunner;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IFileContentReader _fileContentReader;
    private readonly IInterfaceCommandExecuter _interfaceCommandExecuter;
    private readonly ProjectModel _projectModel;
    private readonly IStatusService _statusService;
    private readonly IBoundaryScanTestSession _boundaryScanSession;

    public TestStepRunner(
        IHardwareAccessor hardwareAccessor,
        IScriptRunner scriptRunner,
        ICommandExecutor commandExecutor,
        IExternalProgramRunner externalProgramRunner,
        IMessageBoxService messageBoxService,
        IFileContentReader fileContentReader,
        IInterfaceCommandExecuter interfaceCommandExecuter,
        ProjectModel projectModel,
        IStatusService statusService,
        IBoundaryScanTestSession boundaryScanSession)
    {
        _testHardware = hardwareAccessor.Hardware;
        _scriptRunner = scriptRunner;
        _commandExecutor = commandExecutor;
        _externalProgramRunner = externalProgramRunner;
        _messageBoxService = messageBoxService;
        _fileContentReader = fileContentReader;
        _interfaceCommandExecuter = interfaceCommandExecuter;
        _projectModel = projectModel;
        _statusService = statusService;
        _boundaryScanSession = boundaryScanSession;
    }
    
    /// <summary>
    /// Returns the measured value. NegativeInfinity means the step has nothing to evaluate and passes.
    /// </summary>
    public async Task<OperationResult<double>> ExecuteAsync(TestStepViewModel step, List<CustomVariable> runtimeVariables, CancellationToken token)
    {
        step.ResultDetailLines = [];

        try
        {
            // detect if the matrix channel is unset or not available
            if (step.TestStep.MatrixState.ActiveChannelHigh == -1 || step.TestStep.MatrixState.ActiveChannelLow == -1)
                return OperationResult<double>.Failure("Matrix Channel is not available.");
            
            _testHardware.StimChannelStates = step.TestStep.LiveStimState.ToBoolArray();
            _testHardware.ExtStimChannelStates = step.TestStep.LiveExtStimState.ToBoolArray();
            _testHardware.ActiveMeasChannelH = Convert.ToByte(step.TestStep.MatrixState.ActiveChannelHigh);
            _testHardware.ActiveMeasChannelL = Convert.ToByte(step.TestStep.MatrixState.ActiveChannelLow);
            _testHardware.UseExternalProbe = Convert.ToByte(step.TestStep.MatrixState.IsExternalProbe);

            var result = await _testHardware.UpdateRelayStates();

            if (!result.IsSuccess)
            {
                return OperationResult<double>.Failure(result.ErrorMessage);
            }

            const int updateIntervalMs = 100;

            if (step.TestStep.Delay <= 1000)
            {
                await Task.Delay(step.TestStep.Delay, token);
            }
            else
            {
                var totalMs = step.TestStep.Delay;
                var elapsedMs = 0;

                _statusService.Progress = 0;
                _statusService.Text = LocalizationService.Instance["StatusLabel_ExecuteDelay"];
                _statusService.IsVisible = true;

                while (elapsedMs < totalMs)
                {
                    var chunk = Math.Min(updateIntervalMs, totalMs - elapsedMs);

                    await Task.Delay(chunk, token);

                    elapsedMs += chunk;

                    _statusService.Progress = Math.Min(
                        (double)(elapsedMs + updateIntervalMs) / totalMs,
                        1.0);
                }
                
                _statusService.IsVisible = false;
                _statusService.Progress = 0;
            }
            
            var mask = step.TestStep.IsCustomResponseMask ? step.TestStep.ResponseMask : null;

            switch (step.TestStep.EvaluationSource)
            {
                case TestEvaluationSource.NONE: return OperationResult<double>.Success(double.NegativeInfinity);
                
                case TestEvaluationSource.SCRIPT:
                    return await _scriptRunner.ExecuteAsync<double>(step.TestStep.ScriptId, step.TestStep.TargetDeviceId,
                        step.TestStep.ScriptVariables, token, runtimeVariables, mask);
                
                case TestEvaluationSource.COMMAND:
                    return await _commandExecutor.ExecuteAsync<double>(step.TestStep.Command, step.TestStep.TargetDeviceId, token, runtimeVariables, mask);

                case TestEvaluationSource.INTERFACE:
                {
                    var details = new List<string>();
                    var interfaceResult = await _interfaceCommandExecuter.ExecuteAsync(step.TestStep.InterfaceConfig, runtimeVariables, mask, details);
                    step.ResultDetailLines = details;
                    return interfaceResult;
                }

                case TestEvaluationSource.EXTERNAL_PROGRAM:
                {
                    _statusService.Progress = 0;
                    _statusService.Text = LocalizationService.Instance["StatusLabel_WaitingForExternalApp"];
                    _statusService.IsIndeterminate = true;
                    _statusService.IsVisible = true;
                    var externalProgramResult = await _externalProgramRunner.RunAsync(step.TestStep.ExternalProgram,Path.GetDirectoryName(_projectModel.FilePath), token, runtimeVariables, mask);
                    _statusService.IsVisible = false;
                    _statusService.IsIndeterminate = false;
                    return externalProgramResult;
                }
                
                case TestEvaluationSource.USER_CONFIRMATION:
                {
                    var resolvedComment = CommandProcessor.CompileToString(step.TestStep.Comment, runtimeVariables);
                    
                    var operatorResponse = await _messageBoxService.ShowConfirmationImageAsync(
                        LocalizationService.Instance["Title_AwaitingUserConfirmation"],
                        resolvedComment,
                        step.TestStep.CustomMessageBoxImagePath,
                        DialogFunction.USER_CONFIRMATION);

                    return OperationResult<double>.Success(Convert.ToDouble(operatorResponse));
                }

                case TestEvaluationSource.USER_INPUT:
                {
                    var resolvedComment = CommandProcessor.CompileToString(step.TestStep.Comment, runtimeVariables);

                    var userInput = await _messageBoxService.ShowInputImageAsync(
                        LocalizationService.Instance["Title_AwaitingUserInput"],
                        resolvedComment,
                        step.TestStep.CustomMessageBoxImagePath);

                    if (userInput == null)
                        throw new OperationCanceledException();

                    var processed = ResponseProcessor.Process(Encoding.UTF8.GetBytes(userInput), mask).Trim();
                    
                    return NumberHelper.TryParseDecimal(processed, out var doubleResult)
                        ? OperationResult<double>.Success(doubleResult)
                        : OperationResult<double>.Failure($"Failed to parse user input to double: {processed}");
                }
                
                case TestEvaluationSource.EXPRESSION: 
                {
                    var resolved = CommandProcessor.EvaluateExpression(step.TestStep.Expression, runtimeVariables);
                    return double.TryParse(resolved, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleResult)
                        ? OperationResult<double>.Success(doubleResult)
                        : OperationResult<double>.Failure($"Failed to evaluate expression:{step.TestStep.Expression}. Resolved to: {resolved}");
                }

                case TestEvaluationSource.FILE:
                {
                    var fileResult = await _fileContentReader.ReadAsync(step.TestStep.FilePath, token);

                    if (!fileResult.IsSuccess)
                        return OperationResult<double>.Failure(fileResult.ErrorMessage);

                    var processed = ResponseProcessor.Process(fileResult.Value!, mask);

                    return NumberHelper.TryParseDecimal(processed, out var doubleResult)
                        ? OperationResult<double>.Success(doubleResult)
                        : OperationResult<double>.Failure($"Failed to parse file content to double: {processed}");
                }
                
                case TestEvaluationSource.BOUNDARY_SCAN:
                {
                    var boundaryScanResult = await _boundaryScanSession.ExecuteStepAsync(step.TestStep.BoundaryScan, token);

                    if (boundaryScanResult.IsFailure)
                        return OperationResult<double>.Failure(boundaryScanResult.ErrorMessage);

                    // Steps that only drive pins have nothing to evaluate, checks report the number of mismatches
                    var outcome = boundaryScanResult.Value!;
                    step.ResultDetailLines = outcome.Messages;
                    return OperationResult<double>.Success(outcome.HasChecks ? outcome.Mismatches : double.NegativeInfinity);
                }

                case TestEvaluationSource.BOUNDARY_SCAN_BUS:
                {
                    var busConfig = step.TestStep.BoundaryScanBus;
                    var write = string.IsNullOrWhiteSpace(busConfig.Command)
                        ? []
                        : CommandProcessor.CompileToBytes(busConfig.Command, runtimeVariables);

                    var busResult = await _boundaryScanSession.ExecuteBusTransactionAsync(busConfig, write, token);
                    if (busResult.IsFailure)
                        return OperationResult<double>.Failure(busResult.ErrorMessage);

                    var transaction = busResult.Value!;
                    step.ResultDetailLines = transaction.Messages;

                    // Same results as the Interface source: 0 for a NACK, 1 for a completed write, otherwise the response
                    if (!transaction.Acknowledged)
                        return OperationResult<double>.Success(0);

                    if (busConfig.BytesToRead <= 0)
                        return OperationResult<double>.Success(1);

                    string processedBusResponse;
                    try
                    {
                        processedBusResponse = ResponseProcessor.Process(transaction.Data, mask);
                    }
                    catch (ResponseProcessingException ex)
                    {
                        return OperationResult<double>.Failure(ex.Message);
                    }

                    return NumberHelper.TryParseDecimal(processedBusResponse, out var busValue)
                        ? OperationResult<double>.Success(busValue)
                        : OperationResult<double>.Failure($"Failed to convert result '{processedBusResponse}' to a number.");
                }

                default: return OperationResult<double>.Failure("Unknown evaluation source");
            }
        }
        catch (OperationCanceledException)
        {
            _statusService.IsVisible = false;
            _statusService.IsIndeterminate = false;
            throw new OperationCanceledException();
        }
        catch (Exception ex)
        {
            return OperationResult<double>.Failure(ex.Message);
        }
    }
}
