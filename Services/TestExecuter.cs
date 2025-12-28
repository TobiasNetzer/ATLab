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
    public event Action<int, TestStepViewModel, OperationResult<double>>? StepCompleted;
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

            OperationResult<double> result;
            try
            {
                result = await _runner.ExecuteAsync(step, token);
                if (!result.IsSuccess)
                {
                    _errorService.AddError($"Error in step {step.TestStep.Number}: {result.ErrorMessage}");
                }
            }
            catch (OperationCanceledException)
            {
                _errorService.AddError("Test execution cancelled.");
                result = OperationResult<double>.Failure("Cancelled");
            }
            catch (Exception ex)
            {
                _errorService.AddError($"Unexpected error in step {step.TestStep.Number}: {ex.Message}");
                result = OperationResult<double>.Failure(ex.Message);
            }

            StepCompleted?.Invoke(i, step, result);

            if (!result.IsSuccess)
                break;
        }

        TestCompleted?.Invoke();
    }
}
