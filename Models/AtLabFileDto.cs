using System.Collections.Generic;
using ATLab.Enums;

namespace ATLab.Models;

public class AtlabFileDto
{
    public List<TestStep> TestSteps { get; init; } = new();
    public List<CustomRelayChannelName> StimChannelNames { get; init; } = new();
    public List<CustomRelayChannelName> ExtStimChannelNames { get; init; } = new();
    public List<CustomRelayChannelName> MeasChannelNames { get; init; } = new();
    public List<Device> Devices { get; init; } = new();
    public double DefaultTolerance { get; init; }
    public bool UseSerialNumber { get; init; }
    public bool SaveTestResults { get; init; }
    public SaveTestResultOptions SaveTestResultOptions { get; init; }
    public string SaveTestResultFilePath { get; init; } = string.Empty;
}
