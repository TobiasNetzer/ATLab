using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Helpers;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Services;

public class TestStepRunner : ITestStepRunner
{
    
    private readonly ITestHardware _testHardware;
    private readonly IScriptRunner _scriptRunner;
    private readonly ICommandExecutor _commandExecutor;
    private readonly IShellCommandRunner _shellCommandRunner;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IFileContentReader _fileContentReader;
    private readonly IInterfaceCommandExecuter _interfaceCommandExecuter;
    private readonly ProjectModel _projectModel;
    private readonly IStatusService _statusService;

    public TestStepRunner(
        IHardwareAccessor hardwareAccessor,
        IScriptRunner scriptRunner,
        ICommandExecutor commandExecutor,
        IShellCommandRunner shellCommandRunner,
        IMessageBoxService messageBoxService,
        IFileContentReader fileContentReader,
        IInterfaceCommandExecuter interfaceCommandExecuter,
        ProjectModel projectModel,
        IStatusService statusService)
    {
        _testHardware = hardwareAccessor.Hardware;
        _scriptRunner = scriptRunner;
        _commandExecutor = commandExecutor;
        _shellCommandRunner = shellCommandRunner;
        _messageBoxService = messageBoxService;
        _fileContentReader = fileContentReader;
        _interfaceCommandExecuter = interfaceCommandExecuter;
        _projectModel = projectModel;
        _statusService = statusService;
    }
    
    public async Task<OperationResult<double>> ExecuteAsync(TestStepViewModel step, List<CustomVariable> runtimeVariables, CancellationToken token)
    {
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
                    return await _interfaceCommandExecuter.ExecuteAsync(step.TestStep.InterfaceConfig, runtimeVariables, mask);

                case TestEvaluationSource.SHELL_COMMAND:
                {
                    _statusService.Progress = 0;
                    _statusService.Text = LocalizationService.Instance["StatusLabel_WaitingForExternalApp"];
                    _statusService.IsIndeterminate = true;
                    _statusService.IsVisible = true;
                    var shellCommandResult = await _shellCommandRunner.RunAsync(step.TestStep.ShellCommand,Path.GetDirectoryName(_projectModel.FilePath), token, runtimeVariables);
                    _statusService.IsVisible = false;
                    _statusService.IsIndeterminate = false;
                    return shellCommandResult;
                }
                
                case TestEvaluationSource.USER_RESPONSE:
                {
                    var resolvedComment = CommandProcessor.CompileToString(step.TestStep.Comment, runtimeVariables);
                    
                    var operatorResponse = await _messageBoxService.ShowConfirmationImageAsync(
                        LocalizationService.Instance["Title_AwaitingUserResponse"],
                        resolvedComment,
                        step.TestStep.CustomMessageBoxImagePath,
                        DialogFunction.USER_INPUT);

                    return OperationResult<double>.Success(Convert.ToDouble(operatorResponse));
                }
                
                case TestEvaluationSource.EXPRESSION: 
                {
                    var resolved = CommandProcessor.EvaluateExpression(step.TestStep.Expression, runtimeVariables);
                    return double.TryParse(resolved, CultureInfo.InvariantCulture, out var doubleResult)
                        ? OperationResult<double>.Success(doubleResult)
                        : OperationResult<double>.Failure($"Failed to evaluate expression: {step.TestStep.Expression}. Resolved to: {resolved}");
                }

                case TestEvaluationSource.FILE:
                {
                    var fileResult = await _fileContentReader.ReadAsync(step.TestStep.FilePath, token);

                    if (!fileResult.IsSuccess)
                        return OperationResult<double>.Failure(fileResult.ErrorMessage);

                    var processed = ResponseProcessor.Process(fileResult.Value!, mask);

                    return double.TryParse(processed, CultureInfo.InvariantCulture, out var doubleResult)
                        ? OperationResult<double>.Success(doubleResult)
                        : OperationResult<double>.Failure($"Failed to parse file content to double: {processed}");
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