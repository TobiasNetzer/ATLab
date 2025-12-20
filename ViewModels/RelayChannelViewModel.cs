using CommunityToolkit.Mvvm.ComponentModel;
using ATLab.Models;

namespace ATLab.ViewModels;

public partial class RelayChannelViewModel : ViewModelBase
{
    private readonly RelayChannelState _state;

    [ObservableProperty]
    private CustomRelayChannelName _customName;

    [ObservableProperty]
    private int _channelIndex;

    [ObservableProperty]
    private bool _isEnabled;
    
    public RelayChannelViewModel(RelayChannelState state, CustomRelayChannelName channelName)
    {
        _state = state;
        ChannelIndex = state.ChannelIndex;
        CustomName = channelName;
        IsEnabled = state.IsEnabled;
    }

    partial void OnIsEnabledChanged(bool value)
    {
        _state.IsEnabled = value;
    }
}