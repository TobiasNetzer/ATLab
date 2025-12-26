using CommunityToolkit.Mvvm.ComponentModel;
using ATLab.Models;

namespace ATLab.ViewModels;

public partial class RelayChannelViewModel : ViewModelBase
{
    [ObservableProperty]
    private RelayChannelState _state;

    [ObservableProperty]
    private CustomRelayChannelName _customName;

    public RelayChannelViewModel(RelayChannelState state, CustomRelayChannelName channelName)
    {
        State = state;
        CustomName = channelName;
    }
}