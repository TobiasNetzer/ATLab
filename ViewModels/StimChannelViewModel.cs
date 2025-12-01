using System.Collections.ObjectModel;
using ATLab.Interfaces;

namespace ATLab.ViewModels;

public class StimChannelViewModel : ViewModelBase
{
    public ObservableCollection<RelayChannelViewModel> StimChannels { get; } = new();

    public StimChannelViewModel(ITestHardware testHardware)
    {
            for (int i = 0; i < testHardware.HardwareInfo.StimChannelCount; i++)
            {
                StimChannels.Add(new RelayChannelViewModel(testHardware, i, $"STIM CH {i + 1}"));
            }
    }
}
