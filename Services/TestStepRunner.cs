using System;
using System.Collections.Generic;
using System.Globalization;
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

    public TestStepRunner(
        ITestHardware testHardware,
        IScriptRunner scriptRunner,
        ICommandExecutor commandExecutor,
        IShellCommandRunner shellCommandRunner,
        IMessageBoxService messageBoxService)
    {
        _testHardware = testHardware;
        _scriptRunner = scriptRunner;
        _commandExecutor = commandExecutor;
        _shellCommandRunner = shellCommandRunner;
        _messageBoxService = messageBoxService;
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

            await Task.Delay(step.TestStep.Delay, token);
            
            var mask = step.TestStep.IsCustomMask ? step.TestStep.ResponseMask : null;

            switch (step.TestStep.EvaluationSource)
            {
                case TestEvaluationSource.NONE: return OperationResult<double>.Success(double.NegativeInfinity);
                
                case TestEvaluationSource.SCRIPT:
                    return await _scriptRunner.ExecuteAsync<double>(step.TestStep.ScriptId, step.TestStep.TargetDeviceId,
                        step.TestStep.ScriptVariables, token, runtimeVariables, mask);
                
                case TestEvaluationSource.COMMAND:
                    return await _commandExecutor.ExecuteAsync<double>(step.TestStep.Command, step.TestStep.TargetDeviceId, token, runtimeVariables, mask);

                case TestEvaluationSource.SHELL_COMMAND: return await _shellCommandRunner.RunAsync(step.TestStep.ShellCommand.Command,step.TestStep.ShellCommand.Option, token, runtimeVariables);
                
                case TestEvaluationSource.USER_RESPONSE:
                {
                    var operatorResponse = await _messageBoxService.ShowConfirmationImageAsync(
                        "Awaiting User Response",
                        step.TestStep.Comment,
                        step.TestStep.CustomMessageBoxImagePath,
                        "Pass",
                        "Fail");

                    return OperationResult<double>.Success(Convert.ToDouble(operatorResponse));
                }
                
                case TestEvaluationSource.EXPRESSION: 
                {
                    var resolved = CommandProcessor.EvaluateExpression(step.TestStep.Expression, runtimeVariables);
                    return double.TryParse(resolved, CultureInfo.InvariantCulture, out var doubleResult)
                        ? OperationResult<double>.Success(doubleResult)
                        : OperationResult<double>.Failure($"Failed to evaluate expression: {step.TestStep.Expression}. Resolved to: {resolved}");
                }
                
                default: return OperationResult<double>.Failure("Unknown evaluation source");
            }
        }
        catch (OperationCanceledException)
        {
            throw new OperationCanceledException();
        }
        catch (Exception ex)
        {
            return OperationResult<double>.Failure(ex.Message);
        }
    }
}