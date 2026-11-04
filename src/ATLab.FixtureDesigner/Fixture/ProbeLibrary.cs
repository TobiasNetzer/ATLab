using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ATLab.Pcb.Controls;
using ATLab.Pcb.Fixture;
using ATLab.Pcb.Kicad;
using CommunityToolkit.Mvvm.ComponentModel;


namespace ATLab.FixtureDesigner.Fixture;

/// <summary>
/// The user's probe types, saved as probe-library.json next to ATLab's settings. A new fixture starts with a copy of
/// them; every fixture keeps its own copy, so it stays complete when the library changes.
/// </summary>
public static class ProbeLibrary
{
    private static readonly string FilePath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ATLab", "probe-library.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>The saved library, or example types if none is saved yet.</summary>
    public static List<ProbeType> Load()
    {
        try
        {
            if (File.Exists(FilePath) &&
                JsonSerializer.Deserialize<List<ProbeType>>(File.ReadAllText(FilePath), JsonOptions) is { Count: > 0 } types)
                return types;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // An unreadable library is replaced by the examples; the file stays as it is until the user saves
        }

        return Examples();
    }

    public static void Save(IEnumerable<ProbeType> types)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var temp = FilePath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(types.ToList(), JsonOptions));
        File.Move(temp, FilePath, overwrite: true);
    }

    /// <summary>Adds or replaces a type (same ID) in the saved library.</summary>
    public static void Store(ProbeType type)
    {
        var library = Load();
        library.RemoveAll(t => t.Id == type.Id);
        library.Add(type.Copy());
        Save(library);
    }

    /// <summary>
    /// Starting points for the usual pitches. The dimensions differ between manufacturers, so they are marked as
    /// examples to check against the data sheet.
    /// </summary>
    public static List<ProbeType> Examples() =>
    [
        new() { Id = "example100", Name = "100 mil", MinPadSize = 1.0, DrillDiameter = 1.67, OpeningDiameter = 2.0, MinSpacing = 2.54, ComponentClearance = 1.0 },
        new() { Id = "example75", Name = "75 mil", MinPadSize = 0.8, DrillDiameter = 1.37, OpeningDiameter = 1.6, MinSpacing = 1.91, ComponentClearance = 0.8 },
        new() { Id = "example50", Name = "50 mil", MinPadSize = 0.6, DrillDiameter = 0.91, OpeningDiameter = 1.0, MinSpacing = 1.27, ComponentClearance = 0.6 }
    ];
}