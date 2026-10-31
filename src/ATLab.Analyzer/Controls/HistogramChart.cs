using System;
using System.Collections.Generic;
using System.Linq;
using ATLab.Analyzer.Analysis;
using ATLab.Analyzer.Localization;
using Avalonia;
using Avalonia.Media;

namespace ATLab.Analyzer.Controls;

/// <summary>
/// Histogram of the measured values of a step, with the limits, the mean and the normal distribution of the same mean
/// and standard deviation.
/// </summary>
public sealed class HistogramChart : ChartBase
{
    public static readonly StyledProperty<IReadOnlyList<double>?> ValuesProperty =
        AvaloniaProperty.Register<HistogramChart, IReadOnlyList<double>?>(nameof(Values));

    /// <summary>How the values are divided into bins; the Freedman–Diaconis rule if null.</summary>
    public static readonly StyledProperty<BinSettings?> BinsProperty =
        AvaloniaProperty.Register<HistogramChart, BinSettings?>(nameof(Bins));

    public static readonly StyledProperty<bool> ShowNormalCurveProperty =
        AvaloniaProperty.Register<HistogramChart, bool>(nameof(ShowNormalCurve), true);

    private Histogram _histogram = new([], 0);
    private DescriptiveStatistics? _statistics;

    static HistogramChart()
    {
        AffectsRender<HistogramChart>(ValuesProperty, BinsProperty, ShowNormalCurveProperty);
    }

    public IReadOnlyList<double>? Values { get => GetValue(ValuesProperty); set => SetValue(ValuesProperty, value); }
    public BinSettings? Bins { get => GetValue(BinsProperty); set => SetValue(BinsProperty, value); }
    public bool ShowNormalCurve { get => GetValue(ShowNormalCurveProperty); set => SetValue(ShowNormalCurveProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValuesProperty || change.Property == BinsProperty)
        {
            var values = Values ?? [];
            _histogram = Histogram.Create(values.ToArray(), Bins);
            _statistics = DescriptiveStatistics.Compute(values.ToArray(), null, null);

            // Other values belong to another step; other bins keep the zoomed range
            if (change.Property == ValuesProperty)
                ResetZoom();
        }
    }

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        context.FillRectangle(Brushes.Transparent, bounds);

        if (_histogram.Bins.Count == 0 || _statistics is null)
        {
            DrawPlaceholder(context, Localizer.Get("Chart_NoValues"));
            return;
        }

        var bins = _histogram.Bins;
        var fullX = ValueRange(bins[0].Lower, bins[^1].Upper);
        var (xMin, xMax) = XView ?? fullX;

        // Normal curve scaled to counts: density × number of values × bin width
        var sd = _statistics.StdDev;
        var mean = _statistics.Mean;
        var curveScale = _statistics.Count * _histogram.BinWidth;
        var showCurve = ShowNormalCurve && sd is > 0;

        // The count axis fits the visible bins and the visible part of the curve
        var visibleMax = bins.Where(b => b.Upper > xMin && b.Lower < xMax).Select(b => b.Count).DefaultIfEmpty(0).Max();
        var curvePeak = 0d;
        if (showCurve)
        {
            var nearest = Math.Clamp(mean, xMin, xMax);
            var z = (nearest - mean) / sd!.Value;
            curvePeak = curveScale * Math.Exp(-0.5 * z * z) / (sd.Value * Math.Sqrt(2 * Math.PI));
        }

        var yMax = Math.Max(Math.Max(visibleMax, curvePeak) * 1.1, 1);
        var (yTicks, _) = NiceTicks(0, yMax, 5);
        // Counts are integers
        yTicks = yTicks.Where(t => Math.Abs(t - Math.Round(t)) < 1e-9).ToList();

        var leftMargin = AxisTitleWidth + yTicks.Select(t => Text(t.ToString("0"), MutedBrush).Width).DefaultIfEmpty(10).Max() + 10;
        var plot = new Rect(leftMargin, TopMargin, Math.Max(10, bounds.Width - leftMargin - RightMargin),
            Math.Max(10, bounds.Height - TopMargin - BottomMargin));

        double X(double v) => plot.Left + (v - xMin) / (xMax - xMin) * plot.Width;
        double Y(double count) => plot.Bottom - count / yMax * plot.Height;

        SetZoomFrame(plot, (xMin, xMax), (0, yMax), fullX, (0, yMax));

        // Grid and count axis
        var gridPen = new Pen(GridBrush, 1);
        foreach (var tick in yTicks)
        {
            var y = Math.Round(Y(tick)) + 0.5;
            context.DrawLine(gridPen, new Point(plot.Left, y), new Point(plot.Right, y));
            var label = Text(tick.ToString("0"), MutedBrush);
            context.DrawText(label, new Point(plot.Left - label.Width - 6, y - label.Height / 2));
        }

        DrawValueAxis(context, plot, xMin, xMax, X);

        // Bars, the hovered one highlighted
        var hovered = HoveredBin(plot, X);
        var barBrush = WithOpacity(AccentBrush, 0.55);
        var barPen = new Pen(AccentBrush, 1);
        using (context.PushClip(plot))
        {
            for (var i = 0; i < bins.Count; i++)
            {
                var bin = bins[i];
                if (bin.Count == 0 || bin.Upper < xMin || bin.Lower > xMax) continue;

                var rect = new Rect(new Point(X(bin.Lower), Y(bin.Count)), new Point(X(bin.Upper), plot.Bottom));
                context.DrawRectangle(i == hovered ? AccentBrush : barBrush, barPen, rect);
            }

            if (showCurve)
                DrawNormalCurve(context, plot, xMin, xMax, mean, sd!.Value, curveScale, X, Y);
        }

        DrawVerticalMarker(context, plot, X(mean), Foreground, Localizer.Get("Chart_Mean"));
        if (LowerLimit is { } lsl) DrawVerticalMarker(context, plot, X(lsl), FailBrush, Localizer.Get("Chart_LSL"));
        if (UpperLimit is { } usl) DrawVerticalMarker(context, plot, X(usl), FailBrush, Localizer.Get("Chart_USL"));

        context.DrawLine(new Pen(MutedBrush, 1), new Point(plot.Left, plot.Bottom), new Point(plot.Right, plot.Bottom));
        DrawSelection(context);
        DrawAxisTitles(context, plot, Localizer.Get("Axis_Value"), Localizer.Get("Axis_Count"));

        if (hovered >= 0)
        {
            var bin = bins[hovered];
            var share = (double)bin.Count / _statistics.Count;
            DrawInfo(context, Localizer.Format("Chart_BinInfo",
                ValueFormatter.Format(bin.Lower, Unit), ValueFormatter.Format(bin.Upper, Unit), bin.Count, share.ToString("P1")), plot);
        }
        else
        {
            DrawInfo(context, Localizer.Format("Chart_HistogramInfo", _statistics.Count, bins.Count,
                ValueFormatter.Format(_histogram.BinWidth, Unit)), plot);
        }
    }

    private void DrawValueAxis(DrawingContext context, Rect plot, double min, double max, Func<double, double> x)
    {
        var target = Math.Max(2, (int)(plot.Width / 90));
        var (ticks, step) = NiceTicks(min, max, target);

        foreach (var tick in ticks)
        {
            var px = x(tick);
            context.DrawLine(new Pen(MutedBrush, 1), new Point(px, plot.Bottom), new Point(px, plot.Bottom + 4));
            var label = Text(FormatValue(tick, step), MutedBrush);
            context.DrawText(label, new Point(px - label.Width / 2, plot.Bottom + 5));
        }
    }

    private void DrawNormalCurve(DrawingContext context, Rect plot, double xMin, double xMax, double mean, double sd,
        double scale, Func<double, double> x, Func<double, double> y)
    {
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            const int segments = 160;
            for (var i = 0; i <= segments; i++)
            {
                var v = xMin + (xMax - xMin) * i / segments;
                var z = (v - mean) / sd;
                var density = Math.Exp(-0.5 * z * z) / (sd * Math.Sqrt(2 * Math.PI));
                var point = new Point(x(v), Math.Max(plot.Top, y(density * scale)));

                if (i == 0) g.BeginFigure(point, false);
                else g.LineTo(point);
            }

            g.EndFigure(false);
        }

        context.DrawGeometry(null, new Pen(Foreground, 1.5), geometry);
    }

    private int HoveredBin(Rect plot, Func<double, double> x)
    {
        if (PointerPosition is not { } p || !plot.Contains(p))
            return -1;

        var bins = _histogram.Bins;
        for (var i = 0; i < bins.Count; i++)
        {
            if (p.X >= x(bins[i].Lower) && p.X <= x(bins[i].Upper))
                return i;
        }

        return -1;
    }
}