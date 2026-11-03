using System;
using System.Collections.Generic;
using System.Linq;

namespace ATLab.FixtureDesigner.Plates;

/// <summary>A circle of an outline drawing, in mm.</summary>
public readonly record struct OutlineCircle(double X, double Y, double Diameter);

/// <summary>
/// A point of an outline and how it goes on to the next point: straight (bulge 0) or along a circular arc. The bulge is
/// the tangent of a quarter of the arc's angle, positive counterclockwise, as in DXF.
/// </summary>
public readonly record struct OutlineVertex(double X, double Y, double Bulge = 0);

/// <summary>
/// A closed plate outline in mm (Y up), counterclockwise, with the circles inside it, which become holes of the plate
/// (e.g. the screws of the fixture frame), and the other closed shapes inside it, which are cut out of the plate (e.g. a
/// window for a connector). Read from a DXF or SVG file or from a layer of the board. Arcs stay arcs in
/// <see cref="Vertices"/> and the cut-outs, which the drawings are written from; <see cref="Points"/> follows them in
/// short segments for the checks, the board view and the 3D model.
/// </summary>
public sealed class OutlineShape
{
    private IReadOnlyList<(double X, double Y)>? _points;

    public required IReadOnlyList<OutlineVertex> Vertices { get; init; }
    public IReadOnlyList<OutlineCircle> Circles { get; init; } = [];

    /// <summary>Cut-outs that are not round: closed loops inside the outline, each counterclockwise, arcs as arcs.</summary>
    public IReadOnlyList<IReadOnlyList<OutlineVertex>> Cutouts { get; init; } = [];

    /// <summary>What could not be used, e.g. open lines.</summary>
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
        Cutouts = Cutouts.Select(l => (IReadOnlyList<OutlineVertex>)l.Select(v => v with { X = v.X + dx, Y = v.Y + dy }).ToList()).ToList(),
        Warnings = Warnings
    };

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

/// <summary>
/// Makes a plate outline of the pieces of a drawing: open pieces are joined end to end into closed loops, the largest
/// loop is the outline, circles and round loops inside it become holes, other loops inside it cut-outs. Arcs stay arcs
/// (their bulges move along when a piece is turned around). Everything that cannot be used is reported as a warning.
/// </summary>
public static class OutlineBuilder
{
    /// <summary>Ends closer than this are joined, in mm.</summary>
    private const double JoinTolerance = 0.05;

    /// <summary>Points closer than this along a loop are merged, in mm.</summary>
    private const double PointTolerance = 0.001;

    /// <summary>
    /// A piece of a drawing: its vertices in order; the bulge of a vertex belongs to the edge to the next one, the
    /// last vertex's bulge is not used.
    /// </summary>
    public static OutlineShape? Build(IEnumerable<IReadOnlyList<OutlineVertex>> pieces, IEnumerable<OutlineCircle> circles,
        Func<string, string> text)
    {
        var warnings = new List<string>();
        var loops = new List<List<OutlineVertex>>();
        var open = new List<List<OutlineVertex>>();

        foreach (var piece in pieces.Where(p => p.Count >= 2))
        {
            var vertices = piece.ToList();
            if (Near(vertices[0], vertices[^1]) && (vertices.Count > 3 || vertices.Any(v => v.Bulge != 0)))
                loops.Add(Close(vertices));
            else
                open.Add(vertices);
        }

        // Join open pieces into loops: each one is extended by a piece that starts or ends at one of its ends
        while (open.Count > 0)
        {
            var chain = open[0];
            open.RemoveAt(0);
            var extended = true;
            while (extended && !Near(chain[0], chain[^1]))
            {
                extended = false;
                for (var i = 0; i < open.Count; i++)
                {
                    var piece = open[i];
                    if (Near(chain[^1], piece[0])) Append(chain, piece);
                    else if (Near(chain[^1], piece[^1])) Append(chain, Turned(piece));
                    else if (Near(chain[0], piece[^1])) chain = Joined(piece, chain);
                    else if (Near(chain[0], piece[0])) chain = Joined(Turned(piece), chain);
                    else continue;

                    open.RemoveAt(i);
                    extended = true;
                    break;
                }
            }

            if (Near(chain[0], chain[^1]) && (chain.Count > 3 || chain.Any(v => v.Bulge != 0)))
                loops.Add(Close(chain));
            else
                warnings.Add(text("Outline_OpenLines"));
        }

        if (loops.Count == 0)
            return null;

        var flat = loops.Select(l => (Loop: l, Points: OutlineShape.Flatten(l))).ToList();
        var outer = flat.OrderByDescending(l => Math.Abs(OutlineShape.SignedArea(l.Points))).First();
        var outerVertices = OutlineShape.SignedArea(outer.Points) < 0 ? OutlineShape.Reversed(outer.Loop) : outer.Loop;
        var outerPoints = OutlineShape.Flatten(outerVertices);

        // Holes: circles of the drawing and round loops inside the outline; other loops inside it are cut out
        var holes = circles.Where(c => OutlineShape.Contains(outerPoints, c.X, c.Y)).ToList();
        var cutouts = new List<IReadOnlyList<OutlineVertex>>();
        foreach (var loop in flat.Where(l => !ReferenceEquals(l.Loop, outer.Loop)))
        {
            if (!loop.Points.All(p => OutlineShape.Contains(outerPoints, p.X, p.Y)))
                continue;

            if (AsCircle(loop.Points) is { } circle)
                holes.Add(circle);
            else
                cutouts.Add(OutlineShape.SignedArea(loop.Points) < 0 ? OutlineShape.Reversed(loop.Loop) : loop.Loop);
        }

        return new OutlineShape { Vertices = outerVertices, Circles = holes, Cutouts = cutouts, Warnings = warnings };
    }

    /// <summary>A polyline without arcs as a piece.</summary>
    public static IReadOnlyList<OutlineVertex> Straight(IEnumerable<(double X, double Y)> points) =>
        points.Select(p => new OutlineVertex(p.X, p.Y)).ToList();

    /// <summary>The piece the other way round: each bulge moves to the other end of its edge and changes sign.</summary>
    private static List<OutlineVertex> Turned(IReadOnlyList<OutlineVertex> piece)
    {
        var result = new List<OutlineVertex>(piece.Count);
        for (var i = piece.Count - 1; i >= 0; i--)
            result.Add(piece[i] with { Bulge = i > 0 ? -piece[i - 1].Bulge : 0 });
        return result;
    }

    /// <summary>The piece added at the end of the chain; the chain's last vertex takes the piece's first edge.</summary>
    private static void Append(List<OutlineVertex> chain, IReadOnlyList<OutlineVertex> piece)
    {
        chain[^1] = chain[^1] with { Bulge = piece[0].Bulge };
        chain.AddRange(piece.Skip(1));
    }

    /// <summary>The first piece followed by the second, which starts where the first ends.</summary>
    private static List<OutlineVertex> Joined(IReadOnlyList<OutlineVertex> first, List<OutlineVertex> second)
    {
        var result = first.ToList();
        Append(result, second);
        return result;
    }

    /// <summary>A loop without its repeated closing point and without points on top of each other.</summary>
    private static List<OutlineVertex> Close(List<OutlineVertex> loop)
    {
        var result = new List<OutlineVertex>();
        foreach (var v in loop)
        {
            if (result.Count > 0 && Math.Abs(v.X - result[^1].X) <= PointTolerance && Math.Abs(v.Y - result[^1].Y) <= PointTolerance)
            {
                // A repeated point: the edge leaving it is the later one
                result[^1] = result[^1] with { Bulge = v.Bulge };
                continue;
            }

            result.Add(v);
        }

        // The closing point is the first one again; its edge is the first vertex's
        if (result.Count > 1 && Near(result[0], result[^1]))
            result.RemoveAt(result.Count - 1);
        return OutlineShape.Joined(result);
    }

    /// <summary>A loop whose points all lie at the same distance from their center is a circle.</summary>
    private static OutlineCircle? AsCircle(IReadOnlyList<(double X, double Y)> loop)
    {
        if (loop.Count < 8)
            return null;

        var cx = loop.Average(p => p.X);
        var cy = loop.Average(p => p.Y);
        var radii = loop.Select(p => Math.Sqrt((p.X - cx) * (p.X - cx) + (p.Y - cy) * (p.Y - cy))).ToList();
        var mean = radii.Average();
        return mean > 0 && radii.All(r => Math.Abs(r - mean) <= Math.Max(0.02, mean * 0.02))
            ? new OutlineCircle(cx, cy, 2 * mean)
            : null;
    }

    private static bool Near(OutlineVertex a, OutlineVertex b) =>
        Math.Abs(a.X - b.X) <= JoinTolerance && Math.Abs(a.Y - b.Y) <= JoinTolerance;

    /// <summary>Points of a circular arc from start to end angle (radians), counterclockwise for a positive sweep.</summary>
    public static List<(double X, double Y)> Arc(double cx, double cy, double r, double start, double sweep)
    {
        var segments = Math.Max(4, (int)Math.Ceiling(Math.Abs(sweep) / (2 * Math.PI) * 360));
        var points = new List<(double X, double Y)>(segments + 1);
        for (var i = 0; i <= segments; i++)
        {
            var a = start + sweep * i / segments;
            points.Add((cx + r * Math.Cos(a), cy + r * Math.Sin(a)));
        }

        return points;
    }
}