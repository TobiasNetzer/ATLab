using Avalonia;
using Avalonia.Media;

namespace ATLab.FixtureDesigner.Controls;

/// <summary>
/// Sketch of a probe type, not to scale: two probes in their receptacles, through the moving plate up to the pads of
/// the board, beside a component.
/// </summary>
public sealed class ProbeTypeDrawing : DimensionSketch
{
    protected override void Draw(DrawingContext context)
    {
        const double x1 = 90, x2 = 165, left = 20, right = 280;
        const double boardTop = 60, boardBottom = 70, tipY = 73;
        const double movingTop = 125, movingBottom = 143, plateTop = 205, plateBottom = 227;
        const double receptacleTop = 185, receptacleEnd = plateBottom + 16;
        const double opening = 14, drill = 9, barrel = 6, plunger = 3, pad = 15;
        const double courtyard = 224;

        var linePen = new Pen(Ink, 1.3);
        var muted = Brush("ChartMuted", Brushes.Gray);
        var dimensionPen = new Pen(muted);
        var extensionPen = new Pen(muted, 1, new DashStyle([3, 3], 0));
        var plateBrush = Brush("ChartMuted", Brushes.Gray, 0.25);
        var partBrush = Brush("ChartMuted", Brushes.Gray, 0.6);
        var probeBrush = Brush("InfoColor", Brushes.SteelBlue, 0.45);
        var padBrush = new SolidColorBrush(Color.FromRgb(0xC9, 0xA2, 0x27));
        var tailPen = new Pen(Ink, 1.3, new DashStyle([3, 3], 0));

        static Rect Box(double x0, double y0, double x1, double y1) => new(x0, y0, x1 - x0, y1 - y0);
        void Line(IPen pen, double ax, double ay, double bx, double by) => context.DrawLine(pen, new Point(ax, ay), new Point(bx, by));

        // ----- Board, pads and a component with its courtyard -----
        context.DrawRectangle(BoardBrush, null, Box(left, boardTop, right, boardBottom));
        foreach (var x in new[] { x1, x2 })
            context.DrawRectangle(padBrush, null, Box(x - pad, boardBottom, x + pad, tipY));
        context.DrawRectangle(partBrush, linePen, Box(232, boardBottom, 266, 94));
        context.DrawRectangle(null, extensionPen, Box(courtyard, boardBottom, 274, 102));

        // ----- The plates: the moving plate with an opening for each probe -----
        foreach (var (from, to) in new[] { (left, x1 - opening), (x1 + opening, x2 - opening), (x2 + opening, right) })
            context.DrawRectangle(plateBrush, linePen, Box(from, movingTop, to, movingBottom));
        foreach (var (from, to) in new[] { (left, x1 - drill), (x1 + drill, x2 - drill), (x2 + drill, right) })
            context.DrawRectangle(plateBrush, linePen, Box(from, plateTop, to, plateBottom));

        // ----- The probes in their receptacles -----
        foreach (var x in new[] { x1, x2 })
        {
            context.DrawRectangle(partBrush, null, Box(x - drill, receptacleTop, x + drill, plateBottom));
            context.DrawGeometry(null, linePen, Polygon(
            [
                new(x - drill, plateBottom), new(x - drill, receptacleTop), new(x + drill, receptacleTop), new(x + drill, plateBottom)
            ], false));

            // Below the plate the receptacle goes on for a length the type does not define: dashed and left open
            Line(tailPen, x - drill, plateBottom, x - drill, receptacleEnd);
            Line(tailPen, x + drill, plateBottom, x + drill, receptacleEnd);

            context.DrawGeometry(probeBrush, linePen, Polygon(
            [
                new(x + barrel, receptacleTop), new(x + barrel, 100), new(x + plunger, 100), new(x + plunger, 82), new(x, tipY),
                new(x - plunger, 82), new(x - plunger, 100), new(x - barrel, 100), new(x - barrel, receptacleTop)
            ], true));
        }

        // ----- Dimensions -----
        void Letter(double x, double y, string letter) => DrawLetter(context, new Point(x, y), letter);

        void Across(double y, double fromY, double x, double half, string letter)
        {
            Line(extensionPen, x - half, fromY, x - half, y);
            Line(extensionPen, x + half, fromY, x + half, y);
            Line(dimensionPen, x - half, y, x + half, y);
            Letter(x + half + 12, y, letter);
        }

        // Probe Ø on the barrel, between the plates
        Line(dimensionPen, x1 - barrel, 170, x1 + barrel, 170);
        Letter(x1 - barrel - 12, 170, "A");

        // Drill Ø across the plate, so that the open end of the receptacle below stays open
        Line(new Pen(Ink), x1 - drill, (plateTop + plateBottom) / 2, x1 + drill, (plateTop + plateBottom) / 2);
        Letter(x1 - drill - 12, (plateTop + plateBottom) / 2, "B");
        Across(movingBottom + 12, movingBottom, x2, opening, "C");

        // Height, from the plate to the tip
        Line(extensionPen, x1 - pad, tipY, 48, tipY);
        Line(extensionPen, x1 - drill, plateTop, 48, plateTop);
        Line(dimensionPen, 48, tipY, 48, plateTop);
        Letter(48, 170, "D");

        Across(boardTop - 14, boardBottom, x1, pad, "E");

        // Spacing, center to center
        Line(extensionPen, x1, boardTop, x1, 24);
        Line(extensionPen, x2, boardTop, x2, 24);
        Line(dimensionPen, x1, 24, x2, 24);
        Letter((x1 + x2) / 2, 24, "F");

        // Clearance, from the center of the probe to the courtyard
        Line(extensionPen, courtyard, 102, courtyard, 112);
        Line(dimensionPen, x2, 112, courtyard, 112);
        Letter((x2 + courtyard) / 2 + 6, 112, "G");
    }
}
