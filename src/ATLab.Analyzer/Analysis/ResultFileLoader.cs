using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Analyzer.Localization;
using ATLab.Results;

namespace ATLab.Analyzer.Analysis;

public sealed record LoadError(string FilePath, string Message);

public sealed record LoadResult(IReadOnlyList<LoadedRun> Runs, IReadOnlyList<LoadError> Errors);

/// <summary>Reads test result files, expanding folders to all JSON files below them.</summary>
public static class ResultFileLoader
{
    public static async Task<LoadResult> LoadAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        var files = ExpandPaths(paths, out var errors);

        var runs = new ConcurrentBag<LoadedRun>();
        var readErrors = new ConcurrentBag<LoadError>(errors);

        await Parallel.ForEachAsync(
            files,
            new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = Environment.ProcessorCount },
            async (file, ct) =>
            {
                try
                {
                    await using var stream = File.OpenRead(file);
                    var result = await TestResultJson.ReadAsync(stream, ct);
                    runs.Add(new LoadedRun(file, result));
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException
                                               or JsonException or NotSupportedException)
                {
                    readErrors.Add(new LoadError(file, ex.Message));
                }
            });

        return new LoadResult(
            runs.OrderBy(r => r.StartTime).ThenBy(r => r.FilePath, StringComparer.OrdinalIgnoreCase).ToList(),
            readErrors.OrderBy(e => e.FilePath, StringComparer.OrdinalIgnoreCase).ToList());
    }

    private static List<string> ExpandPaths(IEnumerable<string> paths, out List<LoadError> errors)
    {
        errors = [];
        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                try
                {
                    foreach (var file in Directory.EnumerateFiles(path, "*.json", SearchOption.AllDirectories))
                        files.Add(Path.GetFullPath(file));
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    errors.Add(new LoadError(path, ex.Message));
                }
            }
            else if (File.Exists(path))
            {
                files.Add(Path.GetFullPath(path));
            }
            else
            {
                errors.Add(new LoadError(path, Localizer.Get("Error_NotFound")));
            }
        }

        return files.ToList();
    }
}