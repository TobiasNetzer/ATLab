using ATLab.Interfaces;

namespace ATLab.Wrappers;

public class MeasHChannelGroup : IChannelGroup
{
    private readonly ITestHardware _hardware;
    public MeasHChannelGroup(ITestHardware hardware) => _hardware = hardware;

    public bool this[int index]
    {
        get => _hardware.MeasChannelStatesH[index];
        set => _hardware.MeasChannelStatesH[index] = value;
    }

    public void CommitChanges()
    {
        
    }
}