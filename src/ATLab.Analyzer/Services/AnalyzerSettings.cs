using System;
using System.IO;
using System.Text.Json;

namespace ATLab.Analyzer.Services;

/// <summary>
/// Language and theme, saved as analyzer-settings.json next to ATLab's settings.json. Without saved settings the
/// analyzer starts in ATLab's language and theme.
/// </summary>
public sealed class AnalyzerSettings
{
    private static readonly string Folder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ATLab");

    private static readonly string FilePath = Path.Combine(Folder, "analyzer-settings.json");

    public string? Language { get; set; }
    public bool IsDarkMode { get; set; } = true;

    public static AnalyzerSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AnalyzerSettings>(File.ReadAllText(FilePath)) ?? new AnalyzerSettings();

            // First start: take over ATLab's choice
            var atlab = Path.Combine(Folder, "settings.json");
            if (File.Exists(atlab))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(atlab));
                var root = doc.RootElement;
                return new AnalyzerSettings
                {
                    Language = root.TryGetProperty("Language", out var l) && l.ValueKind == JsonValueKind.String ? l.GetString() : null,
                    IsDarkMode = !root.TryGetProperty("IsDarkMode", out var d) || d.ValueKind != JsonValueKind.False
                };
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // Unreadable settings: start with the defaults
        }

        return new AnalyzerSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Not saving the language is no reason to interrupt the user
        }
    }
}