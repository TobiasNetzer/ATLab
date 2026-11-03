using System;
using System.Collections.Generic;
using System.Linq;
using ATLab.FixtureDesigner.Fixture;
using ATLab.Pcb.Fixture;
using ATLab.Pcb.Kicad;
using ATLab.FixtureDesigner.Localization;

namespace ATLab.FixtureDesigner.Plates;

public enum PlateHoleKind
{
    Probe,
    LocatingPin,

    /// <summary>A circle of the plate outline, e.g. a screw of the fixture frame.</summary>
    Frame,

    /// <summary>A pre-centering pin beside the board (bottom plate).</summary>
    PreCenteringPin,

    /// <summary>A support pin under the board (bottom plate).</summary>
    SupportPin,

    /// <summary>A push rod above the board (top plate).</summary>
    PushRod
}

/// <summary>Hole of a plate in plate coordinates (mm, Y up).</summary>
public sealed record PlateHole(PlateHoleKind Kind, string Name, double X, double Y, double Diameter);

/// <summary>
/// A probe plate as the plate files describe it: seen from its side facing the board, where the receptacles are
/// inserted. The bottom plate is seen from above and has the coordinates of the build sheet; the top plate is seen
/// from below and is therefore mirrored left to right. The origin is the origin of the build sheet.
/// </summary>
public sealed class Plate
{
    public required BoardSide Side { get; init; }

    /// <summary>True for the top plate: X runs the other way than in the build sheet.</summary>
    public bool IsMirrored => Side == BoardSide.Top;

    /// <summary>Outline of the plate, counterclockwise, in plate coordinates, its arcs as arcs: what the drawings cut.</summary>
    public required IReadOnlyList<OutlineVertex> Contour { get; init; }

    /// <summary>The outline with its arcs in short segments, for the checks and the 3D model.</summary>
    public required IReadOnlyList<(double X, double Y)> Outline { get; init; }

    public double MinX => Outline.Min(p => p.X);
    public double MinY => Outline.Min(p => p.Y);
    public double MaxX => Outline.Max(p => p.X);
    public double MaxY => Outline.Max(p => p.Y);

    public double Width => MaxX - MinX;
    public double Height => MaxY - MinY;

    /// <summary>A plain rectangle, as the automatic and the fixed-size outline are.</summary>
    public bool IsRectangle => Outline.Count == 4 &&
                               Outline.All(p => Math.Abs(p.X - MinX) < 1e-9 || Math.Abs(p.X - MaxX) < 1e-9) &&
                               Outline.All(p => Math.Abs(p.Y - MinY) < 1e-9 || Math.Abs(p.Y - MaxY) < 1e-9);

    public List<PlateHole> Holes { get; } = [];

    /// <summary>Cut-outs of the plate that are not round, counterclockwise, arcs as arcs: what the drawings cut.</summary>
    public List<IReadOnlyList<OutlineVertex>> Cutouts { get; } = [];

    /// <summary>The cut-outs in short segments, for the checks and the 3D model.</summary>
    public IEnumerable<IReadOnlyList<(double X, double Y)>> CutoutOutlines => Cutouts.Select(OutlineShape.Flatten);

    /// <summary>Board outline in plate coordinates, as a reference for placing the board.</summary>
    public List<IReadOnlyList<(double X, double Y)>> BoardOutline { get; } = [];

    /// <summary>Holes that overlap or leave the plate; such a plate cannot be made as drawn.</summary>
    public List<string> Problems { get; } = [];

    /// <summary>Smallest wall left between two holes or a hole and the edge, in mm.</summary>
    public const double MinimumWall = 0.2;

    /// <summary>
    /// One plate for every side with probes; the bottom plate also when locating, pre-centering or support pins are
    /// planned, since the board rests on it, and the top plate when push rods are. Probes without pad or without drill
    /// diameter are left out. The outline of each plate is in build sheet coordinates, as seen from the top.
    /// </summary>
    public static List<Plate> Build(Board board, IReadOnlyList<Probe> probes, Func<BoardSide, OutlineShape> outlineOf,
        IReadOnlyList<FixturePart>? parts = null)
    {
        parts ??= [];
        var sides = probes.Where(p => p.Pad is not null).Select(p => p.Side).Distinct().ToList();
        if (parts.Any(p => p.PlateSide == BoardSide.Bottom) && !sides.Contains(BoardSide.Bottom))
            sides.Add(BoardSide.Bottom);
        if (parts.Any(p => p.PlateSide == BoardSide.Top) && !sides.Contains(BoardSide.Top))
            sides.Add(BoardSide.Top);

        return sides.Order().Select(side => Build(board, probes, outlineOf(side), parts, side)).ToList();
    }

    private static Plate Build(Board board, IReadOnlyList<Probe> probes, OutlineShape outline,
        IReadOnlyList<FixturePart> parts, BoardSide side)
    {
        // The top plate is mirrored about the middle of the board, as it is seen from below
        var centerX = PlateOutlines.BoardCenter(board).X;
        var mirror = side == BoardSide.Top;
        (double X, double Y) Flip((double X, double Y) p) => (mirror ? 2 * centerX - p.X : p.X, p.Y);
        (double X, double Y) Map(Point2 boardPoint)
        {
            var p = board.ToFixture(boardPoint);
            return Flip((p.X, p.Y));
        }

        // Mirroring turns the outline around and its arcs the other way; it stays counterclockwise
        var contour = mirror ? OutlineShape.MirrorX(outline.Vertices, centerX) : outline.Vertices.ToList();
        var plate = new Plate { Side = side, Contour = contour, Outline = OutlineShape.Flatten(contour) };

        foreach (var polyline in board.Outline)
            plate.BoardOutline.Add(polyline.Select(Map).ToList());

        foreach (var probe in probes.Where(p => p.Side == side && p.Pad is not null).OrderBy(p => p.Number))
        {
            var drill = probe.Type?.DrillDiameter ?? 0;
            if (drill <= 0)
            {
                plate.Problems.Add(Localizer.Format("Plate_NoDrill", probe.Name));
                continue;
            }

            var (x, y) = Map(probe.Pad!.Position);
            plate.Holes.Add(new PlateHole(PlateHoleKind.Probe, probe.Name, x, y, drill));
        }

        // Locating, pre-centering and support pins in the bottom plate, push rods in the top plate; a part without drill
        // gets no hole
        foreach (var part in parts.Where(p => p.PlateSide == side && p.Type is { DrillDiameter: > 0 }).OrderBy(p => p.Kind).ThenBy(p => p.Number))
        {
            var (x, y) = Map(new Point2(part.BoardX, part.BoardY));
            var kind = part.Kind switch
            {
                FixturePartKind.LocatingPin => PlateHoleKind.LocatingPin,
                FixturePartKind.PreCenteringPin => PlateHoleKind.PreCenteringPin,
                FixturePartKind.SupportPin => PlateHoleKind.SupportPin,
                _ => PlateHoleKind.PushRod
            };
            plate.Holes.Add(new PlateHole(kind, part.Name, x, y, part.Type!.DrillDiameter));
        }

        // Cut-outs mirror with the plate and stay counterclockwise
        foreach (var cutout in outline.Cutouts.Where(c => c.Count >= 2))
            plate.Cutouts.Add(mirror ? OutlineShape.MirrorX(cutout, centerX) : cutout.ToList());

        var number = 1;
        foreach (var circle in outline.Circles.Where(c => c.Diameter > 0))
        {
            var (x, y) = Flip((circle.X, circle.Y));
            plate.Holes.Add(new PlateHole(PlateHoleKind.Frame, $"M{number++}", x, y, circle.Diameter));
        }

        plate.FindProblems();
        return plate;
    }

    private void FindProblems()
    {
        var cutouts = CutoutOutlines.ToList();
        for (var i = 0; i < Holes.Count; i++)
        {
            var a = Holes[i];
            if (!OutlineShape.Contains(Outline, a.X, a.Y) ||
                OutlineShape.DistanceToEdge(Outline, a.X, a.Y) < a.Diameter / 2 + MinimumWall)
                Problems.Add(Localizer.Format("Plate_Edge", a.Name));

            // A hole in a cut-out or too close to its edge has no wall around it
            if (cutouts.Any(c => OutlineShape.Contains(c, a.X, a.Y) ||
                                 OutlineShape.DistanceToEdge(c, a.X, a.Y) < a.Diameter / 2 + MinimumWall))
                Problems.Add(Localizer.Format("Plate_Cutout", a.Name));

            for (var j = i + 1; j < Holes.Count; j++)
            {
                var b = Holes[j];
                var distance = Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
                if (distance < (a.Diameter + b.Diameter) / 2 + MinimumWall)
                    Problems.Add(Localizer.Format("Plate_Overlap", a.Name, b.Name));
            }
        }
    }
}

/// <summary>
/// The outline of the probe plates in build sheet coordinates (seen from the top): the board outline plus the margin,
/// a rectangle of a fixed size or an imported outline centered on the board and moved by the offset, or an outline
/// drawn on a layer of the board.
/// </summary>
public static class PlateOutlines
{
    /// <summary>Corners of the board's area in build sheet coordinates.</summary>
    public static (double MinX, double MinY, double MaxX, double MaxY) BoardArea(Board board)
    {
        var bounds = board.Bounds;
        var a = board.ToFixture(new Point2(bounds.MinX, bounds.MaxY));
        var b = board.ToFixture(new Point2(bounds.MaxX, bounds.MinY));
        return (Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
    }

    public static (double X, double Y) BoardCenter(Board board)
    {
        var (minX, minY, maxX, maxY) = BoardArea(board);
        return ((minX + maxX) / 2, (minY + maxY) / 2);
    }

    public static OutlineShape Automatic(Board board, double margin)
    {
        var (minX, minY, maxX, maxY) = BoardArea(board);
        return OutlineShape.Rectangle(maxX - minX + 2 * margin, maxY - minY + 2 * margin).Moved(minX - margin, minY - margin);
    }

    /// <summary>An outline of its own coordinates, its middle on the middle of the board plus the offset.</summary>
    public static OutlineShape Centered(Board board, OutlineShape shape, double offsetX, double offsetY)
    {
        var (cx, cy) = BoardCenter(board);
        return shape.Moved(cx + offsetX - (shape.MinX + shape.MaxX) / 2, cy + offsetY - (shape.MinY + shape.MaxY) / 2);
    }

    /// <summary>
    /// The closed outline drawn on a layer of the board, in build sheet coordinates; null if there is none. Arcs stay
    /// arcs: KiCad's Y runs down, the build sheet's up, so they turn the other way.
    /// </summary>
    public static OutlineShape? FromBoardLayer(Board board, string layer, Func<string, string> text)
    {
        (double X, double Y) Fixture(Point2 p)
        {
            var f = board.ToFixture(p);
            return (f.X, f.Y);
        }

        var pieces = new List<IReadOnlyList<OutlineVertex>>();
        foreach (var graphic in board.Graphics.Where(g => g.Layer == layer && g.Points.Count >= 2))
        {
            if (graphic.ArcSweep == 0)
            {
                pieces.Add(OutlineBuilder.Straight(graphic.Points.Select(Fixture)));
                continue;
            }

            // The arc's points are spread evenly over its sweep; longer than half a circle it is split at its middle
            var points = graphic.Points;
            var ends = Math.Abs(graphic.ArcSweep) > Math.PI ? new[] { 0, points.Count / 2, points.Count - 1 } : [0, points.Count - 1];
            var piece = new List<OutlineVertex>();
            for (var i = 0; i < ends.Length; i++)
            {
                var (x, y) = Fixture(points[ends[i]]);
                var bulge = i + 1 < ends.Length
                    ? -Math.Tan(graphic.ArcSweep * (ends[i + 1] - ends[i]) / (points.Count - 1) / 4)
                    : 0;
                piece.Add(new OutlineVertex(x, y, bulge));
            }

            pieces.Add(piece);
        }

        return OutlineBuilder.Build(pieces, [], text);
    }

    /// <summary>Layers of the board an outline can be drawn on: the user and drawing layers, and the margin.</summary>
    public static IReadOnlyList<string> OutlineLayers(Board board) =>
        board.Layers.Where(l => l.StartsWith("User.", StringComparison.Ordinal) ||
                                l is "Dwgs.User" or "Cmts.User" or "Eco1.User" or "Eco2.User" or "Margin")
            .ToList();
}