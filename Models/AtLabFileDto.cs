using System.Collections.Generic;

namespace ATLab.Models;

public class AtlabFileDto
{
    public List<TestStep> TestSteps { get; init; } = new();
    public List<CustomRelayChannelName> StimChannelNames { get; init; } = new();
    public List<CustomRelayChannelName> ExtStimChannelNames { get; init; } = new();
    public List<CustomRelayChannelName> MeasChannelNames { get; init; } = new();
    public List<CustomVariable> RuntimeVariables { get; init; } = new();
    public List<Device> Devices { get; init; } = new();
    public ProjectSettings ProjectSettings { get; init; } = new();
    public ProjectDocumentation ProjectDocumentation { get; init; } = new();
    public DeviceUnderTestInfo DeviceUnderTestInfo { get; init; } = new();
}
