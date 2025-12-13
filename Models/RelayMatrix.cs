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
}