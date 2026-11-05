using System;
using System.Collections.Generic;
using System.Linq;

namespace ATLab.FixtureDesigner.Plates;

/// <summary>
/// The lines of a drawing as edges that meet at points (nodes), dividing the plane into areas. Ends closer than the
/// tolerance are one node, an edge another one ends on is split there, and edges drawn twice count once. Edges that
/// cross between their ends are not split (<see cref="HasCrossings"/> finds them). Arcs stay arcs.
/// </summary>
internal sealed class PlanarGraph
{
    /// <summary>
    /// An edge between two nodes with the bulge of its arc from A to B. The exact ends of the drawing are kept for the
    /// directions at the nodes, which tangent edges need to be told apart.
    /// </summary>
    public sealed record Edge(int A, int B, double Bulge, (double X, double Y) Pa, (double X, double Y) Pb);

    /// <summary>
    /// A closed loop of half edges with its area on the left; positive area: counterclockwise, the border of the area
    /// inside it; negative: clockwise, the inner border of the area around it. Half edge 2e runs along edge e from A to
    /// B, half edge 2e + 1 back.
    /// </summary>
    public sealed record Loop(List<int> HalfEdges, List<OutlineVertex> Vertices, List<(double X, double Y)> Points, double Area);

    private readonly double _tolerance;

    public List<(double X, double Y)> Nodes { get; }
    public List<Edge> Edges { get; }

    private PlanarGraph(double tolerance, List<(double X, double Y)> nodes, List<Edge> edges)
    {
        _tolerance = tolerance;
        Nodes = nodes;
        Edges = edges;
    }

    /// <summary>
    /// The graph of pieces of a drawing: their vertices in order; the bulge of a vertex belongs to the edge to the next
    /// one, the last vertex's bulge is not used.
    /// </summary>
    public static PlanarGraph Build(IEnumerable<IReadOnlyList<OutlineVertex>> pieces, double tolerance)
    {
        var nodes = new NodeSet(tolerance);
        var edges = new List<Edge>();
        foreach (var piece in pieces)
        {
            for (var i = 0; i + 1 < piece.Count; i++)
            {
                var (a, b) = (piece[i], piece[i + 1]);
                var (na, nb) = (nodes.Find(a.X, a.Y), nodes.Find(b.X, b.Y));
                if (na != nb)
                    edges.Add(new Edge(na, nb, a.Bulge, (a.X, a.Y), (b.X, b.Y)));
            }
        }

        return new PlanarGraph(tolerance, nodes.Points, Distinct(Split(edges, nodes.Points, tolerance)));
    }

    /// <summary>A closed loop as a piece: it ends at its first vertex again.</summary>
    public static IReadOnlyList<OutlineVertex> Closed(IReadOnlyList<OutlineVertex> loop) => [.. loop, loop[0] with { Bulge = 0 }];

    /// <summary>A circle as a closed piece of two half circles, counterclockwise.</summary>
    public static IReadOnlyList<OutlineVertex> Circle(double x, double y, double diameter)
    {
        var r = diameter / 2;
        return [new(x + r, y, 1), new(x - r, y, 1), new(x + r, y)];
    }

    public static bool IsStraight(double bulge) => Math.Abs(bulge) < 1e-12;

    public int Origin(int halfEdge) => halfEdge % 2 == 0 ? Edges[halfEdge / 2].A : Edges[halfEdge / 2].B;

    public double Bulge(int halfEdge) => halfEdge % 2 == 0 ? Edges[halfEdge / 2].Bulge : -Edges[halfEdge / 2].Bulge;

    /// <summary>The edge from node A to B as two vertices, with A's bulge.</summary>
    public (OutlineVertex A, OutlineVertex B) Ends(int edge)
    {
        var e = Edges[edge];
        return (new OutlineVertex(Nodes[e.A].X, Nodes[e.A].Y, e.Bulge), new OutlineVertex(Nodes[e.B].X, Nodes[e.B].Y));
    }

    public double Length(int edge)
    {
        var e = Edges[edge];
        var chord = Distance(Nodes[e.A], Nodes[e.B]);
        var sweep = Math.Abs(4 * Math.Atan(e.Bulge));
        return sweep < 1e-9 ? chord : chord * sweep / (2 * Math.Sin(sweep / 2));
    }

    // ----- Building -----

    /// <summary>The points of an edge, its arc in short segments, from its exact ends.</summary>
    private static List<(double X, double Y)> Flat(Edge e) =>
        IsStraight(e.Bulge)
            ? [e.Pa, e.Pb]
            : OutlineShape.ArcBetween(new OutlineVertex(e.Pa.X, e.Pa.Y, e.Bulge), new OutlineVertex(e.Pb.X, e.Pb.Y));

    /// <summary>Every edge split where another edge ends on it.</summary>
    private static List<Edge> Split(List<Edge> edges, List<(double X, double Y)> nodes, double tolerance)
    {
        var result = new List<Edge>();
        foreach (var e in edges)
        {
            var flat = Flat(e);
            var (minX, maxX) = (flat.Min(p => p.X) - tolerance, flat.Max(p => p.X) + tolerance);
            var (minY, maxY) = (flat.Min(p => p.Y) - tolerance, flat.Max(p => p.Y) + tolerance);
            var on = new List<(double F, int Node)>();
            for (var n = 0; n < nodes.Count; n++)
            {
                var p = nodes[n];
                if (n == e.A || n == e.B || p.X < minX || p.X > maxX || p.Y < minY || p.Y > maxY)
                    continue;
                if (Distance(p, e.Pa) <= tolerance || Distance(p, e.Pb) <= tolerance)
                    continue;
                if (Parameter(e, p, tolerance) is { } f)
                    on.Add((f, n));
            }

            if (on.Count == 0)
            {
                result.Add(e);
                continue;
            }

            on.Sort((x, y) => x.F.CompareTo(y.F));
            var straight = IsStraight(e.Bulge);
            var (cx, cy, r, start, sweep) = straight
                ? default
                : OutlineShape.ArcGeometry(new OutlineVertex(e.Pa.X, e.Pa.Y, e.Bulge), new OutlineVertex(e.Pb.X, e.Pb.Y));
            (double X, double Y) At(double f) => straight
                ? (e.Pa.X + (e.Pb.X - e.Pa.X) * f, e.Pa.Y + (e.Pb.Y - e.Pa.Y) * f)
                : (cx + r * Math.Cos(start + sweep * f), cy + r * Math.Sin(start + sweep * f));

            var stops = new List<(double F, int Node, (double X, double Y) P)> { (0, e.A, e.Pa) };
            stops.AddRange(on.Select(o => (o.F, o.Node, At(o.F))));
            stops.Add((1, e.B, e.Pb));
            for (var i = 0; i + 1 < stops.Count; i++)
            {
                var (a, b) = (stops[i], stops[i + 1]);
                if (a.Node != b.Node)
                    result.Add(new Edge(a.Node, b.Node, straight ? 0 : Math.Tan(sweep * (b.F - a.F) / 4), a.P, b.P));
            }
        }

        return result;
    }

    /// <summary>Where along an edge (0 to 1) a point lies on it, or null if it does not.</summary>
    private static double? Parameter(Edge e, (double X, double Y) p, double tolerance)
    {
        if (IsStraight(e.Bulge))
        {
            var (dx, dy) = (e.Pb.X - e.Pa.X, e.Pb.Y - e.Pa.Y);
            var t = ((p.X - e.Pa.X) * dx + (p.Y - e.Pa.Y) * dy) / (dx * dx + dy * dy);
            if (t <= 0 || t >= 1)
                return null;
            return Distance(p, (e.Pa.X + t * dx, e.Pa.Y + t * dy)) <= tolerance ? t : null;
        }

        var (cx, cy, r, start, sweep) = OutlineShape.ArcGeometry(new OutlineVertex(e.Pa.X, e.Pa.Y, e.Bulge), new OutlineVertex(e.Pb.X, e.Pb.Y));
        if (Math.Abs(Distance(p, (cx, cy)) - r) > tolerance)
            return null;

        // The angle from the start, in the direction of the arc
        var turn = Math.Atan2(p.Y - cy, p.X - cx) - start;
        turn = sweep > 0 ? Modulo(turn, 2 * Math.PI) : -Modulo(-turn, 2 * Math.PI);
        var f = turn / sweep;
        return f is > 0 and < 1 ? f : null;
    }

    /// <summary>Edges drawn more than once count once: the same ends and the same arc, in either direction.</summary>
    private static List<Edge> Distinct(List<Edge> edges)
    {
        var result = new List<Edge>();
        var byEnds = new Dictionary<(int, int), List<Edge>>();
        foreach (var e in edges)
        {
            var key = (Math.Min(e.A, e.B), Math.Max(e.A, e.B));
            if (!byEnds.TryGetValue(key, out var same))
                byEnds[key] = same = [];

            if (same.Any(s => s.A == e.A ? Math.Abs(s.Bulge - e.Bulge) < 1e-4 : Math.Abs(s.Bulge + e.Bulge) < 1e-4))
                continue;

            same.Add(e);
            result.Add(e);
        }

        return result;
    }

    /// <summary>Whether two edges (or an edge and a circle) cross somewhere else than at the nodes.</summary>
    public bool HasCrossings(IEnumerable<OutlineCircle> circles)
    {
        var lines = Edges.Select(e => (Points: Flat(e), Ends: new[] { Nodes[e.A], Nodes[e.B] }))
            .Concat(circles.Select(c => (Points: OutlineBuilder.Arc(c.X, c.Y, c.Diameter / 2, 0, 2 * Math.PI), Ends: Array.Empty<(double X, double Y)>())))
            .Select(l => (l.Points, l.Ends, MinX: l.Points.Min(p => p.X), MaxX: l.Points.Max(p => p.X),
                MinY: l.Points.Min(p => p.Y), MaxY: l.Points.Max(p => p.Y)))
            .ToList();

        for (var i = 0; i < lines.Count; i++)
        {
            var a = lines[i];
            for (var j = i + 1; j < lines.Count; j++)
            {
                var b = lines[j];
                if (a.MaxX < b.MinX || b.MaxX < a.MinX || a.MaxY < b.MinY || b.MaxY < a.MinY)
                    continue;

                for (var s = 0; s + 1 < a.Points.Count; s++)
                for (var t = 0; t + 1 < b.Points.Count; t++)
                {
                    if (Intersection(a.Points[s], a.Points[s + 1], b.Points[t], b.Points[t + 1]) is { } p &&
                        !a.Ends.Concat(b.Ends).Any(end => Distance(end, p) <= 2 * _tolerance))
                        return true;
                }
            }
        }

        return false;
    }

    /// <summary>The point where two segments cross, if they do (not where they only touch or lie on one line).</summary>
    private static (double X, double Y)? Intersection((double X, double Y) a, (double X, double Y) b, (double X, double Y) c, (double X, double Y) d)
    {
        static double Cross((double X, double Y) o, (double X, double Y) p, (double X, double Y) q) =>
            (p.X - o.X) * (q.Y - o.Y) - (p.Y - o.Y) * (q.X - o.X);

        var (d1, d2) = (Cross(c, d, a), Cross(c, d, b));
        var (d3, d4) = (Cross(a, b, c), Cross(a, b, d));
        if (!(d1 * d2 < 0 && d3 * d4 < 0))
            return null;

        var t = d1 / (d1 - d2);
        return (a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
    }

    // ----- Areas -----

    /// <summary>
    /// The borders of the areas. Every half edge has an area on its left; from the end of a half edge the border goes
    /// on along the next edge clockwise from the one it came along. A border that runs along an edge there and back (the
    /// same area on both sides: a dead end, or a line joining two loops) is split there.
    /// </summary>
    public List<Loop> Loops()
    {
        // The half edges leaving each node, counterclockwise by their direction; tangent edges by how they bend
        var count = 2 * Edges.Count;
        var direction = new (double Angle, double Curvature)[count];
        var leaving = Enumerable.Range(0, Nodes.Count).Select(_ => new List<int>()).ToArray();
        for (var h = 0; h < count; h++)
        {
            var e = Edges[h / 2];
            direction[h] = h % 2 == 0 ? Direction(e.Pa, e.Pb, e.Bulge) : Direction(e.Pb, e.Pa, -e.Bulge);
            leaving[Origin(h)].Add(h);
        }

        var position = new int[count];
        foreach (var list in leaving)
        {
            list.Sort((x, y) =>
            {
                var (a, b) = (direction[x], direction[y]);
                return Math.Abs(a.Angle - b.Angle) > 1e-7 ? a.Angle.CompareTo(b.Angle) : a.Curvature.CompareTo(b.Curvature);
            });
            for (var i = 0; i < list.Count; i++)
                position[list[i]] = i;
        }

        int Next(int h)
        {
            var twin = h ^ 1;
            var list = leaving[Origin(twin)];
            return list[(position[twin] - 1 + list.Count) % list.Count];
        }

        var area = Enumerable.Repeat(-1, count).ToArray();
        var borders = new List<List<int>>();
        for (var h = 0; h < count; h++)
        {
            if (area[h] >= 0)
                continue;

            var border = new List<int>();
            for (var x = h; area[x] < 0; x = Next(x))
            {
                area[x] = borders.Count;
                border.Add(x);
            }

            borders.Add(border);
        }

        var loops = new List<Loop>();
        for (var f = 0; f < borders.Count; f++)
        {
            foreach (var halfEdges in Simple(borders[f], f, area))
            {
                if (Origin(halfEdges[0]) != Origin(halfEdges[^1] ^ 1))
                    continue;

                var vertices = halfEdges.Select(h => new OutlineVertex(Nodes[Origin(h)].X, Nodes[Origin(h)].Y, Bulge(h))).ToList();
                var points = OutlineShape.Flatten(vertices);
                var signed = OutlineShape.SignedArea(points);
                if (Math.Abs(signed) > 1e-9)
                    loops.Add(new Loop(halfEdges, vertices, points, signed));
            }
        }

        return loops;
    }

    /// <summary>A border without the edges it runs along there and back, split into the loops these joined.</summary>
    private static List<List<int>> Simple(List<int> border, int f, int[] area)
    {
        bool Both(int h) => area[h ^ 1] == f;
        var result = new List<List<int>>();
        var start = border.FindIndex(h => !Both(h));
        if (start < 0)
            return result;

        var stack = new List<(int Edge, List<int> HalfEdges)> { (-1, []) };
        for (var k = 0; k < border.Count; k++)
        {
            var h = border[(start + k) % border.Count];
            if (!Both(h))
            {
                stack[^1].HalfEdges.Add(h);
            }
            else if (stack.FindIndex(s => s.Edge == h / 2) is var open and > 0)
            {
                // Back along the edge: what lies between is a loop of its own
                for (var i = stack.Count - 1; i >= open; i--)
                {
                    if (stack[i].HalfEdges.Count > 0)
                        result.Add(stack[i].HalfEdges);
                    stack.RemoveAt(i);
                }
            }
            else
            {
                stack.Add((h / 2, []));
            }
        }

        result.AddRange(stack.Where(s => s.HalfEdges.Count > 0).Select(s => s.HalfEdges));
        return result;
    }

    /// <summary>
    /// Direction (0 to 2π) of an edge where it starts and how it bends: positive to the left. An arc starts turned
    /// against its bending by half its angle from the chord.
    /// </summary>
    private static (double Angle, double Curvature) Direction((double X, double Y) a, (double X, double Y) b, double bulge)
    {
        var sweep = 4 * Math.Atan(bulge);
        var angle = Modulo(Math.Atan2(b.Y - a.Y, b.X - a.X) - sweep / 2, 2 * Math.PI);
        if (angle > 2 * Math.PI - 1e-7)
            angle = 0;
        if (IsStraight(bulge))
            return (angle, 0);

        var radius = Distance(a, b) / (2 * Math.Sin(Math.Abs(sweep) / 2));
        return (angle, Math.Sign(sweep) / radius);
    }

    /// <summary>
    /// A point of the area on the left of a loop, beside the middle of one of its longest segments, as far from it as
    /// possible: the first that passes the test (e.g. lies inside the loop and in none of its holes).
    /// </summary>
    public static (double X, double Y)? PointOnLeft(IReadOnlyList<(double X, double Y)> loop, Func<(double X, double Y), bool> test)
    {
        var segments = Enumerable.Range(0, loop.Count)
            .Select(i => (A: loop[i], B: loop[(i + 1) % loop.Count]))
            .OrderByDescending(s => Distance(s.A, s.B))
            .Take(8);
        foreach (var (a, b) in segments)
        {
            var length = Distance(a, b);
            if (length < 1e-9)
                continue;

            var (nx, ny) = (-(b.Y - a.Y) / length, (b.X - a.X) / length);
            foreach (var offset in new[] { 0.5, 0.1, 0.02, 3e-3, 5e-4 }.Where(o => o < length / 2))
            {
                var p = ((a.X + b.X) / 2 + nx * offset, (a.Y + b.Y) / 2 + ny * offset);
                if (test(p))
                    return p;
            }
        }

        return null;
    }

    private static double Distance((double X, double Y) a, (double X, double Y) b) =>
        Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    private static double Modulo(double value, double m) => (value % m + m) % m;

    /// <summary>Points of the drawing; one closer than the tolerance to a known one is that one.</summary>
    private sealed class NodeSet(double tolerance)
    {
        private readonly Dictionary<(long, long), List<int>> _cells = [];

        public List<(double X, double Y)> Points { get; } = [];

        public int Find(double x, double y)
        {
            var (cx, cy) = ((long)Math.Floor(x / tolerance), (long)Math.Floor(y / tolerance));
            for (var dx = -1; dx <= 1; dx++)
            for (var dy = -1; dy <= 1; dy++)
            {
                if (!_cells.TryGetValue((cx + dx, cy + dy), out var list))
                    continue;
                foreach (var i in list)
                {
                    if (Math.Abs(Points[i].X - x) <= tolerance && Math.Abs(Points[i].Y - y) <= tolerance)
                        return i;
                }
            }

            Points.Add((x, y));
            if (!_cells.TryGetValue((cx, cy), out var cell))
                _cells[(cx, cy)] = cell = [];
            cell.Add(Points.Count - 1);
            return Points.Count - 1;
        }
    }
}
