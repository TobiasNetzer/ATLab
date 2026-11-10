using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace ATLab.FixtureDesigner.Controls;

public enum FixtureIconKind
{
    ToolingPin,
    PreCenteringPin,
    SupportPin,
    PushRod
}

/// <summary>
/// The icons Lucide has no match for: the parts of a fixture, seen from the side. Each is an SVG path
/// on a 24 x 24 grid drawn with the same round 2-unit stroke as Lucide, in the color of the surrounding text.
/// </summary>
public sealed class FixtureIcon : Control
{
    private const double GridSize = 24;

    private static readonly Dictionary<FixtureIconKind, string> Paths = new()
    {
        // A thin pin on the shoulder the board rests on
        [FixtureIconKind.ToolingPin] = "M10 11V3h4v8M6 11h12v10M6 11v10M3 21h18",
        // Wide, with a large chamfer that guides the board in
        [FixtureIconKind.PreCenteringPin] = "M5 21V12l5-9h4l5 9v9M3 21h18",
        // Short, right under the board
        [FixtureIconKind.SupportPin] = "M3 8h18M3 21h18M7 21v-6l2-2h6l2 2v6",
        // Comes down from its plate: a thick body, a chamfer and the thin end
        [FixtureIconKind.PushRod] = "M3 3h18M7 3v9l3 3v6h4v-6l3-3V3"
    };

    private static readonly Dictionary<FixtureIconKind, Geometry> Geometries = new();

    public static readonly StyledProperty<FixtureIconKind> KindProperty =
        AvaloniaProperty.Register<FixtureIcon, FixtureIconKind>(nameof(Kind));

    public static readonly StyledProperty<double> SizeProperty =
        AvaloniaProperty.Register<FixtureIcon, double>(nameof(Size), 16);

    public FixtureIconKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public double Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    static FixtureIcon()
    {
        AffectsRender<FixtureIcon>(KindProperty, SizeProperty, TextElement.ForegroundProperty);
        AffectsMeasure<FixtureIcon>(SizeProperty);
    }

    protected override Size MeasureOverride(Size availableSize) => new(Size, Size);

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (!Geometries.TryGetValue(Kind, out var geometry))
            Geometries[Kind] = geometry = Geometry.Parse(Paths[Kind]);

        var brush = GetValue(TextElement.ForegroundProperty) ?? Brushes.Gray;
        var pen = new Pen(brush, 2, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        var scale = Size / GridSize;
        using (context.PushTransform(Matrix.CreateScale(scale, scale)))
            context.DrawGeometry(null, pen, geometry);
    }
}
