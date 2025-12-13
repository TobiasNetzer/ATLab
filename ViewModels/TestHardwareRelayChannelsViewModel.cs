using System.Collections.ObjectModel;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.ViewModels;

public partial class TestHardwareRelayChannelsViewModel : ViewModelBase
{
    private ObservableCollection<CustomRelayChannelName> StimChannelNames { get; }
    private ObservableCollection<CustomRelayChannelName> ExtStimChannelNames { get; }
    private ObservableCollection<CustomRelayChannelName> MeasChannelNames { get; }
    
    public StimChannelViewModel StimChannelViewModel { get; }
    public ExtStimChannelViewModel ExtStimChannelViewModel { get; }
    public MeasChannelViewModel MeasChannelViewModel { get; }
    
    public TestHardwareRelayChannelsViewModel(ITestHardware testHardware)
    {
        StimChannelNames = new ObservableCollection<CustomRelayChannelName>();
        for (int i = 0; i < 16; i++)
        {
            StimChannelNames.Add(new CustomRelayChannelName("Channel", i));
        }
        var TestStimState = new RelayGroup(16);
        StimChannelViewModel = new StimChannelViewModel(StimChannelNames);
        StimChannelViewModel.LoadRelayStates(TestStimState);
        
        
        ExtStimChannelNames = new ObservableCollection<CustomRelayChannelName>();
        for (int i = 0; i < 4; i++)
        {
            ExtStimChannelNames.Add(new CustomRelayChannelName("Channel", i));
        }
        var TestExtStimState = new RelayGroup(4);
        ExtStimChannelViewModel = new ExtStimChannelViewModel(ExtStimChannelNames);
        ExtStimChannelViewModel.LoadRelayStates(TestExtStimState);
        
        MeasChannelNames = new ObservableCollection<CustomRelayChannelName>();
        for (int i = 0; i < 32; i++)
        {
            MeasChannelNames.Add(new CustomRelayChannelName("Channel", i));
        }
        var TestMeasState = new RelayGroup(32);
        MeasChannelViewModel = new MeasChannelViewModel(MeasChannelNames);
        MeasChannelViewModel.LoadActiveMeasChannels(1, 2);
    }

    public TestHardwareRelayChannelsViewModel()
    {
        StimChannelViewModel = new StimChannelViewModel();
        ExtStimChannelViewModel = new ExtStimChannelViewModel();
        MeasChannelViewModel = new MeasChannelViewModel();
    }

    public void LoadRelayStatesForTestStep()
    {
        
    }
    
}