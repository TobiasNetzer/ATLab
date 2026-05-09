using System;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.Records;

namespace ATLab.Services;

public class TestStepEvaluator : ITestStepEvaluator
{
    public TestEvaluationResult Evaluate(TestStep testStep, double value)
    {
        var isValid = Math.Round(value, 15) >= testStep.LowerLimit && value <= testStep.UpperLimit;

        double deviation = 0;
        
        var diff = value - testStep.NominalValue;
        deviation = testStep.NominalValue != 0 ? Math.Round((diff / testStep.NominalValue) * 100, 6) : diff;
        
        return new TestEvaluationResult(isValid, deviation);
    }

}
