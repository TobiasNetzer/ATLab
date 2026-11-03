using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ATLab.FixtureDesigner.Plates;

/// <summary>
/// Reads a plate outline from an SVG file: paths, rectangles, circles, ellipses, polygons, polylines and lines, with
/// the transforms of their groups. The size comes from width/height with their unit and the viewBox; without a unit
/// a user unit is a CSS pixel (1/96 inch), as in Inkscape and browsers. Y runs down in SVG and up in the plate.
/// </summary>
public static partial class SvgOutlineReader
{
    private const double MillimetersPerPixel = 25.4 / 96;

    public static OutlineShape? Read(string path, Func<string, string> text, List<string> warnings)
    {
        XDocument document;
        try
        {
            document = XDocument.Load(path);
        }
        catch (System.Xml.XmlException)
        {
            return null;
        }

        return Parse(document, text, warnings);
    }

    public static OutlineShape? Parse(XDocument document, Func<string, string> text, List<string> warnings)
    {
        var root = document.Root;
        if (root is null || root.Name.LocalName != "svg")
            return null;

        // User units to mm: width (with unit) over the viewBox width, else pixels
        var scale = MillimetersPerPixel;
        var viewBox = Numbers(root.Attribute("viewBox")?.Value);
        if (Length(root.Attribute("width")?.Value) is { } widthMm && viewBox.Count == 4 && viewBox[2] > 0)
            scale = widthMm / viewBox[2];
        var start = Matrix.Scale(scale, -scale);
        if (viewBox.Count == 4)
            start = start.Multiply(Matrix.Translate(-viewBox[0], -viewBox[1]));

        var polylines = new List<IReadOnlyList<OutlineVertex>>();
        var circles = new List<OutlineCircle>();
        var skipped = new HashSet<string>();
        Walk(root, start, polylines, circles, skipped);

        if (skipped.Count > 0)
            warnings.Add(string.Format(text("Outline_Skipped"), string.Join(", ", skipped.Order())));

        return OutlineBuilder.Build(polylines, circles, text);
    }

    private static void Walk(XElement element, Matrix parent, List<IReadOnlyList<OutlineVertex>> polylines,
        List<OutlineCircle> circles, HashSet<string> skipped)
    {
        foreach (var child in element.Elements())
        {
            var m = parent.Multiply(Transform(child.Attribute("transform")?.Value));
            double A(string name) => Number(child.Attribute(name)?.Value);

            switch (child.Name.LocalName)
            {
                case "g" or "svg" or "a":
                    Walk(child, m, polylines, circles, skipped);
                    break;

                case "path":
                    try
                    {
                        foreach (var piece in PathPieces(child.Attribute("d")?.Value ?? string.Empty))
                            polylines.Add(Place(piece, m));
                    }
                    catch (Exception ex) when (ex is FormatException or ArgumentOutOfRangeException)
                    {
                        // Path data that ends in the middle of a command
                        skipped.Add("path");
                    }

                    break;

                case "rect":
                {
                    var (x, y, w, h) = (A("x"), A("y"), A("width"), A("height"));
                    var rx = child.Attribute("rx") is not null ? A("rx") : A("ry");
                    var ry = child.Attribute("ry") is not null ? A("ry") : rx;
                    rx = Math.Min(rx, w / 2);
                    ry = Math.Min(ry, h / 2);
                    // Round corners stay arcs; elliptic corners become segments
                    var piece = rx > 0 && ry > 0 && Math.Abs(rx - ry) < 1e-9
                        ? RoundedRectangle(x, y, w, h, rx)
                        : rx > 0 && ry > 0
                            ? Straight(EllipticRectangle(x, y, w, h, rx, ry))
                            : Straight([(x, y), (x + w, y), (x + w, y + h), (x, y + h), (x, y)]);
                    polylines.Add(Place(piece, m));
                    break;
                }

                case "circle":
                {
                    var (cx, cy, r) = (A("cx"), A("cy"), A("r"));
                    // A circle stays a circle under a uniform transform; otherwise it is an outline like an ellipse
                    if (m.IsUniform(out var factor))
                    {
                        var (x, y) = m.Apply((cx, cy));
                        circles.Add(new OutlineCircle(x, y, 2 * r * factor));
                    }
                    else
                    {
                        polylines.Add(Straight(OutlineBuilder.Arc(cx, cy, r, 0, 2 * Math.PI).Select(m.Apply)));
                    }

                    break;
                }

                case "ellipse":
                {
                    var (cx, cy, rx, ry) = (A("cx"), A("cy"), A("rx"), A("ry"));
                    var points = Enumerable.Range(0, 97)
                        .Select(i => (cx + rx * Math.Cos(2 * Math.PI * i / 96), cy + ry * Math.Sin(2 * Math.PI * i / 96)));
                    polylines.Add(Straight(points.Select(m.Apply)));
                    break;
                }

                case "polygon" or "polyline":
                {
                    var n = Numbers(child.Attribute("points")?.Value);
                    var points = new List<(double X, double Y)>();
                    for (var i = 0; i + 1 < n.Count; i += 2)
                        points.Add((n[i], n[i + 1]));
                    if (child.Name.LocalName == "polygon" && points.Count > 0)
                        points.Add(points[0]);
                    polylines.Add(Straight(points.Select(m.Apply)));
                    break;
                }

                case "line":
                    polylines.Add(Straight(new List<(double X, double Y)> { (A("x1"), A("y1")), (A("x2"), A("y2")) }.Select(m.Apply)));
                    break;

                case "defs" or "title" or "desc" or "metadata" or "style" or "namedview" or "text" or "clipPath" or "mask":
                    break;

                default:
                    skipped.Add(child.Name.LocalName);
                    break;
            }
        }
    }

    private static IReadOnlyList<OutlineVertex> Straight(IEnumerable<(double X, double Y)> points) => OutlineBuilder.Straight(points);

    /// <summary>
    /// A piece in user units placed with the transform: arcs stay arcs under a uniform transform (a mirroring one turns
    /// them the other way); under any other they would be elliptic, so they become segments.
    /// </summary>
    private static IReadOnlyList<OutlineVertex> Place(IReadOnlyList<OutlineVertex> piece, Matrix m)
    {
        if (m.IsUniform(out _))
        {
            var sign = m.Determinant < 0 ? -1 : 1;
            return piece.Select(v =>
            {
                var (x, y) = m.Apply((v.X, v.Y));
                return new OutlineVertex(x, y, v.Bulge * sign);
            }).ToList();
        }

        var points = new List<(double X, double Y)>();
        for (var i = 0; i < piece.Count; i++)
        {
            points.Add((piece[i].X, piece[i].Y));
            if (i + 1 < piece.Count && Math.Abs(piece[i].Bulge) > 1e-12)
                points.AddRange(OutlineShape.ArcBetween(piece[i], piece[i + 1]).Skip(1).SkipLast(1));
        }

        return Straight(points.Select(m.Apply));
    }

    /// <summary>A rectangle with round corners as a closed piece: four edges and four quarter arcs.</summary>
    private static IReadOnlyList<OutlineVertex> RoundedRectangle(double x, double y, double w, double h, double r)
    {
        // Going right along the top edge (Y runs down), every corner turns by +90° in user units
        var corner = Math.Tan(Math.PI / 8);
        return
        [
            new(x + r, y), new(x + w - r, y, corner),
            new(x + w, y + r), new(x + w, y + h - r, corner),
            new(x + w - r, y + h), new(x + r, y + h, corner),
            new(x, y + h - r), new(x, y + r, corner),
            new(x + r, y)
        ];
    }

    private static List<(double X, double Y)> EllipticRectangle(double x, double y, double w, double h, double rx, double ry)
    {
        var points = new List<(double X, double Y)>();
        void Corner(double cx, double cy, double from)
        {
            for (var i = 0; i <= 12; i++)
            {
                var a = from + Math.PI / 2 * i / 12;
                points.Add((cx + rx * Math.Cos(a), cy + ry * Math.Sin(a)));
            }
        }

        Corner(x + w - rx, y + ry, -Math.PI / 2);
        Corner(x + w - rx, y + h - ry, 0);
        Corner(x + rx, y + h - ry, Math.PI / 2);
        Corner(x + rx, y + ry, Math.PI);
        points.Add(points[0]);
        return points;
    }

    // ----- Path data -----

    /// <summary>
    /// The subpaths of path data as pieces in user units: circular arcs stay arcs, curves and elliptic arcs become
    /// segments.
    /// </summary>
    private static List<List<OutlineVertex>> PathPieces(string data)
    {
        var pieces = new List<List<OutlineVertex>>();
        var tokens = PathToken().Matches(data).Select(m => m.Value).ToList();
        var index = 0;
        var command = ' ';
        var (x, y) = (0.0, 0.0);
        var (startX, startY) = (0.0, 0.0);
        var (lastControlX, lastControlY) = (0.0, 0.0);
        var lastCommand = ' ';
        List<OutlineVertex>? current = null;

        bool HasNumber() => index < tokens.Count && !char.IsLetter(tokens[index][0]);
        double Next() => double.Parse(tokens[index++], NumberStyles.Float, CultureInfo.InvariantCulture);

        void LineTo(double nx, double ny)
        {
            current ??= [new OutlineVertex(x, y)];
            current.Add(new OutlineVertex(nx, ny));
            (x, y) = (nx, ny);
        }

        // A circular arc to the end point: the bulge goes to the vertex it leaves
        void ArcTo(double nx, double ny, double bulge)
        {
            current ??= [new OutlineVertex(x, y)];
            current[^1] = current[^1] with { Bulge = bulge };
            current.Add(new OutlineVertex(nx, ny));
            (x, y) = (nx, ny);
        }

        while (index < tokens.Count)
        {
            if (char.IsLetter(tokens[index][0]))
                command = tokens[index++][0];
            else if (command == ' ')
                break;

            var relative = char.IsLower(command);
            var (ox, oy) = relative ? (x, y) : (0.0, 0.0);
            switch (char.ToUpperInvariant(command))
            {
                case 'M':
                {
                    var (nx, ny) = (Next() + ox, Next() + oy);
                    if (current is { Count: > 1 })
                        pieces.Add(current);
                    current = [new OutlineVertex(nx, ny)];
                    (x, y) = (startX, startY) = (nx, ny);
                    // Further pairs after a move are lines
                    command = relative ? 'l' : 'L';
                    break;
                }

                case 'L':
                    LineTo(Next() + ox, Next() + oy);
                    break;

                case 'H':
                    LineTo(Next() + ox, y);
                    break;

                case 'V':
                    LineTo(x, Next() + oy);
                    break;

                case 'C':
                {
                    var (x1, y1, x2, y2) = (Next() + ox, Next() + oy, Next() + ox, Next() + oy);
                    var (ex, ey) = (Next() + ox, Next() + oy);
                    Cubic(x1, y1, x2, y2, ex, ey);
                    break;
                }

                case 'S':
                {
                    var reflect = lastCommand is 'C' or 'S';
                    var (x1, y1) = reflect ? (2 * x - lastControlX, 2 * y - lastControlY) : (x, y);
                    var (x2, y2) = (Next() + ox, Next() + oy);
                    var (ex, ey) = (Next() + ox, Next() + oy);
                    Cubic(x1, y1, x2, y2, ex, ey);
                    break;
                }

                case 'Q':
                {
                    var (qx, qy) = (Next() + ox, Next() + oy);
                    var (ex, ey) = (Next() + ox, Next() + oy);
                    Quadratic(qx, qy, ex, ey);
                    break;
                }

                case 'T':
                {
                    var reflect = lastCommand is 'Q' or 'T';
                    var (qx, qy) = reflect ? (2 * x - lastControlX, 2 * y - lastControlY) : (x, y);
                    var (ex, ey) = (Next() + ox, Next() + oy);
                    Quadratic(qx, qy, ex, ey);
                    break;
                }

                case 'A':
                {
                    var (rx, ry, rotation) = (Next(), Next(), Next());
                    var (large, sweep) = (Next() != 0, Next() != 0);
                    var (ex, ey) = (Next() + ox, Next() + oy);
                    if (rx > 0 && Math.Abs(rx - ry) <= 1e-9 * Math.Max(1, rx) && (x != ex || y != ey))
                    {
                        // A circular arc: as one or two arcs (each at most half a circle, so the bulge stays finite)
                        var (cx, cy, r, _, _, theta, delta) = ArcParameters(x, y, rx, ry, rotation, large, sweep, ex, ey);
                        var parts = Math.Abs(delta) > Math.PI ? 2 : 1;
                        for (var i = 1; i <= parts; i++)
                        {
                            var a = theta + delta * i / parts + rotation * Math.PI / 180;
                            var (px, py) = i == parts ? (ex, ey) : (cx + r * Math.Cos(a), cy + r * Math.Sin(a));
                            ArcTo(px, py, Math.Tan(delta / parts / 4));
                        }
                    }
                    else
                    {
                        foreach (var p in ArcPoints(x, y, rx, ry, rotation, large, sweep, ex, ey).Skip(1))
                            LineTo(p.X, p.Y);
                    }

                    (x, y) = (ex, ey);
                    break;
                }

                case 'Z':
                    if (current is { Count: > 0 })
                    {
                        LineTo(startX, startY);
                        pieces.Add(current);
                        current = null;
                    }

                    (x, y) = (startX, startY);
                    break;

                default:
                    // An unknown command: skip its numbers
                    while (HasNumber()) index++;
                    break;
            }

            // Numbers without a command letter repeat the last command (the loop keeps it)
            lastCommand = char.ToUpperInvariant(command);
            if (lastCommand is not ('C' or 'S' or 'Q' or 'T'))
                (lastControlX, lastControlY) = (x, y);
        }

        if (current is { Count: > 1 })
            pieces.Add(current);
        return pieces;

        void Cubic(double x1, double y1, double x2, double y2, double ex, double ey)
        {
            var (sx, sy) = (x, y);
            for (var i = 1; i <= 24; i++)
            {
                var t = i / 24.0;
                var u = 1 - t;
                LineTo(u * u * u * sx + 3 * u * u * t * x1 + 3 * u * t * t * x2 + t * t * t * ex,
                    u * u * u * sy + 3 * u * u * t * y1 + 3 * u * t * t * y2 + t * t * t * ey);
            }

            (lastControlX, lastControlY) = (x2, y2);
        }

        void Quadratic(double qx, double qy, double ex, double ey)
        {
            var (sx, sy) = (x, y);
            for (var i = 1; i <= 16; i++)
            {
                var t = i / 16.0;
                var u = 1 - t;
                LineTo(u * u * sx + 2 * u * t * qx + t * t * ex, u * u * sy + 2 * u * t * qy + t * t * ey);
            }

            (lastControlX, lastControlY) = (qx, qy);
        }
    }

    /// <summary>An SVG arc from its endpoints to points, following the conversion of the SVG specification (F.6.5).</summary>
    private static List<(double X, double Y)> ArcPoints(double x1, double y1, double rx, double ry, double rotationDeg,
        bool large, bool sweep, double x2, double y2)
    {
        if (rx == 0 || ry == 0 || (x1 == x2 && y1 == y2))
            return [(x1, y1), (x2, y2)];

        var (cx, cy, rxs, rys, phi, theta, delta) = ArcParameters(x1, y1, rx, ry, rotationDeg, large, sweep, x2, y2);
        var (cos, sin) = (Math.Cos(phi), Math.Sin(phi));
        var segments = Math.Max(4, (int)Math.Ceiling(Math.Abs(delta) / (2 * Math.PI) * 96));
        var points = new List<(double X, double Y)>();
        for (var i = 0; i <= segments; i++)
        {
            var a = theta + delta * i / segments;
            var (ex, ey) = (rxs * Math.Cos(a), rys * Math.Sin(a));
            points.Add((cos * ex - sin * ey + cx, sin * ex + cos * ey + cy));
        }

        return points;
    }

    /// <summary>
    /// Center, radii (scaled up if too small for the endpoints), rotation (radians), start angle and sweep of an SVG arc,
    /// as in the SVG specification (F.6.5); a positive sweep turns the way the angles grow in user units.
    /// </summary>
    private static (double Cx, double Cy, double Rx, double Ry, double Phi, double Theta, double Delta) ArcParameters(
        double x1, double y1, double rx, double ry, double rotationDeg, bool large, bool sweep, double x2, double y2)
    {
        rx = Math.Abs(rx);
        ry = Math.Abs(ry);
        var phi = rotationDeg * Math.PI / 180;
        var (cos, sin) = (Math.Cos(phi), Math.Sin(phi));
        var dx = (x1 - x2) / 2;
        var dy = (y1 - y2) / 2;
        var x1p = cos * dx + sin * dy;
        var y1p = -sin * dx + cos * dy;

        // Radii too small for the endpoints are scaled up
        var lambda = x1p * x1p / (rx * rx) + y1p * y1p / (ry * ry);
        if (lambda > 1)
        {
            rx *= Math.Sqrt(lambda);
            ry *= Math.Sqrt(lambda);
        }

        var numerator = rx * rx * ry * ry - rx * rx * y1p * y1p - ry * ry * x1p * x1p;
        var denominator = rx * rx * y1p * y1p + ry * ry * x1p * x1p;
        var factor = Math.Sqrt(Math.Max(0, numerator / denominator)) * (large == sweep ? -1 : 1);
        var cxp = factor * rx * y1p / ry;
        var cyp = -factor * ry * x1p / rx;
        var cx = cos * cxp - sin * cyp + (x1 + x2) / 2;
        var cy = sin * cxp + cos * cyp + (y1 + y2) / 2;

        static double Angle(double ux, double uy, double vx, double vy) =>
            Math.Atan2(ux * vy - uy * vx, ux * vx + uy * vy);

        var theta = Angle(1, 0, (x1p - cxp) / rx, (y1p - cyp) / ry);
        var delta = Angle((x1p - cxp) / rx, (y1p - cyp) / ry, (-x1p - cxp) / rx, (-y1p - cyp) / ry);
        if (!sweep && delta > 0) delta -= 2 * Math.PI;
        else if (sweep && delta < 0) delta += 2 * Math.PI;

        return (cx, cy, rx, ry, phi, theta, delta);
    }

    [GeneratedRegex(@"[A-Za-z]|[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?")]
    private static partial Regex PathToken();

    [GeneratedRegex(@"[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?")]
    private static partial Regex NumberToken();

    [GeneratedRegex(@"(matrix|translate|scale|rotate|skewX|skewY)\s*\(([^)]*)\)")]
    private static partial Regex TransformToken();

    private static List<double> Numbers(string? text) =>
        text is null ? [] : NumberToken().Matches(text).Select(m => double.Parse(m.Value, NumberStyles.Float, CultureInfo.InvariantCulture)).ToList();

    private static double Number(string? text) => Numbers(text) is [var first, ..] ? first : 0;

    /// <summary>A length with its unit in mm; null without a unit (then it is in user units).</summary>
    private static double? Length(string? text)
    {
        if (text is null || Numbers(text) is not [var value, ..])
            return null;

        var unit = text.Trim().TrimStart('-', '+', '.', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'e', 'E').Trim();
        return unit switch
        {
            "mm" => value,
            "cm" => value * 10,
            "in" => value * 25.4,
            "pt" => value * 25.4 / 72,
            "pc" => value * 25.4 / 6,
            "px" => value * MillimetersPerPixel,
            _ => null
        };
    }

    private static Matrix Transform(string? text)
    {
        var result = Matrix.Identity;
        if (string.IsNullOrWhiteSpace(text))
            return result;

        foreach (Match match in TransformToken().Matches(text))
        {
            var n = Numbers(match.Groups[2].Value);
            var step = match.Groups[1].Value switch
            {
                "matrix" when n.Count == 6 => new Matrix(n[0], n[1], n[2], n[3], n[4], n[5]),
                "translate" => Matrix.Translate(n.ElementAtOrDefault(0), n.ElementAtOrDefault(1)),
                "scale" => Matrix.Scale(n.ElementAtOrDefault(0), n.Count > 1 ? n[1] : n.ElementAtOrDefault(0)),
                "rotate" => Matrix.Rotate(n.ElementAtOrDefault(0), n.ElementAtOrDefault(1), n.ElementAtOrDefault(2)),
                "skewX" => new Matrix(1, 0, Math.Tan(n.ElementAtOrDefault(0) * Math.PI / 180), 1, 0, 0),
                "skewY" => new Matrix(1, Math.Tan(n.ElementAtOrDefault(0) * Math.PI / 180), 0, 1, 0, 0),
                _ => Matrix.Identity
            };
            result = result.Multiply(step);
        }

        return result;
    }

    /// <summary>2D affine transform as in SVG: x' = a·x + c·y + e, y' = b·x + d·y + f.</summary>
    private readonly record struct Matrix(double A, double B, double C, double D, double E, double F)
    {
        public static Matrix Identity => new(1, 0, 0, 1, 0, 0);
        public static Matrix Translate(double x, double y) => new(1, 0, 0, 1, x, y);
        public static Matrix Scale(double x, double y) => new(x, 0, 0, y, 0, 0);

        public static Matrix Rotate(double degrees, double cx, double cy)
        {
            var a = degrees * Math.PI / 180;
            var rotation = new Matrix(Math.Cos(a), Math.Sin(a), -Math.Sin(a), Math.Cos(a), 0, 0);
            return Translate(cx, cy).Multiply(rotation).Multiply(Translate(-cx, -cy));
        }

        /// <summary>This transform applied after the other: (this · other)(p) = this(other(p)).</summary>
        public Matrix Multiply(Matrix o) => new(
            A * o.A + C * o.B, B * o.A + D * o.B,
            A * o.C + C * o.D, B * o.C + D * o.D,
            A * o.E + C * o.F + E, B * o.E + D * o.F + F);

        public (double X, double Y) Apply((double X, double Y) p) => (A * p.X + C * p.Y + E, B * p.X + D * p.Y + F);

        /// <summary>Negative for a transform that mirrors.</summary>
        public double Determinant => A * D - B * C;

        /// <summary>Whether lengths scale the same in every direction (rotation and uniform scaling, maybe mirrored).</summary>
        public bool IsUniform(out double factor)
        {
            var sx = Math.Sqrt(A * A + B * B);
            var sy = Math.Sqrt(C * C + D * D);
            factor = sx;
            return Math.Abs(sx - sy) < 1e-9 * Math.Max(1, sx) && Math.Abs(A * C + B * D) < 1e-9 * Math.Max(1, sx * sy);
        }
    }
}