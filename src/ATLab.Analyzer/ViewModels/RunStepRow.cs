using System.Globalization;
using ATLab.Analyzer.Analysis;
using ATLab.Analyzer.Localization;
using ATLab.Results;

namespace ATLab.Analyzer.ViewModels;

/// <summary>One step of the selected run.</summary>
public sealed class RunStepRow
{
    public RunStepRow(TestStepResult step)
    {
        Step = step;
        ValueText = step.Status == TestValueStatus.Measured
            ? ValueFormatter.Format(step.Value, step.Unit)
            : step.Status == TestValueStatus.NoValue ? string.Empty : Localizer.Enum(step.Status);
        LowerLimitText = ValueFormatter.Format(step.LowerLimit, step.Unit);
        UpperLimitText = ValueFormatter.Format(step.UpperLimit, step.Unit);
        DeviationText = step.DeviationPercent is { } d ? $"{d:0.##} %" : string.Empty;
        DetailsText = step.Details is { Count: > 0 } details ? string.Join("; ", details) : string.Empty;
    }

    public TestStepResult Step { get; }
    public string Module => Step.Module;
    public int Number => Step.Number;
    public string Name => Step.Name;
    public double? Value => Step.Value;
    public string ValueText { get; }
    public string LowerLimitText { get; }
    public string UpperLimitText { get; }
    public string DeviationText { get; }
    public double? Deviation => Step.DeviationPercent;
    public TestVerdict Result => Step.Result;
    public bool IsPassed => Step.Result == TestVerdict.Pass;
    public string DetailsText { get; }
}

/// <summary>A label and value of a fact list, e.g. "Mean" and "4.98V".</summary>
public sealed record Fact(string Label, string Value);

/// <summary>Entry of the project filter; no name stands for all projects.</summary>
public sealed record ProjectOption(string? Name)
{
    public override string ToString() => Name ?? Localizer.Get("Filter_AllProjects");
}

/// <summary>Entry of the language selection, shown in its own language ("English", "Deutsch").</summary>
public sealed record LanguageOption(CultureInfo Culture)
{
    public override string ToString() =>
        Culture.NativeName.Length > 0 ? char.ToUpper(Culture.NativeName[0], Culture) + Culture.NativeName[1..] : Culture.Name;
}