using System.Collections.Generic;

namespace ATLab.Models;

public class AtlabFileDto
{
    public List<TestStep> TestSteps { get; init; } = new();
    public List<CustomRelayChannelName> StimChannelNames { get; init; } = new();
    public List<CustomRelayChannelName> ExtStimChannelNames { get; init; } = new();
    public List<CustomRelayChannelName> MeasChannelNames { get; init; } = new();
    public List<SerialDevices> SerialDevices { get; init; } = new();
    public double DefaultTolerance { get; init; }
    public bool UseSerialNumber { get; init; }
}
