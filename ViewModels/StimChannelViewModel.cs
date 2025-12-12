using System.Collections.ObjectModel;
using ATLab.CTIA;
using ATLab.Interfaces;
using ATLab.Services;
using ATLab.Wrappers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class StimChannelViewModel : ViewModelBase
{
    public ObservableCollection<RelayChannelViewModel> StimChannels { get; }
    
    [ObservableProperty]
    private bool _isExpanded;

    public StimChannelViewModel(ITestHardware testHardware)
    {
        var stimGroup = new StimChannelGroup(testHardware);

        StimChannels = new ObservableCollection<RelayChannelViewModel>();

        for (int i = 0; i < testHardware.HardwareInfo.StimChannelCount; i++)
        {
            StimChannels.Add(new RelayChannelViewModel(stimGroup, i, ""));
        }
    }
    
    public StimChannelViewModel()
    {
        var stimGroup = new StimChannelGroup(new CtiaHardware(new SimulationService()));

        StimChannels = new ObservableCollection<RelayChannelViewModel>();

        for (int i = 0; i < 16; i++)
        {
            StimChannels.Add(new RelayChannelViewModel(stimGroup, i, ""));
        }
    }
}
