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
        var canceled = false;
        for (int i = 0; i < steps.Count; i++)
        {
            var step = steps[i];

            StepStarted?.Invoke(i, step);

            var result = await _runner.ExecuteAsync(step, token);

            if (result.IsSuccess)
            {
                if (result.Value >= 1E+9)
                {
                    step.Result = "Overflow";
                    step.IsValid = false;
                    step.Deviation = string.Empty;
                }
                else
                {
                    step.Result = UnitParser.Format(result.Value, step.TestStep.Unit);
                    var evaluation = _evaluator.Evaluate(step.TestStep, result.Value);
                    step.Deviation = evaluation.Deviation.ToString("F2") + " %";
                    step.IsValid = evaluation.IsValid;
                }
            }
            else
            {
                if (result.ErrorMessage == "Cancelled")
                {
                    step.Result = string.Empty;
                    canceled = true;
                }
                else
                {
                    _errorService.AddError($"Error in step {step.TestStep.Number}: {result.ErrorMessage}");
                    step.Result = "Error";
                }

                step.IsValid = false;
                step.Deviation = string.Empty;
            }

            StepCompleted?.Invoke(i, step);

            if (!result.IsSuccess)
                break;
        }

        TestCompleted?.Invoke(canceled);
    }
}
