using System;
using System.Collections.Generic;
using System.Linq;

namespace ATLab.Pcb.Kicad;

/// <summary>Side of the board, seen from the top (component side in KiCad: F.Cu).</summary>
public enum BoardSide
{
    Top,
    Bottom
}

public enum PadKind
{
    Smd,
    ThroughHole
}

public enum PadShape
{
    Circle,
    Rect,
    Oval,
    RoundRect,
    Other
}

/// <summary>Point in board coordinates of the KiCad file: millimeters, Y pointing down.</summary>
public readonly record struct Point2(double X, double Y)
{
    public static Point2 operator +(Point2 a, Point2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Point2 operator -(Point2 a, Point2 b) => new(a.X - b.X, a.Y - b.Y);

    public double DistanceTo(Point2 other) => Math.Sqrt((X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y));

    /// <summary>
    /// Turned like KiCad turns a footprint: positive angles are counterclockwise as seen on screen, where Y points down.
    /// </summary>
    public Point2 Rotate(double degrees)
    {
        if (degrees == 0)
            return this;

        var a = degrees * Math.PI / 180;
        var cos = Math.Cos(a);
        var sin = Math.Sin(a);
        return new Point2(X * cos + Y * sin, -X * sin + Y * cos);
    }
}

public readonly record struct Bounds2(double MinX, double MinY, double MaxX, double MaxY)
{
    public double Width => MaxX - MinX;
    public double Height => MaxY - MinY;
    public Point2 Center => new((MinX + MaxX) / 2, (MinY + MaxY) / 2);
    public bool IsEmpty => !(Width >= 0 && Height >= 0) || double.IsInfinity(MinX);

    public static Bounds2 Of(IEnumerable<Point2> points)
    {
        double minX = double.PositiveInfinity, minY = double.PositiveInfinity;
        double maxX = double.NegativeInfinity, maxY = double.NegativeInfinity;
        foreach (var p in points)
        {
            minX = Math.Min(minX, p.X);
            minY = Math.Min(minY, p.Y);
            maxX = Math.Max(maxX, p.X);
            maxY = Math.Max(maxY, p.Y);
        }

        return new Bounds2(minX, minY, maxX, maxY);
    }
}

/// <summary>Copper pad of a footprint, with its absolute position on the board.</summary>
public sealed class Pad
{
    public required Footprint Footprint { get; init; }
    public required string Number { get; init; }
    public required PadKind Kind { get; init; }
    public required PadShape Shape { get; init; }
    public required Point2 Position { get; init; }

    /// <summary>Size in mm before rotation.</summary>
    public required double Width { get; init; }

    public required double Height { get; init; }

    /// <summary>Absolute orientation in degrees (KiCad includes the rotation of the footprint).</summary>
    public required double Rotation { get; init; }

    /// <summary>Drill diameter of a through-hole pad, 0 for SMD pads.</summary>
    public double Drill { get; init; }

    public string Net { get; init; } = string.Empty;

    public bool HasTopCopper { get; init; }
    public bool HasBottomCopper { get; init; }
    public bool HasTopMaskOpening { get; init; }
    public bool HasBottomMaskOpening { get; init; }

    /// <summary>Key of the pad in a fixture file: footprint reference and pad number.</summary>
    public string Key => PadKeyOf(Footprint.Reference, Number);

    /// <summary>Copper on that side that is not covered by solder mask, so a probe can touch it.</summary>
    public bool IsAccessibleFrom(BoardSide side) =>
        side == BoardSide.Top ? HasTopCopper && HasTopMaskOpening : HasBottomCopper && HasBottomMaskOpening;

    public bool HasCopperOn(BoardSide side) => side == BoardSide.Top ? HasTopCopper : HasBottomCopper;

    /// <summary>Smallest dimension, the room a probe tip has on the pad.</summary>
    public double MinSize => Math.Min(Width, Height);

    public bool HasNet => Net.Length > 0 && !Net.StartsWith("unconnected-", StringComparison.Ordinal);

    public static string PadKeyOf(string reference, string number) => $"{reference}:{number}";

    public override string ToString() => $"{Footprint.Reference}.{Number}";
}

public sealed class Footprint
{
    public required string Reference { get; init; }
    public string Value { get; init; } = string.Empty;

    /// <summary>Library name, e.g. "TestPoint:TestPoint_Pad_D1.0mm".</summary>
    public string Library { get; init; } = string.Empty;

    public required BoardSide Side { get; init; }
    public required Point2 Position { get; init; }
    public double Rotation { get; init; }

    public List<Pad> Pads { get; } = [];

    /// <summary>Edges of the courtyard (F.CrtYd or B.CrtYd): the area the component takes up on its side.</summary>
    public List<(Point2 A, Point2 B)> CourtyardEdges { get; } = [];

    public bool HasCourtyard => CourtyardEdges.Count > 0;

    private Bounds2? _courtyardBounds;

    /// <summary>Area of the courtyard, to skip far away components quickly.</summary>
    public Bounds2 CourtyardBounds =>
        _courtyardBounds ??= Bounds2.Of(CourtyardEdges.SelectMany(e => new[] { e.A, e.B }));

    /// <summary>Distance of a point to the courtyard: 0 inside, otherwise to the nearest edge.</summary>
    public double DistanceToCourtyard(Point2 p)
    {
        if (!HasCourtyard)
            return double.PositiveInfinity;

        // Crossing test over all edges: works for closed loops however their edges are stored
        var inside = false;
        var nearest = double.PositiveInfinity;
        foreach (var (a, b) in CourtyardEdges)
        {
            if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X)
                inside = !inside;
            nearest = Math.Min(nearest, DistanceToSegment(p, a, b));
        }

        return inside ? 0 : nearest;
    }

    private static double DistanceToSegment(Point2 p, Point2 a, Point2 b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var length = dx * dx + dy * dy;
        var t = length == 0 ? 0 : Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / length, 0, 1);
        return p.DistanceTo(new Point2(a.X + t * dx, a.Y + t * dy));
    }

    /// <summary>Test point footprints, the usual places for probes: library name or reference says so.</summary>
    public bool IsTestPoint =>
        Library.Contains("TestPoint", StringComparison.OrdinalIgnoreCase) ||
        (Reference.StartsWith("TP", StringComparison.OrdinalIgnoreCase) && Reference.Length > 2 && char.IsDigit(Reference[2]));
}

/// <summary>Unplated hole, e.g. a mounting or tooling hole that can take a tooling pin.</summary>
public sealed record Hole(Point2 Position, double Diameter, string FootprintReference)
{
    /// <summary>Key of the hole in a fixture file: footprint reference and position, holes have no number.</summary>
    public string Key => string.Create(System.Globalization.CultureInfo.InvariantCulture,
        $"{FootprintReference}@{Position.X:0.###},{Position.Y:0.###}");
}

/// <summary>
/// Drawing on a board layer: a track, a line, the outline of a shape (Width &gt; 0) or a filled area (Filled), in
/// board coordinates. Tracks, vias and zones carry their net.
/// </summary>
/// <remarks>
/// An arc keeps its points along the curve for drawing, and its sweep (radians, positive counterclockwise in KiCad's
/// coordinates with Y down, i.e. clockwise on screen) from the first to the last point, so it can be written as an arc
/// again; 0 for anything that is not an arc.
/// </remarks>
public sealed record BoardGraphic(string Layer, IReadOnlyList<Point2> Points, double Width, bool Filled, string Net = "",
    double ArcSweep = 0);

/// <summary>Text on a board layer, e.g. a reference designator on the silkscreen.</summary>
public sealed record BoardText(string Layer, string Text, Point2 Position, double Height, double Angle, bool Mirrored, int HorizontalAlignment,
    int VerticalAlignment = 0);

/// <summary>What was read from a KiCad board file.</summary>
public sealed class Board
{
    public required string FilePath { get; init; }

    /// <summary>File format version, e.g. 20260206 for KiCad 10.</summary>
    public int FormatVersion { get; init; }

    public string GeneratorVersion { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Revision { get; init; } = string.Empty;
    public string Company { get; init; } = string.Empty;

    /// <summary>SHA-256 of the file, to notice a changed board.</summary>
    public string Hash { get; init; } = string.Empty;

    public List<Footprint> Footprints { get; } = [];

    /// <summary>Board outline (Edge.Cuts) as polylines; arcs and circles are approximated.</summary>
    public List<IReadOnlyList<Point2>> Outline { get; } = [];

    public List<Hole> Holes { get; } = [];

    /// <summary>Tracks, vias, zones and drawings of all layers except Edge.Cuts.</summary>
    public List<BoardGraphic> Graphics { get; } = [];

    public List<BoardText> Texts { get; } = [];

    /// <summary>Layers that have drawings or texts, in KiCad's order.</summary>
    public IReadOnlyList<string> Layers =>
        _layers ??= Graphics.Select(g => g.Layer).Concat(Texts.Select(t => t.Layer)).Distinct().OrderBy(LayerOrder).ThenBy(l => l).ToList();

    private IReadOnlyList<string>? _layers;

    private Dictionary<string, List<BoardGraphic>>? _copperByNet;

    /// <summary>Tracks, vias and zones of a net on all copper layers.</summary>
    public IReadOnlyList<BoardGraphic> GraphicsOfNet(string net)
    {
        _copperByNet ??= Graphics
            .Where(g => g.Net.Length > 0 && g.Layer.EndsWith(".Cu", StringComparison.Ordinal))
            .GroupBy(g => g.Net)
            .ToDictionary(g => g.Key, g => g.ToList());
        return _copperByNet.TryGetValue(net, out var list) ? list : [];
    }

    private IReadOnlyList<string>? _nets;

    /// <summary>Nets of the board's pads, sorted; nets without a pad are left out.</summary>
    public IReadOnlyList<string> Nets =>
        _nets ??= Pads.Where(p => p.HasNet).Select(p => p.Net).Distinct().Order(StringComparer.OrdinalIgnoreCase).ToList();

    /// <summary>Sort order of a layer: copper, silkscreen, mask, fab, courtyard, then the user layers.</summary>
    public static int LayerOrder(string layer) => layer switch
    {
        "F.Cu" => 0,
        "B.Cu" => 99,
        _ when layer.StartsWith("In", StringComparison.Ordinal) && layer.EndsWith(".Cu", StringComparison.Ordinal) =>
            int.TryParse(layer[2..^3], out var n) ? n : 50,
        "F.SilkS" => 100,
        "B.SilkS" => 101,
        "F.Mask" => 102,
        "B.Mask" => 103,
        "F.Paste" => 104,
        "B.Paste" => 105,
        "F.Fab" => 106,
        "B.Fab" => 107,
        "F.CrtYd" => 108,
        "B.CrtYd" => 109,
        "F.Adhes" => 110,
        "B.Adhes" => 111,
        "Dwgs.User" => 112,
        "Cmts.User" => 113,
        "Eco1.User" => 114,
        "Eco2.User" => 115,
        "Margin" => 116,
        _ => 200
    };

    /// <summary>Board side of a layer: F.* top, B.* bottom, null for inner and user layers.</summary>
    public static BoardSide? SideOfLayer(string layer) =>
        layer.StartsWith("F.", StringComparison.Ordinal) ? BoardSide.Top
        : layer.StartsWith("B.", StringComparison.Ordinal) ? BoardSide.Bottom
        : null;

    /// <summary>
    /// Origin of the coordinates in the build sheet: KiCad's drill/place file origin if it is set, else the bottom
    /// left corner of the board.
    /// </summary>
    public Point2 Origin { get; set; }

    public bool IsOriginFromFile { get; set; }

    public IEnumerable<Pad> Pads => Footprints.SelectMany(f => f.Pads);

    /// <summary>Area of the outline, or of all pads if the board has no outline.</summary>
    public Bounds2 Bounds
    {
        get
        {
            var outline = Bounds2.Of(Outline.SelectMany(p => p));
            if (!outline.IsEmpty)
                return outline;

            var pads = Bounds2.Of(Pads.Select(p => p.Position));
            return pads.IsEmpty ? new Bounds2(0, 0, 100, 100) : pads;
        }
    }

    /// <summary>
    /// Coordinates for the build sheet and the outputs: millimeters from the origin, Y pointing up, as seen from the
    /// top.
    /// </summary>
    public Point2 ToFixture(Point2 boardPoint) => new(boardPoint.X - Origin.X, Origin.Y - boardPoint.Y);

    public Pad? FindPad(string key) => Pads.FirstOrDefault(p => p.Key == key);

    /// <summary>
    /// The pad with the key nearest to a position. A footprint can have several pads with one number, e.g. the two
    /// halves of a solder jumper or a pad split in two, and only the position tells them apart.
    /// </summary>
    public Pad? FindPad(string key, Point2? near) => near is { } point
        ? Pads.Where(p => p.Key == key).OrderBy(p => p.Position.DistanceTo(point)).FirstOrDefault()
        : FindPad(key);
}