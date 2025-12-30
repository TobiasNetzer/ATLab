using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Interfaces;

public interface ITestExecutor
{
    event Action? TestStarted;
    event Action<int, TestStepViewModel>? StepStarted;
    event Action? StepExecuted;
    event Action<int, TestStepViewModel>? StepCompleted;
    event Action<bool>? TestCompleted;

    Task StartTestAsync(IReadOnlyList<TestStepViewModel> steps, int index);
    void CancelTest();
}
