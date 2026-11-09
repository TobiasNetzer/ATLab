using System.Collections.Generic;
using System.Linq;
using ATLab.Pcb.Kicad;

namespace ATLab.FixtureDesigner.Fixture;

/// <summary>A hole of the board a tooling pin can stand in: an unplated hole or a plated through-hole pad.</summary>
public sealed record BoardHole(Point2 Position, double Diameter, string Reference, bool IsPlated);

/// <summary>The holes of a board for tooling pins, and the one at or near a point.</summary>
public static class BoardHoles
{
    /// <summary>How far beside a hole a click may be and still mean it, in mm.</summary>
    public const double SnapDistance = 3;

    /// <summary>A tooling pin counts as standing in a hole within this distance of its center, in mm.</summary>
    private const double Tolerance = 0.01;

    public static List<BoardHole> All(Board board)
    {
        var holes = board.Holes.Select(h => new BoardHole(h.Position, h.Diameter, h.FootprintReference, false)).ToList();
        foreach (var pad in board.Pads.Where(p => p.Kind == PadKind.ThroughHole && p.Drill > 0))
        {
            // Plated mounting holes are listed as holes as well: they count once, as plated
            var hole = new BoardHole(pad.Position, pad.Drill, pad.Footprint.Reference, true);
            var existing = holes.FindIndex(h => h.Position.DistanceTo(pad.Position) < Tolerance);
            if (existing >= 0)
                holes[existing] = hole;
            else
                holes.Add(hole);
        }

        return holes;
    }

    /// <summary>The nearest hole whose edge is within the snap distance of the point, or null.</summary>
    public static BoardHole? Near(Board board, Point2 point) =>
        All(board)
            .Where(h => h.Position.DistanceTo(point) <= h.Diameter / 2 + SnapDistance)
            .OrderBy(h => h.Position.DistanceTo(point))
            .FirstOrDefault();

    /// <summary>The hole a tooling pin at this point stands in, or null.</summary>
    public static BoardHole? At(Board board, Point2 point) =>
        All(board)
            .Where(h => h.Position.DistanceTo(point) < Tolerance)
            .OrderBy(h => h.Position.DistanceTo(point))
            .FirstOrDefault();
}