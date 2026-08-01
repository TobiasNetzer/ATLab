using System;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Services;

public class TestExecutionController : ITestExecutionController
{
    private readonly ITestExecutor _testExecutor;
    private readonly ISerialNumberDialogService _serialNumberDialogService;
    private readonly ITestResultExportService _testResultExportService;
    private readonly ProjectModel _projectModel;

    public TestExecutionController(
        ITestExecutor testExecutor,
        ISerialNumberDialogService serialNumberDialogService,
        ITestResultExportService testResultExportService,
        ProjectModel projectModel)
    {
        _testExecutor = testExecutor;
        _serialNumberDialogService = serialNumberDialogService;
        _testResultExportService = testResultExportService;
        _projectModel = projectModel;
    }

    public void HookExecutorEvents(TestingTabViewModel vm)
    {
        _testExecutor.TestStarted += () =>
        {
            vm.StartTime = DateTimeOffset.Now;
        };

        _testExecutor.StepStarted += (index, step) =>
        {
            vm.SelectedStep = vm.TestSteps[index];
        };

        _testExecutor.StepCompleted += (index, step) =>
        {
            vm.TestDuration = $"{vm.Elapsed.TotalSeconds:F2}s";
            vm.TestProgress = vm.TestSteps.Count == 0
                ? 0
                : (int)Math.Round((double)(index + 1) / vm.TestSteps.Count * 100);

            if (!step.IsPassed)
                vm.NumberFailedSteps++;
            
            step.IsExecuted = true;
        };
        
        _testExecutor.StepRepeated += () =>
        {
            vm.TestDuration = $"{vm.Elapsed.TotalSeconds:F2}s";
            var index = vm.SelectedStep != null ? vm.TestSteps.IndexOf(vm.SelectedStep) : -1;
            vm.TestProgress = vm.TestSteps.Count == 0 || index == -1
                ? 0
                : (int)Math.Round((double)(index + 1) / vm.TestSteps.Count * 100);
        };

        _testExecutor.TestCompleted += () =>
        {
            vm.TestDuration = $"{vm.Elapsed.TotalSeconds:F2}s";
            vm.TestProgress = 100;
            vm.NumberRunTests++;

            if (vm.TestStatus == TestStatus.CANCELLED)
            {
                UpdatePassedPercentage(vm);
                return;
            }

            if (vm.AllowResultSave)
            {
                var testInfo = new TestInfo()
                {
                    ProjectName = _projectModel.ProjectName,
                    Operator = vm.User,
                    Duration = vm.TestDuration,
                    SerialNumber = vm.SerialNumber,
                    DeviceUnderTestInfo = _projectModel.DeviceUnderTestInfo
                };
                
                EnqueueExport(() => _testResultExportService.SaveAsync(vm.TestSteps, testInfo, vm.NumberFailedSteps));
            }

            if (vm.NumberFailedSteps > 0)
            {
                vm.TestStatus = TestStatus.FAILED;
                UpdatePassedPercentage(vm);
                return;
            }

            vm.TestStatus = TestStatus.PASSED;
            vm.NumberPassedTests++;
            UpdatePassedPercentage(vm);
        };

        _testExecutor.TestCancelled += () =>
        {
            vm.TestStatus = TestStatus.CANCELLED;
        };

        _testExecutor.TestRepeated += () =>
        {
            ResetAllResults(vm);
            vm.TestProgress = 0;
            vm.NumberFailedSteps = 0;
            vm.SelectedStep = vm.TestSteps.Count > 0 ? vm.TestSteps[0] : null;
            vm.TestStatus = TestStatus.RUNNING;
        };
    }

    public async Task StartTestAsync(TestingTabViewModel vm)
    {
        using (_projectModel.SuppressDirtyTracking())
        {
            ResetAllResults(vm);

            if (!await RequestSerialNumber(vm))
                return;

            vm.TestStatus = TestStatus.RUNNING;
            vm.NumberFailedSteps = 0;
            vm.TestProgress = 0;
            vm.SelectedStep = vm.TestSteps.Count > 0 ? vm.TestSteps[0] : null;

            vm.AllowResultSave = true;
            await _testExecutor.StartTestAsync(vm.TestSteps, 0, vm.RuntimeVariables);
            vm.AllowResultSave = false;
        }
    }

    public async Task StartRepeatAsync(TestingTabViewModel vm)
    {
        using (_projectModel.SuppressDirtyTracking())
        {
            ResetAllResults(vm);

            if (!await RequestSerialNumber(vm))
                return;

            vm.NumberFailedSteps = 0;
            vm.TestStatus = TestStatus.RUNNING;
            vm.TestProgress = 0;
            vm.SelectedStep = vm.TestSteps.Count > 0 ? vm.TestSteps[0] : null;

            vm.AllowResultSave = true;
            await _testExecutor.StartRepeatTestAsync(vm.TestSteps, 0, vm.RuntimeVariables);
        }
    }

    public async Task StartFromSelectionAsync(TestingTabViewModel vm)
    {
        using (_projectModel.SuppressDirtyTracking())
        {
            ResetAllResults(vm);
            vm.NumberFailedSteps = 0;
            vm.TestStatus = TestStatus.RUNNING;
            vm.TestProgress = 0;

            var index = vm.SelectedStep != null ? vm.TestSteps.IndexOf(vm.SelectedStep) : 0;
            await _testExecutor.StartTestAsync(vm.TestSteps, index, vm.RuntimeVariables);
        }
    }

    public async Task StartSingleStepAsync(TestingTabViewModel vm)
    {
        if (vm.SelectedStep == null)
            return;

        using (_projectModel.SuppressDirtyTracking())
        {
            vm.NumberFailedSteps = 0;
            vm.TestProgress = 0;
            vm.TestDuration = string.Empty;
            vm.TestStatus = TestStatus.RUNNING;

            await _testExecutor.StartSingleStepTest(vm.SelectedStep, vm.RuntimeVariables);

            vm.TestStatus = TestStatus.IDLE;
        }
    }

    public Task CancelAsync() => _testExecutor.CancelTest();

    public void RequestBreakRepeat() => _testExecutor.RequestBreakRepeat();

    public void ResetAllResults(TestingTabViewModel vm)
    {
        vm.TestStatus = TestStatus.IDLE;
        foreach (var step in vm.TestSteps)
            step.ResetResults();
    }

    public async Task<bool> RequestSerialNumber(TestingTabViewModel vm)
    {
        if (_projectModel.Settings.IsUseSerialNumber)
        {
            var serial = await _serialNumberDialogService.AskForSerialNumberAsync();

            if (serial == null)
            {
                vm.SerialNumber = string.Empty;
                return false;
            }

            vm.SerialNumber = serial;
        }
        else
        {
            vm.SerialNumber = string.Empty;
        }

        return true;
    }
    
    private Task _exportQueue = Task.CompletedTask;

    public Task EnqueueExport(Func<Task> work)
    {
        return _exportQueue = _exportQueue.ContinueWith(_ => work()).Unwrap();
    }
    
    private void UpdatePassedPercentage(TestingTabViewModel vm)
    {
        if (vm.NumberRunTests > 0)
            vm.PassedPercentage = Math.Round((double)vm.NumberPassedTests / vm.NumberRunTests * 100, 2);
        else
            vm.PassedPercentage = 0;
    }
}