using ATLab.Analyzer.Analysis;

namespace ATLab.Analyzer.ViewModels;

/// <summary>
/// One line of the step statistics table. Every value exists as a number for sorting and as formatted text for
/// display.
/// </summary>
public sealed class StepRow
{
    /// <summary>Cpk at or above this value counts as capable.</summary>
    public const double CapableCpk = 1.33;

    public StepRow(StepSeries series)
    {
        Series = series;
        var st = series.Statistics;
        var unit = series.Unit;

        Min = st?.Min;
        Max = st?.Max;
        Mean = st?.Mean;
        Median = st?.Median;
        StdDev = st?.StdDev;
        Cp = st?.Cp;
        Cpk = st?.Cpk;

        LowerLimitText = ValueFormatter.Format(series.LowerLimit, unit);
        UpperLimitText = ValueFormatter.Format(series.UpperLimit, unit);
        MinText = ValueFormatter.Format(Min, unit);
        MaxText = ValueFormatter.Format(Max, unit);
        MeanText = ValueFormatter.Format(Mean, unit);
        MedianText = ValueFormatter.Format(Median, unit);
        StdDevText = ValueFormatter.Format(StdDev, unit);
        CpText = Cp?.ToString("0.00") ?? string.Empty;
        CpkText = Cpk?.ToString("0.00") ?? string.Empty;
        FailRateText = series.Executed > 0 ? series.FailRate.ToString("P1") : string.Empty;
    }

    public StepSeries Series { get; }

    public string StepId => Series.StepId;
    public string Module => Series.Module;
    public int Number => Series.Number;
    public string Name => Series.Name;
    public string Unit => Series.Unit;
    public double? LowerLimit => Series.LowerLimit;
    public double? UpperLimit => Series.UpperLimit;
    public bool LimitsChanged => Series.LimitsChanged;
    public int Executed => Series.Executed;
    public int Failed => Series.Failed;
    public double FailRate => Series.FailRate;
    public int Invalid => Series.Invalid;
    public int Measured => Series.MeasuredCount;

    public double? Min { get; }
    public double? Max { get; }
    public double? Mean { get; }
    public double? Median { get; }
    public double? StdDev { get; }
    public double? Cp { get; }
    public double? Cpk { get; }

    public string LowerLimitText { get; }
    public string UpperLimitText { get; }
    public string MinText { get; }
    public string MaxText { get; }
    public string MeanText { get; }
    public string MedianText { get; }
    public string StdDevText { get; }
    public string CpText { get; }
    public string CpkText { get; }
    public string FailRateText { get; }

    public bool HasFailures => Failed > 0;
    public bool IsCpkPoor => Cpk is < 1;
    public bool IsCpkMarginal => Cpk is >= 1 and < CapableCpk;
    public bool IsCpkGood => Cpk is >= CapableCpk;

    /// <summary>Failures relative to the step with the most failures, for the bars of the failure Pareto.</summary>
    public double FailShareOfMax { get; set; }
}