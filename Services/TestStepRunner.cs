using System;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Services;

public class TestStepRunner : ITestStepRunner
{
    
    private readonly ITestHardware _testHardware;
    private readonly IErrorService _errorService;
    private readonly IScriptRunner _scriptRunner;

    public TestStepRunner(ITestHardware testHardware, IErrorService errorService, IScriptRunner scriptRunner)
    {
        _testHardware = testHardware;
        _errorService = errorService;
        _scriptRunner = scriptRunner;
    }
    
    public async Task<TestStepResult> ExecuteAsync(TestStepViewModel step, CancellationToken token)
    {
        _testHardware.StimChannelStates = step.TestStep.LiveStimState.ToBoolArray();
        _testHardware.ExtStimChannelStates = step.TestStep.LiveExtStimState.ToBoolArray();
        _testHardware.ActiveMeasChannelH = (byte)(step.TestStep.MatrixState.ActiveChannelHigh);
        _testHardware.ActiveMeasChannelL = (byte)(step.TestStep.MatrixState.ActiveChannelLow);

        var result = await _testHardware.UpdateRelayStates();
        
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException("Test Hardware Relay update failed: " + result.ErrorMessage);
        }
        
        await Task.Delay(step.TestStep.Delay, token);
        
        var value = await _scriptRunner.ExecuteAsync<double>(step.TestStep.ScriptId, step.TestStep.TargetDevice, step.TestStep.ScriptVariables, token);
        
        return new TestStepResult(true, value);
    }
}