using System;
using System.Collections.Generic;
using System.Linq;

namespace ATLab.Analyzer.Analysis;

/// <summary>Key figures of a set of runs.</summary>
public sealed record RunSummary(
    int Runs,
    int Passed,
    int Failed,
    int Devices,
    double? FirstPassYield,
    double? FinalYield,
    double? MeanDurationSeconds,
    DateTimeOffset? From,
    DateTimeOffset? To)
{
    public static RunSummary Empty { get; } = new(0, 0, 0, 0, null, null, null, null, null);

    /// <summary>Share of passed runs.</summary>
    public double? Yield => Runs == 0 ? null : (double)Passed / Runs;

    /// <remarks>
    /// Devices are told apart by serial number. First pass yield counts devices whose first run passed, final yield
    /// devices whose last run passed; runs without serial number are not part of either.
    /// </remarks>
    public static RunSummary Compute(IReadOnlyCollection<LoadedRun> runs)
    {
        if (runs.Count == 0)
            return Empty;

        var passed = runs.Count(r => r.IsPassed);

        var devices = runs
            .Where(r => !string.IsNullOrWhiteSpace(r.SerialNumber))
            .GroupBy(r => r.SerialNumber!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderBy(r => r.StartTime).ToList())
            .ToList();

        double? firstPassYield = null, finalYield = null;
        if (devices.Count > 0)
        {
            firstPassYield = (double)devices.Count(d => d[0].IsPassed) / devices.Count;
            finalYield = (double)devices.Count(d => d[^1].IsPassed) / devices.Count;
        }

        return new RunSummary(
            runs.Count,
            passed,
            runs.Count - passed,
            devices.Count,
            firstPassYield,
            finalYield,
            runs.Average(r => r.DurationSeconds),
            runs.Min(r => r.StartTime),
            runs.Max(r => r.StartTime));
    }

    /// <summary>The newest run of every serial number; runs without serial number are kept.</summary>
    public static IEnumerable<LoadedRun> LatestPerDevice(IEnumerable<LoadedRun> runs)
    {
        var list = runs.ToList();

        var latest = list
            .Where(r => !string.IsNullOrWhiteSpace(r.SerialNumber))
            .GroupBy(r => r.SerialNumber!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.MaxBy(r => r.StartTime)!)
            .ToHashSet();

        return list.Where(r => string.IsNullOrWhiteSpace(r.SerialNumber) || latest.Contains(r));
    }
}