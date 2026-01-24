using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Helpers;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Services;

public class TestExecutor : ITestExecutor
{
    private readonly ITestHardware _testHardware;
    private readonly ITestStepRunner _runner;
    private readonly IErrorService _errorService;
    private readonly ITestStepEvaluator _evaluator;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IProjectSettings _projectSettings;

    private CancellationTokenSource? _cts;
    private bool _repeatTest;

    private readonly SemaphoreSlim _relaySemaphore = new(1, 1);
    private bool _relayStatesCleared;

    public event Action? TestStarted;
    public event Action<int, TestStepViewModel>? StepStarted;
    public event Action? StepExecuted;
    public event Action<int, TestStepViewModel>? StepCompleted;
    public event Action? TestCompleted;
    public event Action? TestCancelled;
    public event Action? TestRepeated;

    public TestExecutor(
        ITestHardware testHardware,
        ITestStepRunner runner,
        IErrorService errorService,
        ITestStepEvaluator evaluator,
        IMessageBoxService messageBoxService,
        IProjectSettings projectSettings)
    {
        _testHardware = testHardware;
        _runner = runner;
        _errorService = errorService;
        _evaluator = evaluator;
        _messageBoxService = messageBoxService;
        _projectSettings = projectSettings;
    }

    private void ResetRelayClearFlag() => _relayStatesCleared = false;

    private async Task EnsureRelayStatesClearedAsync()
    {
        if (_relayStatesCleared)
            return;

        await _relaySemaphore.WaitAsync();
        try
        {
            if (_relayStatesCleared)
                return;

            var result = await _testHardware.ClearRelayStates();
            if (!result.IsSuccess)
                _errorService.AddError($"Error on resetting relay states: {result.ErrorMessage}");

            _relayStatesCleared = true;
            
            await Task.Delay(500);
        }
        finally
        {
            _relaySemaphore.Release();
        }
    }
    
    public async Task StartTestAsync(IReadOnlyList<TestStepViewModel> steps, int startIndex)
    {
        if (steps.Count == 0)
        {
            _errorService.AddError("No test steps configured.");
            OnTestCancelled();
            return;
        }

        if (startIndex >= steps.Count || startIndex < 0)
        {
            _errorService.AddError("Test step index out of range.");
            OnTestCancelled();
            return;
        }

        ResetRelayClearFlag();
        await EnsureRelayStatesClearedAsync();
        ResetRelayClearFlag();

        _cts = new CancellationTokenSource();
        OnTestStarted();

        try
        {
            await ExecuteAsync(steps, startIndex, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            OnTestCancelled();
        }
        catch (Exception ex)
        {
            _errorService.AddError("Test execution failed: " + ex.Message);
        }
        finally
        {
            OnTestCompleted();
            _cts?.Dispose();
            _cts = null;

            await EnsureRelayStatesClearedAsync();
        }
    }

    public async Task StartRepeatTestAsync(IReadOnlyList<TestStepViewModel> steps, int startIndex)
    {
        _repeatTest = true;

        while (_repeatTest)
        {
            OnTestRepeated();
            await Task.Delay(100);
            await StartTestAsync(steps, startIndex);
        }
    }
    
    public async Task StartSingleStepTest(TestStepViewModel step)
    {
        _cts = new CancellationTokenSource();

        ResetRelayClearFlag();
        
        try
        {
            var result = await _runner.ExecuteAsync(step, _cts.Token);

            if (_cts.Token.IsCancellationRequested)
                return;

            switch (result.Status)
            {
                case OperationStatus.SUCCESS:
                    EvaluateTestStep(step, result.Value);
                    break;
                
                case OperationStatus.TIMEOUT:
                    TestStepExecutionTimedOut(step);
                    break;
                
                case OperationStatus.FAILURE:
                    TestStepExecutionFailed(step, result);
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
        }
        catch (OperationCanceledException)
        {
            //
        }
        catch (Exception ex)
        {
            _errorService.AddError("Test execution failed: " + ex.Message);
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
        }
    }

    public async Task CancelTest()
    {
        _cts?.CancelAsync();
        _repeatTest = false;

        await EnsureRelayStatesClearedAsync();
    }
    
    private async Task ExecuteAsync(
        IReadOnlyList<TestStepViewModel> steps,
        int startIndex,
        CancellationToken token)
    {
        for (var i = startIndex; i < steps.Count; i++)
        {
            var step = steps[i];

            if (step.TestStep.IgnoreStep)
                continue;

            OnStepStarted(i, step);

            OperationResult<double> stepExecutionResult;

            if (step.TestStep.ShowCommentOnTestStart)
            {
                var result = await _messageBoxService.ShowConfirmationImageAsync(
                    "Test Execution Halted",
                    step.TestStep.Comment,
                    step.TestStep.CustomMessageBoxImagePath);

                if (!result)
                    throw new OperationCanceledException();
            }

            do
            {
                stepExecutionResult = await _runner.ExecuteAsync(step, token);

                token.ThrowIfCancellationRequested();

                switch (stepExecutionResult.Status)
                {
                    case OperationStatus.SUCCESS:
                        EvaluateTestStep(step, stepExecutionResult.Value);
                        break;
                
                    case OperationStatus.TIMEOUT:
                        TestStepExecutionTimedOut(step);
                        break;
                
                    case OperationStatus.FAILURE:
                        TestStepExecutionFailed(step, stepExecutionResult);
                        break;
                
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                OnStepExecuted();

            } while (step.TestStep.RepeatUntilPass && !step.IsPassed && !stepExecutionResult.IsFailure);

            OnStepCompleted(i, step);

            if (stepExecutionResult.IsFailure)
                break;
        }
    }

    private void EvaluateTestStep(TestStepViewModel step, double value)
    {
        if (IsOverflow(value))
        {
            step.Result = "Overflow";
            step.ResultNoFormatting = "Overflow";
            step.IsPassed = false;
            step.Deviation = string.Empty;
            return;
        }

        if (double.IsNegativeInfinity(value))
        {
            step.Result = string.Empty;
            step.ResultNoFormatting = string.Empty;
            step.IsPassed = true;
            step.Deviation = string.Empty;
            return;
        }

        var precision = _projectSettings.ResultPrecision;
        var format = "0." + new string('#', precision);

        step.Result = string.IsNullOrEmpty(step.TestStep.Unit)
            ? Math.Round(value, precision).ToString(format, CultureInfo.CurrentCulture)
            : UnitParser.Format(value, step.TestStep.Unit, precision);

        step.ResultNoFormatting = Math.Round(value, precision).ToString(format, CultureInfo.CurrentCulture);

        var evaluation = _evaluator.Evaluate(step.TestStep, value);
        step.Deviation = $"{evaluation.Deviation:F2} %";
        step.IsPassed = evaluation.IsValid;
    }
    
    private void TestStepExecutionTimedOut(TestStepViewModel step)
    {
        step.Result = "Timeout";
        step.ResultNoFormatting = "Timeout";
        step.IsPassed = false;
        step.Deviation = string.Empty;
    }

    private void TestStepExecutionFailed(TestStepViewModel step, OperationResult<double> result)
    {
        if (!string.IsNullOrEmpty(result.ErrorMessage))
            _errorService.AddError($"Error in step {step.TestStep.Number}: {result.ErrorMessage}");

        step.Result = string.Empty;
        step.IsPassed = false;
        step.Deviation = string.Empty;
    }

    private bool IsOverflow(double value) =>
        value >= 1E9;

    private void OnTestStarted() =>
        TestStarted?.Invoke();
    
    private void OnStepStarted(int index, TestStepViewModel step) =>
        StepStarted?.Invoke(index, step);
    
    private void OnStepExecuted() =>
        StepExecuted?.Invoke();

    private void OnStepCompleted(int index, TestStepViewModel step) =>
        StepCompleted?.Invoke(index, step);

    private void OnTestCompleted() =>
        TestCompleted?.Invoke();

    private void OnTestCancelled()
    {
        TestCancelled?.Invoke();
        _repeatTest = false;
    }
    
    private void OnTestRepeated() =>
        TestRepeated?.Invoke();
}