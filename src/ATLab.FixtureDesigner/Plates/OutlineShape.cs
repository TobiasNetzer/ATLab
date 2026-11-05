using System;
using System.Collections.Generic;
using System.Linq;
using ATLab.Pcb.Fixture;

namespace ATLab.FixtureDesigner.Plates;

/// <summary>A circle or cut-out milled only to a depth (mm) from one side of the plate, instead of through it.</summary>
public readonly record struct Pocket(double Depth, PocketSide Side);

/// <summary>A circle of an outline drawing, in mm; through the plate unless it is a pocket.</summary>
public readonly record struct OutlineCircle(double X, double Y, double Diameter, Pocket? Pocket = null);

/// <summary>A closed shape inside the outline, counterclockwise, arcs as arcs; through the plate unless it is a pocket.</summary>
public sealed record OutlineCutout(IReadOnlyList<OutlineVertex> Vertices, Pocket? Pocket = null)
{
    public OutlineCutout Moved(double dx, double dy) => this with { Vertices = Vertices.Select(v => v with { X = v.X + dx, Y = v.Y + dy }).ToList() };
}

/// <summary>
/// A point of an outline and how it goes on to the next point: straight (bulge 0) or along a circular arc. The bulge is
/// the tangent of a quarter of the arc's angle, positive counterclockwise, as in DXF.
/// </summary>
public readonly record struct OutlineVertex(double X, double Y, double Bulge = 0);

/// <summary>
/// A closed plate outline in mm (Y up), counterclockwise, with the circles inside it, which become holes of the plate
/// (e.g. the screws of the fixture frame), and the other closed shapes inside it, which are cut out of the plate (e.g. a
/// window for a connector) or milled as pockets. Read from a DXF or SVG file or from a layer of the board. Arcs stay
/// arcs in <see cref="Vertices"/> and the cut-outs, which the drawings are written from; <see cref="Points"/> follows
/// them in short segments for the checks, the board view and the 3D model.
/// </summary>
public sealed class OutlineShape
{
    private IReadOnlyList<(double X, double Y)>? _points;

    public required IReadOnlyList<OutlineVertex> Vertices { get; init; }
    public IReadOnlyList<OutlineCircle> Circles { get; init; } = [];

    /// <summary>
    /// The other closed shapes inside the outline, each counterclockwise, arcs as arcs. A shape may share edges with
    /// the outline (e.g. a pocket open to the edge) or lie in another shape.
    /// </summary>
    public IReadOnlyList<OutlineCutout> Cutouts { get; init; } = [];

    /// <summary>
    /// Lines of the drawing that are no part of the outline or a shape inside it (open lines, shapes outside the
    /// outline), kept so the drawings of the plate have everything. A closed one ends at its first point again.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<OutlineVertex>> Loose { get; init; } = [];

    /// <summary>What could not be used, e.g. lines that cross.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>The outline with its arcs in short segments.</summary>
    public IReadOnlyList<(double X, double Y)> Points => _points ??= Flatten(Vertices);

    public bool HasArcs => Vertices.Any(v => Math.Abs(v.Bulge) > 1e-12);

    public double MinX => Points.Min(p => p.X);
    public double MinY => Points.Min(p => p.Y);
    public double MaxX => Points.Max(p => p.X);
    public double MaxY => Points.Max(p => p.Y);
    public double Width => MaxX - MinX;
    public double Height => MaxY - MinY;

    /// <summary>A rectangle of the given size with its lower left corner at the origin.</summary>
    public static OutlineShape Rectangle(double width, double height) => new()
    {
        Vertices = [new(0, 0), new(width, 0), new(width, height), new(0, height)]
    };

    /// <summary>The same outline moved by dx, dy.</summary>
    public OutlineShape Moved(double dx, double dy) => new()
    {
        Vertices = Vertices.Select(v => v with { X = v.X + dx, Y = v.Y + dy }).ToList(),
        Circles = Circles.Select(c => c with { X = c.X + dx, Y = c.Y + dy }).ToList(),
        Cutouts = Cutouts.Select(c => c.Moved(dx, dy)).ToList(),
        Loose = Loose.Select(l => (IReadOnlyList<OutlineVertex>)l.Select(v => v with { X = v.X + dx, Y = v.Y + dy }).ToList()).ToList(),
        Warnings = Warnings
    };

    /// <summary>The same shape with other pockets: by index of the circles and of the cut-outs, null for through.</summary>
    public OutlineShape WithPockets(Func<int, Pocket?> circlePocket, Func<int, Pocket?> cutoutPocket) => new()
    {
        Vertices = Vertices,
        Circles = Circles.Select((c, i) => c with { Pocket = circlePocket(i) }).ToList(),
        Cutouts = Cutouts.Select((c, i) => c with { Pocket = cutoutPocket(i) }).ToList(),
        Loose = Loose,
        Warnings = Warnings
    };

    /// <summary>A loose piece mirrored about x = axis: the order stays, the arcs turn the other way.</summary>
    public static List<OutlineVertex> MirrorPiece(IReadOnlyList<OutlineVertex> piece, double axis) =>
        piece.Select(v => v with { X = 2 * axis - v.X, Bulge = -v.Bulge }).ToList();

    /// <summary>
    /// The outline mirrored about x = axis, still counterclockwise: mirroring turns arcs the other way and the loop
    /// around, so the order is reversed, which turns the arcs back again.
    /// </summary>
    public static List<OutlineVertex> MirrorX(IReadOnlyList<OutlineVertex> vertices, double axis)
    {
        var mirrored = vertices.Select(v => v with { X = 2 * axis - v.X, Bulge = -v.Bulge }).ToList();
        return Reversed(mirrored);
    }

    /// <summary>
    /// The loop the other way round: every edge runs backwards, so its bulge moves to its other end and changes sign.
    /// </summary>
    public static List<OutlineVertex> Reversed(IReadOnlyList<OutlineVertex> vertices)
    {
        var n = vertices.Count;
        var result = new List<OutlineVertex>(n);
        for (var i = n - 1; i >= 0; i--)
        {
            // The edge before vertex i (from i-1 to i) now leaves vertex i
            var before = vertices[(i - 1 + n) % n];
            result.Add(vertices[i] with { Bulge = -before.Bulge });
        }

        return result;
    }

    /// <summary>
    /// The closed loop with edges that continue each other joined: arcs on the same circle that turn the same way
    /// become one arc (drawings often split a round end into two quarter arcs), straight edges on one line one edge.
    /// So a 3D model gets one face for each, without a seam. A loop keeps at least two vertices, an arc stays short
    /// of a full circle.
    /// </summary>
    public static List<OutlineVertex> Joined(IReadOnlyList<OutlineVertex> loop)
    {
        var result = loop.ToList();
        var changed = true;
        while (changed && result.Count > 2)
        {
            changed = false;
            for (var i = 0; i < result.Count && result.Count > 2; i++)
            {
                // Edge before vertex i (from prev to i) and the edge leaving it (from i to next)
                var prev = (i - 1 + result.Count) % result.Count;
                var (a, v, b) = (result[prev], result[i], result[(i + 1) % result.Count]);
                if (JoinedBulge(a, v, b) is not { } bulge)
                    continue;

                result[prev] = a with { Bulge = bulge };
                result.RemoveAt(i);
                changed = true;
                i--;
            }
        }

        return result;
    }

    /// <summary>The bulge of the single edge from a to b, if the edges a→v and v→b continue each other.</summary>
    private static double? JoinedBulge(OutlineVertex a, OutlineVertex v, OutlineVertex b)
    {
        const double tolerance = 1e-4; // mm
        var firstStraight = Math.Abs(a.Bulge) < 1e-12;
        var secondStraight = Math.Abs(v.Bulge) < 1e-12;

        if (firstStraight && secondStraight)
        {
            // On one line, in the same direction: v is no corner
            var (ux, uy) = (v.X - a.X, v.Y - a.Y);
            var (wx, wy) = (b.X - v.X, b.Y - v.Y);
            var lengths = Math.Sqrt(ux * ux + uy * uy) * Math.Sqrt(wx * wx + wy * wy);
            return lengths > 0 && Math.Abs(ux * wy - uy * wx) <= 1e-9 * lengths && ux * wx + uy * wy > 0 ? 0 : null;
        }

        if (firstStraight || secondStraight)
            return null;

        var (c1x, c1y, r1, _, s1) = ArcGeometry(a, v);
        var (c2x, c2y, r2, _, s2) = ArcGeometry(v, b);
        if (r1 <= 0 || r2 <= 0 || Math.Sign(s1) != Math.Sign(s2))
            return null;
        if (Math.Abs(c1x - c2x) > tolerance || Math.Abs(c1y - c2y) > tolerance || Math.Abs(r1 - r2) > tolerance)
            return null;

        var sweep = s1 + s2;
        return Math.Abs(sweep) < 2 * Math.PI - 1e-3 ? Math.Tan(sweep / 4) : null;
    }

    /// <summary>A closed loop of vertices as points, its arcs in segments of at most 1°.</summary>
    public static List<(double X, double Y)> Flatten(IReadOnlyList<OutlineVertex> vertices)
    {
        var points = new List<(double X, double Y)>();
        for (var i = 0; i < vertices.Count; i++)
        {
            var a = vertices[i];
            var b = vertices[(i + 1) % vertices.Count];
            points.Add((a.X, a.Y));
            if (Math.Abs(a.Bulge) > 1e-12)
                points.AddRange(ArcBetween(a, b).Skip(1).SkipLast(1));
        }

        return points;
    }

    /// <summary>Points of the arc from a to b with a's bulge, both ends included.</summary>
    public static List<(double X, double Y)> ArcBetween(OutlineVertex a, OutlineVertex b)
    {
        var (cx, cy, radius, start, sweep) = ArcGeometry(a, b);
        return radius <= 0 ? [(a.X, a.Y), (b.X, b.Y)] : OutlineBuilder.Arc(cx, cy, radius, start, sweep);
    }

    /// <summary>
    /// Center, radius, start angle and sweep of the arc from a to b: the center lies on the left normal of the chord, at
    /// chord · (1 − b²) / (4b) from its middle, which puts it on the right side for short and long arcs.
    /// </summary>
    public static (double Cx, double Cy, double Radius, double Start, double Sweep) ArcGeometry(OutlineVertex a, OutlineVertex b)
    {
        var chord = Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
        if (chord < 1e-12 || Math.Abs(a.Bulge) < 1e-12)
            return (0, 0, 0, 0, 0);

        var sweep = 4 * Math.Atan(a.Bulge);
        var (mx, my) = ((a.X + b.X) / 2, (a.Y + b.Y) / 2);
        var (nx, ny) = (-(b.Y - a.Y) / chord, (b.X - a.X) / chord);
        var offset = chord * (1 - a.Bulge * a.Bulge) / (4 * a.Bulge);
        var (cx, cy) = (mx + nx * offset, my + ny * offset);
        var radius = Math.Sqrt((a.X - cx) * (a.X - cx) + (a.Y - cy) * (a.Y - cy));
        return (cx, cy, radius, Math.Atan2(a.Y - cy, a.X - cx), sweep);
    }

    /// <summary>Signed area: positive for a counterclockwise polygon.</summary>
    public static double SignedArea(IReadOnlyList<(double X, double Y)> points)
    {
        var area = 0.0;
        for (var i = 0; i < points.Count; i++)
        {
            var a = points[i];
            var b = points[(i + 1) % points.Count];
            area += a.X * b.Y - b.X * a.Y;
        }

        return area / 2;
    }

    public static bool Contains(IReadOnlyList<(double X, double Y)> polygon, double x, double y)
    {
        var inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            var (xi, yi) = polygon[i];
            var (xj, yj) = polygon[j];
            if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi)
                inside = !inside;
        }

        return inside;
    }

    /// <summary>Smallest distance from a point to the edges of a polygon.</summary>
    public static double DistanceToEdge(IReadOnlyList<(double X, double Y)> polygon, double x, double y)
    {
        var best = double.MaxValue;
        for (var i = 0; i < polygon.Count; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % polygon.Count];
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            var lengthSquared = dx * dx + dy * dy;
            var t = lengthSquared == 0 ? 0 : Math.Clamp(((x - a.X) * dx + (y - a.Y) * dy) / lengthSquared, 0, 1);
            var px = a.X + t * dx - x;
            var py = a.Y + t * dy - y;
            best = Math.Min(best, Math.Sqrt(px * px + py * py));
        }

        return best;
    }
}
