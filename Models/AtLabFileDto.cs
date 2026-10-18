using System.Collections.Generic;

namespace ATLab.Models;

public class AtlabFileDto
{
    public const int CurrentFormatVersion = 2;

    // Files written before modules existed have no version and deserialize as 0
    public int FormatVersion { get; init; }
    public List<TestModule> Modules { get; init; } = new();
    public List<CustomRelayChannelName> StimChannelNames { get; init; } = new();
    public List<CustomRelayChannelName> ExtStimChannelNames { get; init; } = new();
    public List<CustomRelayChannelName> MeasChannelNames { get; init; } = new();
    public List<CustomVariable> RuntimeVariables { get; init; } = new();
    public List<Device> Devices { get; init; } = new();
    public ProjectSettings ProjectSettings { get; init; } = new();
    public ProjectDocumentation ProjectDocumentation { get; init; } = new();
    public DeviceUnderTestInfo DeviceUnderTestInfo { get; init; } = new();
    public string? ScriptFolderPath { get; init; }
}