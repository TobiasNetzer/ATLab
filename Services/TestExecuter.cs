using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
    private CancellationTokenSource? _cts;
    private bool _repeatTest;

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
        IMessageBoxService messageBoxService)
    {
        _testHardware = testHardware;
        _runner = runner;
        _errorService = errorService;
        _evaluator = evaluator;
        _messageBoxService = messageBoxService;
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

    public void CancelTest()
    {
        _cts?.Cancel();
        _repeatTest = false;
    }

    private async Task ExecuteAsync(
    IReadOnlyList<TestStepViewModel> steps,
    int startIndex,
    CancellationToken token)
    {

        for (int i = startIndex; i < steps.Count; i++)
        {
            var step = steps[i];
            OnStepStarted(i, step);
            OperationResult<double> stepExecutionResult;

            if (step.TestStep.ShowCommentOnTestStart)
            {
                var result = await _messageBoxService.ShowConfirmationImageAsync("Test Execution Halted", step.TestStep.Comment, step.TestStep.CustomMessageBoxImagePath);
                if (!result)
                {
                    throw new OperationCanceledException();
                }
            }
            
            do
            {
                stepExecutionResult = await _runner.ExecuteAsync(step, token);
                
                if (token.IsCancellationRequested)
                {
                    throw new OperationCanceledException(token);
                }

                if (stepExecutionResult.IsSuccess)
                {
                    EvaluateTestStep(step, stepExecutionResult.Value);
                }
                else
                {
                    TestStepExecutionFailed(step, stepExecutionResult);
                }

                OnStepExecuted();
                
            } while (step.TestStep.RepeatUntilPass && !step.IsPassed);
            
            OnStepCompleted(i, step);

            if (!stepExecutionResult.IsSuccess)
                break;
        }

        var clearResult = await _testHardware.ClearRelayStates();
        if (!clearResult.IsSuccess) _errorService.AddError($"Error on resetting relay states: {clearResult.ErrorMessage}");
    }

    private void EvaluateTestStep(TestStepViewModel step, double value)
    {
        if (IsOverflow(value))
        {
            // Overflow detected
            step.Result = "Overflow";
            step.IsPassed = false;
            step.Deviation = string.Empty;
            return;
        }

        if (double.IsNegativeInfinity(value))
        {
            // No evaluation source
            step.Result = string.Empty;
            step.IsPassed = true;
            step.Deviation = string.Empty;
            return;
        }

        step.Result = UnitParser.Format(value, step.TestStep.Unit);

        var evaluation = _evaluator.Evaluate(step.TestStep, value);
        step.Deviation = $"{evaluation.Deviation:F2} %";
        step.IsPassed = evaluation.IsValid;
    }

    private void TestStepExecutionFailed(
        TestStepViewModel step,
        OperationResult<double> result)
    {
        if (result.ErrorMessage != string.Empty)
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
