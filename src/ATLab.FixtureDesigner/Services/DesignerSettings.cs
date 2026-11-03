using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ATLab.FixtureDesigner.Services;

/// <summary>
/// Language and theme, saved as fixture-designer-settings.json next to ATLab's settings.json. Without saved settings the
/// Fixture Designer starts in ATLab's language and theme.
/// </summary>
public sealed class DesignerSettings
{
    private static readonly string Folder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ATLab");

    private static readonly string FilePath = Path.Combine(Folder, "fixture-designer-settings.json");

    /// <summary>Settings of the app before it was renamed from Fixture Planner; read once if there are no others.</summary>
    private static readonly string LegacyFilePath = Path.Combine(Folder, "fixture-planner-settings.json");

    public string? Language { get; set; }
    public bool IsDarkMode { get; set; } = true;

    /// <summary>The user's default layers of the build sheet drawings, for fixtures without a choice.</summary>
    public ATLab.Pcb.Rendering.ReportLayerSettings? DefaultBuildSheetLayers { get; set; }

    /// <summary>Layers shown in the board view; null for the defaults.</summary>
    public List<string>? VisibleLayers { get; set; }

    /// <summary>Colors ("#RRGGBB") the user chose for KiCad layers in the board view; the others keep their default.</summary>
    public Dictionary<string, string>? LayerColors { get; set; }

    /// <summary>File types the manufacturing export writes.</summary>
    public bool IsExportDxf { get; set; } = true;
    public bool IsExportStep { get; set; } = true;

    /// <summary>SVG drawings are new and off unless chosen, so existing users get the files they had.</summary>
    public bool IsExportSvg { get; set; }

    public static DesignerSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<DesignerSettings>(File.ReadAllText(FilePath)) ?? new DesignerSettings();

            // Settings saved under the old name are taken over; the next save writes the new file
            if (File.Exists(LegacyFilePath))
                return JsonSerializer.Deserialize<DesignerSettings>(File.ReadAllText(LegacyFilePath)) ?? new DesignerSettings();

            // First start: take over ATLab's choice
            var atlab = Path.Combine(Folder, "settings.json");
            if (File.Exists(atlab))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(atlab));
                var root = doc.RootElement;
                return new DesignerSettings
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

        return new DesignerSettings();
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