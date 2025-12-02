using ATLab.Interfaces;

namespace ATLab.Wrappers;

public class StimChannelGroup : IChannelGroup
{
    private readonly ITestHardware _hardware;
    public StimChannelGroup(ITestHardware hardware) => _hardware = hardware;

    public bool this[int index]
    {
        get => _hardware.StimChannelStates[index];
        set => _hardware.StimChannelStates[index] = value;
    }

    public void CommitChanges() => _hardware.SetStimChannels();
}