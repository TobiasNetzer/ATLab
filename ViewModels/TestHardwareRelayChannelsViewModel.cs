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
    
    public TestHardwareRelayChannelsViewModel()
    {
        StimChannelNames = new ObservableCollection<CustomRelayChannelName>();
        for (int i = 0; i < 16; i++)
        {
            StimChannelNames.Add(new CustomRelayChannelName("Channel", i));
        }
        
        StimChannelViewModel = new StimChannelViewModel(StimChannelNames);
        
        ExtStimChannelNames = new ObservableCollection<CustomRelayChannelName>();
        for (int i = 0; i < 4; i++)
        {
            ExtStimChannelNames.Add(new CustomRelayChannelName("Channel", i));
        }
        ExtStimChannelViewModel = new ExtStimChannelViewModel(ExtStimChannelNames);
        
        MeasChannelNames = new ObservableCollection<CustomRelayChannelName>();
        for (int i = 0; i < 32; i++)
        {
            MeasChannelNames.Add(new CustomRelayChannelName("Channel", i));
        }
        MeasChannelViewModel = new MeasChannelViewModel(MeasChannelNames);
    }

    public void LoadRelayStatesForTestStep()
    {
        
    }
    
}