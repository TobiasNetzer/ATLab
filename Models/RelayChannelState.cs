using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

public partial class RelayChannelState : ObservableObject
{
    [ObservableProperty]
    private bool _isEnabled;
    
    public int ChannelIndex { get; init; }

    public RelayChannelState() { }

    public RelayChannelState(RelayChannelState other)
    {
        IsEnabled = other.IsEnabled;
        ChannelIndex = other.ChannelIndex;
    }
}