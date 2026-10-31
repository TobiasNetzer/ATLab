using System;
using System.IO;
using ATLab.Results;

namespace ATLab.Analyzer.Analysis;

/// <summary>One imported test result file.</summary>
public sealed class LoadedRun
{
    public LoadedRun(string filePath, TestResultFile file)
    {
        FilePath = filePath;
        File = file;
    }

    public string FilePath { get; }
    public string FileName => Path.GetFileName(FilePath);
    public TestResultFile File { get; }

    public DateTimeOffset StartTime => File.Run.StartTime;
    public string? SerialNumber => File.Run.SerialNumber;
    public TestVerdict Result => File.Run.OverallResult;
    public bool IsPassed => Result == TestVerdict.Pass;
    public int FailedSteps => File.Run.FailedSteps;
    public double DurationSeconds => File.Run.DurationSeconds;
    public string? Operator => File.Run.Operator;
    public string? Station => File.Run.Station;
    public TestRunMode RunMode => File.Run.RunMode;
    public int? LoopIndex => File.Run.LoopIndex;
    public string? ProjectName => File.Project.Name;
    public string? DeviceName => File.DeviceUnderTest.DeviceName;
    public string? Variant => File.DeviceUnderTest.Variant;
    public string? Revision => File.DeviceUnderTest.Revision;
    public bool IsSimulator => File.TestHardware.IsSimulator;
    public int StepCount => File.Steps.Count;

    /// <summary>Start time in the local time zone, for display and date filters.</summary>
    public DateTime LocalStartTime => StartTime.LocalDateTime;
}