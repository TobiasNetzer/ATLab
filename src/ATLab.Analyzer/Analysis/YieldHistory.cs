using System;
using System.Collections.Generic;
using System.Linq;

namespace ATLab.Analyzer.Analysis;

/// <summary>Runs of one day or one week (starting on Monday).</summary>
public sealed record YieldBucket(DateOnly Start, bool IsWeek, int Passed, int Failed)
{
    public int Runs => Passed + Failed;
    public double Yield => Runs == 0 ? 0 : (double)Passed / Runs;
}

/// <summary>Passed and failed runs over time, for the yield chart of the overview.</summary>
public static class YieldHistory
{
    /// <summary>More days with runs than this are grouped by week, so the bars stay readable.</summary>
    public const int MaxDays = 60;

    /// <remarks>Only days (or weeks) with runs are listed, so weekends without production leave no gaps.</remarks>
    public static IReadOnlyList<YieldBucket> Build(IEnumerable<LoadedRun> runs)
    {
        var list = runs.ToList();
        var days = list.Select(r => DateOnly.FromDateTime(r.LocalStartTime)).Distinct().Count();
        var byWeek = days > MaxDays;

        return list
            .GroupBy(r => byWeek ? WeekStart(DateOnly.FromDateTime(r.LocalStartTime)) : DateOnly.FromDateTime(r.LocalStartTime))
            .OrderBy(g => g.Key)
            .Select(g => new YieldBucket(g.Key, byWeek, g.Count(r => r.IsPassed), g.Count(r => !r.IsPassed)))
            .ToList();
    }

    internal static DateOnly WeekStart(DateOnly day) => day.AddDays(-(((int)day.DayOfWeek + 6) % 7));
}