using System.Collections.ObjectModel;
using System.Linq;
using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class TestHardwareRelayChannelsViewModel : ViewModelBase
{
    public StimChannelViewModel StimChannelViewModel { get; }
    public ExtStimChannelViewModel ExtStimChannelViewModel { get; }
    public MeasChannelViewModel MeasChannelViewModel { get; }
    
    public TestHardwareRelayChannelsViewModel(ITestHardware testHardware)
    {
        StimChannelViewModel = new StimChannelViewModel(testHardware);
        ExtStimChannelViewModel = new ExtStimChannelViewModel(testHardware);
        MeasChannelViewModel = new MeasChannelViewModel(testHardware);
    }

    public TestHardwareRelayChannelsViewModel()
    {
        StimChannelViewModel = new StimChannelViewModel();
        ExtStimChannelViewModel = new ExtStimChannelViewModel();
        MeasChannelViewModel = new MeasChannelViewModel();
    }
    
}