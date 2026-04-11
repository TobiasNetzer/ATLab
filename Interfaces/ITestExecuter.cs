using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ATLab.ViewModels;

namespace ATLab.Interfaces;

public interface ITestExecutor
{
    event Action? TestStarted;
    event Action<int, TestStepViewModel>? StepStarted;
    event Action<int, TestStepViewModel>? StepCompleted;
    event Action? TestCompleted;
    event Action? TestCancelled;
    event Action? TestRepeated;

    Task StartTestAsync(IReadOnlyList<TestStepViewModel> steps, int index);
    Task StartRepeatTestAsync(IReadOnlyList<TestStepViewModel> steps, int startIndex);
    Task StartSingleStepTest(TestStepViewModel step);
    Task CancelTest();
}