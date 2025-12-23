using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.ViewModels;

namespace ATLab.Services;

public class TestExecutor : ITestExecutor
{
    private readonly ITestStepRunner _runner;

    public event Action<int, TestStepViewModel>? StepStarted;
    public event Action<int, TestStepViewModel, bool>? StepCompleted;
    public event Action? TestCompleted;

    public TestExecutor(ITestStepRunner runner)
    {
        _runner = runner;
    }

    public async Task ExecuteAsync(
        IReadOnlyList<TestStepViewModel> steps,
        CancellationToken token)
    {
        for (int i = 0; i < steps.Count; i++)
        {
            var step = steps[i];

            StepStarted?.Invoke(i, step);

            bool result = await _runner.ExecuteAsync(step, token);

            StepCompleted?.Invoke(i, step, result);

            if (!result)
                break;
        }

        TestCompleted?.Invoke();
    }
}
