using System.Collections.ObjectModel;
using ATLab.Interfaces;
using ATLab.Wrappers;

namespace ATLab.ViewModels;

public class StimChannelViewModel : ViewModelBase
{
    public ObservableCollection<RelayChannelViewModel> StimChannels { get; } = new();

    public StimChannelViewModel(ITestHardware testHardware)
    {
        var stimGroup = new StimChannelGroup(testHardware);

        for (int i = 0; i < testHardware.HardwareInfo.StimChannelCount; i++)
        {
            StimChannels.Add(new RelayChannelViewModel(stimGroup, i, $"STIM CH"));
        }
    }
}
