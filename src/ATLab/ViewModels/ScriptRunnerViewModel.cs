using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Helpers;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.Services;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

/// <summary>
/// Script runner of the Scripting tab: runs the selected script, including unsaved changes, on a device and shows
/// every response with the outcome of its response check. Not available while a test is running.
/// </summary>
public partial class ScriptRunnerViewModel : ViewModelBase
{
    private readonly IScriptRunner _scriptRunner;
    private readonly ICommandExecutor _commandExecutor;
    private readonly ProjectModel _projectModel;

    private CancellationTokenSource? _cts;
    private List<ScriptCommandResult> _lastResults = [];

    public ObservableCollection<Device> Devices => _projectModel.Devices;

    public List<ResponseDisplayMode> DisplayModes { get; } = Enum.GetValues<ResponseDisplayMode>().ToList();

    public ObservableCollection<ScriptCommandResultRowViewModel> Results { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private ScriptViewModel? _script;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private Device? _selectedDevice;

    [ObservableProperty]
    private ResponseDisplayMode _displayMode = ResponseDisplayMode.ASCII;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand), nameof(StopCommand))]
    private bool _isRunning;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private bool _isTestRunning;

    [ObservableProperty]
    private string? _summary;
    
    [ObservableProperty]
    private bool? _isRunPassed;

    public ScriptRunnerViewModel(
        IScriptRunner scriptRunner,
        ICommandExecutor commandExecutor,
        ITestExecutor testExecutor,
        ProjectModel projectModel)
    {
        _scriptRunner = scriptRunner;
        _commandExecutor = commandExecutor;
        _projectModel = projectModel;

        SelectedDevice = Devices.FirstOrDefault();
        Devices.CollectionChanged += (_, _) =>
        {
            if (SelectedDevice is null || !Devices.Contains(SelectedDevice))
                SelectedDevice = Devices.FirstOrDefault();
        };

        testExecutor.TestStarted += () => Dispatcher.UIThread.Post(() => IsTestRunning = true);
        testExecutor.TestCompleted += () =>
        {
            Dispatcher.UIThread.Post(() => IsTestRunning = false);
            return Task.CompletedTask;
        };
    }

    partial void OnScriptChanged(ScriptViewModel? value) => ClearResults();

    partial void OnDisplayModeChanged(ResponseDisplayMode value) => ShowResults();

    private bool CanRun() => Script != null && SelectedDevice != null && !IsRunning && !IsTestRunning;

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task Run()
    {
        if (Script is null || SelectedDevice is null)
            return;

        ClearResults();
        IsRunning = true;
        _cts = new CancellationTokenSource();

        var results = new List<ScriptCommandResult>();
        var script = Script.CreateSnapshot();
        var runtimeVariables = _projectModel.RuntimeVariables.Select(v => v.Clone()).ToList();

        try
        {
            var result = await _scriptRunner.ExecuteScriptAsync(
                script, SelectedDevice.Id, script.Variables, _cts.Token, runtimeVariables, results);

            // The panel does not hold the port between runs, the instrument stays usable for other tools
            await _commandExecutor.ReleaseDeviceAsync();

            _lastResults = results;
            ShowResults();
            Summarize(result, results, _cts.IsCancellationRequested);
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            IsRunning = false;
        }
    }

    [RelayCommand(CanExecute = nameof(IsRunning))]
    private void Stop() => _cts?.Cancel();

    private void Summarize(OperationResult<byte[]> result, List<ScriptCommandResult> results, bool isCanceled)
    {
        var localization = LocalizationService.Instance;
        var last = results.LastOrDefault();

        if (isCanceled)
        {
            Summary = localization["Text_ScriptRunStopped"];
            IsRunPassed = null;
            return;
        }

        if (result.IsTimeout)
        {
            Summary = string.Format(localization["Text_ScriptRunTimeout"], last?.Number);
            IsRunPassed = false;
            return;
        }

        if (result.IsFailure)
        {
            Summary = string.Format(localization["Text_ScriptRunError"], result.ErrorMessage);
            IsRunPassed = false;
            return;
        }

        var checkedCount = results.Count(r => r.IsCheckPassed.HasValue);
        var failedCount = results.Count(r => r.IsCheckPassed == false);

        if (checkedCount == 0)
        {
            Summary = localization["Text_ScriptRunNoChecks"];
            IsRunPassed = null;
        }
        else if (failedCount == 0)
        {
            Summary = string.Format(localization["Text_ScriptRunChecksPassed"], checkedCount);
            IsRunPassed = true;
        }
        else
        {
            Summary = string.Format(localization["Text_ScriptRunChecksFailed"], failedCount, checkedCount);
            IsRunPassed = false;
        }
    }

    private void ShowResults()
    {
        Results.Clear();

        foreach (var result in _lastResults)
            Results.Add(new ScriptCommandResultRowViewModel(result, DisplayMode));
    }

    private void ClearResults()
    {
        _lastResults = [];
        Results.Clear();
        Summary = null;
        IsRunPassed = null;
    }
}

/// <summary>One row of a script runner result: a command, its response and the outcome of its check.</summary>
public class ScriptCommandResultRowViewModel
{
    public int Number { get; }
    public string Command { get; }
    public string Response { get; }
    
    public string? ResponseToolTip => string.IsNullOrEmpty(Response) ? null : Response;

    public string Value { get; }
    public string Status { get; }
    public string? Message { get; }
    
    public bool? IsPassed { get; }

    public ScriptCommandResultRowViewModel(ScriptCommandResult result, ResponseDisplayMode displayMode)
    {
        var localization = LocalizationService.Instance;

        Number = result.Number;
        Command = result.Command;
        Response = FormatResponse(result.Response, displayMode);
        Value = result.CheckValue ?? string.Empty;
        Message = result.CheckMessage;
        IsPassed = result.IsCheckPassed;

        if (result.IsExpectResponse && result.Response is null)
        {
            // The command that was running when the run ended with a timeout or an error
            Status = localization["Text_ScriptRunNoResponse"];
            IsPassed = false;
        }
        else if (result.IsCheckPassed.HasValue)
        {
            Status = result.IsCheckPassed.Value ? localization["Text_CheckPassed"] : localization["Text_CheckFailed"];
        }
        else if (result.IsEvaluate)
        {
            Status = localization["Header_Evaluate"];
        }
        else
        {
            Status = string.Empty;
        }
    }

    // Line endings and other control characters are made visible, they often decide whether a mask matches
    private static string FormatResponse(byte[]? response, ResponseDisplayMode displayMode)
    {
        var text = ResponseProcessor.Format(response, displayMode);

        if (displayMode != ResponseDisplayMode.ASCII)
            return text;

        return text
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");
    }
}