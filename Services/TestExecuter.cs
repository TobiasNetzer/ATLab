using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Helpers;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Services;

public class TestExecutor : ITestExecutor
{
    private readonly ITestStepRunner _runner;
    private readonly IErrorService _errorService;
    private readonly ITestStepEvaluator _evaluator;
    private CancellationTokenSource? _cts;

    public event Action<int, TestStepViewModel>? StepStarted;
    public event Action<int, TestStepViewModel>? StepCompleted;
    public event Action<bool>? TestCompleted;

    public TestExecutor(ITestStepRunner runner, IErrorService errorService, ITestStepEvaluator evaluator)
    {
        _runner = runner;
        _errorService = errorService;
        _evaluator = evaluator;
    }

    public async Task StartTestAsync(IReadOnlyList<TestStepViewModel> steps)
    {
        if (steps.Count == 0)
        {
            _errorService.AddError("No test steps configured.");
            return;
        }

        _cts = new CancellationTokenSource();

        try
        {
            await ExecuteAsync(steps, _cts.Token);
        }
        catch (Exception ex)
        {
            _errorService.AddError("Test execution failed: " + ex.Message);
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
        }
    }

    public void CancelTest()
    {
        _cts?.Cancel();
    }

    private async Task ExecuteAsync(
    IReadOnlyList<TestStepViewModel> steps,
    CancellationToken token)
{

    for (int i = 0; i < steps.Count; i++)
    {
        var step = steps[i];
        OnStepStarted(i, step);
        OperationResult<double> stepExecutionResult;
        do
        {
            stepExecutionResult = await _runner.ExecuteAsync(step, token);

                if (stepExecutionResult.IsSuccess)
                {
                    EvaluateTestStep(step, stepExecutionResult.Value);
                }
                else
                {
                    TestStepExecutionFailed(step, stepExecutionResult);
                }
            } while (step.TestStep.RepeatUntilPass && !token.IsCancellationRequested && !step.IsPassed);
            
            OnStepCompleted(i, step);

        if (!stepExecutionResult.IsSuccess)
            break;
    }

    OnTestCompleted(token.IsCancellationRequested);
}

    private void EvaluateTestStep(TestStepViewModel step, double value)
    {
        if (IsOverflow(value))
        {
            // Overflow detected
            step.Result = "Overflow";
            step.IsPassed = false;
            step.Deviation = string.Empty;
            return;
        }

        if (double.IsNegativeInfinity(value))
        {
            // No evaluation source
            step.Result = string.Empty;
            step.IsPassed = true;
            step.Deviation = string.Empty;
            return;
        }

        step.Result = UnitParser.Format(value, step.TestStep.Unit);

        var evaluation = _evaluator.Evaluate(step.TestStep, value);
        step.Deviation = $"{evaluation.Deviation:F2} %";
        step.IsPassed = evaluation.IsValid;
    }

    private void TestStepExecutionFailed(
        TestStepViewModel step,
        OperationResult<double> result)
    {
        if (result.ErrorMessage != string.Empty)
            _errorService.AddError($"Error in step {step.TestStep.Number}: {result.ErrorMessage}");
        step.Result = string.Empty;
        step.IsPassed = false;
        step.Deviation = string.Empty;
    }

    private bool IsOverflow(double value) =>
        value >= 1E9;

    private void OnStepStarted(int index, TestStepViewModel step) =>
        StepStarted?.Invoke(index, step);

    private void OnStepCompleted(int index, TestStepViewModel step) =>
        StepCompleted?.Invoke(index, step);

    private void OnTestCompleted(bool canceled) =>
        TestCompleted?.Invoke(canceled);

}
