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

    public TestStepRunner(ITestHardware testHardware, IErrorService errorService)
    {
        _testHardware = testHardware;
        _errorService = errorService;
    }
    
    public async Task<TestStepResult> ExecuteAsync(TestStepViewModel step, CancellationToken token)
    {
        if (step.TestStep.LiveStimState == null || step.TestStep.LiveExtStimState == null || step.TestStep.MatrixState == null)
        {
            _errorService.AddError("Test step state is not initialized.");
            return new TestStepResult(false, 0.0);
        }

        _testHardware.StimChannelStates = step.TestStep.LiveStimState.ToBoolArray();
        _testHardware.ExtStimChannelStates = step.TestStep.LiveExtStimState.ToBoolArray();
        _testHardware.ActiveMeasChannelH = (byte)(step.TestStep.MatrixState.ActiveChannelHigh);
        _testHardware.ActiveMeasChannelL = (byte)(step.TestStep.MatrixState.ActiveChannelLow);
        
        bool success = false;
        try
        {
            var result = await _testHardware.UpdateRelayStates();
            
            await Task.Delay(step.TestStep.Delay, token);
            
            // run script

            if (result.IsSuccess)
            {
                success = true;
            }
            else
            {
                _errorService.AddError("Test Hardware Relay update failed: " + result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _errorService.AddError("Exception: " + ex.Message);
        }
        
        return new TestStepResult(success, 0.0);
    }
}