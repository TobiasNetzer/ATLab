using ATLab.Interfaces;

namespace ATLab.Wrappers;

public class MeasChannelGroup : IChannelGroup
{
    private readonly ITestHardware _hardware;
    public MeasChannelGroup(ITestHardware hardware) => _hardware = hardware;

    public bool this[int index]
    {
        get => _hardware.MeasChannelStates[index];
        set => _hardware.MeasChannelStates[index] = value;
    }

    public void CommitChanges()
    {
        
    }
}