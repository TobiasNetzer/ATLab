using System;
using System.Collections.Generic;
using System.Linq;
using ATLab.Results;

namespace ATLab.Analyzer.Analysis;

/// <summary>The result of one step in one run.</summary>
public sealed record StepSample(LoadedRun Run, TestStepResult Result)
{
    public bool IsPassed => Result.Result == TestVerdict.Pass;
    public bool IsMeasured => Result.Status == TestValueStatus.Measured && Result.Value is { } v && double.IsFinite(v);
}

/// <summary>
/// All results of one step across the imported runs. Steps are matched by their stable step ID, so renaming or
/// renumbering a step in the project does not split its history.
/// </summary>
public sealed class StepSeries
{
    private StepSeries(string stepId, IReadOnlyList<StepSample> samples)
    {
        StepId = stepId;
        Samples = samples;

        // Name, number and limits as in the newest run, older runs may have used other values
        var latest = samples[^1].Result;
        ModuleId = latest.ModuleId;
        Module = latest.Module;
        Number = latest.Number;
        Name = latest.Name;
        Unit = latest.Unit;
        LowerLimit = latest.LowerLimit;
        UpperLimit = latest.UpperLimit;

        LimitsChanged = samples.Any(s => s.Result.LowerLimit != LowerLimit || s.Result.UpperLimit != UpperLimit);

        Executed = samples.Count;
        Failed = samples.Count(s => !s.IsPassed);
        Values = samples.Where(s => s.IsMeasured).Select(s => s.Result.Value!.Value).ToArray();
        Invalid = samples.Count(s => s.Result.Status is not (TestValueStatus.Measured or TestValueStatus.NoValue));
        Statistics = DescriptiveStatistics.Compute(Values, LowerLimit, UpperLimit);
    }

    public string StepId { get; }
    public string ModuleId { get; }
    public string Module { get; }
    public int Number { get; }
    public string Name { get; }
    public string Unit { get; }
    public double? LowerLimit { get; }
    public double? UpperLimit { get; }

    /// <summary>True if older runs used other limits than the newest run.</summary>
    public bool LimitsChanged { get; }

    /// <summary>Samples ordered by the start time of their run.</summary>
    public IReadOnlyList<StepSample> Samples { get; }

    /// <summary>Measured values in the order of <see cref="Samples"/>.</summary>
    public IReadOnlyList<double> Values { get; }

    public int Executed { get; }
    public int Failed { get; }
    public int Passed => Executed - Failed;
    public double FailRate => Executed == 0 ? 0 : (double)Failed / Executed;

    /// <summary>Samples with Timeout, NACK, Overflow or an unreadable value.</summary>
    public int Invalid { get; }

    public int MeasuredCount => Values.Count;
    public bool HasValues => Values.Count > 0;
    public DescriptiveStatistics? Statistics { get; }

    /// <summary>Groups the step results of the runs by step ID.</summary>
    /// <remarks>
    /// Steps are listed in the order of the newest run that contains them, steps that only older runs contain follow.
    /// </remarks>
    public static IReadOnlyList<StepSeries> Build(IEnumerable<LoadedRun> runs)
    {
        var chronological = runs.OrderBy(r => r.StartTime).ToList();
        var samples = new Dictionary<string, List<StepSample>>(StringComparer.Ordinal);
        var order = new Dictionary<string, (int Run, int Position)>(StringComparer.Ordinal);

        for (var runIndex = 0; runIndex < chronological.Count; runIndex++)
        {
            var run = chronological[runIndex];
            var steps = run.File.Steps;

            for (var position = 0; position < steps.Count; position++)
            {
                var step = steps[position];
                if (!samples.TryGetValue(step.StepId, out var list))
                    samples[step.StepId] = list = [];

                list.Add(new StepSample(run, step));

                // Newer runs win, so the order follows the newest run that contains the step
                order[step.StepId] = (runIndex, position);
            }
        }

        return samples
            .OrderByDescending(kv => order[kv.Key].Run)
            .ThenBy(kv => order[kv.Key].Position)
            .Select(kv => new StepSeries(kv.Key, kv.Value))
            .ToList();
    }
}