using System;
using System.Collections.Generic;
using System.Linq;
using ATLab.Analyzer.Analysis;
using ATLab.Analyzer.Localization;
using Avalonia;
using Avalonia.Media;

namespace ATLab.Analyzer.Controls;

public sealed record TrendPoint(double Value, bool Passed, string Label);

/// <summary>
/// Run chart of the measured values in the order the runs were executed, with the limits, the mean and the ±3σ band.
/// Zoomable on both axes: wheel for the runs, Ctrl + wheel for the values, a dragged rectangle for both.
/// </summary>
public sealed class TrendChart : ChartBase
{
    public static readonly StyledProperty<IReadOnlyList<TrendPoint>?> PointsProperty =
        AvaloniaProperty.Register<TrendChart, IReadOnlyList<TrendPoint>?>(nameof(Points));

    public static readonly StyledProperty<bool> ShowSigmaBandProperty =
        AvaloniaProperty.Register<TrendChart, bool>(nameof(ShowSigmaBand), true);

    static TrendChart()
    {
        AffectsRender<TrendChart>(PointsProperty, ShowSigmaBandProperty);
    }

    public IReadOnlyList<TrendPoint>? Points { get => GetValue(PointsProperty); set => SetValue(PointsProperty, value); }
    public bool ShowSigmaBand { get => GetValue(ShowSigmaBandProperty); set => SetValue(ShowSigmaBandProperty, value); }

    protected override bool CanZoomY => true;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == PointsProperty)
            ResetZoom();
    }

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        context.FillRectangle(Brushes.Transparent, bounds);

        var points = Points;
        if (points is not { Count: > 0 })
        {
            DrawPlaceholder(context, Localizer.Get("Chart_NoValues"));
            return;
        }

        var n = points.Count;
        var values = points.Select(p => p.Value).ToArray();
        var statistics = DescriptiveStatistics.Compute(values, null, null)!;
        var sd = statistics.StdDev;
        var showSigma = ShowSigmaBand && sd is > 0;

        // Run axis in point indices; a little space on both sides so the outer points are not cut
        var pad = Math.Max(0.5, (n - 1) * 0.01);
        var fullX = n == 1 ? (-1d, 1d) : (-pad, n - 1 + pad);
        var (xMin, xMax) = XView ?? fullX;

        var first = Math.Max(0, (int)Math.Ceiling(xMin));
        var last = Math.Min(n - 1, (int)Math.Floor(xMax));
        var visible = first <= last ? values[first..(last + 1)] : values;

        // The value axis fits the visible points (plus band and limits) unless it is zoomed itself
        var min = visible.Min();
        var max = visible.Max();
        if (showSigma)
        {
            min = Math.Min(min, statistics.Mean - 3 * sd!.Value);
            max = Math.Max(max, statistics.Mean + 3 * sd.Value);
        }

        var fullY = ValueRange(min, max);
        var (yMin, yMax) = YView ?? fullY;
        var (yTicks, yStep) = NiceTicks(yMin, yMax, Math.Max(2, (int)(Bounds.Height / 50)));

        var leftMargin = AxisTitleWidth + yTicks.Select(t => Text(FormatValue(t, yStep), MutedBrush).Width).DefaultIfEmpty(20).Max() + 10;
        var plot = new Rect(leftMargin, TopMargin, Math.Max(10, bounds.Width - leftMargin - RightMargin),
            Math.Max(10, bounds.Height - TopMargin - BottomMargin));

        double X(double index) => plot.Left + (index - xMin) / (xMax - xMin) * plot.Width;
        double Y(double v) => plot.Bottom - (v - yMin) / (yMax - yMin) * plot.Height;

        SetZoomFrame(plot, (xMin, xMax), (yMin, yMax), fullX, fullY);

        var gridPen = new Pen(GridBrush, 1);
        foreach (var tick in yTicks)
        {
            var y = Math.Round(Y(tick)) + 0.5;
            context.DrawLine(gridPen, new Point(plot.Left, y), new Point(plot.Right, y));
            var label = Text(FormatValue(tick, yStep), MutedBrush);
            context.DrawText(label, new Point(plot.Left - label.Width - 6, y - label.Height / 2));
        }

        DrawRunAxis(context, plot, n, xMin, xMax, X);

        var visibleCount = Math.Max(1, last - first + 1);
        var radius = visibleCount > 1000 ? 1.5 : visibleCount > 200 ? 2.2 : 3;

        using (context.PushClip(plot))
        {
            if (showSigma)
            {
                var band = new Rect(new Point(plot.Left, Y(statistics.Mean + 3 * sd!.Value)),
                    new Point(plot.Right, Y(statistics.Mean - 3 * sd.Value)));
                context.FillRectangle(WithOpacity(AccentBrush, 0.08), band);
            }

            // Connecting line including one point beyond each edge, so it runs out of the zoomed area
            var from = Math.Max(0, first - 1);
            var to = Math.Min(n - 1, last + 1);
            if (to > from)
            {
                var geometry = new StreamGeometry();
                using (var g = geometry.Open())
                {
                    g.BeginFigure(new Point(X(from), Y(values[from])), false);
                    for (var i = from + 1; i <= to; i++)
                        g.LineTo(new Point(X(i), Y(values[i])));
                    g.EndFigure(false);
                }

                context.DrawGeometry(null, new Pen(WithOpacity(AccentBrush, 0.5), 1), geometry);
            }

            for (var i = first; i <= last; i++)
            {
                var brush = points[i].Passed ? AccentBrush : FailBrush;
                context.DrawEllipse(brush, null, new Point(X(i), Y(values[i])), radius, radius);
            }
        }

        if (showSigma)
        {
            DrawHorizontalMarker(context, plot, Y(statistics.Mean + 3 * sd!.Value), MutedBrush, "+3σ");
            DrawHorizontalMarker(context, plot, Y(statistics.Mean - 3 * sd.Value), MutedBrush, "−3σ");
        }

        DrawHorizontalMarker(context, plot, Y(statistics.Mean), Foreground, Localizer.Get("Chart_Mean"));
        if (LowerLimit is { } lsl) DrawHorizontalMarker(context, plot, Y(lsl), FailBrush, Localizer.Get("Chart_LSL"));
        if (UpperLimit is { } usl) DrawHorizontalMarker(context, plot, Y(usl), FailBrush, Localizer.Get("Chart_USL"));

        context.DrawLine(new Pen(MutedBrush, 1), new Point(plot.Left, plot.Bottom), new Point(plot.Right, plot.Bottom));
        DrawSelection(context);
        DrawAxisTitles(context, plot, Localizer.Get("Axis_Run"), Localizer.Get("Axis_Value"));

        var hovered = HoveredPoint(plot, xMin, xMax, first, last);
        if (hovered >= 0)
        {
            var center = new Point(X(hovered), Y(values[hovered]));
            context.DrawLine(new Pen(MutedBrush, 1), new Point(center.X, plot.Top), new Point(center.X, plot.Bottom));
            if (plot.Contains(center))
                context.DrawEllipse(null, new Pen(Foreground, 2), center, radius + 3, radius + 3);
            DrawInfo(context, $"#{hovered + 1}  {points[hovered].Label}  {ValueFormatter.Format(values[hovered], Unit)}", plot);
        }
        else
        {
            var failed = points.Count(p => !p.Passed);
            DrawInfo(context, failed > 0
                ? Localizer.Format("Chart_TrendInfoFailed", n, failed)
                : Localizer.Format("Chart_TrendInfo", n), plot);
        }
    }

    /// <summary>Run numbers (1-based) at round steps within the visible range.</summary>
    private void DrawRunAxis(DrawingContext context, Rect plot, int n, double xMin, double xMax, Func<double, double> x)
    {
        var from = Math.Max(1, xMin + 1);
        var to = Math.Min(n, xMax + 1);
        if (to <= from)
            to = from + 1;

        var (ticks, _) = NiceTicks(from, to, Math.Max(2, (int)(plot.Width / 70)));

        foreach (var tick in ticks.Where(t => t >= 1 && t <= n && Math.Abs(t - Math.Round(t)) < 1e-9))
        {
            var px = x(tick - 1);
            context.DrawLine(new Pen(MutedBrush, 1), new Point(px, plot.Bottom), new Point(px, plot.Bottom + 4));
            var label = Text(tick.ToString("0"), MutedBrush);
            context.DrawText(label, new Point(px - label.Width / 2, plot.Bottom + 5));
        }
    }

    /// <summary>The visible point nearest to the pointer on the run axis, -1 if none.</summary>
    private int HoveredPoint(Rect plot, double xMin, double xMax, int first, int last)
    {
        if (first > last || PointerPosition is not { } p || p.X < plot.Left - 6 || p.X > plot.Right + 6 ||
            p.Y < plot.Top || p.Y > plot.Bottom)
            return -1;

        var index = (int)Math.Round(xMin + (p.X - plot.Left) / plot.Width * (xMax - xMin));
        return Math.Clamp(index, first, last);
    }
}