using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ATLab.Analyzer.Analysis;
using ATLab.Analyzer.Localization;
using Avalonia;
using Avalonia.Media;

namespace ATLab.Analyzer.Controls;

/// <summary>
/// Passed and failed runs per day (or week) as stacked bars, with the yield as a line on a second axis on the right.
/// </summary>
public sealed class YieldChart : ChartBase
{
    public static readonly StyledProperty<IReadOnlyList<YieldBucket>?> BucketsProperty =
        AvaloniaProperty.Register<YieldChart, IReadOnlyList<YieldBucket>?>(nameof(Buckets));

    private const double YieldAxisWidth = 44 + AxisTitleWidth;

    static YieldChart()
    {
        AffectsRender<YieldChart>(BucketsProperty);
    }

    public IReadOnlyList<YieldBucket>? Buckets { get => GetValue(BucketsProperty); set => SetValue(BucketsProperty, value); }

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        context.FillRectangle(Brushes.Transparent, bounds);

        var buckets = Buckets;
        if (buckets is not { Count: > 0 })
        {
            DrawPlaceholder(context, Localizer.Get("Chart_NoRuns"));
            return;
        }

        var n = buckets.Count;
        var countMax = buckets.Max(b => b.Runs) * 1.1;
        var (countTicks, _) = NiceTicks(0, countMax, Math.Max(2, (int)(Bounds.Height / 50)));
        countTicks = countTicks.Where(t => Math.Abs(t - Math.Round(t)) < 1e-9).ToList();

        // The yield axis starts at a round value below the worst bucket, so differences near 100 % stay visible
        var yieldMin = Math.Max(0, Math.Floor((buckets.Min(b => b.Yield) - 0.02) * 10) / 10);
        var (yieldTicks, _) = NiceTicks(yieldMin, 1, 4);

        var leftMargin = AxisTitleWidth + countTicks.Select(t => Text(t.ToString("0"), MutedBrush).Width).DefaultIfEmpty(10).Max() + 10;
        var plot = new Rect(leftMargin, TopMargin,
            Math.Max(10, bounds.Width - leftMargin - YieldAxisWidth),
            Math.Max(10, bounds.Height - TopMargin - BottomMargin));

        var slot = plot.Width / n;
        double X(int i) => plot.Left + (i + 0.5) * slot;
        double YCount(double c) => plot.Bottom - c / countMax * plot.Height;
        double YYield(double y) => plot.Bottom - (y - yieldMin) / (1 - yieldMin) * plot.Height;

        // Grid and both axes
        var gridPen = new Pen(GridBrush, 1);
        foreach (var tick in countTicks)
        {
            var y = Math.Round(YCount(tick)) + 0.5;
            context.DrawLine(gridPen, new Point(plot.Left, y), new Point(plot.Right, y));
            var label = Text(tick.ToString("0"), MutedBrush);
            context.DrawText(label, new Point(plot.Left - label.Width - 6, y - label.Height / 2));
        }

        foreach (var tick in yieldTicks)
        {
            var label = Text(tick.ToString("P0", CultureInfo.CurrentCulture), AccentBrush);
            context.DrawText(label, new Point(plot.Right + 6, YYield(tick) - label.Height / 2));
        }

        DrawBucketAxis(context, plot, buckets, X);

        // Stacked bars: passed at the bottom, failed on top
        var hovered = HoveredBucket(plot, n);
        var barWidth = Math.Clamp(slot * 0.7, 1, 40);
        var passBrush = WithOpacity(PassBrush, 0.55);
        var failBrush = WithOpacity(FailBrush, 0.8);

        for (var i = 0; i < n; i++)
        {
            var b = buckets[i];
            var left = X(i) - barWidth / 2;

            if (i == hovered)
                context.FillRectangle(WithOpacity(GridBrush, 1), new Rect(plot.Left + i * slot, plot.Top, slot, plot.Height));

            context.FillRectangle(passBrush, new Rect(new Point(left, YCount(b.Passed)), new Point(left + barWidth, plot.Bottom)));
            if (b.Failed > 0)
                context.FillRectangle(failBrush, new Rect(new Point(left, YCount(b.Runs)), new Point(left + barWidth, YCount(b.Passed))));
        }

        // Yield line with points
        if (n > 1)
        {
            var geometry = new StreamGeometry();
            using (var g = geometry.Open())
            {
                g.BeginFigure(new Point(X(0), YYield(buckets[0].Yield)), false);
                for (var i = 1; i < n; i++)
                    g.LineTo(new Point(X(i), YYield(buckets[i].Yield)));
                g.EndFigure(false);
            }

            context.DrawGeometry(null, new Pen(AccentBrush, 2), geometry);
        }

        var radius = n > 60 ? 2 : 3;
        for (var i = 0; i < n; i++)
            context.DrawEllipse(AccentBrush, null, new Point(X(i), YYield(buckets[i].Yield)), radius, radius);

        context.DrawLine(new Pen(MutedBrush, 1), new Point(plot.Left, plot.Bottom), new Point(plot.Right, plot.Bottom));

        DrawLegend(context, plot);
        DrawAxisTitles(context, plot, Localizer.Get(buckets[0].IsWeek ? "Axis_Week" : "Axis_Day"),
            Localizer.Get("Axis_Runs"), Localizer.Get("Chart_Yield"));

        if (hovered >= 0)
        {
            var b = buckets[hovered];
            DrawInfo(context, Localizer.Format("Chart_YieldInfo", LongLabel(b), b.Runs, b.Failed, b.Yield.ToString("P1")), plot);
        }
        else
        {
            var runs = buckets.Sum(b => b.Runs);
            var passed = buckets.Sum(b => b.Passed);
            DrawInfo(context, Localizer.Format("Chart_YieldTotal", runs, ((double)passed / runs).ToString("P1")), plot);
        }
    }

    private void DrawBucketAxis(DrawingContext context, Rect plot, IReadOnlyList<YieldBucket> buckets, Func<int, double> x)
    {
        // Show as many labels as fit without overlapping
        var sample = Text(ShortLabel(buckets[0]), MutedBrush).Width + 12;
        var every = Math.Max(1, (int)Math.Ceiling(sample / (plot.Width / buckets.Count)));

        for (var i = 0; i < buckets.Count; i += every)
        {
            var label = Text(ShortLabel(buckets[i]), MutedBrush);
            var left = Math.Clamp(x(i) - label.Width / 2, plot.Left - 4, plot.Right - label.Width + 4);
            context.DrawText(label, new Point(left, plot.Bottom + 5));
        }
    }

    private void DrawLegend(DrawingContext context, Rect plot)
    {
        var items = new (IBrush Brush, string Text, bool IsLine)[]
        {
            (WithOpacity(PassBrush, 0.55), Localizer.Enum(ViewModels.ResultFilter.Passed), false),
            (WithOpacity(FailBrush, 0.8), Localizer.Enum(ViewModels.ResultFilter.Failed), false),
            (AccentBrush, Localizer.Get("Chart_Yield"), true)
        };

        var right = plot.Right + YieldAxisWidth;
        foreach (var (brush, text, isLine) in items.Reverse())
        {
            var label = Text(text, MutedBrush);
            right -= label.Width;
            context.DrawText(label, new Point(right, 2));
            right -= 14;
            var center = new Point(right + 5, 2 + label.Height / 2);
            if (isLine)
                context.DrawLine(new Pen(brush, 2), new Point(center.X - 5, center.Y), new Point(center.X + 5, center.Y));
            else
                context.FillRectangle(brush, new Rect(center.X - 5, center.Y - 5, 10, 10));
            right -= 12;
        }
    }

    private static string ShortLabel(YieldBucket b) =>
        b.IsWeek ? Localizer.Format("Chart_Week", ISOWeek.GetWeekOfYear(b.Start.ToDateTime(TimeOnly.MinValue))) : b.Start.ToString("MM-dd");

    private static string LongLabel(YieldBucket b) =>
        b.IsWeek ? $"{ShortLabel(b)} ({b.Start:yyyy-MM-dd})" : b.Start.ToString("yyyy-MM-dd");

    private int HoveredBucket(Rect plot, int n)
    {
        if (PointerPosition is not { } p || !plot.Contains(p))
            return -1;

        return Math.Clamp((int)((p.X - plot.Left) / (plot.Width / n)), 0, n - 1);
    }
}