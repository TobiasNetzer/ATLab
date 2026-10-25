using ATLab.Models;
using ATLab.Records;

namespace ATLab.Interfaces;

/// <summary>
/// Compares a measured value with the limits of a step and calculates the deviation from nominal.
/// </summary>
public interface ITestStepEvaluator
{
    TestEvaluationResult Evaluate(TestStep testStep, double value);
}
