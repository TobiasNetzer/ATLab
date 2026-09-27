using System;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.Records;

namespace ATLab.Services;

public class TestStepEvaluator : ITestStepEvaluator
{
    
    private readonly ILoggingService _loggingService;

    public TestStepEvaluator(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }
    
    public TestEvaluationResult Evaluate(TestStep testStep, double value)
    {
        if (testStep.LowerLimit > testStep.UpperLimit)
        {
            _loggingService.Error($"Step {testStep.Number}: Lower limit is greater than upper limit");
        }
        
        var isValid = Math.Round(value, 15) >= testStep.LowerLimit && value <= testStep.UpperLimit;

        double deviation = 0;
        
        var diff = value - testStep.NominalValue;
        deviation = testStep.NominalValue != 0 ? Math.Round((diff / testStep.NominalValue) * 100, 6) : diff;
        
        return new TestEvaluationResult(isValid, deviation);
    }

}