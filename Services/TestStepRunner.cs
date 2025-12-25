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
        _testHardware.StimChannelStates = step.StimState.ToBoolArray();
        _testHardware.ExtStimChannelStates = step.ExtStimState.ToBoolArray();
        _testHardware.ActiveMeasChannelH = (byte)(step.MatrixState.ActiveChannelHigh);
        _testHardware.ActiveMeasChannelL = (byte)(step.MatrixState.ActiveChannelLow);
        
        bool success = false;
        try
        {
            var result = await _testHardware.UpdateRelayStates();
            
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
        
        //await Task.Delay(500, token);
        return new TestStepResult(success, 0.0);
    }
}