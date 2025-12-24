namespace ATLab.Models;

public class RelayMatrix
{
    public int ActiveChannelHigh {get; set;}
    public int ActiveChannelLow {get; set;}

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