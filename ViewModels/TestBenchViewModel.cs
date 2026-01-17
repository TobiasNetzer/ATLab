using System;
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

    [ObservableProperty]
    private TestHardwareRelayChannelsViewModel _testHardwareRelayChannels;

    [ObservableProperty]
    private ScriptSelectorViewModel _scriptSelectorViewModel;
    
    [ObservableProperty]
    private Device? _selectedDevice;
    
    [ObservableProperty]
    private ScriptItemViewModel? _selectedScript;
    
    [ObservableProperty]
    private bool _isBusy;
        
    private readonly RelayGroup _testStimState;
    private readonly RelayGroup _testExtStimState;
    private readonly RelayMatrix _testMatrixState;

    public TestBenchViewModel(
        IErrorService errorService,
        ITestHardware testHardware,
        TestHardwareRelayChannelsViewModel testHardwareRelayChannels,
        ISimulationService simulationService,
        IScriptRunner scriptRunner,
        ScriptSelectorViewModel scriptSelector)
    {
        _errorService = errorService;
        _testHardware = testHardware;
        TestHardwareRelayChannels = testHardwareRelayChannels;
        _simulationService = simulationService;
        _scriptRunner = scriptRunner;
        ScriptSelectorViewModel = scriptSelector;
        
        ScriptSelectorViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName is nameof(ScriptSelectorViewModel.SelectedDevice) 
                               or nameof(ScriptSelectorViewModel.SelectedScript))
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
    
    private bool CanUpdateRelayStates() => !_simulationService.IsSimulationMode && !IsBusy;

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
    
    private bool CanRunScript() => 
        ScriptSelectorViewModel.SelectedDevice != null && 
        ScriptSelectorViewModel.SelectedScript != null && 
        !IsBusy;
    
    [RelayCommand(CanExecute = nameof(CanRunScript))]
    private async Task RunScript()
    {
        if (ScriptSelectorViewModel.SelectedDevice == null || ScriptSelectorViewModel.SelectedScript == null) return;

        IsBusy = true;
        try
        {
            var result = await _scriptRunner.ExecuteAsync(
                ScriptSelectorViewModel.SelectedScript.Id, 
                ScriptSelectorViewModel.SelectedDevice.Name, 
                ScriptSelectorViewModel.SelectedScript.Variables, 
                CancellationToken.None);

            if (!result.IsSuccess)
            {
                _errorService.AddError($"Failed to run script: {result.ErrorMessage}");
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
    partial void OnIsBusyChanged(bool value) => RunScriptCommand.NotifyCanExecuteChanged();
}