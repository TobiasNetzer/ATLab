using System;
using System.Collections.Generic;
using System.Linq;

namespace ATLab.Analyzer.Analysis;

/// <summary>How the histogram divides the values into bins.</summary>
public enum BinRule
{
    /// <summary>
    /// Freedman–Diaconis rule (width 2·IQR/∛n), robust against outliers; quantized values are put on the bin centers.
    /// </summary>
    Auto,

    /// <summary>A given number of bins.</summary>
    Count,

    /// <summary>A given bin width, with bin edges on multiples of it.</summary>
    Width
}

/// <param name="Rule">How the bins are chosen.</param>
/// <param name="Count">Number of bins for <see cref="BinRule.Count"/>.</param>
/// <param name="Width">Bin width for <see cref="BinRule.Width"/>, in the unit of the values.</param>
public sealed record BinSettings(BinRule Rule = BinRule.Auto, int Count = 20, double Width = 0)
{
    public static BinSettings Default { get; } = new();
}

public sealed record HistogramBin(double Lower, double Upper, int Count)
{
    public double Center => (Lower + Upper) / 2;
}

/// <summary>Values sorted into bins of equal width.</summary>
/// <param name="Resolution">
/// The step between the measured values if they are quantized (e.g. whole numbers or an ADC step), else null.
/// </param>
public sealed record Histogram(IReadOnlyList<HistogramBin> Bins, double BinWidth, double? Resolution = null)
{
    public const int MinBins = 5;

    /// <summary>Upper bound for the automatic bins: about 5 px per bar at the usual panel width.</summary>
    public const int MaxAutoBins = 100;

    /// <summary>Upper bound for a given count or width, so a tiny width cannot create millions of bins.</summary>
    public const int MaxBins = 500;

    public const int MaxCount = 200;

    public int MaxBinCount => Bins.Count == 0 ? 0 : Bins.Max(b => b.Count);

    /// <param name="values">The values to sort; non-finite values are ignored.</param>
    /// <param name="settings">How to choose the bins, automatic if null.</param>
    public static Histogram Create(IReadOnlyCollection<double> values, BinSettings? settings = null)
    {
        settings ??= BinSettings.Default;

        var data = values.Where(double.IsFinite).Order().ToArray();
        if (data.Length == 0)
            return new Histogram([], 0);

        var min = data[0];
        var max = data[^1];

        // All values equal: one bin centered on the value
        if (max - min <= Math.Abs(max) * 1e-12)
        {
            var halfWidth = settings.Rule == BinRule.Width && settings.Width > 0
                ? settings.Width / 2
                : Math.Abs(min) > 0 ? Math.Abs(min) * 0.01 : 0.5;
            return new Histogram([new HistogramBin(min - halfWidth, min + halfWidth, data.Length)], 2 * halfWidth);
        }

        var resolution = DetectResolution(data);
        var (start, width) = Layout(data, settings, resolution);

        var count = Math.Max(1, (int)Math.Ceiling((max - start) / width - 1e-9));
        if (start + count * width < max - width * 1e-9)
            count++;
        count = Math.Min(count, MaxBins);

        var counts = new int[count];
        foreach (var value in data)
        {
            var index = Math.Clamp((int)Math.Floor((value - start) / width + 1e-9), 0, count - 1);
            counts[index]++;
        }

        var bins = new HistogramBin[count];
        for (var i = 0; i < count; i++)
            bins[i] = new HistogramBin(start + i * width, start + (i + 1) * width, counts[i]);

        return new Histogram(bins, width, resolution);
    }

    /// <summary>Start of the first bin and the bin width.</summary>
    private static (double Start, double Width) Layout(double[] sorted, BinSettings settings, double? resolution)
    {
        var min = sorted[0];
        var range = sorted[^1] - min;

        switch (settings.Rule)
        {
            case BinRule.Count:
                return (min, range / Math.Clamp(settings.Count, 1, MaxCount));

            case BinRule.Width when settings.Width > 0:
            {
                // Edges on multiples of the width, e.g. 4.95, 5.00, 5.05 V
                var width = Math.Max(settings.Width, range / MaxBins);
                return (Math.Floor(min / width) * width, width);
            }

            default:
            {
                var bins = Math.Clamp(SuggestBinCount(sorted), Math.Min(MinBins, sorted.Length), MaxAutoBins);
                var width = range / bins;

                // Quantized values: a width that is no multiple of the step leaves every other bin empty
                if (resolution is { } step && step > 0)
                {
                    width = Math.Max(1, Math.Round(width / step)) * step;
                    return (min - step / 2, width);
                }

                return (min, width);
            }
        }
    }

    /// <summary>
    /// Freedman–Diaconis rule, which copes with outliers better than the other common rules; Sturges' rule is the
    /// fallback when the interquartile range is zero (e.g. mostly identical values).
    /// </summary>
    internal static int SuggestBinCount(double[] sorted)
    {
        var n = sorted.Length;
        var range = sorted[^1] - sorted[0];
        var iqr = Quantile(sorted, 0.75) - Quantile(sorted, 0.25);

        return iqr > 0 && range > 0
            ? (int)Math.Ceiling(range / (2 * iqr / Math.Cbrt(n)))
            : (int)Math.Ceiling(Math.Log2(n)) + 1;
    }

    /// <summary>
    /// The step between quantized values: every gap between distinct values is a whole multiple of the smallest gap.
    /// Null for continuous data, which has (almost) as many distinct values as values.
    /// </summary>
    internal static double? DetectResolution(double[] sorted)
    {
        var distinct = new List<double>();
        foreach (var v in sorted)
        {
            if (distinct.Count == 0 || v - distinct[^1] > Math.Abs(v) * 1e-12)
                distinct.Add(v);
        }

        // Only meaningful when values repeat; with mostly unique values any small gap would count as "step"
        if (distinct.Count < 2 || distinct.Count > sorted.Length / 2)
            return null;

        var step = double.MaxValue;
        for (var i = 1; i < distinct.Count; i++)
            step = Math.Min(step, distinct[i] - distinct[i - 1]);

        for (var i = 1; i < distinct.Count; i++)
        {
            var multiple = (distinct[i] - distinct[i - 1]) / step;
            if (Math.Abs(multiple - Math.Round(multiple)) > 1e-6 * Math.Max(1, multiple))
                return null;
        }

        return step;
    }

    /// <summary>Linear interpolation between closest ranks of sorted data.</summary>
    internal static double Quantile(double[] sorted, double p)
    {
        if (sorted.Length == 1)
            return sorted[0];

        var position = p * (sorted.Length - 1);
        var lower = (int)Math.Floor(position);
        var upper = Math.Min(lower + 1, sorted.Length - 1);
        return sorted[lower] + (position - lower) * (sorted[upper] - sorted[lower]);
    }
}