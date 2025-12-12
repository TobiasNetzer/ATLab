using System.Collections.ObjectModel;
using ATLab.CTIA;
using ATLab.Interfaces;
using ATLab.Services;
using ATLab.Wrappers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class ExtStimChannelViewModel : ViewModelBase
{
    public ObservableCollection<RelayChannelViewModel> ExtStimChannels { get; }
    
    [ObservableProperty]
    private bool _isExpanded;

    public ExtStimChannelViewModel(ITestHardware testHardware)
    {
        var extStimGroup = new ExtStimChannelGroup(testHardware);

        ExtStimChannels = new ObservableCollection<RelayChannelViewModel>();

        for (int i = 0; i < testHardware.HardwareInfo.ExtStimChannelCount; i++)
        {
            ExtStimChannels.Add(new RelayChannelViewModel(extStimGroup, i, ""));
        }
    }

    public ExtStimChannelViewModel()
    {
        ExtStimChannels = new ObservableCollection<RelayChannelViewModel>();
        var extStimGroup = new ExtStimChannelGroup(new CtiaHardware(new SimulationService()));
        
        for (int i = 0; i < 4; i++)
        {
            ExtStimChannels.Add(new RelayChannelViewModel(extStimGroup, i, $""));
        }
    }
}