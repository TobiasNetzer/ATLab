using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
    private readonly ProjectSettings _projectSettings;
    private readonly ICommandExecutor _commandExecutor;

    private CancellationTokenSource? _cts;
    private bool _repeatTest;

    private readonly SemaphoreSlim _relaySemaphore = new(1, 1);
    private bool _relayStatesCleared;

    public event Action? TestStarted;
    public event Action<int, TestStepViewModel>? StepStarted;
    public event Action<int, TestStepViewModel>? StepCompleted;
    public event Action? StepRepeated;
    public event Action? TestCompleted;
    public event Action? TestCancelled;
    public event Action? TestRepeated;
    
    private const int MinRepeatDelayMs = 5;
    private volatile bool _breakRepeatRequested;

    public TestExecutor(
        ITestHardware testHardware,
        ITestStepRunner runner,
        IErrorService errorService,
        ITestStepEvaluator evaluator,
        IMessageBoxService messageBoxService,
        ProjectSettings projectSettings,
        ICommandExecutor commandExecutor)
    {
        _testHardware = testHardware;
        _runner = runner;
        _errorService = errorService;
        _evaluator = evaluator;
        _messageBoxService = messageBoxService;
        _projectSettings = projectSettings;
        _commandExecutor = commandExecutor;
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
        }
        finally
        {
            _relaySemaphore.Release();
        }
    }
    
    public async Task StartTestAsync(IReadOnlyList<TestStepViewModel> steps, int startIndex, List<CustomVariable> runtimeVariables)
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
        
        await Task.Delay(200);

        _cts = new CancellationTokenSource();
        OnTestStarted();

        try
        {
            await ExecuteAsync(steps, startIndex, runtimeVariables, _cts.Token);
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
            await EnsureRelayStatesClearedAsync();
            await _commandExecutor.ReleaseDeviceAsync();
            OnTestCompleted();
            _cts?.Dispose();
            _cts = null;
        }
    }

    public async Task StartRepeatTestAsync(IReadOnlyList<TestStepViewModel> steps, int startIndex, List<CustomVariable> runtimeVariables)
    {
        _repeatTest = true;

        while (_repeatTest)
        {
            OnTestRepeated();
            await StartTestAsync(steps, startIndex, runtimeVariables);
        }
    }
    
    public async Task StartSingleStepTest(TestStepViewModel step, List<CustomVariable> runtimeVariables)
    {
        _cts = new CancellationTokenSource();

        ResetRelayClearFlag();
        
        try
        {
            var stepModel = step.TestStep;
            
            var nominal = ResolveNumeric(
                stepModel.NominalValueExpression,
                stepModel.NominalValue,
                stepModel.Unit,
                runtimeVariables);

            var lower = ResolveNumeric(
                stepModel.LowerLimitExpression,
                stepModel.LowerLimit,
                stepModel.Unit,
                runtimeVariables);

            var upper = ResolveNumeric(
                stepModel.UpperLimitExpression,
                stepModel.UpperLimit,
                stepModel.Unit,
                runtimeVariables);

            var delaySeconds = ResolveNumeric(
                stepModel.DelayExpression,
                stepModel.Delay / 1000.0,
                "s",
                runtimeVariables);
            
            stepModel.NominalValue = nominal;
            stepModel.LowerLimit = lower;
            stepModel.UpperLimit = upper;
            stepModel.Delay = (int)Math.Round(delaySeconds * 1000);
            
            var result = await _runner.ExecuteAsync(step, runtimeVariables, _cts.Token);

            if (_cts.Token.IsCancellationRequested)
                return;

            switch (result.Status)
            {
                case OperationStatus.SUCCESS:
                    EvaluateTestStep(step, result.Value, runtimeVariables);
                    break;
                
                case OperationStatus.TIMEOUT:
                    TestStepExecutionTimedOut(step, runtimeVariables);
                    break;
                
                case OperationStatus.FAILURE:
                    TestStepExecutionFailed(step, result, runtimeVariables);
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
        await _commandExecutor.ReleaseDeviceAsync();
    }
    
    public void RequestBreakRepeat() => _breakRepeatRequested = true;
    
    private async Task ExecuteAsync(
        IReadOnlyList<TestStepViewModel> steps,
        int startIndex,
        List<CustomVariable> runtimeVariables,
        CancellationToken token)
    {
        for (var i = startIndex; i < steps.Count; i++)
        {
            var step = steps[i];

            if (step.TestStep.IsIgnoreStep)
                continue;

            OnStepStarted(i, step);

            OperationResult<double> stepExecutionResult;

            if (step.TestStep.IsShowComment && step.TestStep.EvaluationSource != TestEvaluationSource.USER_RESPONSE)
            {
                var result = await _messageBoxService.ShowConfirmationImageAsync(
                    "Awaiting User Response",
                    step.TestStep.Comment,
                    step.TestStep.CustomMessageBoxImagePath,
                    "Ok",
                    "Cancel",
                    true);

                if (!result)
                    throw new OperationCanceledException();
            }
            
            var stepModel = step.TestStep;
            
            var nominal = ResolveNumeric(
                stepModel.NominalValueExpression,
                stepModel.NominalValue,
                stepModel.Unit,
                runtimeVariables);

            var lower = ResolveNumeric(
                stepModel.LowerLimitExpression,
                stepModel.LowerLimit,
                stepModel.Unit,
                runtimeVariables);

            var upper = ResolveNumeric(
                stepModel.UpperLimitExpression,
                stepModel.UpperLimit,
                stepModel.Unit,
                runtimeVariables);

            var delaySeconds = ResolveNumeric(
                stepModel.DelayExpression,
                stepModel.Delay / 1000.0,
                "s",
                runtimeVariables);
            
            stepModel.NominalValue = nominal;
            stepModel.LowerLimit = lower;
            stepModel.UpperLimit = upper;
            stepModel.Delay = (int)Math.Round(delaySeconds * 1000);
            
            stepExecutionResult = await _runner.ExecuteAsync(step, runtimeVariables, token);

            token.ThrowIfCancellationRequested();

            switch (stepExecutionResult.Status)
            {
                case OperationStatus.SUCCESS:
                    EvaluateTestStep(step, stepExecutionResult.Value, runtimeVariables);
                    break;
            
                case OperationStatus.TIMEOUT:
                    TestStepExecutionTimedOut(step, runtimeVariables);
                    break;
            
                case OperationStatus.FAILURE:
                    TestStepExecutionFailed(step, stepExecutionResult, runtimeVariables);
                    break;
            
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            var nextIndex = EvaluateNextStepIndex(steps, i, step);

            if (i == nextIndex)
            {
                OnStepRepeated();
                
                if (_breakRepeatRequested)
                {
                    _breakRepeatRequested = false;
                    nextIndex = i + 1;
                }
            }
            else
            {
                _breakRepeatRequested = false;
                OnStepCompleted(i, step);
            }
            
            if (stepExecutionResult.IsFailure)
                break; // END_TEST
            
            if (nextIndex == null)
                break; // END_TEST

            i = nextIndex.Value - 1;
            
            if ((step.TestStep.OnPass.Mode == PassFailMode.REPEAT && step.TestStep.Delay <= 0) || (step.TestStep.OnFail.Mode == PassFailMode.REPEAT && step.TestStep.Delay <= 0))
            {
                await Task.Delay(MinRepeatDelayMs, token); // minimum delay between repeats to prevent UI lockup
            }
        }
    }

    private void EvaluateTestStep(TestStepViewModel step, double value, List<CustomVariable> runtimeVariables)
    {
        var variable = runtimeVariables.FirstOrDefault(v => v.Name == step.TestStep.VariableName);
        
        if (IsOverflow(value))
        {
            step.Result = "Overflow";
            step.ResultNoFormatting = "Overflow";
            step.IsPassed = false;
            step.Deviation = string.Empty;
            variable?.Value = "Overflow";
            return;
        }

        if (double.IsNegativeInfinity(value))
        {
            step.Result = string.Empty;
            step.ResultNoFormatting = string.Empty;
            step.IsPassed = true;
            step.Deviation = string.Empty;
            variable?.Value = string.Empty;
            return;
        }

        var precision = _projectSettings.DisplayedDecimalPlaces;
        var format = "0." + new string('#', precision);

        step.Result = string.IsNullOrEmpty(step.TestStep.Unit)
            ? Math.Round(value, precision).ToString(format, CultureInfo.CurrentCulture)
            : UnitParser.Format(value, step.TestStep.Unit, precision);

        step.ResultNoFormatting = Math.Round(value, 12).ToString(CultureInfo.CurrentCulture);

        var evaluation = _evaluator.Evaluate(step.TestStep, value);
        step.Deviation = $"{evaluation.Deviation:F2} %";
        step.IsPassed = evaluation.IsValid;
        variable?.Value = step.ResultNoFormatting;
    }
    
    private void TestStepExecutionTimedOut(TestStepViewModel step, List<CustomVariable> runtimeVariables)
    {
        step.Result = "Timeout";
        step.ResultNoFormatting = "Timeout";
        step.IsPassed = false;
        step.Deviation = string.Empty;
        
        var variable = runtimeVariables.FirstOrDefault(v => v.Name == step.TestStep.VariableName);
        variable?.Value = "Timeout";
    }

    private void TestStepExecutionFailed(TestStepViewModel step, OperationResult<double> result, List<CustomVariable> runtimeVariables)
    {
        if (!string.IsNullOrEmpty(result.ErrorMessage))
            _errorService.AddError($"Error in step {step.TestStep.Number}: {result.ErrorMessage}");

        step.Result = string.Empty;
        step.IsPassed = false;
        step.Deviation = string.Empty;
    }
    
    private static int FindStepIndexById(IReadOnlyList<TestStepViewModel> steps, string id)
    {
        for (var i = 0; i < steps.Count; i++)
            if (steps[i].TestStep.Id == id)
                return i;

        return -1;
    }
    
    private int? EvaluateNextStepIndex(
        IReadOnlyList<TestStepViewModel> steps,
        int currentIndex,
        TestStepViewModel step)
    {
        var action = step.IsPassed ? step.TestStep.OnPass : step.TestStep.OnFail;
        
        if (action.IsInvertResult)
            step.IsPassed = !step.IsPassed;

        switch (action.Mode)
        {
            case PassFailMode.CONTINUE:
                return currentIndex + 1;

            case PassFailMode.REPEAT:
                return currentIndex;

            case PassFailMode.END_TEST:
                return null; // signals end test

            case PassFailMode.JUMP_TO:
                var targetIndex = FindStepIndexById(steps, action.JumpToId);
                
                if (targetIndex < 0)
                {
                    _errorService.AddError($"Step {step.TestStep.Number} {step.TestStep.Name} tries to jump to a non-existent step.");
                    step.IsPassed = false;
                    return null;
                }

                if (steps[targetIndex].TestStep.IsIgnoreStep)
                {
                    _errorService.AddError($"Step {step.TestStep.Number} {step.TestStep.Name} tries to jump to ignored step {steps[targetIndex].TestStep.Number} {steps[targetIndex].TestStep.Name}.");
                    step.IsPassed = false;
                    return null;
                }
                
                return targetIndex;


            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    private double ResolveNumeric(
        string expression,
        double runtimeFallback,
        string unit,
        List<CustomVariable> runtimeVariables)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return runtimeFallback;
        
        if (expression.Contains("{"))
        {
            var resolved = CommandProcessor.CompileToString(expression, runtimeVariables);
            if (UnitParser.TryParse(resolved, out var result, unit))
                return result;

            return runtimeFallback;
        }
        
        if (UnitParser.TryParse(expression, out var litResult, unit))
            return litResult;
        
        return runtimeFallback;
    }

    private bool IsOverflow(double value) =>
        value >= 9.9E37;

    private void OnTestStarted() =>
        TestStarted?.Invoke();
    
    private void OnStepStarted(int index, TestStepViewModel step) =>
        StepStarted?.Invoke(index, step);

    private void OnStepCompleted(int index, TestStepViewModel step) =>
        StepCompleted?.Invoke(index, step);
    
    private void OnStepRepeated() =>
        StepRepeated?.Invoke();

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