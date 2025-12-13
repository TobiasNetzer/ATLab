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

    public bool IsEnabled
    {
        get => _state.IsEnabled;
        set
        {
            if (_state.IsEnabled != value)
            {
                _state.IsEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    public RelayChannelViewModel(RelayChannelState state, CustomRelayChannelName channelName)
    {
        _state = state;
        ChannelIndex = state.ChannelIndex;
        CustomName = channelName;
    }
}