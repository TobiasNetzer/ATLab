using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace ATLab.FixtureDesigner.Controls;

/// <summary>
/// Base of the sketches of the part and probe types: drawn in a fixed area that is scaled to fit, in the colors of the
/// theme, with a letter at every dimension. The letters are the ones beside the fields of the type.
/// </summary>
public abstract class DimensionSketch : Control
{
    private const double DesignWidth = 300;
    private const double DesignHeight = 270;

    static DimensionSketch() => AffectsRender<DimensionSketch>(TextElement.ForegroundProperty);

    protected DimensionSketch() => ActualThemeVariantChanged += (_, _) => InvalidateVisual();

    protected static IBrush BoardBrush { get; } = new SolidColorBrush(Color.FromRgb(0x2F, 0x9E, 0x5B));

    protected IBrush Ink => GetValue(TextElement.ForegroundProperty) ?? Brushes.Gray;

    protected abstract void Draw(DrawingContext context);

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var scale = Math.Min(Bounds.Width / DesignWidth, Bounds.Height / DesignHeight);
        if (scale <= 0)
            return;

        var offset = Matrix.CreateTranslation((Bounds.Width / scale - DesignWidth) / 2, (Bounds.Height / scale - DesignHeight) / 2);
        using (context.PushTransform(offset * Matrix.CreateScale(scale, scale)))
            Draw(context);
    }

    protected IBrush Brush(string key, IBrush fallback, double opacity = 1)
    {
        if (!this.TryFindResource(key, ActualThemeVariant, out var value))
            return fallback;

        return value switch
        {
            Color color => new SolidColorBrush(color, opacity),
            ISolidColorBrush brush => new SolidColorBrush(brush.Color, opacity),
            IBrush brush => brush,
            _ => fallback
        };
    }

    protected void DrawLetter(DrawingContext context, Point center, string letter)
    {
        context.DrawEllipse(Brush("InfoColor", Brushes.SteelBlue), null, center, 8, 8);
        var text = new FormattedText(letter, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold), 11, Brushes.White);
        context.DrawText(text, new Point(center.X - text.Width / 2, center.Y - text.Height / 2));
    }

    protected static StreamGeometry Polygon(IReadOnlyList<Point> points, bool closed)
    {
        var geometry = new StreamGeometry();
        using var figure = geometry.Open();
        figure.BeginFigure(points[0], closed);
        foreach (var point in points.Skip(1))
            figure.LineTo(point);
        figure.EndFigure(closed);
        return geometry;
    }
}
