using System;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

public class TestStepEvaluator : ITestStepEvaluator
{
    public TestEvaluationResult Evaluate(TestStep testStep, double value)
    {
        // Using Math.Clamp to check if the value is within [LowerLimit, UpperLimit].
        // It returns 'value' if it's within the range, otherwise it returns the nearest limit.
        // Note: This assumes LowerLimit <= UpperLimit.
        bool isValid = Math.Clamp(value, testStep.LowerLimit, testStep.UpperLimit) == value;
        
        double deviation = 0;
        if (testStep.NominalValue != 0)
        {
            // Calculate percentage deviation from nominal
            double rawDeviation = ((value - testStep.NominalValue) / testStep.NominalValue) * 100;
            
            // Round to a reasonable precision
            deviation = Math.Round(rawDeviation, 4);
        }
        else if (value != 0)
        {
            deviation = double.PositiveInfinity;
        }

        return new TestEvaluationResult(isValid, deviation);
    }
}
