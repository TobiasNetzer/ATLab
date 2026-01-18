using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class TestBenchViewModel : ViewModelBase
{
    private readonly IErrorService _errorService;
    private readonly ITestHardware _testHardware;
    private readonly ISimulationService _simulationService;
    private readonly IScriptRunner _scriptRunner;
    private readonly IShellCommandRunner _shellCommandRunner;

    [ObservableProperty]
    private TestHardwareRelayChannelsViewModel _testHardwareRelayChannels;
    
    [ObservableProperty]
    private ScriptSelectorViewModel _scriptSelector;
    
    [ObservableProperty]
    private ShellCommandEditorViewModel _shellCommandEditor;
    
    [ObservableProperty]
    private Device? _selectedDevice;
    
    [ObservableProperty]
    private ScriptItemViewModel? _selectedScript;
    
    [ObservableProperty]
    private bool _isBusy;
    
    [ObservableProperty]
    private string _scriptResult = string.Empty;
        
    private readonly RelayGroup _testStimState;
    private readonly RelayGroup _testExtStimState;
    private readonly RelayMatrix _testMatrixState;

    public TestBenchViewModel(
        IErrorService errorService,
        ITestHardware testHardware,
        TestHardwareRelayChannelsViewModel testHardwareRelayChannels,
        ISimulationService simulationService,
        IScriptRunner scriptRunner,
        IShellCommandRunner shellCommandRunner,
        ScriptSelectorViewModel scriptSelector,
        ShellCommandEditorViewModel shellCommandEditor)
    {
        _errorService = errorService;
        _testHardware = testHardware;
        TestHardwareRelayChannels = testHardwareRelayChannels;
        _simulationService = simulationService;
        _scriptRunner = scriptRunner;
        _shellCommandRunner = shellCommandRunner;
        ScriptSelector = scriptSelector;
        ShellCommandEditor = shellCommandEditor;
        
        ScriptSelector.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName is nameof(ScriptSelector.SelectedDevice) 
                               or nameof(ScriptSelector.SelectedScript))
            {
                RunScriptCommand.NotifyCanExecuteChanged();
            }
        };
        
        Title = "Test Bench";
        
        _testStimState = new RelayGroup(_testHardware.HardwareInfo.StimChannelCount);
        _testExtStimState = new RelayGroup(_testHardware.HardwareInfo.ExtStimChannelCount);
        _testMatrixState = new RelayMatrix(0,0);
        LoadTestBenchTabState();
    }

    [RelayCommand(CanExecute = nameof(CanUpdateRelayStates))]
    private async Task UpdateTestHardwareRelayStates()
    {
        _testHardware.StimChannelStates = _testStimState.ToBoolArray();
        _testHardware.ExtStimChannelStates = _testExtStimState.ToBoolArray();
        _testHardware.ActiveMeasChannelH = (byte)(_testMatrixState.ActiveChannelHigh);
        _testHardware.ActiveMeasChannelL = (byte)(_testMatrixState.ActiveChannelLow);
        
        try
        {
            IsBusy = true;
            var result = await _testHardware.UpdateRelayStates();

            if (!result.IsSuccess)
            {
                _errorService.AddError("Relay update failed: " + result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _errorService.AddError("Exception: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void LoadTestBenchTabState()
    {
        TestHardwareRelayChannels.StimChannelViewModel.LoadRelayStates(_testStimState);
        TestHardwareRelayChannels.ExtStimChannelViewModel.LoadRelayStates(_testExtStimState);
        TestHardwareRelayChannels.MeasChannelViewModel.LoadActiveMeasChannels(_testMatrixState);
    }
    
    [RelayCommand(CanExecute = nameof(CanRunScript))]
    private async Task RunScript()
    {
        if (ScriptSelector.SelectedDevice == null || ScriptSelector.SelectedScript == null) return;

        IsBusy = true;
        try
        {
            var result = await _scriptRunner.ExecuteAsync<double>(
                ScriptSelector.SelectedScript.Id, 
                ScriptSelector.SelectedDevice.Name, 
                ScriptSelector.SelectedScript.Variables, 
                CancellationToken.None);

            if (!result.IsSuccess)
            {
                _errorService.AddError($"Failed to run script: {result.ErrorMessage}");
            }
            else
            {
               ScriptResult = result.Value.ToString(CultureInfo.CurrentCulture);
            }
        }
        catch (Exception ex)
        {
            _errorService.AddError($"Unexpected error while running script: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRunShell))]
    private async Task RunShell()
    {
        if (string.IsNullOrWhiteSpace(ShellCommandEditor.ShellCommand.Command))
            return;
        
        IsBusy = true;
        
        try
        {
            var result = await _shellCommandRunner.RunAsync(ShellCommandEditor.ShellCommand.Command,ShellCommandEditor.ShellCommand.Option, CancellationToken.None);

            if (!result.IsSuccess)
            {
                _errorService.AddError($"Failed to run shell command: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            _errorService.AddError($"Unexpected error while running shell command: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private bool CanUpdateRelayStates() => !_simulationService.IsSimulationMode && !IsBusy;
    private bool CanRunScript() => 
        ScriptSelector.SelectedDevice != null && 
        ScriptSelector.SelectedScript != null && 
        !IsBusy;

    private bool CanRunShell() => !IsBusy;

    partial void OnIsBusyChanged(bool value)
    {
        RunScriptCommand.NotifyCanExecuteChanged();
        RunShellCommand.NotifyCanExecuteChanged();
    } 
}