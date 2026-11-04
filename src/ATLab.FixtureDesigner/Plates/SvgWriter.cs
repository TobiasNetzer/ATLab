using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ATLab.FixtureDesigner.Plates;

/// <summary>
/// Plate as SVG in millimeters, e.g. for laser cutters and Inkscape: like the DXF only what is machined. The plate
/// outline is one closed path with true arcs, the holes are circles; each kind is a group of its own (an Inkscape layer)
/// so it can get its own cutting settings. Lines are hairlines without fill, as cutting software expects. SVG's Y runs
/// down, so the plate is turned upside down into it and its lowest edge is at the bottom of the drawing.
/// </summary>
public static class SvgWriter
{
    public static string Write(Plate plate)
    {
        var (minX, maxX, minY, maxY) = (plate.MinX, plate.MaxX, plate.MinY, plate.MaxY);

        // Holes may reach beyond the outline only if the plate has problems; the drawing shows them anyway
        foreach (var hole in plate.Holes)
        {
            minX = Math.Min(minX, hole.X - hole.Diameter / 2);
            maxX = Math.Max(maxX, hole.X + hole.Diameter / 2);
            minY = Math.Min(minY, hole.Y - hole.Diameter / 2);
            maxY = Math.Max(maxY, hole.Y + hole.Diameter / 2);
        }

        var (width, height) = (maxX - minX, maxY - minY);
        string X(double x) => N(x - minX);
        string Y(double y) => N(maxY - y);

        var svg = new StringBuilder();
        svg.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
        svg.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:inkscape=\"http://www.inkscape.org/namespaces/inkscape\" " +
                   $"width=\"{N(width)}mm\" height=\"{N(height)}mm\" viewBox=\"0 0 {N(width)} {N(height)}\">\n");

        // A closed contour: M to the first point, then L for straight edges and A for arcs, closed with Z
        string Path(IReadOnlyList<OutlineVertex> contour)
        {
            var path = new StringBuilder();
            path.Append($"M {X(contour[0].X)} {Y(contour[0].Y)}");
            for (var i = 0; i < contour.Count; i++)
            {
                var a = contour[i];
                var b = contour[(i + 1) % contour.Count];
                if (Math.Abs(a.Bulge) < 1e-12)
                {
                    if (i + 1 < contour.Count)
                        path.Append($" L {X(b.X)} {Y(b.Y)}");
                    continue;
                }

                // Turned upside down, the angle of a counterclockwise arc of the plate decreases in SVG's coordinates
                // (Y down), which is SVG's sweep flag 0; a clockwise arc has flag 1
                var (_, _, radius, _, sweep) = OutlineShape.ArcGeometry(a, b);
                var large = Math.Abs(sweep) > Math.PI ? 1 : 0;
                var sweepFlag = sweep > 0 ? 0 : 1;
                path.Append($" A {N(radius)} {N(radius)} 0 {large} {sweepFlag} {X(b.X)} {Y(b.Y)}");
            }

            return path.Append(" Z").ToString();
        }

        Group(svg, DxfWriter.PlateLayer, "#000000", $"    <path d=\"{Path(plate.Contour)}\"/>\n");
        if (plate.Cutouts.Count > 0)
            Group(svg, DxfWriter.CutoutLayer, "#D97706", string.Concat(plate.Cutouts.Select(c => $"    <path d=\"{Path(c)}\"/>\n")));

        foreach (var (kind, color) in new[]
                 {
                     (PlateHoleKind.Probe, "#FF0000"),
                     (PlateHoleKind.ProbeOpening, "#FF7F7F"),
                     (PlateHoleKind.LocatingPin, "#00A000"),
                     (PlateHoleKind.Frame, "#0000FF"),
                     (PlateHoleKind.PreCenteringPin, "#C026D3"),
                     (PlateHoleKind.SupportPin, "#0D9488"),
                     (PlateHoleKind.PushRod, "#7C3AED"),
                     (PlateHoleKind.PushRodOpening, "#A78BFA")
                 })
        {
            var name = DxfWriter.LayerOf(kind);
            var circles = string.Concat(plate.Holes.Where(h => h.Kind == kind)
                .Select(h => $"    <circle cx=\"{X(h.X)}\" cy=\"{Y(h.Y)}\" r=\"{N(h.Diameter / 2)}\"/>\n"));
            if (kind == plate.OpeningKind)
                circles += string.Concat(plate.MergedOpenings.Select(m => $"    <path d=\"{Path(m.Contour)}\"/>\n"));
            if (circles.Length > 0)
                Group(svg, name, color, circles);
        }

        svg.Append("</svg>\n");
        return svg.ToString();
    }

    private static void Group(StringBuilder svg, string name, string color, string content) =>
        svg.Append($"  <g id=\"{name}\" inkscape:groupmode=\"layer\" inkscape:label=\"{name}\" fill=\"none\" stroke=\"{color}\" stroke-width=\"0.01\">\n")
            .Append(content)
            .Append("  </g>\n");

    private static string N(double value) => value.ToString("0.#####", CultureInfo.InvariantCulture);

    public static void Save(Plate plate, string path) => File.WriteAllText(path, Write(plate), new UTF8Encoding(false));
}