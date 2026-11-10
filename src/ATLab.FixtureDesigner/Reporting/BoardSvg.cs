using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ATLab.FixtureDesigner.Fixture;
using ATLab.Pcb.Controls;
using ATLab.Pcb.Fixture;
using ATLab.Pcb.Rendering;
using ATLab.Pcb.Kicad;
using ATLab.FixtureDesigner.Localization;
using ATLab.FixtureDesigner.Plates;
using ATLab.FixtureDesigner.ViewModels;

namespace ATLab.FixtureDesigner.Reporting;

/// <summary>The outline of a plate with its frame holes and cut-outs in board coordinates, for a drawing.</summary>
public sealed record PlateOutlineDrawing(FixturePlate Plate, IReadOnlyList<IReadOnlyList<Point2>> Outline);

/// <summary>
/// A probe in a drawing: its marker, as large as its neighbours allow, and its number inside it. Style is the index of
/// its probe type in the assembly drawing (shape, then hollow from the fifth type on) and 0 in the wiring drawing, where
/// hollow means not wired.
/// </summary>
public sealed record ProbeMarker(Probe Probe, Point2 Position, double Radius, double LabelFont, int Style, bool Hollow)
{
    public string Label => Probe.Number.ToString(CultureInfo.InvariantCulture);
}

/// <summary>An area of the drawing shown enlarged on a page of its own, because its probe numbers are too small on the whole.</summary>
public sealed record DrawingDetail(string Letter, Bounds2 Window);

/// <summary>
/// The layout of the drawing of one side, worked out before it is rendered: its extent, the probe markers and the areas
/// that need a detail view. Lengths are in board millimeters.
/// </summary>
public sealed class BoardDrawing
{
    public required BoardSide Side { get; init; }
    public required BoardSide View { get; init; }
    public required bool Wiring { get; init; }
    public required Bounds2 Bounds { get; init; }
    public required double Font { get; init; }
    public required double Margin { get; init; }

    /// <summary>The smallest probe number that is readable on the whole drawing.</summary>
    public required double MinLabelFont { get; init; }

    public required IReadOnlyList<ProbeMarker> Markers { get; init; }

    /// <summary>Probe types of the side in the order of their marker styles; empty for the wiring drawing.</summary>
    public required IReadOnlyList<ProbeType?> Types { get; init; }

    public required IReadOnlyList<DrawingDetail> Details { get; init; }

    public bool Mirror => View == BoardSide.Bottom;
    public double Width => Bounds.Width + 2 * Margin;
    public double Height => Bounds.Height + 2 * Margin;

    /// <summary>Board coordinates to drawing coordinates; the bottom view is mirrored left to right.</summary>
    public (double X, double Y) Map(Point2 p) =>
        (Mirror ? Bounds.MaxX - p.X + Margin : p.X - Bounds.MinX + Margin, p.Y - Bounds.MinY + Margin);

    public bool IsReadable(ProbeMarker marker) => marker.LabelFont >= MinLabelFont * 0.999;
}

/// <summary>
/// Drawing of one side's probes and parts on the board for the build sheet, in millimeters: outline, pads, holes, the
/// origin and the numbered probes. Seen from the top, or from below and then mirrored.
/// </summary>
public static class BoardSvg
{
    private static string ProbeColor => FixtureColors.Get(ColorItem.Probe);
    private const string OriginColor = "#16A34A";
    private const string DetailColor = "#374151";

    /// <summary>Smallest probe number on paper (pt) that is still read without effort.</summary>
    public const double MinLabelPt = 4.6;

    /// <summary>A detail is enlarged this much beyond what its smallest number needs, so its numbers read easily.</summary>
    private const double DetailZoom = 1.75;

    /// <summary>A detail of only a few probes is enlarged at most this much further, so it keeps some of the board around it.</summary>
    private const double MaxDetailZoom = 1.25;

    /// <summary>
    /// Color of each plate's outline and openings: the probe plates gray, the moving plate orange, the pressure plate
    /// blue, apart from the colors of the probes and parts drawn with them.
    /// </summary>
    public static string PlateColor(FixturePlate plate) => FixtureColors.Get(FixtureColors.Of(plate));

    /// <summary>
    /// Lays out the drawing of a side for an area of the page (pt). Every probe number is drawn inside its marker, so it
    /// can't collide with another; a marker is as large as its nearest neighbour allows. Numbers too small to read at the
    /// scale of the whole drawing are left out there and shown in detail views, enlarged until they are readable.
    /// </summary>
    /// <param name="side">The plate: its probes and parts are drawn.</param>
    /// <param name="view">Where the drawing is seen from; from the bottom it is mirrored.</param>
    /// <param name="wiring">The wiring drawing: unwired probes hollow, no probe types; else the assembly, a shape per probe type.</param>
    public static BoardDrawing Plan(Board board, IReadOnlyList<Probe> probes, BoardSide side, BoardSide view, bool wiring,
        IReadOnlyList<FixturePart>? parts, IReadOnlyList<PlateOutlineDrawing>? plates, double areaWidth, double areaHeight)
    {
        // Pre-centering pins stand beside the board and the plates are larger: the drawing includes both
        var bounds = board.Bounds;
        foreach (var p in (plates ?? []).SelectMany(d => d.Outline).SelectMany(l => l))
            bounds = new Bounds2(Math.Min(bounds.MinX, p.X), Math.Min(bounds.MinY, p.Y), Math.Max(bounds.MaxX, p.X), Math.Max(bounds.MaxY, p.Y));
        foreach (var part in parts ?? [])
        {
            var r = (part.Type?.Diameter ?? 2) / 2;
            bounds = new Bounds2(Math.Min(bounds.MinX, part.BoardX - r), Math.Min(bounds.MinY, part.BoardY - r),
                Math.Max(bounds.MaxX, part.BoardX + r), Math.Max(bounds.MaxY, part.BoardY + r));
        }

        var size = Math.Max(bounds.Width, bounds.Height);
        var font = Math.Clamp(size / 65, 1.5, 5);

        // Room for the origin labels and part names beyond the board edge
        var margin = font * 3.5;
        var scale = Math.Min(areaWidth / (bounds.Width + 2 * margin), areaHeight / (bounds.Height + 2 * margin));

        var sideProbes = probes.Where(p => p.Side == side && p.Pad is not null).ToList();
        var types = wiring
            ? []
            : sideProbes.Select(p => p.Type).Distinct().OrderBy(t => t is null).ThenBy(t => t?.Name, StringComparer.CurrentCulture).ToList();

        // A marker fills most of the way to its nearest neighbour, up to the size of a part name; its number is as large
        // as fits inside it
        var maxRadius = font * 0.75;
        var markers = new List<ProbeMarker>(sideProbes.Count);
        foreach (var probe in sideProbes)
        {
            var at = probe.Pad!.Position;
            var nearest = double.MaxValue;
            foreach (var other in sideProbes)
                if (other != probe)
                    nearest = Math.Min(nearest, at.DistanceTo(other.Pad!.Position));

            // Never smaller than a third of a part name, so probes almost on top of each other don't need an endless zoom
            var radius = Math.Clamp(nearest * 0.46, font * 0.3, maxRadius);
            var style = wiring ? 0 : types.IndexOf(probe.Type);
            var hollow = wiring ? probe.Connections.Length == 0 : style >= Shapes;
            var inner = InnerRadius(style, radius) * (hollow ? 0.88 : 1);
            var chars = Math.Max(1, probe.Number.ToString(CultureInfo.InvariantCulture).Length);
            var labelFont = Math.Min(font * 0.8, Math.Min(2 * inner * 0.88 / (chars * 0.58), inner * 1.45));
            markers.Add(new ProbeMarker(probe, at, radius, labelFont, style, hollow));
        }

        var minFont = MinLabelPt / scale;
        var mirror = view == BoardSide.Bottom;
        var extent = new Bounds2(bounds.MinX - margin, bounds.MinY - margin, bounds.MaxX + margin, bounds.MaxY + margin);
        var details = PlanDetails(markers.Where(m => m.LabelFont < minFont * 0.999).ToList(), areaWidth, areaHeight, font, maxRadius, extent)
            .OrderBy(w => w.MinY).ThenBy(w => mirror ? -w.MaxX : w.MinX)
            .Select((w, i) => new DrawingDetail(Letter(i), w))
            .ToList();

        return new BoardDrawing
        {
            Side = side, View = view, Wiring = wiring, Bounds = bounds, Font = font, Margin = margin, MinLabelFont = minFont,
            Markers = markers, Types = types, Details = details
        };
    }

    /// <summary>
    /// Windows around the probes whose numbers are too small, in board coordinates, all at the zoom the smallest number
    /// needs, enlarged a bit more: page-sized tiles laid from the top left, each from the topmost probe not yet shown, so they don't overlap.
    /// Each tile then shrinks to its probes and grows to the shape of the page.
    /// </summary>
    private static List<Bounds2> PlanDetails(List<ProbeMarker> small, double areaWidth, double areaHeight, double font, double maxRadius,
        Bounds2 extent)
    {
        var windows = new List<Bounds2>();
        if (small.Count == 0)
            return windows;

        var needed = MinLabelPt / small.Min(m => m.LabelFont) * DetailZoom;
        var pad = maxRadius + font * 0.6;
        var tileWidth = Math.Max(areaWidth / needed - 2 * pad, 0.1);
        var tileHeight = Math.Max(areaHeight / needed - 2 * pad, 0.1);

        var left = new List<ProbeMarker>(small);
        while (left.Count > 0)
        {
            var top = left.Min(m => m.Position.Y);
            var band = left.Where(m => m.Position.Y <= top + tileHeight).ToList();
            var x = band.Min(m => m.Position.X);
            var tile = band.Where(m => m.Position.X <= x + tileWidth).ToHashSet();
            left.RemoveAll(tile.Contains);

            var box = new Bounds2(tile.Min(m => m.Position.X) - pad, tile.Min(m => m.Position.Y) - pad,
                tile.Max(m => m.Position.X) + pad, tile.Max(m => m.Position.Y) + pad);
            windows.Add(Fill(box));
        }

        return windows;

        // The window grown to the shape of the page and to at most a little more zoom than the numbers need, moved back
        // inside the drawing where it grew beyond it
        Bounds2 Fill(Bounds2 box)
        {
            var width = Math.Max(box.Width, areaWidth / (needed * MaxDetailZoom));
            var height = Math.Max(box.Height, areaHeight / (needed * MaxDetailZoom));
            if (width / height < areaWidth / areaHeight)
                width = height * areaWidth / areaHeight;
            else
                height = width * areaHeight / areaWidth;

            static double Inside(double center, double size, double min, double max) =>
                size >= max - min ? (min + max) / 2 : Math.Clamp(center, min + size / 2, max - size / 2);

            var x = Inside(box.Center.X, width, extent.MinX, extent.MaxX);
            var y = Inside(box.Center.Y, height, extent.MinY, extent.MaxY);
            return new Bounds2(x - width / 2, y - height / 2, x + width / 2, y + height / 2);
        }
    }

    private static string Letter(int index) =>
        index < 26 ? ((char)('A' + index)).ToString() : Letter(index / 26 - 1) + (char)('A' + index % 26);

    /// <param name="style">Layers of the board side seen, with their order, colors and opacity.</param>
    /// <param name="plates">The outlines of the plates drawn, each in its color; outlines plates share are drawn once in their colors by turns.</param>
    /// <param name="withOpenings">The openings of the moving plate for the bottom probes, of the top probe plate for the push rods.</param>
    /// <param name="detail">The area to show enlarged; null for the whole drawing, with the detail areas framed.</param>
    public static string Render(BoardDrawing drawing, Board board, BoardLayerStyle style, IReadOnlyList<FixturePart>? parts = null,
        IReadOnlyList<PlateOutlineDrawing>? plates = null, bool withOpenings = false, DrawingDetail? detail = null)
    {
        plates ??= [];
        var side = drawing.Side;
        var mirror = drawing.Mirror;
        var font = drawing.Font;
        var Map = drawing.Map;

        // The whole drawing, or the window of the detail
        var (vx, vy, vw, vh) = (0.0, 0.0, drawing.Width, drawing.Height);
        if (detail is not null)
        {
            var a = Map(new Point2(detail.Window.MinX, detail.Window.MinY));
            var b = Map(new Point2(detail.Window.MaxX, detail.Window.MaxY));
            (vx, vy, vw, vh) = (Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Abs(b.X - a.X), Math.Abs(b.Y - a.Y));
        }

        var svg = new StringBuilder();
        svg.Append(Invariant($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"{vx:0.###} {vy:0.###} {vw:0.###} {vh:0.###}\" width=\"{vw:0.###}mm\" height=\"{vh:0.###}mm\">"));
        svg.Append(Invariant($"<rect x=\"{vx:0.###}\" y=\"{vy:0.###}\" width=\"{vw:0.###}\" height=\"{vh:0.###}\" fill=\"#FFFFFF\"/>"));

        // Everything clipped to the window: the PDF would otherwise show what lies beyond a detail over the rest of the page
        svg.Append(Invariant($"<defs><clipPath id=\"window\"><rect x=\"{vx:0.###}\" y=\"{vy:0.###}\" width=\"{vw:0.###}\" height=\"{vh:0.###}\"/></clipPath></defs><g clip-path=\"url(#window)\">"));

        // The board as seen from where the drawing looks, with the chosen layers of that side (black and white by
        // default, as in ATLab's report); over them the plate outline
        BoardSvgRenderer.AppendBoard(svg, board, drawing.View, mirror, p => Map(p), drawing.Bounds, drawing.Margin, font, style);

        // Plate outlines and frame holes, dashed as in the board view, each plate in its color; an outline that several
        // plates share is drawn once, its dashes in their colors by turns
        var dash = font * 0.6;
        var gap = font * 0.4;
        foreach (var group in GroupByOutline(plates))
        {
            for (var i = 0; i < group.Count; i++)
            {
                var pattern = group.Count == 1
                    ? Invariant($"{dash:0.###} {gap:0.###}")
                    : Invariant($"{dash:0.###} {(group.Count - 1) * (dash + gap) + gap:0.###}");
                var offset = -i * (dash + gap);
                foreach (var polyline in group[i].Outline)
                {
                    var points = string.Join(" ", polyline.Select(p => Map(p)).Select(p => Invariant($"{p.X:0.###},{p.Y:0.###}")));
                    svg.Append(Invariant($"<polyline points=\"{points}\" fill=\"none\" stroke=\"{PlateColor(group[i].Plate)}\" stroke-width=\"{font * 0.12:0.###}\" stroke-dasharray=\"{pattern}\" stroke-dashoffset=\"{offset:0.###}\"/>"));
                }
            }
        }

        AppendOrigin(svg, board, p => Map(p), mirror, font);

        // Parts of this side's plate at their real size with the symbol of their kind as in the board view: tooling,
        // pre-centering and support pins in the bottom drawing, push rods in the top one. The name centered above, so the
        // symbol stays free
        foreach (var part in (parts ?? []).Where(p => p.PlateSide == side))
        {
            var c = Map(new Point2(part.BoardX, part.BoardY));
            var r = Math.Max((part.Type?.Diameter ?? 2) / 2, font * 0.3);
            var color = PartColor(part.Kind);
            svg.Append(PartSymbol(part.Kind, c.X, c.Y, r, color, font * 0.12, font * 0.08));
            svg.Append(Invariant($"<text x=\"{c.X:0.###}\" y=\"{c.Y - r - font * 0.25:0.###}\" text-anchor=\"middle\" font-family=\"Arial, Helvetica, sans-serif\" font-size=\"{font:0.###}\" font-weight=\"bold\" fill=\"{color}\">{Escape(part.Name)}</text>"));
        }

        // Openings at their real size, thin in the color of their plate and under the probes, so they don't cross the
        // numbers: one around its probe shows where it is larger. The bottom probes come through the moving plate, the
        // push rods through the top probe plate, if the drawing has it
        if (withOpenings && OpeningPlate(plates.Select(p => p.Plate)) is { } openingPlate)
        {
            // Openings too close to each other are merged as in the plate files and drawn with their outline
            var openings = openingPlate == FixturePlate.MovingPlate
                ? drawing.Markers.Select(m => m.Probe).Where(p => p.HasOpening && p.Opening > 0)
                    .Select(p => new OpeningCircle(p.Name, p.Pad!.Position.X, p.Pad.Position.Y, p.Opening))
                : (parts ?? []).Where(p => p.Kind == FixturePartKind.PushRod && p.Opening > 0)
                    .Select(p => new OpeningCircle(p.Name, p.BoardX, p.BoardY, p.Opening));
            var (single, merged) = OpeningMerge.Merge(openings.ToList(), Plate.MinimumWall);
            var openingStroke = Invariant($"fill=\"none\" stroke=\"{PlateColor(openingPlate)}\" stroke-width=\"{font * 0.06:0.###}\"");
            foreach (var opening in single)
            {
                var c = Map(new Point2(opening.X, opening.Y));
                svg.Append(Invariant($"<circle cx=\"{c.X:0.###}\" cy=\"{c.Y:0.###}\" r=\"{opening.Diameter / 2:0.###}\" {openingStroke}/>"));
            }

            foreach (var opening in merged)
            {
                var points = string.Join(" ", OutlineShape.Flatten(opening.Contour)
                    .Select(p => Map(new Point2(p.X, p.Y))).Select(p => Invariant($"{p.X:0.###},{p.Y:0.###}")));
                svg.Append($"<polygon points=\"{points}\" {openingStroke}/>");
            }
        }

        // Probe markers of this side; probes of the other side are not part of this plate
        var stroke = font * 0.07;
        foreach (var m in drawing.Markers)
        {
            var c = Map(m.Position);
            svg.Append(MarkerShape(m.Style, m.Hollow, c.X, c.Y, m.Radius, ProbeColor, Math.Max(stroke, m.Radius * 0.14)));
        }

        // Numbers inside their markers, on top of everything, in black; white only on a filled marker too dark for
        // black. On the whole drawing only those readable at its scale; the others are in the detail views
        var onFill = TextOn(ProbeColor);
        foreach (var m in drawing.Markers)
        {
            if (detail is null && !drawing.IsReadable(m))
                continue;

            var c = Map(m.Position);
            svg.Append(Invariant($"<text x=\"{c.X:0.###}\" y=\"{c.Y + m.LabelFont * 0.36:0.###}\" text-anchor=\"middle\" font-family=\"Arial, Helvetica, sans-serif\" font-size=\"{m.LabelFont:0.###}\" font-weight=\"bold\" fill=\"{(m.Hollow ? "#000000" : onFill)}\">{m.Label}</text>"));
        }

        // The detail areas framed with their letter on the whole drawing
        if (detail is null)
        {
            foreach (var d in drawing.Details)
            {
                var a = Map(new Point2(d.Window.MinX, d.Window.MinY));
                var b = Map(new Point2(d.Window.MaxX, d.Window.MaxY));
                var (x, y) = (Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
                svg.Append(Invariant($"<rect x=\"{x:0.###}\" y=\"{y:0.###}\" width=\"{Math.Abs(b.X - a.X):0.###}\" height=\"{Math.Abs(b.Y - a.Y):0.###}\" fill=\"none\" stroke=\"{DetailColor}\" stroke-width=\"{font * 0.1:0.###}\"/>"));
                svg.Append(Invariant($"<text x=\"{x + font * 0.3:0.###}\" y=\"{y - font * 0.3:0.###}\" font-family=\"Arial, Helvetica, sans-serif\" font-size=\"{font:0.###}\" font-weight=\"bold\" fill=\"{DetailColor}\">{d.Letter}</text>"));
            }
        }

        svg.Append("</g></svg>");
        return svg.ToString();
    }

    /// <summary>Number of marker shapes; from then on the shapes repeat hollow.</summary>
    private const int Shapes = 4;

    /// <summary>Radius of the largest circle inside the marker of a style, where its number goes.</summary>
    private static double InnerRadius(int style, double radius) => (style % Shapes) switch
    {
        1 => radius * 0.9,                    // square, half its side 0.9 r
        2 => radius * 1.08 / Math.Sqrt(2),    // diamond, half its diagonal 1.08 r
        3 => radius * 1.05 * 0.866,           // hexagon, corners at 1.05 r
        _ => radius                           // circle
    };

    /// <summary>The marker of a probe: circle, square, diamond or hexagon after its style, filled or hollow.</summary>
    private static string MarkerShape(int style, bool hollow, double x, double y, double r, string color, double strokeWidth)
    {
        var paint = hollow
            ? Invariant($"fill=\"#FFFFFF\" stroke=\"{color}\" stroke-width=\"{strokeWidth:0.###}\"")
            : Invariant($"fill=\"{color}\" stroke=\"#FFFFFF\" stroke-width=\"{strokeWidth * 0.4:0.###}\"");
        switch (Math.Max(0, style) % Shapes)
        {
            case 1:
            {
                var h = r * 0.9;
                return Invariant($"<rect x=\"{x - h:0.###}\" y=\"{y - h:0.###}\" width=\"{2 * h:0.###}\" height=\"{2 * h:0.###}\" {paint}/>");
            }
            case 2:
            {
                var h = r * 1.08;
                return Invariant($"<polygon points=\"{x:0.###},{y - h:0.###} {x + h:0.###},{y:0.###} {x:0.###},{y + h:0.###} {x - h:0.###},{y:0.###}\" {paint}/>");
            }
            case 3:
            {
                var h = r * 1.05;
                var points = string.Join(" ", Enumerable.Range(0, 6).Select(i =>
                    Invariant($"{x + h * Math.Cos(i * Math.PI / 3):0.###},{y + h * Math.Sin(i * Math.PI / 3):0.###}")));
                return $"<polygon points=\"{points}\" {paint}/>";
            }
            default:
                return Invariant($"<circle cx=\"{x:0.###}\" cy=\"{y:0.###}\" r=\"{r:0.###}\" {paint}/>");
        }
    }

    /// <summary>Black, or white on a color ("#RRGGBB") too dark for black.</summary>
    private static string TextOn(string color)
    {
        var hex = color.TrimStart('#');
        if (hex.Length >= 6 && int.TryParse(hex[^6..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
        {
            var luminance = (0.299 * ((rgb >> 16) & 0xFF) + 0.587 * ((rgb >> 8) & 0xFF) + 0.114 * (rgb & 0xFF)) / 255;
            return luminance < 0.3 ? "#FFFFFF" : "#000000";
        }

        return "#000000";
    }

    /// <summary>
    /// The symbol of a part as in the board view: a ring with its kind's fill, a cross for a tooling pin (with a heavier
    /// ring), a dot for a support pin and an × for a push rod.
    /// </summary>
    private static string PartSymbol(FixturePartKind kind, double x, double y, double r, string color, double stroke, double thin)
    {
        var fill = kind switch
        {
            FixturePartKind.ToolingPin => 0.25,
            FixturePartKind.PreCenteringPin => 0.45,
            _ => 0.2
        };
        var ringWidth = kind == FixturePartKind.ToolingPin ? stroke * 1.6 : stroke;
        var svg = new StringBuilder(Invariant(
            $"<circle cx=\"{x:0.###}\" cy=\"{y:0.###}\" r=\"{r:0.###}\" fill=\"{color}\" fill-opacity=\"{fill:0.##}\" stroke=\"{color}\" stroke-width=\"{ringWidth:0.###}\"/>"));
        string Line(double x1, double y1, double x2, double y2) =>
            Invariant($"<line x1=\"{x1:0.###}\" y1=\"{y1:0.###}\" x2=\"{x2:0.###}\" y2=\"{y2:0.###}\" stroke=\"{color}\" stroke-width=\"{thin:0.###}\"/>");
        switch (kind)
        {
            case FixturePartKind.ToolingPin:
                svg.Append(Line(x - r * 0.6, y, x + r * 0.6, y)).Append(Line(x, y - r * 0.6, x, y + r * 0.6));
                break;
            case FixturePartKind.SupportPin:
                svg.Append(Invariant($"<circle cx=\"{x:0.###}\" cy=\"{y:0.###}\" r=\"{r * 0.25:0.###}\" fill=\"{color}\"/>"));
                break;
            case FixturePartKind.PushRod:
                var d = r * 0.6;
                svg.Append(Line(x - d, y - d, x + d, y + d)).Append(Line(x - d, y + d, x + d, y - d));
                break;
        }

        return svg.ToString();
    }

    private static string PartColor(FixturePartKind kind) => FixtureColors.Get(FixtureColors.Of(kind));

    /// <summary>The plate of a drawing with openings: the moving plate below the board, the top probe plate above it; null for none.</summary>
    private static FixturePlate? OpeningPlate(IEnumerable<FixturePlate> plates)
    {
        var list = plates.ToList();
        return list.Contains(FixturePlate.MovingPlate) ? FixturePlate.MovingPlate
            : list.Contains(FixturePlate.TopProbePlate) ? FixturePlate.TopProbePlate
            : null;
    }

    /// <summary>Plates with the same outline together, in the order they come.</summary>
    public static List<List<PlateOutlineDrawing>> GroupByOutline(IReadOnlyList<PlateOutlineDrawing> plates)
    {
        static bool Same(IReadOnlyList<IReadOnlyList<Point2>> a, IReadOnlyList<IReadOnlyList<Point2>> b) =>
            a.Count == b.Count && a.Zip(b).All(l => l.First.Count == l.Second.Count &&
                                                    l.First.Zip(l.Second).All(p => p.First.DistanceTo(p.Second) < 1e-6));

        var groups = new List<List<PlateOutlineDrawing>>();
        foreach (var plate in plates)
        {
            if (groups.FirstOrDefault(g => Same(g[0].Outline, plate.Outline)) is { } group)
                group.Add(plate);
            else
                groups.Add([plate]);
        }

        return groups;
    }

    /// <summary>
    /// The probes, parts and plates the drawing shows, each with its symbol as a small SVG (12 × 12) and its name: only
    /// the kinds the drawing has. The probes by type in the assembly drawing, wired and not wired in the wiring drawing.
    /// </summary>
    public static IReadOnlyList<(string Symbol, string Label)> Legend(BoardDrawing drawing, IReadOnlyList<FixturePart>? parts = null,
        IReadOnlyList<FixturePlate>? plates = null, bool withOpenings = false)
    {
        static string Symbol(string body) =>
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 12 12\" width=\"12\" height=\"12\">{body}</svg>";

        var side = drawing.Side;
        var items = new List<(string, string)>();
        if (drawing.Wiring)
        {
            if (drawing.Markers.Any(m => !m.Hollow))
                items.Add((Symbol(MarkerShape(0, false, 6, 6, 4.5, ProbeColor, 1)), Localizer.Get("Report_LegendWired")));
            if (drawing.Markers.Any(m => m.Hollow))
                items.Add((Symbol(MarkerShape(0, true, 6, 6, 4.2, ProbeColor, 1.2)), Localizer.Get("Report_LegendNotWired")));
        }
        else
        {
            for (var i = 0; i < drawing.Types.Count; i++)
            {
                var hollow = i >= Shapes;
                items.Add((Symbol(MarkerShape(i, hollow, 6, 6, hollow ? 4.2 : 4.5, ProbeColor, 1.2)),
                    Localizer.Format("Report_LegendProbeType", drawing.Types[i]?.Name ?? Localizer.Get("Report_NoType"))));
            }
        }

        var sideParts = (parts ?? []).Where(p => p.PlateSide == side).ToList();
        foreach (var (kind, key) in new[]
                 {
                     (FixturePartKind.ToolingPin, "Board_LegendToolingPins"),
                     (FixturePartKind.PreCenteringPin, "Board_LegendPreCenteringPins"),
                     (FixturePartKind.SupportPin, "Board_LegendSupportPins"),
                     (FixturePartKind.PushRod, "Board_LegendPushRods")
                 })
        {
            if (sideParts.All(p => p.Kind != kind))
                continue;

            items.Add((Symbol(PartSymbol(kind, 6, 6, 4.5, PartColor(kind), 1.2, 0.8)), Localizer.Get(key)));
        }

        foreach (var plate in plates ?? [])
            items.Add((Symbol($"<line x1=\"0.5\" y1=\"6\" x2=\"11.5\" y2=\"6\" stroke=\"{PlateColor(plate)}\" stroke-width=\"1.2\" stroke-dasharray=\"3 2\"/>"),
                Localizer.Format("Report_LegendPlateOutline", PlateRow.NameOf(plate))));

        // Openings only where the drawing has them
        if (withOpenings)
        {
            var plate = OpeningPlate(plates ?? []);
            var any = plate == FixturePlate.MovingPlate
                ? drawing.Markers.Any(m => m.Probe.HasOpening && m.Probe.Opening > 0)
                : (parts ?? []).Any(p => p.Kind == FixturePartKind.PushRod && p.Opening > 0);
            if (plate is not null && any)
                items.Add((Symbol($"<circle cx=\"6\" cy=\"6\" r=\"4.5\" fill=\"none\" stroke=\"{PlateColor(plate.Value)}\" stroke-width=\"1\"/>"),
                    Localizer.Format("Board_LegendOpenings", PlateRow.NameOf(plate.Value))));
        }

        return items;
    }

    /// <summary>Cross at the origin of the coordinates with the directions of X and Y.</summary>
    private static void AppendOrigin(StringBuilder svg, Board board, Func<Point2, (double X, double Y)> map, bool mirror, double font)
    {
        var o = map(board.Origin);
        var length = font * 3;
        var xEnd = (X: o.X + (mirror ? -length : length), o.Y);
        var yEnd = (o.X, Y: o.Y - length);
        var stroke = Invariant($"stroke=\"{OriginColor}\" stroke-width=\"{font * 0.12:0.###}\"");

        svg.Append(Invariant($"<line x1=\"{o.X:0.###}\" y1=\"{o.Y:0.###}\" x2=\"{xEnd.Item1:0.###}\" y2=\"{xEnd.Item2:0.###}\" {stroke}/>"));
        svg.Append(Invariant($"<line x1=\"{o.X:0.###}\" y1=\"{o.Y:0.###}\" x2=\"{yEnd.Item1:0.###}\" y2=\"{yEnd.Y:0.###}\" {stroke}/>"));
        svg.Append(Invariant($"<circle cx=\"{o.X:0.###}\" cy=\"{o.Y:0.###}\" r=\"{font * 0.25:0.###}\" fill=\"{OriginColor}\"/>"));

        var labelX = mirror ? xEnd.Item1 - font * 1.0 : xEnd.Item1 + font * 0.2;
        svg.Append(Invariant($"<text x=\"{labelX:0.###}\" y=\"{o.Y + font * 0.35:0.###}\" font-family=\"Arial, Helvetica, sans-serif\" font-size=\"{font:0.###}\" fill=\"{OriginColor}\">X</text>"));
        svg.Append(Invariant($"<text x=\"{o.X - font * 0.35:0.###}\" y=\"{yEnd.Y - font * 0.25:0.###}\" font-family=\"Arial, Helvetica, sans-serif\" font-size=\"{font:0.###}\" fill=\"{OriginColor}\">Y</text>"));
        svg.Append(Invariant($"<text x=\"{o.X + (mirror ? -font * 2.2 : font * 0.4):0.###}\" y=\"{o.Y + font * 1.3:0.###}\" font-family=\"Arial, Helvetica, sans-serif\" font-size=\"{font * 0.8:0.###}\" fill=\"{OriginColor}\">0,0</text>"));
    }

    private static string Invariant(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);

    private static string Escape(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    /// <summary>Caption of the drawing: the assembly of a side of the board, or the wiring of the probe plate of that side.</summary>
    public static string Caption(BoardSide side, bool wiring, FixturePlate probePlate) => wiring
        ? Localizer.Format("Report_WiringOf", PlateRow.NameOf(probePlate))
        : Localizer.Get(side == BoardSide.Top ? "Report_AssemblyTop" : "Report_AssemblyBottom");

    public static string ViewHint(BoardSide view) =>
        Localizer.Get(view == BoardSide.Top ? "Report_ViewFromTop" : "Report_ViewFromBottom");
}
