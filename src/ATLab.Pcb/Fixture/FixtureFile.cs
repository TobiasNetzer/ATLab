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
    /// Version 1: first format. Version 2: probe types, locating pins and plate settings. Version 3: the outline of the
    /// probe plates (fixed size, imported from DXF or SVG, or a layer of the board). Version 4: pre-centering pins, support
    /// pins and push rods with their types. Version 5: locating pins are parts with types as well, in any hole of the board.
    /// </summary>
    public const int CurrentFormatVersion = 5;

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

    /// <summary>Probe types of this fixture, copied from the library so the file is complete on its own.</summary>
    public List<ProbeType> ProbeTypes { get; set; } = [];

    /// <summary>ID of the type new probes get.</summary>
    public string? DefaultProbeType { get; set; }

    /// <summary>Types of the locating, pre-centering and support pins and push rods, copied from the library like the probe types.</summary>
    public List<PartType> PartTypes { get; set; } = [];

    /// <summary>The placed locating, pre-centering and support pins and push rods.</summary>
    public List<FixturePartEntry> Parts { get; set; } = [];

    /// <summary>Distance of the plate outline from the board outline, in mm.</summary>
    public double PlateMargin { get; set; } = DefaultPlateMargin;

    /// <summary>Thickness of the plates in the STEP files, in mm.</summary>
    public double PlateThickness { get; set; } = DefaultPlateThickness;

    /// <summary>Board layers of the build sheet drawings per side, their order, colors and opacity; null for the defaults.</summary>
    public ATLab.Pcb.Rendering.ReportLayerSettings? BuildSheetLayers { get; set; }

    /// <summary>Outline of the bottom plate, and of the top plate too while it has none of its own; null for the board outline plus the margin.</summary>
    public PlateOutlineEntry? BottomPlateOutline { get; set; }

    /// <summary>Outline of the top plate if it has one of its own; null when it has the bottom plate's.</summary>
    public PlateOutlineEntry? TopPlateOutline { get; set; }

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

    /// <summary>ID of the probe type; files of version 1 have none and get the default type.</summary>
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

/// <summary>Where the outline of the probe plates comes from.</summary>
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
/// Outline of the probe plates, shared by the top and the bottom plate. A fixed size or an imported outline is
/// centered on the board and moved by the offset; an outline on a board layer is used where it is drawn.
/// </summary>
public sealed class PlateOutlineEntry
{
    public PlateOutlineMode Mode { get; set; }

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
}