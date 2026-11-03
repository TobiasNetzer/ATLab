using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ATLab.FixtureDesigner.Plates;

/// <summary>
/// Reads a plate outline from a DXF file (ASCII, any version): lines, arcs, circles, polylines with bulges, ellipses
/// and the control points of splines. Arcs and the bulges of polylines stay arcs; ellipses and splines become short
/// segments. The unit comes from $INSUNITS; without it the drawing is taken to be in mm.
/// </summary>
public static class DxfOutlineReader
{
    public static OutlineShape? Read(string path, Func<string, string> text, List<string> warnings) =>
        Parse(File.ReadAllLines(path), text, warnings);

    public static OutlineShape? Parse(IReadOnlyList<string> lines, Func<string, string> text, List<string> warnings)
    {
        // Pairs of group code and value
        var pairs = new List<(int Code, string Value)>();
        for (var i = 0; i + 1 < lines.Count; i += 2)
        {
            if (int.TryParse(lines[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var code))
                pairs.Add((code, lines[i + 1].Trim()));
        }

        var scale = UnitScale(pairs);
        var polylines = new List<IReadOnlyList<OutlineVertex>>();
        var circles = new List<OutlineCircle>();
        var skipped = new HashSet<string>();

        var entities = pairs.FindIndex(p => p.Code == 2 && p.Value == "ENTITIES");
        if (entities < 0)
            return null;

        // The entities: each starts with group 0 and its name
        var index = entities + 1;
        List<(double X, double Y, double Bulge)>? polylineVertices = null;
        var polylineClosed = false;

        while (index < pairs.Count)
        {
            if (pairs[index].Code != 0)
            {
                index++;
                continue;
            }

            var name = pairs[index].Value;
            var end = index + 1;
            while (end < pairs.Count && pairs[end].Code != 0)
                end++;
            var groups = pairs.GetRange(index + 1, end - index - 1);
            index = end;

            if (name == "ENDSEC")
                break;

            double G(int code, double fallback = 0) =>
                groups.FirstOrDefault(g => g.Code == code) is { Value: { } v } && double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)
                    ? d
                    : fallback;

            switch (name)
            {
                case "LINE":
                    polylines.Add([new(G(10) * scale, G(20) * scale), new(G(11) * scale, G(21) * scale)]);
                    break;

                case "CIRCLE":
                    circles.Add(new OutlineCircle(G(10) * scale, G(20) * scale, 2 * G(40) * scale));
                    break;

                case "ARC":
                {
                    var start = G(50) * Math.PI / 180;
                    var stop = G(51) * Math.PI / 180;
                    var sweep = stop - start;
                    while (sweep <= 0) sweep += 2 * Math.PI;
                    polylines.Add(ArcPiece(G(10) * scale, G(20) * scale, G(40) * scale, start, sweep));
                    break;
                }

                case "ELLIPSE":
                {
                    var (cx, cy) = (G(10) * scale, G(20) * scale);
                    var (mx, my) = (G(11) * scale, G(21) * scale);
                    var ratio = G(40, 1);
                    var t0 = G(41);
                    var t1 = G(42, 2 * Math.PI);
                    var sweep = t1 - t0;
                    while (sweep <= 0) sweep += 2 * Math.PI;
                    var points = new List<(double X, double Y)>();
                    for (var i = 0; i <= 96; i++)
                    {
                        var t = t0 + sweep * i / 96;
                        // Point = center + major * cos t + minor * sin t, the minor axis is the major turned by 90°
                        points.Add((cx + mx * Math.Cos(t) - my * ratio * Math.Sin(t), cy + my * Math.Cos(t) + mx * ratio * Math.Sin(t)));
                    }

                    polylines.Add(OutlineBuilder.Straight(points));
                    break;
                }

                case "LWPOLYLINE":
                {
                    var closed = ((int)G(70) & 1) == 1;
                    var vertices = new List<(double X, double Y, double Bulge)>();
                    foreach (var g in groups)
                    {
                        var value = double.TryParse(g.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0;
                        if (g.Code == 10) vertices.Add((value * scale, 0, 0));
                        else if (g.Code == 20 && vertices.Count > 0) vertices[^1] = (vertices[^1].X, value * scale, vertices[^1].Bulge);
                        else if (g.Code == 42 && vertices.Count > 0) vertices[^1] = (vertices[^1].X, vertices[^1].Y, value);
                    }

                    polylines.Add(WithBulges(vertices, closed));
                    break;
                }

                case "POLYLINE":
                    polylineVertices = [];
                    polylineClosed = ((int)G(70) & 1) == 1;
                    break;

                case "VERTEX" when polylineVertices is not null:
                    polylineVertices.Add((G(10) * scale, G(20) * scale, G(42)));
                    break;

                case "SEQEND" when polylineVertices is not null:
                    polylines.Add(WithBulges(polylineVertices, polylineClosed));
                    polylineVertices = null;
                    break;

                case "SPLINE":
                {
                    // Fit points if there are any, else the control points: close enough for a frame outline
                    var fit = Points(groups, 11, 21, scale);
                    var points = fit.Count >= 2 ? fit : Points(groups, 10, 20, scale);
                    if (points.Count >= 2)
                    {
                        if (((int)G(70) & 1) == 1)
                            points.Add(points[0]);
                        polylines.Add(OutlineBuilder.Straight(points));
                    }

                    break;
                }

                case "POINT" or "TEXT" or "MTEXT" or "DIMENSION" or "HATCH" or "SOLID" or "VIEWPORT":
                    break;

                default:
                    skipped.Add(name);
                    break;
            }
        }

        if (skipped.Count > 0)
            warnings.Add(string.Format(text("Outline_Skipped"), string.Join(", ", skipped.Order())));

        return OutlineBuilder.Build(polylines, circles, text);
    }

    private static List<(double X, double Y)> Points(List<(int Code, string Value)> groups, int xCode, int yCode, double scale)
    {
        var points = new List<(double X, double Y)>();
        foreach (var g in groups)
        {
            var value = double.TryParse(g.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d * scale : 0;
            if (g.Code == xCode) points.Add((value, 0));
            else if (g.Code == yCode && points.Count > 0) points[^1] = (points[^1].X, value);
        }

        return points;
    }

    /// <summary>
    /// A polyline as a piece with its bulges (the bulge of a vertex is the tangent of a quarter of the arc's angle to
    /// the next vertex, positive counterclockwise); a closed one ends at its first vertex again.
    /// </summary>
    private static List<OutlineVertex> WithBulges(List<(double X, double Y, double Bulge)> vertices, bool closed)
    {
        var piece = vertices.Select(v => new OutlineVertex(v.X, v.Y, v.Bulge)).ToList();
        if (closed && piece.Count > 0)
            piece.Add(piece[0] with { Bulge = 0 });
        else if (piece.Count > 0)
            piece[^1] = piece[^1] with { Bulge = 0 };
        return piece;
    }

    /// <summary>An arc as a piece; longer than half a circle it is split, so each part's bulge stays finite.</summary>
    private static List<OutlineVertex> ArcPiece(double cx, double cy, double r, double start, double sweep)
    {
        var parts = sweep > Math.PI ? 2 : 1;
        var piece = new List<OutlineVertex>();
        for (var i = 0; i <= parts; i++)
        {
            var a = start + sweep * i / parts;
            piece.Add(new OutlineVertex(cx + r * Math.Cos(a), cy + r * Math.Sin(a), i < parts ? Math.Tan(sweep / parts / 4) : 0));
        }

        return piece;
    }

    /// <summary>mm per drawing unit from $INSUNITS (1 inch, 2 feet, 4 mm, 5 cm, 6 m); unitless counts as mm.</summary>
    private static double UnitScale(List<(int Code, string Value)> pairs)
    {
        var at = pairs.FindIndex(p => p.Code == 9 && p.Value == "$INSUNITS");
        if (at < 0 || at + 1 >= pairs.Count || !int.TryParse(pairs[at + 1].Value, out var units))
            return 1;

        return units switch
        {
            1 => 25.4,
            2 => 304.8,
            5 => 10,
            6 => 1000,
            8 => 0.0000254,
            9 => 0.0254,
            10 => 914.4,
            13 => 0.001,
            14 => 100,
            _ => 1
        };
    }
}