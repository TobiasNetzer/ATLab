using System;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels.Tabs;

public partial class LabTabViewModel : ViewModelBase
{
    private readonly IErrorService _errorService;
    private readonly ITestHardware _testHardware;

    [ObservableProperty]
    private TestHardwareRelayChannelsViewModel _testHardwareRelayChannels;
    
    private RelayGroup _testStimState;
    private RelayGroup _testExtStimState;
    private RelayMatrix _testMatrixState;

    [ObservableProperty]
    private string _title = "Lab";

    public LabTabViewModel(IErrorService errorService, ITestHardware testHardware, TestHardwareRelayChannelsViewModel testHardwareRelayChannels)
    {
        _errorService = errorService;
        _testHardware = testHardware;
        TestHardwareRelayChannels = testHardwareRelayChannels;
        
        _testStimState = new RelayGroup(_testHardware.HardwareInfo.StimChannelCount);
        TestHardwareRelayChannels.StimChannelViewModel.LoadRelayStates(_testStimState);
        _testExtStimState = new RelayGroup(_testHardware.HardwareInfo.ExtStimChannelCount);
        TestHardwareRelayChannels.ExtStimChannelViewModel.LoadRelayStates(_testExtStimState);
        _testMatrixState = new RelayMatrix(0,0);
        TestHardwareRelayChannels.MeasChannelViewModel.LoadActiveMeasChannels(_testMatrixState);
    }
    
    public LabTabViewModel()
    {
        TestHardwareRelayChannels = new TestHardwareRelayChannelsViewModel(new DummyHardwareInfo());
    }
    
    [ObservableProperty]
    private bool _isBusy;

    private bool CanUpdateRelayStates() => !App.SimulationMode && !IsBusy;

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
}