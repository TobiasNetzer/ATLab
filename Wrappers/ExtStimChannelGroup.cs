using ATLab.Interfaces;

namespace ATLab.Wrappers;

public class ExtStimChannelGroup : IChannelGroup
{
    private readonly ITestHardware _hardware;
    public ExtStimChannelGroup(ITestHardware hardware) => _hardware = hardware;

    public bool this[int index]
    {
        get => _hardware.ExtStimChannelStates[index];
        set => _hardware.ExtStimChannelStates[index] = value;
    }

    public void CommitChanges() => _hardware.SetExtStimChannels();
}