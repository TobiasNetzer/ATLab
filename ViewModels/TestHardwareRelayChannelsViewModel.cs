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
    
    public TestHardwareRelayChannelsViewModel(IHardwareInfo hardwareInfo)
    {
        StimChannelNames = new ObservableCollection<CustomRelayChannelName>();
        for (int i = 0; i < hardwareInfo.StimChannelCount; i++)
        {
            StimChannelNames.Add(new CustomRelayChannelName("Channel", i));
        }
        
        StimChannelViewModel = new StimChannelViewModel(StimChannelNames);
        
        ExtStimChannelNames = new ObservableCollection<CustomRelayChannelName>();
        for (int i = 0; i < hardwareInfo.ExtStimChannelCount; i++)
        {
            ExtStimChannelNames.Add(new CustomRelayChannelName("Channel", i));
        }
        ExtStimChannelViewModel = new ExtStimChannelViewModel(ExtStimChannelNames);
        
        MeasChannelNames = new ObservableCollection<CustomRelayChannelName>();
        for (int i = 0; i < hardwareInfo.MeasChannelCount; i++)
        {
            MeasChannelNames.Add(new CustomRelayChannelName("Channel", i));
        }
        MeasChannelViewModel = new MeasChannelViewModel(MeasChannelNames);
    }
    
}