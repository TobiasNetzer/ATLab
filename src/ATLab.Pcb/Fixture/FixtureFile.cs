using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ATLab.Pcb.Kicad;

namespace ATLab.Pcb.Fixture;

/// <summary>
/// Contents of a fixture file (.atlabfix): the KiCad board, the probes and how they are wired to the adapter.
/// Plain JSON like ATLab's project files, so it can be kept under version control.
/// </summary>
public sealed class FixtureFile
{
    public const string Extension = ".atlabfix";
    public const string ExpectedFormatId = "atlab-fixture";

    /// <summary>
    /// Version 1: the board, the probes with their types and wiring, the parts (tooling, pre-centering and support pins,
    /// push rods) with their types, and the plates (probe plates, moving plate, pressure plate) with their outlines,
    /// thicknesses and openings, the components in the wiring and the wires besides the probes' own pins.
    /// </summary>
    public const int CurrentFormatVersion = 1;

    public string FormatId { get; set; } = ExpectedFormatId;
    public int FormatVersion { get; set; } = CurrentFormatVersion;

    public string Name { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceRevision { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    /// <summary>Device ID of the test hardware, which selects its pinout.</summary>
    public string TestHardwareDeviceId { get; set; } = string.Empty;

    /// <summary>Path of the KiCad board, relative to the fixture file if possible.</summary>
    public string BoardFile { get; set; } = string.Empty;

    /// <summary>SHA-256 of the board file when the fixture was saved.</summary>
    public string BoardHash { get; set; } = string.Empty;

    public List<ProbeEntry> Probes { get; set; } = [];

    /// <summary>Components in the wiring between the board and the test hardware, e.g. series resistors or relays.</summary>
    public List<ComponentEntry> Components { get; set; } = [];

    /// <summary>Wires besides a probe's own pin: to components, between probes, between pins.</summary>
    public List<WireEntry> Wires { get; set; } = [];

    /// <summary>Probe types of this fixture, copied from the library so the file is complete on its own.</summary>
    public List<ProbeType> ProbeTypes { get; set; } = [];

    /// <summary>ID of the type new probes get.</summary>
    public string? DefaultProbeType { get; set; }

    /// <summary>Types of the tooling, pre-centering and support pins and push rods, copied from the library like the probe types.</summary>
    public List<PartType> PartTypes { get; set; } = [];

    /// <summary>The placed tooling, pre-centering and support pins and push rods.</summary>
    public List<FixturePartEntry> Parts { get; set; } = [];

    /// <summary>The board lies upside down in the fixture: its top side faces the bottom plates.</summary>
    public bool IsBoardFlipped { get; set; }

    /// <summary>One plate above the board holds the top probes and the push rods; else a top probe plate of its own.</summary>
    public bool IsTopPlateCombined { get; set; }

    /// <summary>Thickness of each plate in the STEP files, in mm; a plate not listed has the default.</summary>
    public Dictionary<FixturePlate, double>? PlateThicknesses { get; set; }

    /// <summary>Board layers of the build sheet drawings per side, their order, colors and opacity; null for the defaults.</summary>
    public ATLab.Pcb.Rendering.ReportLayerSettings? BuildSheetLayers { get; set; }

    /// <summary>Outline every plate has unless it has one of its own; null for the board outline plus the margin.</summary>
    public PlateOutlineEntry? PlateOutline { get; set; }

    /// <summary>The plates with an outline of their own.</summary>
    public Dictionary<FixturePlate, PlateOutlineEntry>? OwnPlateOutlines { get; set; }

    public const double DefaultPlateMargin = 10;
    public const double DefaultPlateThickness = 8;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public static FixtureFile Load(string path)
    {
        FixtureFile? file;
        try
        {
            file = JsonSerializer.Deserialize<FixtureFile>(File.ReadAllText(path), JsonOptions);
        }
        catch (JsonException)
        {
            throw new FixtureFileException(FixtureFileError.Invalid);
        }

        if (file is null || file.FormatId != ExpectedFormatId)
            throw new FixtureFileException(FixtureFileError.Invalid);
        if (file.FormatVersion > CurrentFormatVersion)
            throw new FixtureFileException(FixtureFileError.Newer);

        return file;
    }

    public void Save(string path)
    {
        FormatId = ExpectedFormatId;
        FormatVersion = CurrentFormatVersion;

        // Write to a temporary file first, so a failed save never leaves a half written fixture
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(this, JsonOptions));
        File.Move(temp, path, overwrite: true);
    }

    /// <summary>Board path stored relative to the fixture file when both are on the same drive.</summary>
    public static string ToStoredPath(string boardPath, string fixturePath)
    {
        var folder = Path.GetDirectoryName(Path.GetFullPath(fixturePath));
        if (folder is null)
            return boardPath;

        var relative = Path.GetRelativePath(folder, boardPath);
        return Path.IsPathRooted(relative) ? boardPath : relative.Replace('\\', '/');
    }

    public static string ResolveStoredPath(string storedPath, string fixturePath)
    {
        if (Path.IsPathRooted(storedPath))
            return storedPath;

        var folder = Path.GetDirectoryName(Path.GetFullPath(fixturePath)) ?? string.Empty;
        return Path.GetFullPath(Path.Combine(folder, storedPath));
    }
}

/// <summary>A probe of the fixture: the pad it touches, from which side, and the adapter pin it is wired to.</summary>
public sealed class ProbeEntry
{
    public int Number { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Pad { get; set; } = string.Empty;
    public BoardSide Side { get; set; }

    /// <summary>Pin of the adapter's DUT connector, e.g. "c7"; null if not wired yet.</summary>
    public string? Pin { get; set; }

    public string Note { get; set; } = string.Empty;

    /// <summary>Note on the wire to its pin, for the wiring table of the build sheet.</summary>
    public string WiringNote { get; set; } = string.Empty;

    /// <summary>ID of the probe type; a probe without one, or with an unknown one, gets the default type.</summary>
    public string? ProbeType { get; set; }

    /// <summary>Net and position (mm, board coordinates) when saved, to notice changes of the board.</summary>
    public string Net { get; set; } = string.Empty;

    public double X { get; set; }
    public double Y { get; set; }
}

public enum FixtureFileError
{
    Invalid,
    Newer
}

public sealed class FixtureFileException(FixtureFileError error) : Exception(error.ToString())
{
    public FixtureFileError Error { get; } = error;
}

/// <summary>Where the outline of a plate comes from.</summary>
public enum PlateOutlineMode
{
    /// <summary>The board outline plus the plate margin.</summary>
    Automatic,

    /// <summary>A rectangle of a given width and height, e.g. the size of the fixture frame.</summary>
    FixedSize,

    /// <summary>A closed outline from a DXF file.</summary>
    Dxf,

    /// <summary>A closed outline from an SVG file.</summary>
    Svg,

    /// <summary>A closed outline drawn on a layer of the board in KiCad, e.g. User.1.</summary>
    BoardLayer
}

/// <summary>
/// Outline of a plate, or the one the plates share. A fixed size or an imported outline is
/// centered on the board and moved by the offset; an outline on a board layer is used where it is drawn.
/// </summary>
public sealed class PlateOutlineEntry
{
    public PlateOutlineMode Mode { get; set; }

    /// <summary>Distance of the automatic outline from the board outline, in mm.</summary>
    public double Margin { get; set; } = FixtureFile.DefaultPlateMargin;

    /// <summary>Size of a fixed-size outline, in mm.</summary>
    public double Width { get; set; }
    public double Height { get; set; }

    /// <summary>Shift of a fixed-size or imported outline from the centered position, in mm (X right, Y up).</summary>
    public double OffsetX { get; set; }
    public double OffsetY { get; set; }

    /// <summary>DXF or SVG file, relative to the fixture file if possible.</summary>
    public string? SourceFile { get; set; }

    /// <summary>Layer of the board with the outline.</summary>
    public string? Layer { get; set; }

    /// <summary>
    /// The outline read from the DXF or SVG file, in mm (Y up), so the fixture does not need the file afterwards:
    /// the points of the closed outline, the circles inside it, which become holes, and the other closed shapes inside
    /// it, which are cut out.
    /// </summary>
    public List<double[]>? Points { get; set; }
    public List<double[]>? Circles { get; set; }
    public List<List<double[]>>? Cutouts { get; set; }

    /// <summary>
    /// Lines of the drawing that are not part of a closed shape inside the outline, kept so the plate drawings have
    /// everything the file had; a closed one ends at its first point again.
    /// </summary>
    public List<List<double[]>>? Loose { get; set; }

    /// <summary>The circles and cut-outs milled only to a depth; every other one goes through the plate.</summary>
    public List<PlatePocketEntry>? Pockets { get; set; }
}

/// <summary>The side of a plate a pocket is milled from.</summary>
public enum PocketSide
{
    /// <summary>The side facing the board.</summary>
    BoardSide,

    /// <summary>The side facing away from the board.</summary>
    FarSide
}

/// <summary>A circle or cut-out of an imported outline that is milled only to a depth.</summary>
public sealed class PlatePocketEntry
{
    /// <summary>A circle (index into the circles) or a cut-out (index into the cut-outs).</summary>
    public bool IsCircle { get; set; }

    public int Index { get; set; }

    /// <summary>Depth in mm.</summary>
    public double Depth { get; set; }

    public PocketSide Side { get; set; }
}