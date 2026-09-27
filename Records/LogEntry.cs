using System;
using ATLab.Enums;

namespace ATLab.Records;

public sealed record LogEntry(
    DateTime Timestamp,
    LogLevel Level,
    string Message,
    string? File = null,
    string? Member = null,
    int? Line = null)
{
    public string DisplayText =>
        Level == LogLevel.ERROR &&
        !string.IsNullOrWhiteSpace(File)
            ? $"[{Timestamp:dd.MM.yyyy HH:mm:ss}] [{Level}] {Message} ({File}:{Line} in {Member}())"
            : $"[{Timestamp:dd.MM.yyyy HH:mm:ss}] [{Level}] {Message}";
}