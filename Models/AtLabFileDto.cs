using System.Collections.Generic;

namespace ATLab.Models;

/// <summary>Content of a project file as it is serialized to JSON. ProjectModel converts from and to it.</summary>
public class AtlabFileDto
{
    public const int CurrentFormatVersion = 2;
    public const string CurrentFormatId = "0xA71AB";
    
    public string? FormatId { get; init; }
    
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
    public BoundaryScanConfiguration BoundaryScan { get; init; } = new();
    public string? ScriptFolderPath { get; init; }
}