using System;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Services;

public class TestStepRunner : ITestStepRunner
{
    
    private readonly ITestHardware _testHardware;
    private readonly IScriptRunner _scriptRunner;

    public TestStepRunner(ITestHardware testHardware, IScriptRunner scriptRunner)
    {
        _testHardware = testHardware;
        _scriptRunner = scriptRunner;
    }
    
    public async Task<OperationResult<double>> ExecuteAsync(TestStepViewModel step, CancellationToken token)
    {
        try
        {
            _testHardware.StimChannelStates = step.TestStep.LiveStimState.ToBoolArray();
            _testHardware.ExtStimChannelStates = step.TestStep.LiveExtStimState.ToBoolArray();
            _testHardware.ActiveMeasChannelH = (byte)(step.TestStep.MatrixState.ActiveChannelHigh);
            _testHardware.ActiveMeasChannelL = (byte)(step.TestStep.MatrixState.ActiveChannelLow);

            var result = await _testHardware.UpdateRelayStates();

            if (!result.IsSuccess)
            {
                return OperationResult<double>.Failure("Communication with test hardware failed: " + result.ErrorMessage);
            }

            await Task.Delay(step.TestStep.Delay, token);

            switch (step.TestStep.EvaluationSource)
            {
                case TestEvaluationSource.NONE: return OperationResult<double>.Success(0);
                case TestEvaluationSource.SCRIPT:
                    return await _scriptRunner.ExecuteAsync<double>(step.TestStep.ScriptId, step.TestStep.TargetDevice,
                        step.TestStep.ScriptVariables, token);
                case TestEvaluationSource.COMMAND: return OperationResult<double>.Success(0);

                default: return OperationResult<double>.Failure("Unknown evaluation source");
            }
        }
        catch (OperationCanceledException)
        {
            return OperationResult<double>.Failure("Cancelled");
        }
        catch (Exception ex)
        {
            return OperationResult<double>.Failure(ex.Message);
        }
    }
}