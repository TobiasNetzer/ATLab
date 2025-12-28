using ATLab.Models;

namespace ATLab.Interfaces;

public interface ITestStepEvaluator
{
    TestEvaluationResult Evaluate(TestStep testStep, double value);
}

public record TestEvaluationResult(bool IsValid, double Deviation);
