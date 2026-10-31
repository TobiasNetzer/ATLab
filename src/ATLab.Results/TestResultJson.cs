using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace ATLab.Results;

/// <summary>
/// Serializer settings of the JSON test result file, used by ATLab to write the files and by evaluation tools to
/// read them, so both sides always agree on names and enum spelling.
/// </summary>
public static class TestResultJson
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Reads a test result file and checks its format ID and version.
    /// </summary>
    /// <exception cref="InvalidDataException">The file is not a test result file or has an unsupported version.</exception>
    /// <exception cref="JsonException">The file is not valid JSON or does not match the format.</exception>
    public static async Task<TestResultFile> ReadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var file = await JsonSerializer.DeserializeAsync<TestResultFile>(stream, Options, cancellationToken)
                   ?? throw new InvalidDataException("The file is empty.");

        Validate(file);
        return file;
    }

    /// <inheritdoc cref="ReadAsync(Stream, CancellationToken)"/>
    public static TestResultFile Read(string json)
    {
        var file = JsonSerializer.Deserialize<TestResultFile>(json, Options)
                   ?? throw new InvalidDataException("The file is empty.");

        Validate(file);
        return file;
    }

    public static async Task WriteAsync(Stream stream, TestResultFile file, CancellationToken cancellationToken = default) =>
        await JsonSerializer.SerializeAsync(stream, file, Options, cancellationToken);

    private static void Validate(TestResultFile file)
    {
        if (!string.Equals(file.FormatId, TestResultFile.CurrentFormatId, StringComparison.Ordinal))
            throw new InvalidDataException($"Not an ATLab test result file (format ID '{file.FormatId}').");

        if (file.FormatVersion > TestResultFile.CurrentFormatVersion)
            throw new InvalidDataException(
                $"Format version {file.FormatVersion} is newer than the supported version {TestResultFile.CurrentFormatVersion}.");
    }
}