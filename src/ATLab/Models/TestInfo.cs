using System;
using ATLab.Results;

namespace ATLab.Models;

/// <summary>General information about a run for the report, the JSON result file and the export file names.</summary>
public class TestInfo
{
    public string? ProjectName { get; init; }
    public string? Operator { get; init; }

    /// <summary>Start of the run; used for the report, the JSON file and the export file names.</summary>
    public DateTimeOffset StartTime { get; init; } = DateTimeOffset.Now;

    public string Date => StartTime.ToString("dd.MM.yyyy");
    public string Time => StartTime.ToString("HH:mm:ss");
    public string? Duration { get; init; }
    public TimeSpan Elapsed { get; init; }

    public int FailedSteps { get; init; }
    public TestRunMode RunMode { get; init; } = TestRunMode.Single;

    /// <summary>1-based loop number of a continuous run, null for a single run.</summary>
    public int? LoopIndex { get; init; }

    public string? SerialNumber { get; init; }
    public DeviceUnderTestInfo DeviceUnderTestInfo { get; init; } = new();
}
