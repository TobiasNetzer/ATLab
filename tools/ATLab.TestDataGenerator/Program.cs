using System;
using System.Globalization;
using System.IO;
using System.Linq;
using ATLab.Results;
using ATLab.TestDataGenerator;

const string usage = """
    Writes synthetic ATLab JSON test result files, e.g. to try out ATLab Analyzer.

    Usage: dotnet run --project tools/ATLab.TestDataGenerator -- [options]

      -o, --output <folder>   Output folder (default: test-results)
      -n, --runs <count>      Number of runs (default: 500)
      -d, --days <count>      Calendar days the runs are spread over, weekends stay empty (default: 14)
      --start <yyyy-MM-dd>    First day (default: today minus --days)
      --seed <number>         Random seed; the same seed gives the same files (default: 1)
      --sort-by-verdict       Put the files into Pass and Fail subfolders
      -h, --help              Show this help
    """;

GeneratorOptions? options;
try
{
    options = Parse(args);
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine();
    Console.Error.WriteLine(usage);
    return 2;
}

if (options is null)
{
    Console.WriteLine(usage);
    return 0;
}

var runs = new Generator(options).Generate();
var output = Path.GetFullPath(options.OutputDirectory);

foreach (var run in runs)
{
    var path = Path.Combine(output, run.RelativePath);
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    await using var stream = File.Create(path);
    await TestResultJson.WriteAsync(stream, run.File);
}

var failed = runs.Count(r => r.File.Run.OverallResult == TestVerdict.Fail);
var devices = runs.Select(r => r.File.Run.SerialNumber).Distinct().Count();
Console.WriteLine($"{runs.Count} runs ({runs.Count - failed} passed, {failed} failed, {devices} serial numbers) written to {output}");
return 0;

// Null when the help was requested
static GeneratorOptions? Parse(string[] args)
{
    if (args.Any(a => a is "-h" or "--help" or "/?"))
        return null;

    var options = new GeneratorOptions();

    for (var i = 0; i < args.Length; i++)
    {
        string Value() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"Missing value for {args[i]}.");

        int Number(int min)
        {
            var name = args[i];
            var text = Value();
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) || n < min)
                throw new ArgumentException($"{name} needs a whole number of at least {min}, got '{text}'.");
            return n;
        }

        options = args[i] switch
        {
            "-o" or "--output" => options with { OutputDirectory = Value() },
            "-n" or "--runs" => options with { Runs = Number(1) },
            "-d" or "--days" => options with { Days = Number(1) },
            "--seed" => options with { Seed = Number(int.MinValue) },
            "--start" => options with
            {
                StartDate = DateOnly.TryParseExact(Value(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
                    ? d
                    : throw new ArgumentException($"--start needs a date like 2026-10-01, got '{args[i]}'.")
            },
            "--sort-by-verdict" => options with { SortByVerdict = true },
            _ => throw new ArgumentException($"Unknown option '{args[i]}'.")
        };
    }

    return options;
}