using System;
using System.Collections.Generic;
using System.Linq;

namespace ATLab.FixtureDesigner.Plates;

/// <summary>
/// Makes a plate outline of the pieces of a drawing. The pieces become a <see cref="PlanarGraph"/>: several edges may
/// meet at a point (e.g. a pocket open to the edge of the plate, drawn with the edge across its mouth), so the edges
/// divide the drawing into areas. The outer border of everything is the outline, every area inside it but the plate
/// itself is a shape: round ones become circles, the others cut-outs. Whatever is no part of these is kept as loose
/// lines, so nothing of the drawing is lost. Lines that cross each other between their ends are only reported.
/// </summary>
public static class OutlineBuilder
{
    /// <summary>Ends closer than this are one point, in mm.</summary>
    private const double JoinTolerance = 0.05;

    /// <summary>
    /// A piece of a drawing: its vertices in order; the bulge of a vertex belongs to the edge to the next one, the
    /// last vertex's bulge is not used.
    /// </summary>
    public static OutlineShape? Build(IEnumerable<IReadOnlyList<OutlineVertex>> pieces, IEnumerable<OutlineCircle> circles,
        Func<string, string> text)
    {
        var warnings = new List<string>();
        var graph = PlanarGraph.Build(pieces, JoinTolerance);
        var drawnCircles = DistinctCircles(circles);
        if (graph.HasCrossings(drawnCircles))
            warnings.Add(text("Outline_Crossing"));

        var loops = graph.Loops();
        var outer = loops.Where(l => l.Area < 0).MinBy(l => l.Area);
        if (outer is null)
            return null;

        // The outline: the outer border, counterclockwise
        var outlineVertices = OutlineShape.Joined(OutlineShape.Reversed(outer.Vertices));
        var outlinePoints = OutlineShape.Flatten(outlineVertices);
        var used = outer.HalfEdges.Select(h => h / 2).ToHashSet();

        // The plate itself: the area along most of the outline; every other area inside the outline is a shape
        var areas = loops.Where(l => l.Area > 0).ToList();
        var plate = areas.MaxBy(l => l.HalfEdges.Where(h => used.Contains(h / 2)).Sum(h => graph.Length(h / 2)));

        var holes = new List<OutlineCircle>();
        var cutouts = new List<OutlineCutout>();
        foreach (var area in areas.Where(a => !ReferenceEquals(a, plate)))
        {
            var inside = PlanarGraph.PointOnLeft(area.Points, p => OutlineShape.Contains(area.Points, p.X, p.Y));
            if (inside is not { } p || !OutlineShape.Contains(outlinePoints, p.X, p.Y))
                continue;

            used.UnionWith(area.HalfEdges.Select(h => h / 2));
            if (AsCircle(area.Points) is { } circle)
                holes.Add(circle);
            else
                cutouts.Add(new OutlineCutout(OutlineShape.Joined(area.Vertices)));
        }

        // The circles of the drawing inside the outline are holes; the others stay in the drawing as loose lines
        var loose = LoosePieces(graph, Enumerable.Range(0, graph.Edges.Count).Where(e => !used.Contains(e)).ToList());
        var circlesInside = new List<OutlineCircle>();
        foreach (var circle in drawnCircles)
        {
            if (OutlineShape.Contains(outlinePoints, circle.X, circle.Y))
                circlesInside.Add(circle);
            else
                loose.Add(PlanarGraph.Circle(circle.X, circle.Y, circle.Diameter));
        }

        if (loose.Count > 0)
            warnings.Add(string.Format(text("Outline_Loose"), loose.Count));

        return new OutlineShape
        {
            Vertices = outlineVertices,
            Circles = [.. circlesInside, .. holes],
            Cutouts = cutouts.OrderBy(c => Math.Round(c.Vertices.Min(v => v.X), 3)).ThenBy(c => c.Vertices.Min(v => v.Y)).ToList(),
            Loose = loose,
            Warnings = warnings
        };
    }

    /// <summary>A polyline without arcs as a piece.</summary>
    public static IReadOnlyList<OutlineVertex> Straight(IEnumerable<(double X, double Y)> points) =>
        points.Select(p => new OutlineVertex(p.X, p.Y)).ToList();

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

    private static List<OutlineCircle> DistinctCircles(IEnumerable<OutlineCircle> circles)
    {
        var result = new List<OutlineCircle>();
        foreach (var c in circles.Where(c => c.Diameter > 0))
        {
            if (!result.Any(o => Math.Abs(o.X - c.X) < 1e-3 && Math.Abs(o.Y - c.Y) < 1e-3 && Math.Abs(o.Diameter - c.Diameter) < 1e-3))
                result.Add(c);
        }

        return result;
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

    /// <summary>Edges of no shape joined into pieces where exactly two of them meet; a closed one ends where it starts.</summary>
    private static List<IReadOnlyList<OutlineVertex>> LoosePieces(PlanarGraph graph, List<int> edges)
    {
        var at = new Dictionary<int, List<int>>();
        foreach (var e in edges)
        {
            foreach (var n in new[] { graph.Edges[e].A, graph.Edges[e].B })
            {
                if (!at.TryGetValue(n, out var list))
                    at[n] = list = [];
                list.Add(e);
            }
        }

        var done = new HashSet<int>();
        var pieces = new List<IReadOnlyList<OutlineVertex>>();

        void Walk(int node, int first)
        {
            var piece = new List<OutlineVertex>();
            var (n, e) = (node, first);
            while (true)
            {
                done.Add(e);
                var edge = graph.Edges[e];
                var forward = edge.A == n;
                piece.Add(new OutlineVertex(graph.Nodes[n].X, graph.Nodes[n].Y, forward ? edge.Bulge : -edge.Bulge));
                n = forward ? edge.B : edge.A;
                var next = at[n].Count == 2 ? at[n].FirstOrDefault(j => !done.Contains(j), -1) : -1;
                if (next < 0)
                    break;
                e = next;
            }

            piece.Add(new OutlineVertex(graph.Nodes[n].X, graph.Nodes[n].Y));
            pieces.Add(piece);
        }

        // Pieces from their open ends and branch points first, then the closed ones
        foreach (var (node, list) in at.Where(p => p.Value.Count != 2))
        foreach (var e in list.Where(e => !done.Contains(e)))
            Walk(node, e);
        foreach (var e in edges.Where(e => !done.Contains(e)))
            Walk(graph.Edges[e].A, e);

        return pieces;
    }
}
