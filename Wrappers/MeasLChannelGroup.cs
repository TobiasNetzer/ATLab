using System;
using ATLab.Interfaces;

namespace ATLab.Wrappers;

public class MeasLChannelGroup : IChannelGroup
{
    private readonly ITestHardware _hardware;
    public MeasLChannelGroup(ITestHardware hardware) => _hardware = hardware;

    public bool this[int index]
    {
        get => _hardware.MeasChannelStatesL[index];
        set => _hardware.MeasChannelStatesL[index] = value;
    }

    public void CommitChanges()
    {
        
    }
}