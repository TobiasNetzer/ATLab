using System.Collections.Generic;

namespace ATLab.Models;

/// <summary>Content of a project file as it is serialized to JSON. ProjectModel converts from and to it.</summary>
public class AtlabFileDto
{
    // Version 3 added TargetTestHardwareId. Older ATLab versions refuse version 3 files, so they cannot open a
    // project for hardware they do not know.
    public const int CurrentFormatVersion = 3;
    public const string CurrentFormatId = "0xA71AB";

    public string? FormatId { get; init; }

    public int FormatVersion { get; init; }

    /// <summary>
    /// Device ID of the test hardware the project was developed for, as reported by the hardware, written as hex
    /// (e.g. "0xA101", see <see cref="TestHardwareIdFormat"/>). Required: files without a valid ID are rejected.
    /// </summary>
    public string? TargetTestHardwareId { get; init; }

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