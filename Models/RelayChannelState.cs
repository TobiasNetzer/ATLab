using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

/// <summary>On/off state of one relay channel.</summary>
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