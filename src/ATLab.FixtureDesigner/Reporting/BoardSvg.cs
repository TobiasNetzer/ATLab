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
/// Drawing of one side's probes and parts on the board for the build sheet, in millimeters: outline, pads, holes, the
/// origin and the numbered probes. Seen from the top, or from below and then mirrored.
/// </summary>
public static class BoardSvg
{
    private const string ProbeColor = MarkerColors.Probe;
    private const string OriginColor = "#16A34A";

    /// <summary>
    /// Color of each plate's outline and openings: the probe plates gray, the moving plate orange, the pressure plate
    /// blue, apart from the colors of the probes and parts drawn with them.
    /// </summary>
    public static string PlateColor(FixturePlate plate) => plate switch
    {
        FixturePlate.MovingPlate => "#EA580C",
        FixturePlate.PressurePlate => "#2563EB",
        _ => "#52525B"
    };

    /// <param name="side">The plate: its probes and parts are drawn.</param>
    /// <param name="view">Where the drawing is seen from; from the bottom it is mirrored.</param>
    /// <param name="style">Layers of the board side seen, with their order, colors and opacity.</param>
    /// <param name="plates">The outlines of the plates drawn, each in its color; outlines plates share are drawn once in their colors by turns.</param>
    /// <param name="withPins">Each probe is labelled with the pin of the test hardware it is wired to, e.g. "P1 - c10".</param>
    /// <param name="withOpenings">The openings of the moving plate for the bottom probes, of the top probe plate for the push rods.</param>
    public static string Render(Board board, IReadOnlyList<Probe> probes, BoardSide side, BoardSide view, BoardLayerStyle style,
        IReadOnlyList<FixturePart>? parts = null, IReadOnlyList<PlateOutlineDrawing>? plates = null, bool withPins = false,
        bool withOpenings = false)
    {
        plates ??= [];

        // Pre-centering pins stand beside the board and the plates are larger: the drawing includes both
        var bounds = board.Bounds;
        foreach (var p in plates.SelectMany(d => d.Outline).SelectMany(l => l))
            bounds = new Bounds2(Math.Min(bounds.MinX, p.X), Math.Min(bounds.MinY, p.Y), Math.Max(bounds.MaxX, p.X), Math.Max(bounds.MaxY, p.Y));
        foreach (var part in parts ?? [])
        {
            var r = (part.Type?.Diameter ?? 2) / 2;
            bounds = new Bounds2(Math.Min(bounds.MinX, part.BoardX - r), Math.Min(bounds.MinY, part.BoardY - r),
                Math.Max(bounds.MaxX, part.BoardX + r), Math.Max(bounds.MaxY, part.BoardY + r));
        }

        var mirror = view == BoardSide.Bottom;
        var size = Math.Max(bounds.Width, bounds.Height);
        var font = Math.Clamp(size / 65, 1.5, 5);

        // Room for the origin labels and probe numbers beyond the board edge
        var margin = font * 3.5;
        var width = bounds.Width + 2 * margin;
        var height = bounds.Height + 2 * margin;

        // Board coordinates to drawing coordinates; the bottom view is mirrored left to right
        (double X, double Y) Map(Point2 p) =>
            (mirror ? bounds.MaxX - p.X + margin : p.X - bounds.MinX + margin, p.Y - bounds.MinY + margin);

        var svg = new StringBuilder();
        svg.Append(Invariant($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {width:0.###} {height:0.###}\" width=\"{width:0.###}mm\" height=\"{height:0.###}mm\">"));
        svg.Append("<rect width=\"100%\" height=\"100%\" fill=\"#FFFFFF\"/>");

        // The board as seen from where the drawing looks, with the chosen layers of that side (black and white by
        // default, as in ATLab's report); over them the plate outline
        BoardSvgRenderer.AppendBoard(svg, board, view, mirror, Map, bounds, margin, font, style);

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

        AppendOrigin(svg, board, Map, mirror, font);

        // Parts of this side's plate at their real size: locating, pre-centering and support pins in the bottom drawing,
        // push rods in the top one
        foreach (var part in (parts ?? []).Where(p => p.PlateSide == side))
        {
            var c = Map(new Point2(part.BoardX, part.BoardY));
            var r = Math.Max((part.Type?.Diameter ?? 2) / 2, font * 0.3);
            var color = PartColor(part.Kind);
            svg.Append(Invariant($"<circle cx=\"{c.X:0.###}\" cy=\"{c.Y:0.###}\" r=\"{r:0.###}\" fill=\"{color}\" fill-opacity=\"0.25\" stroke=\"{color}\" stroke-width=\"{font * 0.12:0.###}\"/>"));
            if (part.Kind == FixturePartKind.LocatingPin)
            {
                svg.Append(Invariant($"<line x1=\"{c.X - r:0.###}\" y1=\"{c.Y:0.###}\" x2=\"{c.X + r:0.###}\" y2=\"{c.Y:0.###}\" stroke=\"{color}\" stroke-width=\"{font * 0.08:0.###}\"/>"));
                svg.Append(Invariant($"<line x1=\"{c.X:0.###}\" y1=\"{c.Y - r:0.###}\" x2=\"{c.X:0.###}\" y2=\"{c.Y + r:0.###}\" stroke=\"{color}\" stroke-width=\"{font * 0.08:0.###}\"/>"));
            }
            else if (part.Kind == FixturePartKind.PushRod)
            {
                var d = r * 0.7;
                svg.Append(Invariant($"<line x1=\"{c.X - d:0.###}\" y1=\"{c.Y - d:0.###}\" x2=\"{c.X + d:0.###}\" y2=\"{c.Y + d:0.###}\" stroke=\"{color}\" stroke-width=\"{font * 0.08:0.###}\"/>"));
                svg.Append(Invariant($"<line x1=\"{c.X - d:0.###}\" y1=\"{c.Y + d:0.###}\" x2=\"{c.X + d:0.###}\" y2=\"{c.Y - d:0.###}\" stroke=\"{color}\" stroke-width=\"{font * 0.08:0.###}\"/>"));
            }

            svg.Append(Invariant($"<text x=\"{c.X + r + font * 0.2:0.###}\" y=\"{c.Y + font * 0.35:0.###}\" font-family=\"Arial, Helvetica, sans-serif\" font-size=\"{font:0.###}\" font-weight=\"bold\" fill=\"{color}\">{Escape(part.Name)}</text>"));
        }

        // Probes of this side with their numbers; probes of the other side are not part of this plate
        var probesOfSide = probes.Where(p => p.Side == side && p.Pad is not null).ToList();
        var markers = probesOfSide
            .Select(p => (Probe: p, Center: Map(p.Pad!.Position), Radius: Math.Clamp(p.Pad.MinSize / 2, font * 0.4, font * 0.65)))
            .ToList();

        foreach (var (probe, c, r) in markers)
        {
            svg.Append(Invariant($"<circle cx=\"{c.X:0.###}\" cy=\"{c.Y:0.###}\" r=\"{r:0.###}\" fill=\"{ProbeColor}\" fill-opacity=\"0.85\" stroke=\"#FFFFFF\" stroke-width=\"{font * 0.06:0.###}\"/>"));
        }

        // Openings at their real size, thin in the color of their plate and over the probes, which may be larger: the
        // bottom probes come through the moving plate, the push rods through the top probe plate, if the drawing has it
        if (withOpenings && OpeningPlate(plates.Select(p => p.Plate)) is { } openingPlate)
        {
            // Openings too close to each other are merged as in the plate files and drawn with their outline
            var openings = openingPlate == FixturePlate.MovingPlate
                ? probes.Where(p => p.Side == side && p.HasOpening && p.Pad is not null && p.Opening > 0)
                    .Select(p => new OpeningCircle(p.Name, p.Pad!.Position.X, p.Pad.Position.Y, p.Opening))
                : (parts ?? []).Where(p => p.Kind == FixturePartKind.PushRod && p.Opening > 0)
                    .Select(p => new OpeningCircle(p.Name, p.BoardX, p.BoardY, p.Opening));
            var (single, merged) = OpeningMerge.Merge(openings.ToList(), Plate.MinimumWall);
            var stroke = Invariant($"fill=\"none\" stroke=\"{PlateColor(openingPlate)}\" stroke-width=\"{font * 0.08:0.###}\"");
            foreach (var opening in single)
            {
                var c = Map(new Point2(opening.X, opening.Y));
                svg.Append(Invariant($"<circle cx=\"{c.X:0.###}\" cy=\"{c.Y:0.###}\" r=\"{opening.Diameter / 2:0.###}\" {stroke}/>"));
            }

            foreach (var opening in merged)
            {
                var points = string.Join(" ", OutlineShape.Flatten(opening.Contour)
                    .Select(p => Map(new Point2(p.X, p.Y))).Select(p => Invariant($"{p.X:0.###},{p.Y:0.###}")));
                svg.Append($"<polygon points=\"{points}\" {stroke}/>");
            }
        }

        // Numbers next to their marker, at the first of eight places around it that overlaps no other number or marker;
        // if all are taken, the same eight places further out. For the wiring with the pin of the test hardware (an
        // unwired probe with its number only). The width of a text is only estimated, so a label left of its marker is
        // anchored at its right end and one above or below at its middle: the gap to the marker is the same on all sides
        var taken = markers.Select(m => Box.Around(m.Center, m.Radius)).ToList();
        foreach (var (probe, c, r) in markers)
        {
            var label = withPins && probe.Pin is { } pin ? $"{probe.Name} - {pin.Pin}" : probe.Name;
            var textWidth = label.Length * font * 0.62;
            var textHeight = font * 0.8;

            // Each place: the anchor of the text, how the text hangs from it, and its baseline
            (double X, string Anchor, double Y)[] Candidates(double gap) =>
            [
                (c.X + gap, "start", c.Y + textHeight * 0.35),         // right
                (c.X - gap, "end", c.Y + textHeight * 0.35),           // left
                (c.X, "middle", c.Y - gap),                            // above
                (c.X, "middle", c.Y + gap + textHeight),               // below
                (c.X + gap * 0.75, "start", c.Y - gap * 0.75),         // top right
                (c.X - gap * 0.75, "end", c.Y - gap * 0.75),           // top left
                (c.X + gap * 0.75, "start", c.Y + gap * 0.75 + textHeight), // bottom right
                (c.X - gap * 0.75, "end", c.Y + gap * 0.75 + textHeight)    // bottom left
            ];

            Box BoxAt((double X, string Anchor, double Y) p)
            {
                var left = p.Anchor switch { "end" => p.X - textWidth, "middle" => p.X - textWidth / 2, _ => p.X };
                return new Box(left, p.Y - textHeight, left + textWidth, p.Y);
            }

            // Ring 0 right next to the marker, each further ring a line height further out; nothing free: right next to it
            var place = Candidates(r + font * 0.2)[0];
            for (var ring = 0; ring < 5; ring++)
            {
                var free = Candidates(r + font * (0.2 + 1.3 * ring)).Where(p => !taken.Any(t => t.Overlaps(BoxAt(p)))).ToList();
                if (free.Count == 0)
                    continue;

                place = free[0];
                break;
            }

            taken.Add(BoxAt(place));
            svg.Append(Invariant($"<text x=\"{place.X:0.###}\" y=\"{place.Y - textHeight * 0.1:0.###}\" text-anchor=\"{place.Anchor}\" font-family=\"Arial, Helvetica, sans-serif\" font-size=\"{font:0.###}\" font-weight=\"bold\" fill=\"{ProbeColor}\">{Escape(label)}</text>"));
        }

        svg.Append("</svg>");
        return svg.ToString();
    }

    private static string PartColor(FixturePartKind kind) => kind switch
    {
        FixturePartKind.LocatingPin => MarkerColors.LocatingPin,
        FixturePartKind.PreCenteringPin => MarkerColors.PreCenteringPin,
        FixturePartKind.SupportPin => MarkerColors.SupportPin,
        _ => MarkerColors.PushRod
    };

    /// <summary>The plate of a drawing with openings: the moving plate below the board, the top probe plate above it; null for none.</summary>
    private static FixturePlate? OpeningPlate(IEnumerable<FixturePlate> plates)
    {
        var list = plates.ToList();
        return list.Contains(FixturePlate.MovingPlate) ? FixturePlate.MovingPlate
            : list.Contains(FixturePlate.TopProbePlate) ? FixturePlate.TopProbePlate
            : null;
    }

    /// <summary>Plates with the same outline together, in the order they come.</summary>
    private static List<List<PlateOutlineDrawing>> GroupByOutline(IReadOnlyList<PlateOutlineDrawing> plates)
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
    /// The probes, parts and plates the drawing of this side shows, each with its symbol as a small SVG (12 × 12) and its
    /// name: only the kinds the drawing has.
    /// </summary>
    public static IReadOnlyList<(string Symbol, string Label)> Legend(IReadOnlyList<Probe> probes, BoardSide side,
        IReadOnlyList<FixturePart>? parts = null, IReadOnlyList<FixturePlate>? plates = null, bool withOpenings = false)
    {
        static string Symbol(string body) =>
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 12 12\" width=\"12\" height=\"12\">{body}</svg>";
        static string Disc(string color, double opacity = 0.85) =>
            Invariant($"<circle cx=\"6\" cy=\"6\" r=\"4.5\" fill=\"{color}\" fill-opacity=\"{opacity:0.##}\"/>");
        static string Ring(string color) =>
            $"<circle cx=\"6\" cy=\"6\" r=\"4.5\" fill=\"{color}\" fill-opacity=\"0.25\" stroke=\"{color}\" stroke-width=\"1.2\"/>";

        var items = new List<(string, string)>();
        var sideProbes = probes.Where(p => p.Side == side && p.Pad is not null).ToList();
        if (sideProbes.Count > 0)
            items.Add((Symbol(Disc(ProbeColor)), Localizer.Get("Board_LegendProbes")));

        var sideParts = (parts ?? []).Where(p => p.PlateSide == side).ToList();
        foreach (var (kind, key) in new[]
                 {
                     (FixturePartKind.LocatingPin, "Board_LegendLocatingPins"),
                     (FixturePartKind.PreCenteringPin, "Board_LegendPreCenteringPins"),
                     (FixturePartKind.SupportPin, "Board_LegendSupportPins"),
                     (FixturePartKind.PushRod, "Board_LegendPushRods")
                 })
        {
            if (sideParts.All(p => p.Kind != kind))
                continue;

            var color = PartColor(kind);
            var mark = kind switch
            {
                FixturePartKind.LocatingPin =>
                    $"<line x1=\"1.5\" y1=\"6\" x2=\"10.5\" y2=\"6\" stroke=\"{color}\" stroke-width=\"0.8\"/><line x1=\"6\" y1=\"1.5\" x2=\"6\" y2=\"10.5\" stroke=\"{color}\" stroke-width=\"0.8\"/>",
                FixturePartKind.PushRod =>
                    $"<line x1=\"2.8\" y1=\"2.8\" x2=\"9.2\" y2=\"9.2\" stroke=\"{color}\" stroke-width=\"0.8\"/><line x1=\"2.8\" y1=\"9.2\" x2=\"9.2\" y2=\"2.8\" stroke=\"{color}\" stroke-width=\"0.8\"/>",
                _ => string.Empty
            };
            items.Add((Symbol(Ring(color) + mark), Localizer.Get(key)));
        }

        foreach (var plate in plates ?? [])
            items.Add((Symbol($"<line x1=\"0.5\" y1=\"6\" x2=\"11.5\" y2=\"6\" stroke=\"{PlateColor(plate)}\" stroke-width=\"1.2\" stroke-dasharray=\"3 2\"/>"),
                Localizer.Format("Report_LegendPlateOutline", PlateRow.NameOf(plate))));

        // Openings only where the drawing has them
        if (withOpenings)
        {
            var plate = OpeningPlate(plates ?? []);
            var any = plate == FixturePlate.MovingPlate
                ? probes.Any(p => p.Side == side && p.HasOpening && p.Pad is not null && p.Opening > 0)
                : (parts ?? []).Any(p => p.Kind == FixturePartKind.PushRod && p.Opening > 0);
            if (plate is not null && any)
                items.Add((Symbol($"<circle cx=\"6\" cy=\"6\" r=\"4.5\" fill=\"none\" stroke=\"{PlateColor(plate.Value)}\" stroke-width=\"1\"/>"),
                    Localizer.Format("Board_LegendOpenings", PlateRow.NameOf(plate.Value))));
        }

        return items;
    }

    private readonly record struct Box(double Left, double Top, double Right, double Bottom)
    {
        public static Box Around((double X, double Y) c, double r) => new(c.X - r, c.Y - r, c.X + r, c.Y + r);

        public bool Overlaps(Box other) =>
            Left < other.Right && other.Left < Right && Top < other.Bottom && other.Top < Bottom;
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
    public static string Caption(BoardSide side, bool wiring, bool isBoardFlipped) => wiring
        ? Localizer.Format("Report_WiringOf", PlateRow.NameOf(FixturePlates.ProbePlate(side, isBoardFlipped)))
        : Localizer.Get(side == BoardSide.Top ? "Report_AssemblyTop" : "Report_AssemblyBottom");

    public static string ViewHint(BoardSide view) =>
        Localizer.Get(view == BoardSide.Top ? "Report_ViewFromTop" : "Report_ViewFromBottom");
}