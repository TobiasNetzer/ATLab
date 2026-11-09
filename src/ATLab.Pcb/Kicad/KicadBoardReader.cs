using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace ATLab.Pcb.Kicad;

/// <summary>
/// Reads footprints, pads with their nets, the board outline and unplated holes from a KiCad board file
/// (.kicad_pcb). Supports the S-expression format of KiCad 6 and newer; tested with KiCad 8 to 10.
/// </summary>
public static class KicadBoardReader
{
    /// <summary>First file format of KiCad 6. Older files use a different arc and pad syntax.</summary>
    public const int MinimumFormatVersion = 20211014;

    /// <summary>Segments a full circle is approximated with in the outline.</summary>
    private const int CircleSegments = 72;

    public static Board Read(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        var text = System.Text.Encoding.UTF8.GetString(bytes);
        return Parse(text, path, hash);
    }

    public static Board Parse(string text, string path, string hash = "")
    {
        var root = SExpressionParser.Parse(text);
        if (root.Name != "kicad_pcb")
            throw new KicadFormatException(KicadError.NotABoard);

        var version = (int)(root.Child("version")?.ArgDouble(0) ?? 0);
        if (version < MinimumFormatVersion)
            throw new KicadFormatException(KicadError.TooOld);

        var titleBlock = root.Child("title_block");
        var board = new Board
        {
            FilePath = path,
            FormatVersion = version,
            GeneratorVersion = root.Child("generator_version")?.Arg(0) ?? string.Empty,
            Title = titleBlock?.Child("title")?.Arg(0) ?? string.Empty,
            Revision = titleBlock?.Child("rev")?.Arg(0) ?? string.Empty,
            Company = titleBlock?.Child("company")?.Arg(0) ?? string.Empty,
            Hash = hash
        };

        // Net names: KiCad 10 writes them in the pads, older versions a number that refers to this table
        var netNames = root.Children("net")
            .Where(n => n.Items.Count >= 3)
            .GroupBy(n => n.Arg(0) ?? string.Empty)
            .ToDictionary(g => g.Key, g => g.First().Arg(1) ?? string.Empty);

        foreach (var node in root.Children("footprint"))
            ReadFootprint(node, board, netNames);

        foreach (var node in root.Items.Where(i => i.IsList && IsOutline(i)))
            if (Shape(node, p => p) is { Count: > 1 } polyline)
                board.Outline.Add(polyline);

        ReadBoardGraphics(root, board, netNames);
        SetOrigin(board, root);
        return board;
    }

    private static void ReadFootprint(SNode node, Board board, Dictionary<string, string> netNames)
    {
        var at = node.Child("at");
        var position = new Point2(at?.ArgDouble(0) ?? 0, at?.ArgDouble(1) ?? 0);
        var rotation = at?.ArgDouble(2) ?? 0;

        var footprint = new Footprint
        {
            Reference = Property(node, "Reference") ?? "?",
            Value = Property(node, "Value") ?? string.Empty,
            Library = node.Arg(0) ?? string.Empty,
            Side = node.Child("layer")?.Arg(0) == "B.Cu" ? BoardSide.Bottom : BoardSide.Top,
            Position = position,
            Rotation = rotation
        };

        Point2 ToBoard(Point2 local) => position + local.Rotate(rotation);

        foreach (var padNode in node.Children("pad"))
        {
            var type = padNode.Arg(1);
            var padAt = padNode.Child("at");
            var padPosition = ToBoard(new Point2(padAt?.ArgDouble(0) ?? 0, padAt?.ArgDouble(1) ?? 0));
            var drill = padNode.Child("drill");
            var drillSize = DrillDiameter(drill);

            // Unplated holes take tooling pins, they are no probe targets
            if (type == "np_thru_hole")
            {
                if (drillSize > 0)
                    board.Holes.Add(new Hole(padPosition, drillSize, footprint.Reference));
                continue;
            }

            // "connect" pads have no exposed copper; skip anything unknown as well
            if (type is not ("smd" or "thru_hole"))
                continue;

            var layers = padNode.Child("layers")?.Args().ToList() ?? [];
            var size = padNode.Child("size");
            var kind = type == "thru_hole" ? PadKind.ThroughHole : PadKind.Smd;

            var pad = new Pad
            {
                Footprint = footprint,
                Number = padNode.Arg(0) ?? string.Empty,
                Kind = kind,
                Shape = padNode.Arg(2) switch
                {
                    "circle" => PadShape.Circle,
                    "rect" => PadShape.Rect,
                    "oval" => PadShape.Oval,
                    "roundrect" => PadShape.RoundRect,
                    _ => PadShape.Other
                },
                Position = padPosition,
                Width = size?.ArgDouble(0) ?? 0,
                Height = size?.ArgDouble(1) ?? size?.ArgDouble(0) ?? 0,
                Rotation = padAt?.ArgDouble(2) ?? 0,
                Drill = kind == PadKind.ThroughHole ? drillSize : 0,
                Net = NetName(padNode.Child("net"), netNames),
                HasTopCopper = HasLayer(layers, "F.Cu"),
                HasBottomCopper = HasLayer(layers, "B.Cu"),
                HasTopMaskOpening = HasLayer(layers, "F.Mask"),
                HasBottomMaskOpening = HasLayer(layers, "B.Mask")
            };

            footprint.Pads.Add(pad);

            // Plated mounting holes without net are holes for tooling pins as well
            if (kind == PadKind.ThroughHole && !pad.HasNet &&
                footprint.Library.Contains("MountingHole", StringComparison.OrdinalIgnoreCase) && drillSize > 0)
                board.Holes.Add(new Hole(padPosition, drillSize, footprint.Reference));
        }

        // Cut-outs drawn inside a footprint belong to the outline too
        foreach (var graphic in node.Items.Where(i => i.IsList && IsOutline(i)))
            if (Shape(graphic, ToBoard) is { Count: > 1 } polyline)
                board.Outline.Add(polyline);

        // Drawings of the other layers; the courtyard also tells where the component is
        foreach (var graphic in node.Items.Where(i => i.IsList && IsDrawing(i) && !IsOutline(i)))
        {
            if (Drawing(graphic, ToBoard) is not { } drawing)
                continue;

            board.Graphics.Add(drawing);
            if (drawing.Layer is "F.CrtYd" or "B.CrtYd")
                for (var i = 1; i < drawing.Points.Count; i++)
                    footprint.CourtyardEdges.Add((drawing.Points[i - 1], drawing.Points[i]));
        }

        // Reference, value and other texts that are not hidden
        foreach (var text in node.Children("property").Concat(node.Children("fp_text")))
        {
            var content = text.Arg(1);
            if (content is null || IsHidden(text))
                continue;

            content = content.Replace("${REFERENCE}", footprint.Reference).Replace("${VALUE}", footprint.Value);
            if (Text(text, content, ToBoard) is { } boardText)
                board.Texts.Add(boardText);
        }

        board.Footprints.Add(footprint);
    }

    /// <summary>Tracks, vias, filled zones, drawings and texts outside the footprints.</summary>
    private static void ReadBoardGraphics(SNode root, Board board, Dictionary<string, string> netNames)
    {
        foreach (var node in root.Items)
        {
            if (!node.IsList)
                continue;

            switch (node.Name)
            {
                case "segment":
                    board.Graphics.Add(new BoardGraphic(node.Child("layer")?.Arg(0) ?? string.Empty,
                        [XY(node.Child("start")), XY(node.Child("end"))], node.Child("width")?.ArgDouble(0) ?? 0.2, false,
                        NetName(node.Child("net"), netNames)));
                    break;

                case "arc":
                    board.Graphics.Add(new BoardGraphic(node.Child("layer")?.Arg(0) ?? string.Empty,
                        Arc(XY(node.Child("start")), XY(node.Child("mid")), XY(node.Child("end"))),
                        node.Child("width")?.ArgDouble(0) ?? 0.2, false, NetName(node.Child("net"), netNames)));
                    break;

                case "via":
                {
                    // A via connects its first and last layer; drawn on both outer layers it touches
                    var circle = Circle(XY(node.Child("at")), XY(node.Child("at")) + new Point2(node.Child("size")?.ArgDouble(0) / 2 ?? 0.3, 0));
                    var viaNet = NetName(node.Child("net"), netNames);
                    foreach (var layer in node.Child("layers")?.Args() ?? [])
                        board.Graphics.Add(new BoardGraphic(layer, circle, 0, true, viaNet));
                    break;
                }

                case "zone":
                {
                    var zoneNet = NetName(node.Child("net"), netNames);
                    foreach (var filled in node.Children("filled_polygon"))
                        if (Polygon(filled.Child("pts")) is { Count: > 2 } area)
                            board.Graphics.Add(new BoardGraphic(filled.Child("layer")?.Arg(0) ?? node.Child("layer")?.Arg(0) ?? string.Empty,
                                area, 0, true, zoneNet));
                    break;
                }

                case "gr_text":
                    if (!IsHidden(node) && node.Arg(0) is { } content && Text(node, content, p => p) is { } text)
                        board.Texts.Add(text);
                    break;

                default:
                    if (IsDrawing(node) && !IsOutline(node) && Drawing(node, p => p) is { } drawing)
                        board.Graphics.Add(drawing);
                    break;
            }
        }
    }

    private static bool IsDrawing(SNode node) =>
        node.Name is "gr_line" or "gr_arc" or "gr_circle" or "gr_rect" or "gr_poly" or "gr_curve"
            or "fp_line" or "fp_arc" or "fp_circle" or "fp_rect" or "fp_poly" or "fp_curve";

    /// <summary>A drawing with its line width; filled shapes become areas.</summary>
    private static BoardGraphic? Drawing(SNode node, Func<Point2, Point2> transform)
    {
        var layer = node.Child("layer")?.Arg(0);
        if (layer is null || Shape(node, transform) is not { Count: > 1 } points)
            return null;

        var width = node.Child("stroke")?.Child("width")?.ArgDouble(0) ?? node.Child("width")?.ArgDouble(0) ?? 0.1;
        var fill = node.Child("fill")?.Arg(0);
        var filled = fill is "yes" or "solid" && node.Name[3..] is "circle" or "rect" or "poly";

        // An arc keeps its sweep, taken from the transformed points, so a mirrored footprint turns it the right way
        var sweep = node.Name[3..] == "arc" ? ArcSweep(points[0], points[points.Count / 2], points[^1]) : 0;
        return new BoardGraphic(layer, points, width, filled, ArcSweep: sweep);
    }

    /// <summary>KiCad 8+: (hide yes) in the text; older versions: the atom hide in the text or its effects.</summary>
    private static bool IsHidden(SNode text)
    {
        if (text.Child("hide") is { } hide)
            return hide.Arg(0) != "no";
        if (text.Items.Any(i => i.Atom == "hide"))
            return true;
        var effects = text.Child("effects");
        return effects is not null && (effects.Items.Any(i => i.Atom == "hide") || effects.Child("hide")?.Arg(0) == "yes");
    }

    /// <summary>
    /// Text with position, height and angle. The angle in the file is absolute; texts are turned so they stay readable,
    /// as KiCad shows them.
    /// </summary>
    private static BoardText? Text(SNode node, string content, Func<Point2, Point2> transform)
    {
        var layer = node.Child("layer")?.Arg(0);
        if (layer is null || content.Length == 0)
            return null;

        var at = node.Child("at");
        var effects = node.Child("effects");
        var height = effects?.Child("font")?.Child("size")?.ArgDouble(0) ?? 1;
        var justify = effects?.Child("justify")?.Args().ToList() ?? [];

        var angle = (at?.ArgDouble(2) ?? 0) % 360;
        if (angle < 0)
            angle += 360;
        if (angle > 90 && angle <= 270)
            angle -= 180;

        var alignment = justify.Contains("left") ? -1 : justify.Contains("right") ? 1 : 0;
        var vertical = justify.Contains("top") ? -1 : justify.Contains("bottom") ? 1 : 0;
        return new BoardText(layer, content, transform(new Point2(at?.ArgDouble(0) ?? 0, at?.ArgDouble(1) ?? 0)),
            height, angle, justify.Contains("mirror"), alignment, vertical);
    }

    /// <summary>KiCad 7+ stores reference and value as properties, KiCad 6 as fp_text.</summary>
    private static string? Property(SNode footprint, string name)
    {
        foreach (var property in footprint.Children("property"))
            if (property.Arg(0) == name)
                return property.Arg(1);

        var kind = name.ToLowerInvariant();
        foreach (var text in footprint.Children("fp_text"))
            if (text.Arg(0) == kind)
                return text.Arg(1);

        return null;
    }

    /// <summary>KiCad 10: (net "GND"); up to KiCad 9: (net 3 "GND").</summary>
    private static string NetName(SNode? net, Dictionary<string, string> netNames)
    {
        if (net is null)
            return string.Empty;

        if (net.Items.Count >= 3)
            return net.Arg(1) ?? string.Empty;

        var single = net.Arg(0) ?? string.Empty;
        return int.TryParse(single, out _) && netNames.TryGetValue(single, out var name) ? name : single;
    }

    /// <summary>"*.Cu" and "F&amp;B.Cu" stand for several layers.</summary>
    private static bool HasLayer(List<string> layers, string layer)
    {
        var suffix = layer[(layer.IndexOf('.') + 1)..];
        var side = layer[0];
        return layers.Any(l => l == layer || l == "*." + suffix || (l == "F&B." + suffix && side is 'F' or 'B'));
    }

    /// <summary>(drill 1.0) or (drill oval 1.0 2.0): the smaller size of an oval hole.</summary>
    private static double DrillDiameter(SNode? drill)
    {
        if (drill is null)
            return 0;

        var values = drill.Items.Skip(1).Where(i => i.Atom is not null and not "oval").Select(i => i.Atom!).ToList();
        var sizes = values
            .Select(v => double.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0)
            .Where(d => d > 0)
            .ToList();
        return sizes.Count > 0 ? sizes.Min() : 0;
    }

    private static bool IsOutline(SNode node) =>
        node.Name is "gr_line" or "gr_arc" or "gr_circle" or "gr_rect" or "gr_poly" or "gr_curve"
            or "fp_line" or "fp_arc" or "fp_circle" or "fp_rect" or "fp_poly" or "fp_curve" &&
        node.Child("layer")?.Arg(0) == "Edge.Cuts";

    private static Point2 XY(SNode? node) => new(node?.ArgDouble(0) ?? 0, node?.ArgDouble(1) ?? 0);

    /// <summary>A drawing on Edge.Cuts as polyline, transformed into board coordinates.</summary>
    private static List<Point2>? Shape(SNode node, Func<Point2, Point2> transform)
    {
        var kind = node.Name[3..];
        List<Point2>? points = kind switch
        {
            "line" => [XY(node.Child("start")), XY(node.Child("end"))],
            "arc" => Arc(XY(node.Child("start")), XY(node.Child("mid")), XY(node.Child("end"))),
            "circle" => Circle(XY(node.Child("center")), XY(node.Child("end"))),
            "rect" => Rectangle(XY(node.Child("start")), XY(node.Child("end"))),
            "poly" => Polygon(node.Child("pts")),
            "curve" => Bezier(node.Child("pts")),
            _ => null
        };

        return points?.Select(transform).ToList();
    }

    private static List<Point2> Rectangle(Point2 a, Point2 b) =>
        [a, new Point2(b.X, a.Y), b, new Point2(a.X, b.Y), a];

    private static List<Point2> Circle(Point2 center, Point2 onCircle)
    {
        var radius = center.DistanceTo(onCircle);
        var points = new List<Point2>(CircleSegments + 1);
        for (var i = 0; i <= CircleSegments; i++)
        {
            var a = 2 * Math.PI * i / CircleSegments;
            points.Add(new Point2(center.X + radius * Math.Cos(a), center.Y + radius * Math.Sin(a)));
        }

        return points;
    }

    /// <summary>Sweep of the arc through three points from start to end (radians, signed); 0 if they are in line.</summary>
    internal static double ArcSweep(Point2 start, Point2 mid, Point2 end)
    {
        var d = 2 * (start.X * (mid.Y - end.Y) + mid.X * (end.Y - start.Y) + end.X * (start.Y - mid.Y));
        if (Math.Abs(d) < 1e-12)
            return 0;

        var s2 = start.X * start.X + start.Y * start.Y;
        var m2 = mid.X * mid.X + mid.Y * mid.Y;
        var e2 = end.X * end.X + end.Y * end.Y;
        var center = new Point2(
            (s2 * (mid.Y - end.Y) + m2 * (end.Y - start.Y) + e2 * (start.Y - mid.Y)) / d,
            (s2 * (end.X - mid.X) + m2 * (start.X - end.X) + e2 * (mid.X - start.X)) / d);

        static double Normalize(double angle)
        {
            angle %= 2 * Math.PI;
            return angle < 0 ? angle + 2 * Math.PI : angle;
        }

        var a0 = Math.Atan2(start.Y - center.Y, start.X - center.X);
        var am = Math.Atan2(mid.Y - center.Y, mid.X - center.X);
        var a1 = Math.Atan2(end.Y - center.Y, end.X - center.X);
        var sweep = Normalize(a1 - a0);
        return Normalize(am - a0) > sweep ? sweep - 2 * Math.PI : sweep;
    }

    /// <summary>Arc through three points, approximated with short segments.</summary>
    internal static List<Point2> Arc(Point2 start, Point2 mid, Point2 end)
    {
        // Center of the circle through the three points
        var d = 2 * (start.X * (mid.Y - end.Y) + mid.X * (end.Y - start.Y) + end.X * (start.Y - mid.Y));
        if (Math.Abs(d) < 1e-12)
            return [start, end];

        var s2 = start.X * start.X + start.Y * start.Y;
        var m2 = mid.X * mid.X + mid.Y * mid.Y;
        var e2 = end.X * end.X + end.Y * end.Y;
        var center = new Point2(
            (s2 * (mid.Y - end.Y) + m2 * (end.Y - start.Y) + e2 * (start.Y - mid.Y)) / d,
            (s2 * (end.X - mid.X) + m2 * (start.X - end.X) + e2 * (mid.X - start.X)) / d);
        var radius = center.DistanceTo(start);

        var a0 = Math.Atan2(start.Y - center.Y, start.X - center.X);
        var am = Math.Atan2(mid.Y - center.Y, mid.X - center.X);
        var a1 = Math.Atan2(end.Y - center.Y, end.X - center.X);

        // Sweep from start to end in the direction that passes the middle point
        var sweep = Normalize(a1 - a0);
        if (Normalize(am - a0) > sweep)
            sweep -= 2 * Math.PI;

        var steps = Math.Max(2, (int)Math.Ceiling(Math.Abs(sweep) / (2 * Math.PI) * CircleSegments));
        var points = new List<Point2>(steps + 1);
        for (var i = 0; i <= steps; i++)
        {
            var a = a0 + sweep * i / steps;
            points.Add(new Point2(center.X + radius * Math.Cos(a), center.Y + radius * Math.Sin(a)));
        }

        return points;

        static double Normalize(double angle)
        {
            angle %= 2 * Math.PI;
            return angle < 0 ? angle + 2 * Math.PI : angle;
        }
    }

    /// <summary>(pts (xy …) (arc (start …) (mid …) (end …)) …), closed.</summary>
    private static List<Point2>? Polygon(SNode? pts)
    {
        if (pts is null)
            return null;

        var points = new List<Point2>();
        foreach (var item in pts.Items.Skip(1))
        {
            if (item.Name == "xy")
                points.Add(XY(item));
            else if (item.Name == "arc")
                points.AddRange(Arc(XY(item.Child("start")), XY(item.Child("mid")), XY(item.Child("end"))));
        }

        if (points.Count > 2 && points[0] != points[^1])
            points.Add(points[0]);
        return points;
    }

    private static List<Point2>? Bezier(SNode? pts)
    {
        var p = pts?.Children("xy").Select(XY).ToList();
        if (p is not { Count: 4 })
            return null;

        const int steps = 24;
        var points = new List<Point2>(steps + 1);
        for (var i = 0; i <= steps; i++)
        {
            var t = (double)i / steps;
            var u = 1 - t;
            points.Add(new Point2(
                u * u * u * p[0].X + 3 * u * u * t * p[1].X + 3 * u * t * t * p[2].X + t * t * t * p[3].X,
                u * u * u * p[0].Y + 3 * u * u * t * p[1].Y + 3 * u * t * t * p[2].Y + t * t * t * p[3].Y));
        }

        return points;
    }

    private static void SetOrigin(Board board, SNode root)
    {
        var aux = root.Child("setup")?.Child("aux_axis_origin");
        if (aux is not null && (aux.ArgDouble(0) != 0 || aux.ArgDouble(1) != 0))
        {
            board.Origin = XY(aux);
            board.IsOriginFromFile = true;
            return;
        }

        var bounds = board.Bounds;
        board.Origin = new Point2(bounds.MinX, bounds.MaxY);
    }
}

public enum KicadError
{
    NotABoard,
    TooOld
}

public sealed class KicadFormatException(KicadError error) : Exception(error.ToString())
{
    public KicadError Error { get; } = error;
}