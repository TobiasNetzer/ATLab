using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ATLab.Results;

namespace ATLab.TestDataGenerator;

internal sealed record GeneratorOptions
{
    public string OutputDirectory { get; init; } = "test-results";
    public int Runs { get; init; } = 500;
    public int Days { get; init; } = 14;
    public DateOnly? StartDate { get; init; }
    public int Seed { get; init; } = 1;

    /// <summary>Put the files into Pass and Fail subfolders, like ATLab's "sort by verdict" option.</summary>
    public bool SortByVerdict { get; init; }
}

/// <summary>A generated file and where it goes, relative to the output folder.</summary>
internal sealed record GeneratedRun(string RelativePath, TestResultFile File);

/// <summary>
/// Simulates a production line over a period of working days: new devices, retests of failed devices, occasional
/// continuous runs on a golden sample and runs in simulator mode. The same seed always gives the same data.
/// </summary>
internal sealed class Generator
{
    private const double RetestProbability = 0.75;
    private const double ContinuousBurstProbability = 0.005;
    private const double SimulatorProbability = 0.03;
    private const double ExcludedFailureProbability = 0.01;
    private const double UnsavedChangesProbability = 0.02;

    private readonly GeneratorOptions _options;
    private readonly Random _random;
    private readonly Dictionary<string, int> _serialCounters = new();

    public Generator(GeneratorOptions options)
    {
        _options = options;
        _random = new Random(options.Seed);
    }

    public IReadOnlyList<GeneratedRun> Generate()
    {
        var times = Timeline().GetEnumerator();
        var runs = new List<GeneratedRun>(_options.Runs);

        while (runs.Count < _options.Runs && times.MoveNext())
        {
            var time = times.Current;
            var progress = (double)runs.Count / _options.Runs;

            if (_random.NextDouble() < ContinuousBurstProbability)
            {
                runs.AddRange(ContinuousBurst(time, progress).Take(_options.Runs - runs.Count));
                continue;
            }

            var project = PickProject();
            var simulator = _random.NextDouble() < SimulatorProbability;
            var serial = simulator ? $"SIM-{_random.Next(1, 999):000}" : NextSerial(project);
            var station = Pick(Scenario.Stations);
            var variant = Pick(project.Variants);

            var run = Run(project, serial, time, progress, station, variant, simulator, TestRunMode.Single, null, notes: null);
            runs.Add(run);

            // A failed device is usually repaired and tested again a few minutes later
            if (run.File.Run.OverallResult == TestVerdict.Fail && !simulator && _random.NextDouble() < RetestProbability &&
                runs.Count < _options.Runs)
            {
                var retestTime = time.AddMinutes(_random.Next(3, 25)).AddSeconds(_random.Next(60));
                runs.Add(Run(project, serial, retestTime, progress, station, variant, false, TestRunMode.Single, null,
                    notes: _random.NextDouble() < 0.5 ? "Rework: re-soldered connector X3" : null));
            }
        }

        return runs;
    }

    /// <summary>Start times on working days (Mon–Fri, 06:00–22:00), evenly spread with some jitter.</summary>
    private IEnumerable<DateTimeOffset> Timeline()
    {
        var start = _options.StartDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-_options.Days));
        var workingDays = Enumerable.Range(0, Math.Max(1, _options.Days))
            .Select(start.AddDays)
            .Where(d => d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            .ToList();

        if (workingDays.Count == 0)
            workingDays.Add(start);

        const int minutesPerDay = 16 * 60;
        var interval = (double)workingDays.Count * minutesPerDay / _options.Runs;
        var minute = 0.0;

        while (true)
        {
            minute += interval * (0.5 + _random.NextDouble());
            var dayIndex = (int)(minute / minutesPerDay);
            if (dayIndex >= workingDays.Count)
            {
                // More runs than fit: continue from the start of the period, times are not checked for collisions
                minute = _random.NextDouble() * interval;
                dayIndex = 0;
            }

            var local = workingDays[dayIndex].ToDateTime(new TimeOnly(6, 0)).AddMinutes(minute % minutesPerDay);
            local = local.AddSeconds(_random.Next(60));
            yield return new DateTimeOffset(local, TimeZoneInfo.Local.GetUtcOffset(local));
        }
    }

    /// <summary>The golden sample tested in a loop, e.g. to check the stability of the test setup.</summary>
    private IEnumerable<GeneratedRun> ContinuousBurst(DateTimeOffset time, double progress)
    {
        var project = Scenario.Projects[0];
        var station = Pick(Scenario.Stations);
        var loops = _random.Next(5, 21);

        for (var loop = 1; loop <= loops; loop++)
        {
            yield return Run(project, "GOLDEN-01", time, progress, station, project.Variants[0], false,
                TestRunMode.Continuous, loop, notes: "Golden sample");
            time = time.AddSeconds(project.BaseDurationSeconds + _random.Next(5, 15));
        }
    }

    private GeneratedRun Run(ProjectDefinition project, string serial, DateTimeOffset time, double progress, string station,
        string variant, bool simulator, TestRunMode mode, int? loop, string? notes)
    {
        var context = new RunContext(time, progress, station, variant, simulator);
        var steps = project.Steps
            .Where(s => s.OnlyVariant is null || s.OnlyVariant == variant)
            .Select(s => Measure(s, context))
            .ToList();

        var failed = steps.Count(s => s.Result == TestVerdict.Fail);

        // A failed step marked "Exclude from export" counts for the verdict but is not in the file
        if (_random.NextDouble() < ExcludedFailureProbability)
            failed++;

        var limitRevision = project.Steps.Any(s => s.LaterLimits is { } l && progress >= l.From) ? 2 : 1;
        var unsaved = _random.NextDouble() < UnsavedChangesProbability;
        var shift = Scenario.Shifts.FirstOrDefault(s => time.Hour >= s.FromHour && time.Hour < s.ToHour, Scenario.Shifts[^1]);

        var file = new TestResultFile
        {
            Run = new TestRunMetadata
            {
                SerialNumber = serial,
                StartTime = time,
                DurationSeconds = Math.Round(project.BaseDurationSeconds * (0.92 + 0.16 * _random.NextDouble()) +
                                             (variant == "Pro" ? 4 : 0), 3),
                OverallResult = failed > 0 ? TestVerdict.Fail : TestVerdict.Pass,
                FailedSteps = failed,
                Operator = Pick(shift.Operators),
                Station = station,
                RunMode = mode,
                LoopIndex = loop
            },
            Project = new TestProjectMetadata
            {
                Name = project.Name,
                FileHash = Hash($"{project.Name} rev {limitRevision}"),
                HasUnsavedChanges = unsaved,
                AtlabVersion = progress < 0.5 ? "1.6.20" : "1.6.24"
            },
            DeviceUnderTest = new DeviceUnderTestMetadata
            {
                DeviceName = project.DeviceName,
                Revision = progress < 0.7 ? "C" : "D",
                Variant = variant,
                PartNumber = project.PartNumber
            },
            TestHardware = simulator
                ? new TestHardwareMetadata { Name = "Simulator", IsSimulator = true }
                : new TestHardwareMetadata
                {
                    Name = "cTIA",
                    SerialNumber = station == "TEST-PC1" ? "T-0042" : "T-0057",
                    FirmwareVersion = "2.3.1"
                },
            TestEquipment =
            [
                new TestEquipmentMetadata
                {
                    Name = "DMM",
                    // Sometimes the meter is switched off
                    Identification = _random.NextDouble() < 0.01 ? null : "KEITHLEY INSTRUMENTS,MODEL DMM6500,04512345,1.7.12b"
                },
                new TestEquipmentMetadata { Name = "PSU", Identification = "Rohde&Schwarz,HMP4040,123456,HW50020001/SW2.70" }
            ],
            Notes = notes,
            Steps = steps
        };

        var folder = _options.SortByVerdict ? (failed > 0 ? "Fail" : "Pass") : "";
        var fileName = $"{serial}_{time:yyyy-MM-dd_HH-mm-ss}.json";
        return new GeneratedRun(Path.Combine(folder, fileName), file);
    }

    private TestStepResult Measure(StepDefinition step, RunContext context)
    {
        var (lower, upper) = step.LaterLimits is { } later && context.Progress >= later.From
            ? (later.Lower, later.Upper)
            : (step.Lower, step.Upper);
        var name = step.LaterName is { } renamed && context.Progress >= renamed.From ? renamed.Name : step.Name;

        TestValueStatus status;
        double? value = null;
        IReadOnlyList<string>? details = null;
        bool passed;

        if (_random.NextDouble() < step.ErrorRate)
        {
            if (step.ErrorStatus == "Rejected")
            {
                // Operator confirmation answered with No: a measured 0
                status = TestValueStatus.Measured;
                value = 0;
            }
            else
            {
                status = Enum.Parse<TestValueStatus>(step.ErrorStatus);
            }

            passed = false;
            if (step.ErrorDetail is not null)
                details = [step.ErrorDetail];
        }
        else if (step.PinCheck is { } pinCheck)
        {
            // Number of mismatching pins, one detail line per pin like ATLab's boundary scan session
            var mismatches = _random.NextDouble() < pinCheck.FailureRate ? MismatchingPins(pinCheck.Pins) : [];
            status = TestValueStatus.Measured;
            value = mismatches.Count;
            passed = InLimits(value, lower, upper);
            if (mismatches.Count > 0)
                details = mismatches.Select(PinMessage).ToList();
        }
        else if (step.Mean is { } mean)
        {
            var shifted = mean + (step.Shift?.Invoke(context) ?? 0);
            var v = step.Distribution == Distribution.LogNormal
                ? shifted * Math.Exp(Gaussian() * step.StdDev)
                : shifted + Gaussian() * step.StdDev;

            // Instruments return a limited number of digits
            value = RoundSignificant(v, 7);
            status = TestValueStatus.Measured;
            passed = InLimits(value, lower, upper);

            // A failed script response check fails the step whatever the value is
            if (step.CheckFailure is { } check && _random.NextDouble() < check.Rate)
            {
                passed = false;
                details = [check.Detail];
            }
        }
        else
        {
            status = TestValueStatus.NoValue;
            passed = true;
        }

        return new TestStepResult
        {
            ModuleId = step.ModuleId,
            Module = step.Module,
            StepId = step.StepId,
            Number = step.Number,
            Name = name,
            LowerLimit = lower,
            UpperLimit = upper,
            Unit = step.Unit,
            Status = status,
            Value = value,
            DeviationPercent = value is { } m ? Deviation(m, lower, upper) : null,
            Result = passed ? TestVerdict.Pass : TestVerdict.Fail,
            Details = details
        };
    }

    private static bool InLimits(double? value, double? lower, double? upper) =>
        (lower is null || value >= lower) && (upper is null || value <= upper);

    /// <summary>Usually one pin, sometimes a few; weighted so recurring defects show up in the statistics.</summary>
    private List<string> MismatchingPins(IReadOnlyList<(string Pin, double Weight)> pins)
    {
        var count = _random.NextDouble() < 0.8 ? 1 : _random.Next(2, Math.Min(3, pins.Count) + 1);
        var left = pins.ToList();
        var result = new List<string>();

        while (result.Count < count && left.Count > 0)
        {
            var x = _random.NextDouble() * left.Sum(p => p.Weight);
            var index = 0;
            while (index < left.Count - 1 && (x -= left[index].Weight) > 0)
                index++;

            result.Add(left[index].Pin);
            left.RemoveAt(index);
        }

        return result;
    }

    /// <summary>Same text as BoundaryScanTestSession: an expected input or a driven output read back wrong.</summary>
    private string PinMessage(string pin)
    {
        var expected = _random.NextDouble() < 0.7 ? 1 : 0;
        var kind = _random.NextDouble() < 0.5 ? "expected" : "driven";
        return $"{pin}: {kind} {expected}, read {1 - expected}";
    }

    /// <summary>Deviation from the middle of the limits in percent, null without both limits.</summary>
    private static double? Deviation(double value, double? lower, double? upper)
    {
        if (lower is not { } l || upper is not { } u)
            return null;

        var nominal = (l + u) / 2;
        return nominal == 0 ? null : Math.Round((value - nominal) / Math.Abs(nominal) * 100, 3);
    }

    private ProjectDefinition PickProject()
    {
        var x = _random.NextDouble() * Scenario.Projects.Sum(p => p.Weight);
        foreach (var project in Scenario.Projects)
        {
            x -= project.Weight;
            if (x <= 0) return project;
        }

        return Scenario.Projects[^1];
    }

    private string NextSerial(ProjectDefinition project)
    {
        _serialCounters.TryGetValue(project.SerialPrefix, out var counter);
        _serialCounters[project.SerialPrefix] = ++counter;
        return $"{project.SerialPrefix}-{10000 + counter}";
    }

    private T Pick<T>(IReadOnlyList<T> items) => items[_random.Next(items.Count)];

    /// <summary>Standard normal value (Box–Muller).</summary>
    private double Gaussian()
    {
        var u1 = 1 - _random.NextDouble();
        var u2 = _random.NextDouble();
        return Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
    }

    private static double RoundSignificant(double value, int digits)
    {
        if (value == 0 || !double.IsFinite(value)) return value;
        var scale = Math.Pow(10, digits - 1 - (int)Math.Floor(Math.Log10(Math.Abs(value))));
        return Math.Round(value * scale) / scale;
    }

    private static string Hash(string text) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}