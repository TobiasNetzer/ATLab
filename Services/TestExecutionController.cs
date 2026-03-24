using System;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Services;

public class TestExecutionController
{
    private readonly ITestExecutor _testExecutor;
    private readonly ISerialNumberDialogService _serialNumberDialogService;
    private readonly TestResultExportService _testResultExportService;
    private readonly ProjectSettings _projectSettings;

    public TestExecutionController(
        ITestExecutor testExecutor,
        ISerialNumberDialogService serialNumberDialogService,
        TestResultExportService testResultExportService,
        ProjectSettings projectSettings)
    {
        _testExecutor = testExecutor;
        _serialNumberDialogService = serialNumberDialogService;
        _testResultExportService = testResultExportService;
        _projectSettings = projectSettings;
    }

    public void HookExecutorEvents(TestingTabViewModel vm)
    {
        _testExecutor.TestStarted += () =>
        {
            vm.StartTime = DateTimeOffset.Now;
        };

        _testExecutor.StepStarted += (index, step) =>
        {
            vm.SelectedStepIndex = index;
        };

        _testExecutor.StepExecuted += () =>
        {
            vm.TestDuration = $"{vm.Elapsed.TotalSeconds:F2}s";
        };

        _testExecutor.StepCompleted += (index, step) =>
        {
            vm.TestDuration = $"{vm.Elapsed.TotalSeconds:F2}s";
            vm.TestProgress = vm.TestSteps.Count == 0
                ? 0
                : (int)Math.Round((double)(vm.SelectedStepIndex + 1) / vm.TestSteps.Count * 100);

            if (!step.IsPassed)
                vm.NumberFailedSteps++;
        };

        _testExecutor.TestCompleted += () =>
        {
            vm.TestDuration = $"{vm.Elapsed.TotalSeconds:F2}s";
            vm.TestProgress = 100;
            vm.NumberRunTests++;

            if (vm.TestStatus == TestStatus.CANCELLED)
                return;

            if (vm.AllowResultSave)
                _ = _testResultExportService.SaveAsync(vm.TestSteps, vm.SerialNumber, vm.NumberFailedSteps);

            if (vm.NumberFailedSteps > 0)
            {
                vm.TestStatus = TestStatus.FAILED;
                return;
            }

            vm.TestStatus = TestStatus.PASSED;
            vm.NumberPassedTests++;
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
            vm.SelectedStepIndex = 0;
            vm.TestStatus = TestStatus.RUNNING;
        };
    }

    public async Task StartTestAsync(TestingTabViewModel vm)
    {
        using (vm.SuppressDirtyTracking())
        {
            ResetAllResults(vm);

            if (!await RequestSerialNumber(vm))
                return;

            vm.TestStatus = TestStatus.RUNNING;
            vm.NumberFailedSteps = 0;
            vm.TestProgress = 0;
            vm.SelectedStepIndex = 0;

            vm.AllowResultSave = true;
            await _testExecutor.StartTestAsync(vm.TestSteps, vm.SelectedStepIndex);
            vm.AllowResultSave = false;
        }
    }

    public async Task StartRepeatAsync(TestingTabViewModel vm)
    {
        using (vm.SuppressDirtyTracking())
        {
            ResetAllResults(vm);

            if (!await RequestSerialNumber(vm))
                return;

            vm.NumberFailedSteps = 0;
            vm.TestStatus = TestStatus.RUNNING;
            vm.TestProgress = 0;
            vm.SelectedStepIndex = 0;

            vm.AllowResultSave = true;
            await _testExecutor.StartRepeatTestAsync(vm.TestSteps, vm.SelectedStepIndex);
        }
    }

    public async Task StartFromSelectionAsync(TestingTabViewModel vm)
    {
        using (vm.SuppressDirtyTracking())
        {
            ResetAllResults(vm);
            vm.NumberFailedSteps = 0;
            vm.TestStatus = TestStatus.RUNNING;
            vm.TestProgress = 0;

            await _testExecutor.StartTestAsync(vm.TestSteps, vm.SelectedStepIndex);
        }
    }

    public async Task StartSingleStepAsync(TestingTabViewModel vm)
    {
        if (vm.SelectedStep == null)
            return;

        using (vm.SuppressDirtyTracking())
        {
            vm.NumberFailedSteps = 0;
            vm.TestProgress = 0;
            vm.TestDuration = string.Empty;
            vm.TestStatus = TestStatus.RUNNING;

            await _testExecutor.StartSingleStepTest(vm.SelectedStep);

            vm.TestStatus = TestStatus.IDLE;
        }
    }

    public Task CancelAsync() => _testExecutor.CancelTest();

    private void ResetAllResults(TestingTabViewModel vm)
    {
        foreach (var step in vm.TestSteps)
            step.ResetResults();
    }

    private async Task<bool> RequestSerialNumber(TestingTabViewModel vm)
    {
        if (_projectSettings.UseSerialNumber)
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
}