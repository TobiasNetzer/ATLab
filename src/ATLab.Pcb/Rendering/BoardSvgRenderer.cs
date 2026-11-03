using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ATLab.Pcb.Controls;
using ATLab.Pcb.Kicad;
using Avalonia.Media;

namespace ATLab.Pcb.Rendering;

/// <summary>
/// Drawing of a board for printed documents (SVG, millimeters): outline, pads in light grey and on top the highlighted
/// nets, pads, components and markers in their colors. The bottom side is mirrored, as seen from below. Zones of the
/// highlighted nets are left out unless asked for.
/// </summary>
public static class BoardSvgRenderer
{
    private const string OutlineColor = "#27272A";
    private const string PadColor = "#D4D4D8";
    private const string FarPadColor = "#EBEBED";

    public static string Render(Board board, BoardSide side, IReadOnlyList<NetHighlight> nets, IReadOnlyList<PadHighlight> pads,
        IReadOnlyList<FootprintHighlight> footprints, IReadOnlyList<IBoardMarker> markers, bool includeZones = false,
        IReadOnlyCollection<string>? layers = null, IReadOnlyDictionary<string, double>? layerOpacity = null,
        IReadOnlyList<string>? layerStack = null, IReadOnlyDictionary<string, Color>? layerColors = null)
    {
        // Without a choice: only the pads of the side drawn
        layers ??= [LayerColors.PadsOf(side)];
        var bounds = board.Bounds;
        var mirror = side == BoardSide.Bottom;
        var font = Math.Clamp(Math.Max(bounds.Width, bounds.Height) / 65, 1.5, 5);
        var margin = font * 2.5;
        var width = bounds.Width + 2 * margin;
        var height = bounds.Height + 2 * margin;

        (double X, double Y) Map(Point2 p) =>
            (mirror ? bounds.MaxX - p.X + margin : p.X - bounds.MinX + margin, p.Y - bounds.MinY + margin);

        string Points(IEnumerable<Point2> points) =>
            string.Join(" ", points.Select(Map).Select(p => Invariant($"{p.X:0.###},{p.Y:0.###}")));

        var svg = new StringBuilder();
        svg.Append(Invariant($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {width:0.###} {height:0.###}\" width=\"{width:0.###}mm\" height=\"{height:0.###}mm\">"));
        svg.Append("<rect width=\"100%\" height=\"100%\" fill=\"#FFFFFF\"/>");

        AppendBoard(svg, board, side, mirror, Map, bounds, margin, font,
            new BoardLayerStyle(layers, layerOpacity, layerStack, layerColors));

        // Highlighted nets: copper of the viewed side strong, of the far side faint; the first solid net on top
        var ordered = nets.Where(h => h.Style == NetHighlightStyle.Outline)
            .Concat(nets.Where(h => h.Style == NetHighlightStyle.Solid).Reverse());
        foreach (var highlight in ordered)
        {
            var color = Hex(highlight.Color);
            var dashed = highlight.Style == NetHighlightStyle.Outline;
            foreach (var graphic in board.GraphicsOfNet(highlight.Net).OrderBy(g => Board.SideOfLayer(g.Layer) == side ? 1 : 0))
            {
                var opacity = Board.SideOfLayer(graphic.Layer) == side || Board.SideOfLayer(graphic.Layer) is null ? 0.9 : 0.35;
                if (graphic.Filled)
                {
                    // Zones only on request and filled faintly (a ground pour would cover everything else), without
                    // outline: KiCad joins every hole of a filled zone to its edge with a zero-width cut, which an
                    // outline would draw as a line
                    if (!dashed && includeZones)
                        svg.Append(Invariant($"<polygon points=\"{Points(graphic.Points)}\" fill=\"{color}\" fill-opacity=\"{opacity * 0.28:0.##}\"/>"));
                    continue;
                }

                var stroke = dashed ? font * 0.08 : Math.Max(graphic.Width, font * 0.05);
                var dash = dashed ? Invariant($" stroke-dasharray=\"{font * 0.3:0.###} {font * 0.2:0.###}\"") : string.Empty;
                svg.Append(Invariant($"<polyline points=\"{Points(graphic.Points)}\" fill=\"none\" stroke=\"{color}\" stroke-opacity=\"{opacity:0.##}\" stroke-width=\"{stroke:0.###}\" stroke-linecap=\"round\" stroke-linejoin=\"round\"{dash}/>"));
            }

            foreach (var pad in board.Pads.Where(p => p.Net == highlight.Net))
                AppendPad(svg, pad, Map, mirror, dashed ? "none" : color, color);
        }

        foreach (var highlight in footprints)
        {
            var footprint = board.Footprints.FirstOrDefault(f => f.Reference == highlight.Reference);
            if (footprint is null)
                continue;

            var color = Hex(highlight.Color);
            foreach (var (a, b) in footprint.CourtyardEdges)
            {
                var pa = Map(a);
                var pb = Map(b);
                svg.Append(Invariant($"<line x1=\"{pa.X:0.###}\" y1=\"{pa.Y:0.###}\" x2=\"{pb.X:0.###}\" y2=\"{pb.Y:0.###}\" stroke=\"{color}\" stroke-width=\"{font * 0.12:0.###}\"/>"));
            }

            var anchor = footprint.HasCourtyard
                ? Map(new Point2(footprint.CourtyardBounds.MinX, footprint.CourtyardBounds.MinY))
                : Map(footprint.Position);
            AppendText(svg, footprint.Reference, anchor.X, anchor.Y - font * 0.3, font, color);
        }

        var labels = new List<(double X, double Y, string Text, string Color)>();
        foreach (var highlight in pads)
        {
            var color = Hex(highlight.Color);
            AppendPad(svg, highlight.Pad, Map, mirror, color, "#FFFFFF");
            if (!string.IsNullOrEmpty(highlight.Label))
            {
                var c = Map(highlight.Pad.Position);
                labels.Add((c.X + highlight.Pad.MinSize / 2 + font * 0.2, c.Y + font * 0.35, highlight.Label, color));
            }
        }

        foreach (var marker in markers)
        {
            if (marker.MarkerPosition is not { } position)
                continue;

            var color = marker.MarkerColor is { } markerColor ? Hex(markerColor) : "#1D4ED8";
            var c = Map(position);
            var r = Math.Clamp(marker.MarkerSize / 2, font * 0.35, font * 0.6);
            var fromViewedSide = marker.MarkerSide is null || marker.MarkerSide == side;
            svg.Append(fromViewedSide
                ? Invariant($"<circle cx=\"{c.X:0.###}\" cy=\"{c.Y:0.###}\" r=\"{r:0.###}\" fill=\"{color}\" stroke=\"#FFFFFF\" stroke-width=\"{font * 0.06:0.###}\"/>")
                : Invariant($"<circle cx=\"{c.X:0.###}\" cy=\"{c.Y:0.###}\" r=\"{r:0.###}\" fill=\"none\" stroke=\"{color}\" stroke-width=\"{font * 0.1:0.###}\" stroke-dasharray=\"{font * 0.25:0.###} {font * 0.15:0.###}\"/>"));
            labels.Add((c.X + r + font * 0.15, c.Y - r * 0.4, marker.MarkerLabel, color));
        }

        foreach (var (x, y, text, color) in labels)
            AppendText(svg, text, x, y, font, color);

        svg.Append("</svg>");
        return svg.ToString();
    }

    /// <summary>
    /// The chosen layers, pads and holes of a board from the bottom of the stack up, and the board outline over them,
    /// for a picture that draws its own marks on top (the build sheet). The side decides which pads are the near ones
    /// and the automatic order; mirror flips the picture left to right; map turns board into picture coordinates and
    /// must be the same mapping as bounds and margin give.
    /// </summary>
    public static void AppendBoard(StringBuilder svg, Board board, BoardSide side, bool mirror, Func<Point2, (double X, double Y)> map,
        Bounds2 bounds, double margin, double font, BoardLayerStyle style)
    {
        string ColorOf(string layer) =>
            Hex(style.Colors is not null && style.Colors.TryGetValue(layer, out var chosen) ? chosen : DefaultLayerColor(layer));
        double Opacity(string layer) =>
            Math.Clamp(style.Opacity is not null && style.Opacity.TryGetValue(layer, out var value) ? value : DefaultLayerOpacity(layer), 0, 1);
        string Points(IEnumerable<Point2> points) =>
            string.Join(" ", points.Select(map).Select(p => Invariant($"{p.X:0.###},{p.Y:0.###}")));

        // The chosen layers, pads and holes from the bottom of the stack up; the board outline over them
        var chosen = style.Layers.ToHashSet();
        var available = board.Layers.Concat([LayerColors.TopPads, LayerColors.BottomPads, LayerColors.Holes]).Where(chosen.Contains).ToList();
        foreach (var layer in DrawOrder(available, style.Stack, DefaultLayerStack(board, side, available)))
        {
            if (layer is LayerColors.TopPads or LayerColors.BottomPads)
            {
                // The pads with copper on that side; seen from the other side lighter (through-hole pads are on both)
                var padSide = layer == LayerColors.TopPads ? BoardSide.Top : BoardSide.Bottom;
                var padColor = ColorOf(layer);
                var color = padSide == side ? padColor
                    : style.Colors?.ContainsKey(layer) == true ? Blend(padColor, 0.5) : FarPadColor;
                svg.Append(Invariant($"<g opacity=\"{Opacity(layer):0.##}\">"));
                foreach (var pad in board.Pads.Where(p => p.HasCopperOn(padSide)))
                    AppendPad(svg, pad, map, mirror, color, null);
                svg.Append("</g>");
            }
            else if (layer == LayerColors.Holes)
            {
                svg.Append(Invariant($"<g opacity=\"{Opacity(layer):0.##}\">"));
                foreach (var hole in board.Holes)
                {
                    var c = map(hole.Position);
                    svg.Append(Invariant($"<circle cx=\"{c.X:0.###}\" cy=\"{c.Y:0.###}\" r=\"{hole.Diameter / 2:0.###}\" fill=\"#FFFFFF\" stroke=\"{ColorOf(layer)}\" stroke-width=\"{font * 0.03:0.###}\"/>"));
                }
                svg.Append("</g>");
            }
            else
            {
                // A layer of the far side is seen through the board: see AppendLayer
                var far = Board.SideOfLayer(layer) is { } layerSide && layerSide != side;
                AppendLayer(svg, board, mirror, layer, ColorOf(layer), Opacity(layer), bounds, margin, far);
            }
        }

        foreach (var polyline in board.Outline)
            svg.Append(Invariant($"<polyline points=\"{Points(polyline)}\" fill=\"none\" stroke=\"{OutlineColor}\" stroke-width=\"{font * 0.1:0.###}\"/>"));
    }

    /// <summary>
    /// Color of a layer without a choice of the user: black for the KiCad layers, so only the highlights are in color;
    /// light grey pads and dark grey holes.
    /// </summary>
    public static Color DefaultLayerColor(string layer) => layer switch
    {
        LayerColors.TopPads or LayerColors.BottomPads => Color.Parse(PadColor),
        LayerColors.Holes => Color.Parse(OutlineColor),
        _ => Colors.Black
    };

    /// <summary>A color mixed with white, for the pads of the far side.</summary>
    private static string Blend(string hex, double share)
    {
        var c = Color.Parse(hex);
        byte Mix(byte v) => (byte)Math.Round(v * share + 255 * (1 - share));
        return $"#{Mix(c.R):X2}{Mix(c.G):X2}{Mix(c.B):X2}";
    }

    /// <summary>
    /// Opacity of a layer without a choice of the user: copper and the areas (courtyard, margin) faint, as they cover
    /// much of the board; fab and silkscreen stronger, since they name the components.
    /// </summary>
    public static double DefaultLayerOpacity(string layer) => layer switch
    {
        LayerColors.TopPads or LayerColors.BottomPads or LayerColors.Holes => 1,
        _ when layer.EndsWith(".Cu", StringComparison.Ordinal) => 0.2,
        _ when layer.EndsWith(".CrtYd", StringComparison.Ordinal) || layer == "Margin" => 0.25,
        _ when layer.EndsWith(".Fab", StringComparison.Ordinal) => 0.5,
        _ when layer.EndsWith(".SilkS", StringComparison.Ordinal) => 0.8,
        _ => 0.6
    };

    /// <summary>
    /// The automatic order of layers, pads and holes, the topmost first: fab, silkscreen, holes, pads, the other
    /// layers (courtyard, user layers …) and the copper last; within each the viewed side over no side over the far
    /// side.
    /// </summary>
    public static IReadOnlyList<string> DefaultLayerStack(Board board, BoardSide side, IEnumerable<string> names)
    {
        var set = names.ToHashSet();
        static int Group(string layer) => layer switch
        {
            _ when layer.EndsWith(".Fab", StringComparison.Ordinal) => 0,
            _ when layer.EndsWith(".SilkS", StringComparison.Ordinal) => 1,
            LayerColors.Holes => 2,
            LayerColors.TopPads or LayerColors.BottomPads => 3,
            _ when layer.EndsWith(".Cu", StringComparison.Ordinal) => 5,
            _ => 4
        };
        static BoardSide? SideOf(string layer) => layer switch
        {
            LayerColors.TopPads => BoardSide.Top,
            LayerColors.BottomPads => BoardSide.Bottom,
            _ => Board.SideOfLayer(layer)
        };
        int Side(string layer) => SideOf(layer) is not { } s ? 1 : s == side ? 0 : 2;

        // Board.Layers keeps KiCad's order (front to back), the stable sort keeps it within a group and side
        return board.Layers.Concat([LayerColors.TopPads, LayerColors.BottomPads, LayerColors.Holes])
            .Where(set.Contains)
            .Select((layer, index) => (Layer: layer, Index: index))
            .OrderBy(l => Group(l.Layer)).ThenBy(l => Side(l.Layer)).ThenBy(l => l.Index)
            .Select(l => l.Layer)
            .ToList();
    }

    /// <summary>
    /// The drawing order, the bottom first: the user's stack (topmost first) where it names the layer, the automatic
    /// order for layers it does not know, which go to the bottom.
    /// </summary>
    private static IEnumerable<string> DrawOrder(IReadOnlyList<string> available, IReadOnlyList<string>? stack,
        IReadOnlyList<string> defaultStack)
    {
        if (stack is null || stack.Count == 0)
            return defaultStack.Reverse();

        var known = stack.Where(available.Contains).ToList();
        var rest = defaultStack.Where(l => !known.Contains(l));
        return known.Concat(rest).Reverse();
    }

    /// <summary>
    /// A chosen KiCad layer in black with its opacity, in board coordinates: its group moves (and for the bottom side
    /// mirrors) it onto the picture, so texts turn and mirror as on the board. Every layer is a group of its own, so
    /// lines of one layer that overlap do not get darker.
    /// </summary>
    /// <remarks>
    /// A layer of the far side (<paramref name="far"/>), e.g. the bottom courtyards in a view from the top, is drawn as
    /// seen through the board: lines dashed and at least 0.15 mm wide (KiCad's thin courtyard lines would vanish when
    /// dashed), areas as dashed outlines, and texts readable instead of mirrored.
    /// </remarks>
    private static void AppendLayer(StringBuilder svg, Board board, bool mirror, string layer, string color,
        double opacity, Bounds2 bounds, double margin, bool far = false)
    {
        const string dash = " stroke-dasharray=\"0.6 0.4\"";
        svg.Append(mirror
            ? Invariant($"<g opacity=\"{opacity:0.##}\" transform=\"translate({bounds.MaxX + margin:0.###} {margin - bounds.MinY:0.###}) scale(-1 1)\">")
            : Invariant($"<g opacity=\"{opacity:0.##}\" transform=\"translate({margin - bounds.MinX:0.###} {margin - bounds.MinY:0.###})\">"));

        foreach (var graphic in board.Graphics.Where(g => g.Layer == layer && g.Points.Count > 0))
        {
            var points = string.Join(" ", graphic.Points.Select(p => Invariant($"{p.X:0.###},{p.Y:0.###}")));
            if (graphic.Filled && far)
                svg.Append(Invariant($"<polygon points=\"{points}\" fill=\"none\" stroke=\"{color}\" stroke-width=\"0.15\"{dash}/>"));
            else if (graphic.Filled)
                svg.Append(Invariant($"<polygon points=\"{points}\" fill=\"{color}\"/>"));
            else
                svg.Append(Invariant($"<polyline points=\"{points}\" fill=\"none\" stroke=\"{color}\" stroke-width=\"{Math.Max(graphic.Width, far ? 0.15 : 0.05):0.###}\" stroke-linecap=\"round\" stroke-linejoin=\"round\"{(far ? dash : string.Empty)}/>"));
        }

        foreach (var text in board.Texts.Where(t => t.Layer == layer && t.Text.Length > 0))
        {
            // KiCad's text height is about the height of capitals; the anchor follows its justification. SVG does
            // not break lines, so every line of a multi-line text is a line of its own
            var size = Math.Max(text.Height * 1.4, 0.1);
            var lineHeight = size * 1.2;
            var lines = text.Text.Replace("\r", string.Empty).Split('\n');
            var anchor = text.HorizontalAlignment switch { -1 => "start", 1 => "end", _ => "middle" };
            var block = (lines.Length - 1) * lineHeight;
            var first = text.VerticalAlignment switch { -1 => text.Height, 1 => -block, _ => text.Height / 2 - block / 2 };
            // Seen through the board a text would read backwards: it is turned so it reads as on its own side
            var mirrored = (far ? mirror : text.Mirrored) ? " scale(-1 1)" : string.Empty;
            svg.Append(Invariant(
                $"<text text-anchor=\"{anchor}\" font-family=\"Arial, Helvetica, sans-serif\" font-size=\"{size:0.###}\" fill=\"{color}\" transform=\"translate({text.Position.X:0.###} {text.Position.Y:0.###}) rotate({-text.Angle:0.###}){mirrored}\">"));
            for (var i = 0; i < lines.Length; i++)
                svg.Append(Invariant($"<tspan x=\"0\" y=\"{first + i * lineHeight:0.###}\">{Escape(lines[i])}</tspan>"));
            svg.Append("</text>");
        }

        svg.Append("</g>");
    }

    /// <summary>
    /// The side to draw for a set of highlights: the bottom only when more highlighted copper is there than on the
    /// top.
    /// </summary>
    public static BoardSide BestSide(Board board, IReadOnlyList<NetHighlight> nets, IReadOnlyList<PadHighlight> pads)
    {
        var top = 0;
        var bottom = 0;
        foreach (var pad in nets.SelectMany(n => board.Pads.Where(p => p.Net == n.Net)).Concat(pads.Select(p => p.Pad)))
        {
            if (pad.HasTopCopper) top++;
            if (pad.HasBottomCopper) bottom++;
        }

        return bottom > top ? BoardSide.Bottom : BoardSide.Top;
    }

    private static void AppendPad(StringBuilder svg, Pad pad, Func<Point2, (double X, double Y)> map, bool mirror, string fill, string? stroke)
    {
        var c = map(pad.Position);
        var strokeAttribute = stroke is null ? string.Empty : Invariant($" stroke=\"{stroke}\" stroke-width=\"{Math.Max(pad.MinSize * 0.08, 0.05):0.###}\"");
        if (pad.Shape == PadShape.Circle && Math.Abs(pad.Width - pad.Height) < 1e-6)
        {
            svg.Append(Invariant($"<circle cx=\"{c.X:0.###}\" cy=\"{c.Y:0.###}\" r=\"{pad.Width / 2:0.###}\" fill=\"{fill}\"{strokeAttribute}/>"));
            return;
        }

        var radius = pad.Shape switch
        {
            PadShape.Circle or PadShape.Oval => pad.MinSize / 2,
            PadShape.RoundRect => pad.MinSize * 0.25,
            _ => 0
        };

        // KiCad turns counterclockwise on screen; SVG's rotate() turns clockwise, mirroring flips the direction again
        var angle = mirror ? pad.Rotation : -pad.Rotation;
        svg.Append(Invariant(
            $"<rect x=\"{-pad.Width / 2:0.###}\" y=\"{-pad.Height / 2:0.###}\" width=\"{pad.Width:0.###}\" height=\"{pad.Height:0.###}\" rx=\"{radius:0.###}\" fill=\"{fill}\"{strokeAttribute} transform=\"translate({c.X:0.###} {c.Y:0.###}) rotate({angle:0.###})\"/>"));
    }

    private static void AppendText(StringBuilder svg, string text, double x, double y, double size, string color) =>
        svg.Append(Invariant($"<text x=\"{x:0.###}\" y=\"{y:0.###}\" font-family=\"Arial, Helvetica, sans-serif\" font-size=\"{size:0.###}\" font-weight=\"bold\" fill=\"{color}\">{Escape(text)}</text>"));

    private static string Hex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    private static string Invariant(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);

    private static string Escape(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}