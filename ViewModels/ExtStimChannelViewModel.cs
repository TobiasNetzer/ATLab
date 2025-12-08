using System.Collections.ObjectModel;
using ATLab.Interfaces;
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
            ExtStimChannels.Add(new RelayChannelViewModel(extStimGroup, i, $"EXT STIM CH"));
        }
    }
}