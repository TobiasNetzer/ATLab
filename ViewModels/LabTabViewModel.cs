using System;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class LabTabViewModel : ViewModelBase
{
    private readonly IErrorService _errorService;
    private readonly ITestHardware _testHardware;
    private readonly ISimulationService _simulationService;

    [ObservableProperty]
    private TestHardwareRelayChannelsViewModel _testHardwareRelayChannels;
    
    private readonly RelayGroup _testStimState;
    private readonly RelayGroup _testExtStimState;
    private readonly RelayMatrix _testMatrixState;

    public LabTabViewModel(IErrorService errorService, ITestHardware testHardware, TestHardwareRelayChannelsViewModel testHardwareRelayChannels, ISimulationService simulationService)
    {
        _errorService = errorService;
        _testHardware = testHardware;
        TestHardwareRelayChannels = testHardwareRelayChannels;
        _simulationService = simulationService;
        
        Title = "Lab";
        
        _testStimState = new RelayGroup(_testHardware.HardwareInfo.StimChannelCount);
        _testExtStimState = new RelayGroup(_testHardware.HardwareInfo.ExtStimChannelCount);
        _testMatrixState = new RelayMatrix(0,0);
        LoadLabTabState();
    }
    
    public LabTabViewModel()
    {
        _errorService = new Services.ErrorService();
        _testHardware = new CTIA.CtiaHardware(new Services.SimulationService());
        TestHardwareRelayChannels = new TestHardwareRelayChannelsViewModel(_testHardware.HardwareInfo);
        _simulationService = new Services.SimulationStateService { IsSimulationMode = true };
        
        _testStimState = new RelayGroup(_testHardware.HardwareInfo.StimChannelCount);
        _testExtStimState = new RelayGroup(_testHardware.HardwareInfo.ExtStimChannelCount);
        _testMatrixState = new RelayMatrix(0,0);
    }
    
    [ObservableProperty]
    private bool _isBusy;

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

    public void LoadLabTabState()
    {
        TestHardwareRelayChannels.StimChannelViewModel.LoadRelayStates(_testStimState);
        TestHardwareRelayChannels.ExtStimChannelViewModel.LoadRelayStates(_testExtStimState);
        TestHardwareRelayChannels.MeasChannelViewModel.LoadActiveMeasChannels(_testMatrixState);
    }
}