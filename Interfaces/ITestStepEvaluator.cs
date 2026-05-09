using ATLab.Models;
using ATLab.Records;

namespace ATLab.Interfaces;

public interface ITestStepEvaluator
{
    TestEvaluationResult Evaluate(TestStep testStep, double value);
}
