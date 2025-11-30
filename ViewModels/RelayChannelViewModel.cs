using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class RelayChannelViewModel : ViewModelBase
{
    private readonly IUniversalTestHardwareInterface _testHardware;

    private readonly int _channelIndex;

    [ObservableProperty]
    private string _channelName = "";

    [ObservableProperty]
    private bool _isEnabled = false;

    public RelayChannelViewModel(IUniversalTestHardwareInterface testHardware, int channelIndex, string channelName)
    {
        _testHardware = testHardware;
        _channelIndex = channelIndex;
        ChannelName = channelName;
        IsEnabled = _testHardware.StimChannelStates[channelIndex];
    }

    partial void OnIsEnabledChanged(bool value)
    {
        // Update model when UI changes
        _testHardware.StimChannelStates[_channelIndex] = value;
        _testHardware.SetStimChannels();
    }
}