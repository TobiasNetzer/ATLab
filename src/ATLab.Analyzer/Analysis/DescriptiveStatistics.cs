using System;
using System.Collections.Generic;
using System.Linq;

namespace ATLab.Analyzer.Analysis;

/// <summary>Location, spread and process capability of a set of measured values.</summary>
public sealed record DescriptiveStatistics(
    int Count,
    double Mean,
    double? StdDev,
    double Min,
    double Max,
    double Median,
    double? Cp,
    double? Cpk)
{
    public double Range => Max - Min;

    /// <returns>null if there are no values.</returns>
    public static DescriptiveStatistics? Compute(IReadOnlyCollection<double> values, double? lowerLimit, double? upperLimit)
    {
        if (values.Count == 0)
            return null;

        var sorted = values.Order().ToArray();
        var n = sorted.Length;
        var mean = sorted.Average();

        // Sample standard deviation; undefined for a single value
        double? stdDev = null;
        if (n > 1)
        {
            var sumSquares = sorted.Sum(v => (v - mean) * (v - mean));
            stdDev = Math.Sqrt(sumSquares / (n - 1));
        }

        var median = n % 2 == 1
            ? sorted[n / 2]
            : (sorted[n / 2 - 1] + sorted[n / 2]) / 2;

        var (cp, cpk) = Capability(mean, stdDev, lowerLimit, upperLimit);

        return new DescriptiveStatistics(n, mean, stdDev, sorted[0], sorted[^1], median, cp, cpk);
    }

    /// <summary>
    /// Cp needs both limits. Cpk uses the nearer limit; with a single limit it is the one-sided index (Cpl or Cpu).
    /// Both are undefined without spread, and without tolerance: equal limits (e.g. 0 … 0 mismatching pins, 1 … 1 for
    /// a confirmation) are pass/fail checks, not a range the scatter has to fit into.
    /// </summary>
    internal static (double? Cp, double? Cpk) Capability(double mean, double? stdDev, double? lowerLimit, double? upperLimit)
    {
        if (stdDev is not { } sd || sd <= 0 || !double.IsFinite(sd))
            return (null, null);

        if (lowerLimit is { } lower && upperLimit is { } upper && upper <= lower)
            return (null, null);

        double? cp = lowerLimit is { } l && upperLimit is { } u ? (u - l) / (6 * sd) : null;

        var candidates = new List<double>(2);
        if (upperLimit is { } usl) candidates.Add((usl - mean) / (3 * sd));
        if (lowerLimit is { } lsl) candidates.Add((mean - lsl) / (3 * sd));

        double? cpk = candidates.Count > 0 ? candidates.Min() : null;
        return (cp, cpk);
    }
}