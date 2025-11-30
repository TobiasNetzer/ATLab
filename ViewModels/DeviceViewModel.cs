using System.Collections.ObjectModel;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.ViewModels;

public class DeviceViewModel : ViewModelBase
{
    public ObservableCollection<RelayChannelViewModel> StimChannels { get; } = new();

    public DeviceViewModel(IUniversalTestHardwareInterface testHardware)
    {

        for (int i = 0; i < testHardware.HardwareInfoProvider.StimChannelCount; i++)
        {
            StimChannels.Add(new RelayChannelViewModel(testHardware, i, $"STIM CH {i + 1}"));
        }
    }
}
