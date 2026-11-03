using System.Collections.Generic;

namespace ATLab.Models;

/// <summary>Content of a project file as it is serialized to JSON. ProjectModel converts from and to it.</summary>
public class AtlabFileDto
{
    // Version 3 added TargetTestHardwareId. Older ATLab versions refuse version 3 files, so they cannot open a
    // project for hardware they do not know.
    // Version 4 added Board (KiCad board and the nets of the adapter pins). Older versions refuse version 4 files
    // instead of dropping the board link when they save; version 3 files are still read.
    public const int CurrentFormatVersion = 4;

    /// <summary>Oldest format version this version of ATLab reads.</summary>
    public const int MinimumFormatVersion = 3;
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

    /// <summary>KiCad board and pin nets; missing in version 3 files.</summary>
    public BoardLink? Board { get; init; }
    public string? ScriptFolderPath { get; init; }
}