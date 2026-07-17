using System;
using System.Threading.Tasks;
using ATLab.ViewModels;

namespace ATLab.Interfaces;

public interface ITestExecutionController
{
    void HookExecutorEvents(TestingTabViewModel vm);
    Task StartTestAsync(TestingTabViewModel vm);
    Task StartRepeatAsync(TestingTabViewModel vm);
    Task StartFromSelectionAsync(TestingTabViewModel vm);
    Task StartSingleStepAsync(TestingTabViewModel vm);
    Task CancelAsync();
    public void RequestBreakRepeat();
    void ResetAllResults(TestingTabViewModel vm);
    Task<bool> RequestSerialNumber(TestingTabViewModel vm);
    Task EnqueueExport(Func<Task> work);
}