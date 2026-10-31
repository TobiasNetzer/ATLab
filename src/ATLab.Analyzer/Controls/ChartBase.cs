using System;
using System.Collections.Generic;
using System.Globalization;
using ATLab.Analyzer.Analysis;
using ATLab.Analyzer.Localization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace ATLab.Analyzer.Controls;

/// <summary>
/// Common parts of the charts: theme brushes, axis ticks and text. The brushes are set from the theme in
/// App.axaml, so the charts follow light and dark mode.
/// </summary>
public abstract class ChartBase : Control
{
    public static readonly StyledProperty<IBrush> ForegroundProperty =
        AvaloniaProperty.Register<ChartBase, IBrush>(nameof(Foreground), Brushes.Gray);

    public static readonly StyledProperty<IBrush> MutedBrushProperty =
        AvaloniaProperty.Register<ChartBase, IBrush>(nameof(MutedBrush), Brushes.DimGray);

    public static readonly StyledProperty<IBrush> GridBrushProperty =
        AvaloniaProperty.Register<ChartBase, IBrush>(nameof(GridBrush), new SolidColorBrush(Color.FromArgb(40, 128, 128, 128)));

    public static readonly StyledProperty<IBrush> AccentBrushProperty =
        AvaloniaProperty.Register<ChartBase, IBrush>(nameof(AccentBrush), Brushes.SteelBlue);

    public static readonly StyledProperty<IBrush> PassBrushProperty =
        AvaloniaProperty.Register<ChartBase, IBrush>(nameof(PassBrush), new SolidColorBrush(Color.Parse("#00C853")));

    public static readonly StyledProperty<IBrush> FailBrushProperty =
        AvaloniaProperty.Register<ChartBase, IBrush>(nameof(FailBrush), new SolidColorBrush(Color.Parse("#FF1744")));

    public static readonly StyledProperty<string?> UnitProperty =
        AvaloniaProperty.Register<ChartBase, string?>(nameof(Unit));

    public static readonly StyledProperty<double?> LowerLimitProperty =
        AvaloniaProperty.Register<ChartBase, double?>(nameof(LowerLimit));

    public static readonly StyledProperty<double?> UpperLimitProperty =
        AvaloniaProperty.Register<ChartBase, double?>(nameof(UpperLimit));

    /// <summary>Widens the value axis so the limits are always visible, even when they are far from the values.</summary>
    public static readonly StyledProperty<bool> IncludeLimitsInRangeProperty =
        AvaloniaProperty.Register<ChartBase, bool>(nameof(IncludeLimitsInRange), true);

    protected const double FontSize = 11;
    protected const double TopMargin = 40;
    protected const double RightMargin = 14;
    protected const double BottomMargin = 40;

    /// <summary>Space for a rotated axis title next to the tick labels.</summary>
    protected const double AxisTitleWidth = 18;

    private static readonly Typeface Typeface = new(FontFamily.Default);

    static ChartBase()
    {
        AffectsRender<ChartBase>(
            ForegroundProperty, MutedBrushProperty, GridBrushProperty, AccentBrushProperty, PassBrushProperty,
            FailBrushProperty, UnitProperty, LowerLimitProperty, UpperLimitProperty, IncludeLimitsInRangeProperty);
    }

    public IBrush Foreground { get => GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }
    public IBrush MutedBrush { get => GetValue(MutedBrushProperty); set => SetValue(MutedBrushProperty, value); }
    public IBrush GridBrush { get => GetValue(GridBrushProperty); set => SetValue(GridBrushProperty, value); }
    public IBrush AccentBrush { get => GetValue(AccentBrushProperty); set => SetValue(AccentBrushProperty, value); }
    public IBrush PassBrush { get => GetValue(PassBrushProperty); set => SetValue(PassBrushProperty, value); }
    public IBrush FailBrush { get => GetValue(FailBrushProperty); set => SetValue(FailBrushProperty, value); }
    public string? Unit { get => GetValue(UnitProperty); set => SetValue(UnitProperty, value); }
    public double? LowerLimit { get => GetValue(LowerLimitProperty); set => SetValue(LowerLimitProperty, value); }
    public double? UpperLimit { get => GetValue(UpperLimitProperty); set => SetValue(UpperLimitProperty, value); }

    public bool IncludeLimitsInRange
    {
        get => GetValue(IncludeLimitsInRangeProperty);
        set => SetValue(IncludeLimitsInRangeProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Another step or other limits: the zoomed range no longer fits
        if (change.Property == IncludeLimitsInRangeProperty || change.Property == LowerLimitProperty ||
            change.Property == UpperLimitProperty || change.Property == UnitProperty)
            ResetZoom();
    }

    // Texts drawn in Render are not bound, so a language change needs a new rendering
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Localizer.Instance.CultureChanged += OnCultureChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Localizer.Instance.CultureChanged -= OnCultureChanged;
    }

    private void OnCultureChanged(object? sender, EventArgs e) => InvalidateVisual();

    /// <summary>Pointer position over the chart, null when the pointer is outside.</summary>
    protected Point? PointerPosition { get; private set; }

    // ----- Zoom -----
    // Mouse wheel zooms around the pointer, dragging zooms to the selection, a double-click resets.

    /// <summary>Visible range of the horizontal axis, null for the whole data.</summary>
    protected (double Min, double Max)? XView { get; private set; }

    /// <summary>Visible range of the vertical axis, null to fit the visible data.</summary>
    protected (double Min, double Max)? YView { get; private set; }

    /// <summary>True if the vertical axis can be zoomed too (Ctrl + wheel, rectangle selection).</summary>
    protected virtual bool CanZoomY => false;

    // Set by Render: plot area, the ranges drawn and the ranges without zoom, to convert pixels to values
    private Rect _plot;
    private (double XMin, double XMax, double YMin, double YMax) _ranges;
    private (double Min, double Max) _fullX;
    private (double Min, double Max) _fullY;

    private Point? _dragStart;

    /// <summary>Called by Render after it chose the axis ranges.</summary>
    protected void SetZoomFrame(Rect plot, (double Min, double Max) x, (double Min, double Max) y,
        (double Min, double Max) fullX, (double Min, double Max) fullY)
    {
        _plot = plot;
        _ranges = (x.Min, x.Max, y.Min, y.Max);
        _fullX = fullX;
        _fullY = fullY;
    }

    public void ResetZoom()
    {
        XView = null;
        YView = null;
        _dragStart = null;
        InvalidateVisual();
    }

    private double DataX(double px) => _ranges.XMin + (px - _plot.Left) / _plot.Width * (_ranges.XMax - _ranges.XMin);
    private double DataY(double py) => _ranges.YMin + (_plot.Bottom - py) / _plot.Height * (_ranges.YMax - _ranges.YMin);

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var point = e.GetCurrentPoint(this);
        if (!point.Properties.IsLeftButtonPressed || !_plot.Contains(point.Position))
            return;

        if (e.ClickCount == 2)
        {
            ResetZoom();
            return;
        }

        _dragStart = point.Position;
        e.Pointer.Capture(this);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        PointerPosition = e.GetPosition(this);
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_dragStart is not { } start)
            return;

        _dragStart = null;
        e.Pointer.Capture(null);

        var end = Clamp(e.GetPosition(this));
        var width = Math.Abs(end.X - start.X);
        var height = Math.Abs(end.Y - start.Y);

        // A click without dragging does not zoom
        if (width >= 6)
            XView = Ordered(DataX(start.X), DataX(end.X));
        if (CanZoomY && height >= 6)
            YView = Ordered(DataY(start.Y), DataY(end.Y));

        InvalidateVisual();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        var p = e.GetPosition(this);
        if (!_plot.Contains(p) || e.Delta.Y == 0)
            return;

        var factor = e.Delta.Y > 0 ? 0.8 : 1.25;

        if (CanZoomY && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            YView = ZoomAround(DataY(p.Y), (_ranges.YMin, _ranges.YMax), _fullY, factor);
        else
            XView = ZoomAround(DataX(p.X), (_ranges.XMin, _ranges.XMax), _fullX, factor);

        e.Handled = true;
        InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        PointerPosition = null;
        InvalidateVisual();
    }

    /// <returns>Null when zoomed out to the whole range again.</returns>
    internal static (double, double)? ZoomAround(double center, (double Min, double Max) current, (double Min, double Max) full,
        double factor)
    {
        var min = center - (center - current.Min) * factor;
        var max = center + (current.Max - center) * factor;
        return max - min >= full.Max - full.Min ? null : (min, max);
    }

    internal static (double, double)? Ordered(double a, double b) => a == b ? null : (Math.Min(a, b), Math.Max(a, b));

    private Point Clamp(Point p) =>
        new(Math.Clamp(p.X, _plot.Left, _plot.Right), Math.Clamp(p.Y, _plot.Top, _plot.Bottom));

    /// <summary>The rectangle being dragged, drawn over the plot.</summary>
    protected void DrawSelection(DrawingContext context)
    {
        if (_dragStart is not { } start || PointerPosition is not { } current)
            return;

        var end = Clamp(current);
        var rect = CanZoomY
            ? new Rect(start, end).Normalize()
            : new Rect(new Point(Math.Min(start.X, end.X), _plot.Top), new Point(Math.Max(start.X, end.X), _plot.Bottom));

        context.DrawRectangle(WithOpacity(AccentBrush, 0.15), new Pen(AccentBrush, 1), rect);
    }


    protected FormattedText Text(string text, IBrush brush, double size = FontSize, FontWeight weight = FontWeight.Normal) =>
        new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(Typeface.FontFamily, FontStyle.Normal, weight), size, brush);

    protected void DrawText(DrawingContext context, string text, IBrush brush, Point origin) =>
        context.DrawText(Text(text, brush), origin);

    /// <summary>
    /// Axis titles: below the horizontal axis, and rotated next to the vertical axis on the left (and right, for a
    /// second axis).
    /// </summary>
    protected void DrawAxisTitles(DrawingContext context, Rect plot, string xTitle, string yTitle, string? yRightTitle = null)
    {
        var x = Text(xTitle, MutedBrush);
        context.DrawText(x, new Point(plot.Center.X - x.Width / 2, Bounds.Height - x.Height - 1));

        DrawRotated(context, Text(yTitle, MutedBrush), new Point(1 + x.Height / 2, plot.Center.Y));

        if (yRightTitle is not null)
            DrawRotated(context, Text(yRightTitle, MutedBrush), new Point(Bounds.Width - 1 - x.Height / 2, plot.Center.Y));
    }

    /// <summary>Text turned by 90° counterclockwise, centered on the point.</summary>
    private static void DrawRotated(DrawingContext context, FormattedText text, Point center)
    {
        var transform = Matrix.CreateTranslation(-text.Width / 2, -text.Height / 2) *
                        Matrix.CreateRotation(-Math.PI / 2) *
                        Matrix.CreateTranslation(center.X, center.Y);

        using (context.PushTransform(transform))
            context.DrawText(text, new Point(0, 0));
    }

    /// <summary>Message in the middle of an empty chart.</summary>
    protected void DrawPlaceholder(DrawingContext context, string message)
    {
        var text = Text(message, MutedBrush, 13);
        context.DrawText(text, new Point((Bounds.Width - text.Width) / 2, (Bounds.Height - text.Height) / 2));
    }

    /// <summary>Hover information in the top left corner.</summary>
    protected void DrawInfo(DrawingContext context, string info, Rect plot)
    {
        var text = Text(info, Foreground, FontSize, FontWeight.SemiBold);
        context.DrawText(text, new Point(plot.Left, 0));
    }

    protected string FormatValue(double value, double tickStep)
    {
        // Enough decimals to tell neighboring ticks apart
        // Ticks never need more digits than the step between them has, relative to the value
        var digits = 5;
        if (tickStep > 0 && value != 0)
            digits = Math.Clamp((int)Math.Floor(Math.Log10(Math.Abs(value))) - (int)Math.Floor(Math.Log10(tickStep)) + 2, 2, 8);
        return ValueFormatter.Format(value, Unit, digits);
    }

    /// <summary>The value range widened by the limits (if enabled) and a small margin.</summary>
    protected (double Min, double Max) ValueRange(double min, double max)
    {
        if (IncludeLimitsInRange)
        {
            if (LowerLimit is { } l) { min = Math.Min(min, l); max = Math.Max(max, l); }
            if (UpperLimit is { } u) { min = Math.Min(min, u); max = Math.Max(max, u); }
        }

        var span = max - min;
        if (span <= 0 || !double.IsFinite(span))
        {
            span = Math.Abs(max) > 0 ? Math.Abs(max) * 0.02 : 1;
            return (min - span, max + span);
        }

        return (min - span * 0.05, max + span * 0.05);
    }

    /// <summary>Round tick values ("nice numbers": 1, 2 or 5 times a power of ten) covering the range.</summary>
    internal static (List<double> Ticks, double Step) NiceTicks(double min, double max, int targetCount)
    {
        var ticks = new List<double>();
        var span = max - min;
        if (!(span > 0) || !double.IsFinite(span) || targetCount < 1)
            return (ticks, 0);

        var rough = span / targetCount;
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(rough)));
        var residual = rough / magnitude;
        // Nearest nice step, so the tick count stays close to the target
        var step = residual switch
        {
            >= 7 => 10 * magnitude,
            >= 3 => 5 * magnitude,
            >= 1.5 => 2 * magnitude,
            _ => magnitude
        };

        var first = Math.Ceiling(min / step) * step;
        for (var v = first; v <= max + step * 1e-9; v += step)
            ticks.Add(Math.Abs(v) < step * 1e-9 ? 0 : v);

        return (ticks, step);
    }

    protected static Pen DashedPen(IBrush brush, double thickness = 1.2) =>
        new(brush, thickness, new DashStyle([4, 3], 0));

    /// <summary>Vertical limit or reference line with a label at the top.</summary>
    protected void DrawVerticalMarker(DrawingContext context, Rect plot, double x, IBrush brush, string label)
    {
        if (x < plot.Left - 0.5 || x > plot.Right + 0.5)
            return;

        context.DrawLine(DashedPen(brush), new Point(x, plot.Top), new Point(x, plot.Bottom));
        var text = Text(label, brush, 10, FontWeight.SemiBold);
        var left = Math.Clamp(x - text.Width / 2, plot.Left, plot.Right - text.Width);
        context.DrawText(text, new Point(left, plot.Top - text.Height - 1));
    }

    /// <summary>Horizontal limit or reference line with a label on the right end.</summary>
    protected void DrawHorizontalMarker(DrawingContext context, Rect plot, double y, IBrush brush, string label)
    {
        if (y < plot.Top - 0.5 || y > plot.Bottom + 0.5)
            return;

        context.DrawLine(DashedPen(brush), new Point(plot.Left, y), new Point(plot.Right, y));
        var text = Text(label, brush, 10, FontWeight.SemiBold);
        context.DrawText(text, new Point(plot.Right - text.Width - 2, y - text.Height - 1));
    }

    protected static IBrush WithOpacity(IBrush brush, double opacity) =>
        brush is ISolidColorBrush solid
            ? new SolidColorBrush(solid.Color, opacity * solid.Opacity)
            : brush;
}