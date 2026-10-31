using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

/// <summary>
/// Matrix channels connected to the measurement bus (high and low side) and whether the external probe is used.
/// </summary>
public partial class RelayMatrix : ObservableObject
{
    [ObservableProperty]
    private int _activeChannelHigh;
    
    [ObservableProperty]
    private int _activeChannelLow;

    [ObservableProperty]
    private bool _isExternalProbe;

    public RelayMatrix(int activeChannelHigh, int activeChannelLow)
    {
        ActiveChannelHigh = activeChannelHigh;
        ActiveChannelLow = activeChannelLow;
    }
    
    public RelayMatrix(RelayMatrix other)
    {
        ActiveChannelHigh = other.ActiveChannelHigh;
        ActiveChannelLow = other.ActiveChannelLow;
        IsExternalProbe = other.IsExternalProbe;
    }
    
    public RelayMatrix() {}
}