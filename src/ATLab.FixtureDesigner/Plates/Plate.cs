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
    /// <summary>A receptacle of a probe (probe plates).</summary>
    Probe,

    /// <summary>An opening a bottom probe comes through (moving plate).</summary>
    ProbeOpening,

    LocatingPin,

    /// <summary>A circle of the plate outline, e.g. a screw of the fixture frame.</summary>
    Frame,

    /// <summary>A pre-centering pin beside the board (moving plate).</summary>
    PreCenteringPin,

    /// <summary>A support pin under the board (moving plate).</summary>
    SupportPin,

    /// <summary>A push rod above the board (pressure plate).</summary>
    PushRod,

    /// <summary>An opening a push rod comes through (top probe plate).</summary>
    PushRodOpening
}

/// <summary>Hole of a plate in plate coordinates (mm, Y up).</summary>
public sealed record PlateHole(PlateHoleKind Kind, string Name, double X, double Y, double Diameter);

/// <summary>
/// A plate of the fixture as the plate files describe it: seen from its side facing the board. The plates below the
/// board are seen from above and have the coordinates of the build sheet; the plates above it are seen from below and
/// are therefore mirrored left to right. The origin is the origin of the build sheet.
/// </summary>
public sealed class Plate
{
    public required FixturePlate Kind { get; init; }

    /// <summary>The board lies upside down in the fixture.</summary>
    public bool IsBoardFlipped { get; init; }

    /// <summary>The side of the board the plate faces.</summary>
    public BoardSide Side => Kind.Side(IsBoardFlipped);

    /// <summary>
    /// True when X runs the other way than in the build sheet: for the plates above the board, or, with the board
    /// upside down, for the plates below it.
    /// </summary>
    public bool IsMirrored => !Kind.IsBelowBoard() != IsBoardFlipped;

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

    /// <summary>Openings too close to each other, merged into one each; their circles are not in <see cref="Holes"/>.</summary>
    public List<MergedOpening> MergedOpenings { get; } = [];

    /// <summary>The kind of the plate's openings, for the layer of the merged ones.</summary>
    public PlateHoleKind OpeningKind => Kind == FixturePlate.TopProbePlate ? PlateHoleKind.PushRodOpening : PlateHoleKind.ProbeOpening;

    /// <summary>Everything cut out inside the outline that is not round: the cut-outs and the merged openings.</summary>
    public IEnumerable<IReadOnlyList<OutlineVertex>> CutContours => Cutouts.Concat(MergedOpenings.Select(m => m.Contour));

    /// <summary>Board outline in plate coordinates, as a reference for placing the board.</summary>
    public List<IReadOnlyList<(double X, double Y)>> BoardOutline { get; } = [];

    /// <summary>Holes that overlap or leave the plate; such a plate cannot be made as drawn.</summary>
    public List<string> Problems { get; } = [];

    /// <summary>Smallest wall left between two holes or a hole and the edge, in mm.</summary>
    public const double MinimumWall = 0.2;

    /// <summary>
    /// The plates the fixture has: the bottom probe plate, the moving plate and the pressure plate always, the top probe
    /// plate when there are probes from above, unless the pressure plate holds them together with the push rods.
    /// </summary>
    public static IReadOnlyList<FixturePlate> PlatesOf(IReadOnlyList<Probe> probes, bool isBoardFlipped, bool isTopPlateCombined = false) =>
        !isTopPlateCombined && probes.Any(p => p.Side != FixturePlates.DownSide(isBoardFlipped))
            ? [FixturePlate.BottomProbePlate, FixturePlate.MovingPlate, FixturePlate.TopProbePlate, FixturePlate.PressurePlate]
            : [FixturePlate.BottomProbePlate, FixturePlate.MovingPlate, FixturePlate.PressurePlate];

    /// <summary>
    /// Every plate of the fixture with its holes. Probes without pad or without drill diameter are left out. The outline
    /// of each plate is in build sheet coordinates, as seen from the top.
    /// </summary>
    public static List<Plate> Build(Board board, IReadOnlyList<Probe> probes, Func<FixturePlate, OutlineShape> outlineOf,
        IReadOnlyList<FixturePart>? parts = null, bool isBoardFlipped = false, bool isTopPlateCombined = false)
    {
        parts ??= [];
        return PlatesOf(probes, isBoardFlipped, isTopPlateCombined)
            .Select(kind => Build(board, probes, outlineOf(kind), parts, kind, isBoardFlipped, isTopPlateCombined)).ToList();
    }

    private static Plate Build(Board board, IReadOnlyList<Probe> probes, OutlineShape outline,
        IReadOnlyList<FixturePart> parts, FixturePlate kind, bool isBoardFlipped, bool isTopPlateCombined)
    {
        // The plates above the board are mirrored about the middle of the board, as they are seen from below; with the
        // board upside down (turned over left to right) the plates below it instead
        var centerX = PlateOutlines.BoardCenter(board).X;
        var side = kind.Side(isBoardFlipped);
        var mirror = !kind.IsBelowBoard() != isBoardFlipped;
        (double X, double Y) Flip((double X, double Y) p) => (mirror ? 2 * centerX - p.X : p.X, p.Y);
        (double X, double Y) Map(Point2 boardPoint)
        {
            var p = board.ToFixture(boardPoint);
            return Flip((p.X, p.Y));
        }

        // Mirroring turns the outline around and its arcs the other way; it stays counterclockwise
        var contour = mirror ? OutlineShape.MirrorX(outline.Vertices, centerX) : outline.Vertices.ToList();
        var plate = new Plate { Kind = kind, IsBoardFlipped = isBoardFlipped, Contour = contour, Outline = OutlineShape.Flatten(contour) };

        foreach (var polyline in board.Outline)
            plate.BoardOutline.Add(polyline.Select(Map).ToList());

        // The receptacles of the probes of the plate's side: in the probe plates, and in a pressure plate that holds the
        // top probes together with the push rods
        var holdsProbes = kind is FixturePlate.BottomProbePlate or FixturePlate.TopProbePlate ||
                          (kind == FixturePlate.PressurePlate && isTopPlateCombined);
        if (holdsProbes)
        {
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
        }

        // The probes from below come through the moving plate; a probe without any diameter is reported by its probe plate
        if (kind == FixturePlate.MovingPlate)
        {
            foreach (var probe in probes.Where(p => p.Side == side && p.Pad is not null && p.Opening > 0).OrderBy(p => p.Number))
            {
                var (x, y) = Map(probe.Pad!.Position);
                plate.Holes.Add(new PlateHole(PlateHoleKind.ProbeOpening, probe.Name, x, y, probe.Opening));
            }
        }

        // Locating, pre-centering and support pins in the moving plate, push rods in the pressure plate; a part without
        // drill gets no hole
        foreach (var part in parts.Where(p => p.Plate == kind && p.Type is { DrillDiameter: > 0 }).OrderBy(p => p.Kind).ThenBy(p => p.Number))
        {
            var (x, y) = Map(new Point2(part.BoardX, part.BoardY));
            var holeKind = part.Kind switch
            {
                FixturePartKind.LocatingPin => PlateHoleKind.LocatingPin,
                FixturePartKind.PreCenteringPin => PlateHoleKind.PreCenteringPin,
                FixturePartKind.SupportPin => PlateHoleKind.SupportPin,
                _ => PlateHoleKind.PushRod
            };
            plate.Holes.Add(new PlateHole(holeKind, part.Name, x, y, part.Type!.DrillDiameter));
        }

        // The push rods come through a top probe plate of its own; a combined plate holds them
        if (kind == FixturePlate.TopProbePlate)
        {
            foreach (var rod in parts.Where(p => p.Kind == FixturePartKind.PushRod && p.Opening > 0).OrderBy(p => p.Number))
            {
                var (x, y) = Map(new Point2(rod.BoardX, rod.BoardY));
                plate.Holes.Add(new PlateHole(PlateHoleKind.PushRodOpening, rod.Name, x, y, rod.Opening));
            }
        }

        // Openings too close to each other become one opening each
        var openings = plate.Holes.Where(h => h.Kind is PlateHoleKind.ProbeOpening or PlateHoleKind.PushRodOpening).ToList();
        if (openings.Count > 1)
        {
            var (_, merged) = OpeningMerge.Merge(openings.Select(h => new OpeningCircle(h.Name, h.X, h.Y, h.Diameter)).ToList(), MinimumWall);
            var names = merged.SelectMany(m => m.Names).ToHashSet();
            plate.Holes.RemoveAll(h => h.Kind is PlateHoleKind.ProbeOpening or PlateHoleKind.PushRodOpening && names.Contains(h.Name));
            plate.MergedOpenings.AddRange(merged);
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

            // A hole in a cut-out or too close to its edge has no wall around it; an opening may lie in a cut-out, which
            // is then a larger opening for several probes
            if (a.Kind is not (PlateHoleKind.ProbeOpening or PlateHoleKind.PushRodOpening) &&
                cutouts.Any(c => OutlineShape.Contains(c, a.X, a.Y) ||
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

        // A merged opening needs its wall to the edge and to every hole (pins, frame) like a round one
        var mergedOutlines = MergedOpenings.Select(m => (m.Name, Outline: OutlineShape.Flatten(m.Contour))).ToList();
        for (var i = 0; i < mergedOutlines.Count; i++)
        {
            var (name, outline) = mergedOutlines[i];
            if (outline.Any(p => !OutlineShape.Contains(Outline, p.X, p.Y) || OutlineShape.DistanceToEdge(Outline, p.X, p.Y) < MinimumWall))
                Problems.Add(Localizer.Format("Plate_Edge", name));

            foreach (var hole in Holes.Where(h => OutlineShape.Contains(outline, h.X, h.Y) ||
                                                  OutlineShape.DistanceToEdge(outline, h.X, h.Y) < h.Diameter / 2 + MinimumWall))
                Problems.Add(Localizer.Format("Plate_Overlap", name, hole.Name));

            foreach (var (other, otherOutline) in mergedOutlines.Skip(i + 1))
                if (otherOutline.Any(p => OutlineShape.Contains(outline, p.X, p.Y) || OutlineShape.DistanceToEdge(outline, p.X, p.Y) < MinimumWall))
                    Problems.Add(Localizer.Format("Plate_Overlap", name, other));
        }
    }
}

/// <summary>
/// The outline of a plate in build sheet coordinates (seen from the top): the board outline plus the margin,
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