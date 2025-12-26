using System.Collections.Generic;

namespace ATLab.Models;

public class AtlabFileDto
{
    public List<TestStep> TestSteps { get; set; } = new();
    public List<CustomRelayChannelName> StimChannelNames { get; set; } = new();
    public List<CustomRelayChannelName> ExtStimChannelNames { get; set; } = new();
    public List<CustomRelayChannelName> MeasChannelNames { get; set; } = new();
    public List<SerialDevices> SerialDevices { get; set; } = new();
}
