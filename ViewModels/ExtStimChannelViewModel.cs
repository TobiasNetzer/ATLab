using System.Collections.ObjectModel;
using ATLab.Interfaces;
using ATLab.Wrappers;

namespace ATLab.ViewModels;

public class ExtStimChannelViewModel
{
    public ObservableCollection<RelayChannelViewModel> ExtStimChannels { get; } = new();

    public ExtStimChannelViewModel(ITestHardware testHardware)
    {
        var extStimGroup = new ExtStimChannelGroup(testHardware);

        for (int i = 0; i < testHardware.HardwareInfo.ExtStimChannelCount; i++)
        {
            ExtStimChannels.Add(new RelayChannelViewModel(extStimGroup, i, $"EXT STIM CH"));
        }
    }
}