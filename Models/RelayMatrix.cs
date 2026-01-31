using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

public partial class RelayMatrix : ObservableObject
{
    [ObservableProperty]
    private int _activeChannelHigh;
    
    [ObservableProperty]
    private int _activeChannelLow;

    [ObservableProperty]
    private bool _useExternalProbe;

    public RelayMatrix(int activeChannelHigh, int activeChannelLow)
    {
        ActiveChannelHigh = activeChannelHigh;
        ActiveChannelLow = activeChannelLow;
    }
    
    public RelayMatrix(RelayMatrix other)
    {
        ActiveChannelHigh = other.ActiveChannelHigh;
        ActiveChannelLow = other.ActiveChannelLow;
        UseExternalProbe = other.UseExternalProbe;
    }
    
    public RelayMatrix() {}
}