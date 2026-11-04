using System;
using System.Collections.Generic;
using System.Linq;

namespace ATLab.FixtureDesigner.Plates;

/// <summary>A round opening before merging: a probe or push rod that comes through a plate.</summary>
public sealed record OpeningCircle(string Name, double X, double Y, double Diameter);

/// <summary>Openings that overlap or stand too close merged into one: the outline of their circles, counterclockwise, arcs as arcs.</summary>
public sealed record MergedOpening(IReadOnlyList<string> Names, IReadOnlyList<OutlineVertex> Contour)
{
    public string Name => string.Join("+", Names);
}

/// <summary>
/// Openings of probes close to each other cannot be drilled one by one: they overlap, or the wall between them would
/// break. Such openings become one opening, milled along the outline of their circles. Two openings that only stand too
/// close are joined by a small round bridge across the thin wall; the openings keep their size.
/// </summary>
public static class OpeningMerge
{
    public static (List<OpeningCircle> Single, List<MergedOpening> Merged) Merge(IReadOnlyList<OpeningCircle> circles, double minimumWall)
    {
        var n = circles.Count;
        var group = Enumerable.Range(0, n).ToArray();
        int Find(int i) => group[i] == i ? i : group[i] = Find(group[i]);

        double Gap(int i, int j) =>
            Math.Sqrt(Math.Pow(circles[i].X - circles[j].X, 2) + Math.Pow(circles[i].Y - circles[j].Y, 2)) -
            circles[i].Diameter / 2 - circles[j].Diameter / 2;

        // Openings that overlap or leave too thin a wall belong together, also over others
        for (var i = 0; i < n; i++)
        for (var j = i + 1; j < n; j++)
            if (Gap(i, j) < minimumWall)
                group[Find(i)] = Find(j);

        var single = new List<OpeningCircle>();
        var merged = new List<MergedOpening>();
        foreach (var members in Enumerable.Range(0, n).GroupBy(Find).Select(g => g.ToList()))
        {
            if (members.Count == 1)
            {
                single.Add(circles[members[0]]);
                continue;
            }

            var shapes = members.Select(i => (circles[i].X, circles[i].Y, R: circles[i].Diameter / 2)).ToList();

            // A bridge over every thin wall: a circle in the middle of the gap that reaches into both openings
            foreach (var i in members)
            foreach (var j in members.Where(j => j > i))
            {
                var gap = Gap(i, j);
                if (gap < 0 || gap >= minimumWall)
                    continue;

                var (a, b) = (circles[i], circles[j]);
                var d = Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
                var t = (a.Diameter / 2 + gap / 2) / d;
                shapes.Add((a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, gap / 2 + minimumWall));
            }

            var contour = Union(shapes);
            if (contour is null)
                single.AddRange(members.Select(i => circles[i]));
            else
                merged.Add(new MergedOpening(members.Select(i => circles[i].Name).ToList(), contour));
        }

        return (single, merged);
    }

    /// <summary>
    /// The outer outline of overlapping circles: the parts of every circle inside no other, joined where they meet.
    /// Material enclosed by the circles would fall out when milled, so inner outlines are left away. Null if the circles
    /// do not form one outline.
    /// </summary>
    public static List<OutlineVertex>? Union(IReadOnlyList<(double X, double Y, double R)> circles)
    {
        const double eps = 1e-9;
        var arcs = new List<(int Circle, double Start, double Sweep)>();
        for (var i = 0; i < circles.Count; i++)
        {
            var (xi, yi, ri) = circles[i];
            var covered = new List<(double From, double To)>();
            var inside = false;
            for (var j = 0; j < circles.Count && !inside; j++)
            {
                if (j == i)
                    continue;
                var (xj, yj, rj) = circles[j];
                var dx = xj - xi;
                var dy = yj - yi;
                var d = Math.Sqrt(dx * dx + dy * dy);
                if (d >= ri + rj - eps)
                    continue;

                // The same circle twice counts once; a circle inside another adds nothing
                if (d < eps && Math.Abs(ri - rj) < eps)
                {
                    inside = j < i;
                    continue;
                }

                if (d + ri <= rj + eps)
                {
                    inside = true;
                    continue;
                }

                if (d + rj <= ri + eps)
                    continue;

                var middle = Math.Atan2(dy, dx);
                var half = Math.Acos(Math.Clamp((ri * ri + d * d - rj * rj) / (2 * ri * d), -1, 1));
                covered.Add((middle - half, middle + half));
            }

            if (inside)
                continue;

            if (covered.Count == 0)
            {
                arcs.Add((i, 0, 2 * Math.PI));
                continue;
            }

            // Covered parts in [0, 2π), then the free parts between them, the last one around to the first
            var parts = new List<(double From, double To)>();
            foreach (var (from, to) in covered)
            {
                var a = Normalize(from);
                var b = a + (to - from);
                if (b > 2 * Math.PI)
                {
                    parts.Add((a, 2 * Math.PI));
                    parts.Add((0, b - 2 * Math.PI));
                }
                else
                {
                    parts.Add((a, b));
                }
            }

            parts.Sort((p, q) => p.From.CompareTo(q.From));
            var joined = new List<(double From, double To)> { parts[0] };
            foreach (var part in parts.Skip(1))
            {
                if (part.From <= joined[^1].To + eps)
                    joined[^1] = (joined[^1].From, Math.Max(joined[^1].To, part.To));
                else
                    joined.Add(part);
            }

            for (var k = 0; k < joined.Count; k++)
            {
                var start = joined[k].To;
                var end = k + 1 < joined.Count ? joined[k + 1].From : joined[0].From + 2 * Math.PI;
                if (end - start > 1e-7)
                    arcs.Add((i, start, end - start));
            }
        }

        if (arcs.Count == 0)
            return null;

        (double X, double Y) Point(int circle, double angle) =>
            (circles[circle].X + circles[circle].R * Math.Cos(angle), circles[circle].Y + circles[circle].R * Math.Sin(angle));

        // Each arc ends where the next one starts; follow them into closed loops
        var used = new bool[arcs.Count];
        var loops = new List<List<OutlineVertex>>();
        var tolerance = 1e-6 * Math.Max(1, circles.Max(c => c.R));
        for (var first = 0; first < arcs.Count; first++)
        {
            if (used[first])
                continue;

            var loop = new List<OutlineVertex>();
            var current = first;
            while (true)
            {
                used[current] = true;
                var (circle, start, sweep) = arcs[current];

                // An arc of more than half a circle in two, as the outlines keep them
                var pieces = sweep > Math.PI + 1e-9 ? 2 : 1;
                for (var p = 0; p < pieces; p++)
                {
                    var (x, y) = Point(circle, start + sweep * p / pieces);
                    loop.Add(new OutlineVertex(x, y, Math.Tan(sweep / pieces / 4)));
                }

                var end = Point(circle, start + sweep);
                var (sx, sy) = Point(arcs[first].Circle, arcs[first].Start);
                if (Math.Abs(end.X - sx) < tolerance && Math.Abs(end.Y - sy) < tolerance)
                    break;

                var next = -1;
                var best = tolerance;
                for (var k = 0; k < arcs.Count; k++)
                {
                    if (used[k])
                        continue;
                    var s = Point(arcs[k].Circle, arcs[k].Start);
                    var distance = Math.Max(Math.Abs(s.X - end.X), Math.Abs(s.Y - end.Y));
                    if (distance < best)
                    {
                        best = distance;
                        next = k;
                    }
                }

                if (next < 0)
                    return null;
                current = next;
            }

            loops.Add(loop);
        }

        // The outer outline runs counterclockwise and encloses the most
        return loops
            .Select(l => (Loop: l, Area: OutlineShape.SignedArea(OutlineShape.Flatten(l))))
            .Where(l => l.Area > 0)
            .OrderByDescending(l => l.Area)
            .Select(l => l.Loop)
            .FirstOrDefault();
    }

    private static double Normalize(double angle)
    {
        var a = angle % (2 * Math.PI);
        return a < 0 ? a + 2 * Math.PI : a;
    }
}
