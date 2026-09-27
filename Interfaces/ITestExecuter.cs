using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Interfaces;

public interface ITestExecutor
{
    event Action TestStarted;
    event Action<int, TestStepViewModel> StepStarted;
    event Action<int, TestStepViewModel> StepCompleted;
    event Action StepRepeated;
    event Func<Task> TestCompleted;
    event Action TestCanceled;
    event Action TestRepeated;
    event Action ExecutionError;
    event Action<bool> SingleStepContinueRequestedChanged;

    Task StartTestAsync(IReadOnlyList<TestStepViewModel> steps, int index, List<CustomVariable> runtimeVariables);
    Task StartRepeatTestAsync(IReadOnlyList<TestStepViewModel> steps, int startIndex, List<CustomVariable> runtimeVariables);
    Task StartSingleStepTest(TestStepViewModel step, List<CustomVariable> runtimeVariables);
    Task CancelTest();
    public void RequestBreakRepeat();
    public void RequestSingleStepContinue();
    public void SetDebugMode(bool state);
}