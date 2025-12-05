using ATLab.DesignViewModels;
using ATLab.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class LabTabViewModel : ViewModelBase
{
    private ITestHardware _testHardware;
    public StimChannelViewModel StimChannelViewModel { get; }
    public ExtStimChannelViewModel ExtStimChannelViewModel { get; }
    public MeasChannelViewModel MeasChannelViewModel { get; }

    [ObservableProperty]
    private string _title = "Lab";

    public LabTabViewModel(ITestHardware testHardware)
    {
        _testHardware = testHardware;
        StimChannelViewModel = new StimChannelViewModel(testHardware);
        ExtStimChannelViewModel = new  ExtStimChannelViewModel(testHardware);
        MeasChannelViewModel = new  MeasChannelViewModel(testHardware);
        
        
    }
    
    public LabTabViewModel()
    {
        StimChannelViewModel = new StimChannelDesignViewModel();
        ExtStimChannelViewModel = new ExtStimChannelDesignViewModel();

        UpdateTestHardwareRelayStatesCommand.NotifyCanExecuteChanged();
    }
    
    private bool CanUpdateRelayStates() => !App.SimulationMode;

    [RelayCommand(CanExecute = nameof(CanUpdateRelayStates))]
    public void UpdateTestHardwareRelayStates()
    {
        _testHardware.SetStimChannels();
        _testHardware.SetExtStimChannels();
        _testHardware.SetMeasChannelH(MeasChannelViewModel.IsSelectedH);
        _testHardware.SetMeasChannelL(MeasChannelViewModel.IsSelectedL);
    }
}