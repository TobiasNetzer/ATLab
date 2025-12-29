using System;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

public class TestStepEvaluator : ITestStepEvaluator
{
    public TestEvaluationResult Evaluate(TestStep testStep, double value)
    {
        bool isValid = Math.Round(value, 15) >= testStep.LowerLimit && value <= testStep.UpperLimit;

        double deviation = 0;

        if (testStep.NominalValue != 0)
        {
            double diff = value - testStep.NominalValue;
            deviation = Math.Round((diff / testStep.NominalValue) * 100, 6);
        }

        return new TestEvaluationResult(isValid, deviation);
    }

}
