using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Interfaces;

public interface ITestExecutor
{
    event Action<int, TestStepViewModel>? StepStarted;
    event Action<int, TestStepViewModel, TestStepResult>? StepCompleted;
    event Action? TestCompleted;

    Task ExecuteAsync(
        IReadOnlyList<TestStepViewModel> steps,
        CancellationToken token);
}
