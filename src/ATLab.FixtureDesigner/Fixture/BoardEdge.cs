using System;
using System.Linq;
using ATLab.Pcb.Kicad;

namespace ATLab.FixtureDesigner.Fixture;

/// <summary>
/// The board outline (Edge.Cuts) for the parts: whether a point is on the board, how far it is from the edge, and the
/// place of a pre-centering pin beside the edge.
/// </summary>
public static class BoardEdge
{
    /// <summary>A pre-centering pin counts as at its distance from the edge within this tolerance, in mm.</summary>
    public const double TouchTolerance = 0.05;

    /// <summary>Crossing test over all edges, so cut-outs inside the board count as outside.</summary>
    public static bool Contains(Board board, Point2 p)
    {
        var inside = false;
        foreach (var polyline in board.Outline)
        {
            for (var i = 1; i < polyline.Count; i++)
            {
                var (a, b) = (polyline[i - 1], polyline[i]);
                if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X)
                    inside = !inside;
            }
        }

        return inside;
    }

    public static double Distance(Board board, Point2 p) => Nearest(board, p).Distance;

    /// <summary>The nearest point of the board edge.</summary>
    public static (Point2 Point, double Distance) Nearest(Board board, Point2 p)
    {
        var best = (Point: p, Distance: double.PositiveInfinity);
        foreach (var polyline in board.Outline)
        {
            for (var i = 1; i < polyline.Count; i++)
            {
                var (a, b) = (polyline[i - 1], polyline[i]);
                var dx = b.X - a.X;
                var dy = b.Y - a.Y;
                var lengthSquared = dx * dx + dy * dy;
                var t = lengthSquared == 0 ? 0 : Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lengthSquared, 0, 1);
                var q = new Point2(a.X + t * dx, a.Y + t * dy);
                var distance = q.DistanceTo(p);
                if (distance < best.Distance)
                    best = (q, distance);
            }
        }

        return best;
    }

    /// <summary>
    /// The center of a pin at the given distance (radius plus gap) from the board edge, outside it, next to the point: moved from
    /// the nearest point of the edge outwards by that distance. A point on the board is moved out across the edge.
    /// </summary>
    public static Point2 Snap(Board board, Point2 p, double radius)
    {
        if (board.Outline.Count == 0)
            return p;

        var (edge, distance) = Nearest(board, p);
        double ux, uy;
        if (distance > 1e-6)
        {
            // From the edge towards the point, turned outwards if the point is on the board
            (ux, uy) = ((p.X - edge.X) / distance, (p.Y - edge.Y) / distance);
            if (Contains(board, p))
                (ux, uy) = (-ux, -uy);
        }
        else
        {
            // Exactly on the edge: the side of a small step that is off the board
            (ux, uy) = Outward(board, edge);
        }

        return new Point2(edge.X + ux * radius, edge.Y + uy * radius);
    }

    private static (double X, double Y) Outward(Board board, Point2 edge)
    {
        foreach (var angle in Enumerable.Range(0, 16).Select(i => i * Math.PI / 8))
        {
            var (ux, uy) = (Math.Cos(angle), Math.Sin(angle));
            if (!Contains(board, new Point2(edge.X + ux * 0.1, edge.Y + uy * 0.1)))
                return (ux, uy);
        }

        return (0, -1);
    }
}