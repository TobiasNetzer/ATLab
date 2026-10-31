using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ATLab.Results;

namespace ATLab.Analyzer.Analysis;

/// <summary>
/// Tab separated tables like ATLab's CSV export: numbers culture independent (decimal point), so scripts and other
/// tools read them the same on every computer.
/// </summary>
public static class CsvExporter
{
    private const string Separator = "\t";

    public static string StepStatistics(IEnumerable<StepSeries> steps, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.InvariantCulture;
        var sb = new StringBuilder();

        sb.AppendLine(string.Join(Separator,
            "Module", "Number", "Step", "Unit", "Lower limit", "Upper limit", "Executed", "Passed", "Failed",
            "Fail rate %", "Invalid", "Measured", "Min", "Max", "Mean", "Median", "Std dev", "Cp", "Cpk",
            "Limits changed", "Step ID"));

        foreach (var s in steps)
        {
            var st = s.Statistics;
            sb.AppendLine(string.Join(Separator,
                Text(s.Module), s.Number.ToString(culture), Text(s.Name), Text(ValueFormatter.DisplayUnit(s.Unit)),
                Num(s.LowerLimit, culture), Num(s.UpperLimit, culture),
                s.Executed.ToString(culture), s.Passed.ToString(culture), s.Failed.ToString(culture),
                Num(s.FailRate * 100, culture), s.Invalid.ToString(culture), s.MeasuredCount.ToString(culture),
                Num(st?.Min, culture), Num(st?.Max, culture), Num(st?.Mean, culture), Num(st?.Median, culture),
                Num(st?.StdDev, culture), Num(st?.Cp, culture), Num(st?.Cpk, culture),
                s.LimitsChanged ? "yes" : "no", s.StepId));
        }

        return sb.ToString();
    }

    public static string Runs(IEnumerable<LoadedRun> runs, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.InvariantCulture;
        var sb = new StringBuilder();

        sb.AppendLine(string.Join(Separator,
            "Start time", "Serial number", "Result", "Failed steps", "Duration s", "Operator", "Station", "Project",
            "Device", "Variant", "Revision", "Run mode", "Loop", "Simulator", "File"));

        foreach (var r in runs.OrderBy(r => r.StartTime))
        {
            sb.AppendLine(string.Join(Separator,
                r.LocalStartTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                Text(r.SerialNumber), r.Result.ToString(), r.FailedSteps.ToString(culture),
                Num(r.DurationSeconds, culture), Text(r.Operator), Text(r.Station), Text(r.ProjectName),
                Text(r.DeviceName), Text(r.Variant), Text(r.Revision), r.RunMode.ToString(),
                r.LoopIndex?.ToString(culture) ?? "", r.IsSimulator ? "yes" : "no", Text(r.FilePath)));
        }

        return sb.ToString();
    }

    /// <summary>
    /// One step in every run that executed it, oldest first, without what the file name already says (the step).
    /// Limits stay per row because they can change between runs.
    /// </summary>
    public static string StepValues(StepSeries step, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.InvariantCulture;
        var sb = new StringBuilder();

        sb.AppendLine(string.Join(Separator,
            "Start time", "Serial number", "Run result", "Lower limit", "Value", "Upper limit", "Unit",
            "Deviation (%)", "Result", "Details", "Operator", "Station"));

        foreach (var sample in step.Samples)
        {
            var (r, s) = (sample.Run, sample.Result);
            sb.AppendLine(string.Join(Separator,
                StartTime(r), Text(r.SerialNumber), r.Result.ToString(),
                Num(s.LowerLimit, culture), ValueOrStatus(s, culture), Num(s.UpperLimit, culture),
                Text(ValueFormatter.DisplayUnit(s.Unit)), Num(s.DeviationPercent, culture), s.Result.ToString(),
                Details(s.Details), Text(r.Operator), Text(r.Station)));
        }

        return sb.ToString();
    }

    /// <summary>
    /// All steps of one run with the same columns as ATLab's CSV export, plus the detail lines (e.g. failed pin checks)
    /// that only the JSON file has. Serial number and start time are in the file name.
    /// </summary>
    public static string RunSteps(LoadedRun run, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.InvariantCulture;
        var sb = new StringBuilder();

        sb.AppendLine(string.Join(Separator,
            "Module", "Step", "Name", "Lower Limit", "Measured Value", "Upper Limit", "Unit", "Deviation (%)", "Result",
            "Details"));

        foreach (var s in run.File.Steps)
        {
            sb.AppendLine(string.Join(Separator,
                Text(s.Module), s.Number.ToString(culture), Text(s.Name),
                Num(s.LowerLimit, culture), ValueOrStatus(s, culture), Num(s.UpperLimit, culture),
                Text(ValueFormatter.DisplayUnit(s.Unit)), Num(s.DeviationPercent, culture), s.Result.ToString(),
                Details(s.Details)));
        }

        return sb.ToString();
    }

    /// <summary>
    /// One row per run and one column per step with the measured value, for analyses in Excel or other tools.
    /// A step without value shows its status (e.g. Timeout), a step the run did not execute stays empty.
    /// </summary>
    public static string ValueMatrix(IEnumerable<LoadedRun> runs, IReadOnlyList<StepSeries> steps, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.InvariantCulture;
        var sb = new StringBuilder();

        var stepHeaders = steps.Select(s =>
        {
            var unit = ValueFormatter.DisplayUnit(s.Unit);
            return Text($"{s.Module} {s.Number} {s.Name}" + (unit.Length > 0 ? $" [{unit}]" : ""));
        });
        sb.AppendLine(string.Join(Separator, new[] { "Start time", "Serial number", "Result" }.Concat(stepHeaders)));

        foreach (var run in runs.OrderBy(r => r.StartTime))
        {
            var byId = new Dictionary<string, TestStepResult>();
            foreach (var s in run.File.Steps)
                byId.TryAdd(s.StepId, s);

            var cells = steps.Select(step => byId.TryGetValue(step.StepId, out var s) ? ValueOrStatus(s, culture) : "");
            sb.AppendLine(string.Join(Separator,
                new[] { StartTime(run), Text(run.SerialNumber), run.Result.ToString() }.Concat(cells)));
        }

        return sb.ToString();
    }

    /// <summary>The measured value, or the status (Timeout, NACK…) like ATLab's CSV; empty for a step without value.</summary>
    private static string ValueOrStatus(TestStepResult s, CultureInfo culture) => s.Status switch
    {
        TestValueStatus.Measured => Num(s.Value, culture),
        TestValueStatus.NoValue => "",
        _ => s.Status.ToString()
    };

    private static string StartTime(LoadedRun run) =>
        run.LocalStartTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

    private static string Details(IReadOnlyList<string>? details) =>
        details is { Count: > 0 } ? Text(string.Join("; ", details)) : "";

    private static string Num(double? value, CultureInfo culture) =>
        value is { } v && double.IsFinite(v) ? v.ToString("G15", culture) : "";

    /// <summary>
    /// Like ATLab's CSV export: text with a tab, quote or line break is put in quotes (quotes doubled), so it stays in
    /// its column.
    /// </summary>
    internal static string Text(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        var escaped = value.Replace("\"", "\"\"");
        return escaped.Contains('\t') || escaped.Contains('"') || escaped.Contains('\n') || escaped.Contains('\r')
            ? $"\"{escaped}\""
            : escaped;
    }
}