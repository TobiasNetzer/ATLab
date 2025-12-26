using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

public partial class RelayMatrix : ObservableObject
{
    [ObservableProperty]
    private int _activeChannelHigh;
    
    [ObservableProperty]
    private int _activeChannelLow;

    public RelayMatrix(int activeChannelHigh, int activeChannelLow)
    {
        ActiveChannelHigh = activeChannelHigh;
        ActiveChannelLow = activeChannelLow;
    }
    
    public RelayMatrix(RelayMatrix other)
    {
        ActiveChannelHigh = other.ActiveChannelHigh;
        ActiveChannelLow = other.ActiveChannelLow;
    }
    
    public RelayMatrix() {}
}