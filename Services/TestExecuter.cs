using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Services;

public class TestExecutor : ITestExecutor
{
    private readonly ITestStepRunner _runner;
    private readonly IErrorService _errorService;

    public event Action<int, TestStepViewModel>? StepStarted;
    public event Action<int, TestStepViewModel, TestStepResult>? StepCompleted;
    public event Action? TestCompleted;

    public TestExecutor(ITestStepRunner runner, IErrorService errorService)
    {
        _runner = runner;
        _errorService = errorService;
    }

    public async Task ExecuteAsync(
        IReadOnlyList<TestStepViewModel> steps,
        CancellationToken token)
    {
        for (int i = 0; i < steps.Count; i++)
        {
            var step = steps[i];

            StepStarted?.Invoke(i, step);

            TestStepResult result;
            try
            {
                result = await _runner.ExecuteAsync(step, token);
            }
            catch (OperationCanceledException)
            {
                _errorService.AddError("Test execution cancelled.");
                result = new TestStepResult(false, 0);
            }
            catch (Exception ex)
            {
                _errorService.AddError($"Error in step {step.TestStep.Number}: {ex.Message}");
                result = new TestStepResult(false, 0);
            }

            StepCompleted?.Invoke(i, step, result);

            if (!result.IsSuccess)
                break;
        }

        TestCompleted?.Invoke();
    }
}
