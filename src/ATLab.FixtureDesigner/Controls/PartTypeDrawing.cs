using System;
using System.ComponentModel;
using System.Linq;
using ATLab.FixtureDesigner.Plates;
using ATLab.Pcb.Fixture;
using Avalonia;
using Avalonia.Media;

namespace ATLab.FixtureDesigner.Controls;

/// <summary>
/// Sketch of a part type, not to scale: its shape in its plate and where the board meets it, with a letter at every
/// dimension. The letters are the ones beside the fields of the type. It follows the type's options as they change.
/// </summary>
public sealed class PartTypeDrawing : DimensionSketch
{
    public static readonly StyledProperty<PartType?> TypeProperty =
        AvaloniaProperty.Register<PartTypeDrawing, PartType?>(nameof(Type));

    public PartType? Type
    {
        get => GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    static PartTypeDrawing() => AffectsRender<PartTypeDrawing>(TypeProperty);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != TypeProperty)
            return;

        if (change.OldValue is PartType old)
            old.PropertyChanged -= OnTypeChanged;
        if (change.NewValue is PartType type)
            type.PropertyChanged += OnTypeChanged;
    }

    private void OnTypeChanged(object? sender, PropertyChangedEventArgs e) => InvalidateVisual();

    protected override void Draw(DrawingContext context)
    {
        if (Type is { } type)
            Draw(context, type);
    }

    private void Draw(DrawingContext context, PartType type)
    {
        const double centerX = 130, plateThickness = 22, plateHalf = 100;
        const double fullHeight = 128, thin = 62, stepChamfer = 9, boardThickness = 10;
        var tip = type.IsPreCenteringPin ? 20.0 : 8.0;

        // Support pins are short, usually lower than they are wide
        var total = type.Kind == FixturePartKind.SupportPin ? 34.0 : fullHeight;

        var ink = Ink;
        var muted = Brush("ChartMuted", Brushes.Gray);
        var linePen = new Pen(ink, 1.3);
        var dimensionPen = new Pen(muted);
        var extensionPen = new Pen(muted, 1, new DashStyle([3, 3], 0));

        var hasBody = type.HasBody;
        // Pre-centering pins are drawn wide, as they usually are: they guide the board in
        var r = hasBody ? 13.0 : type.IsPreCenteringPin ? 36.0 : 20.0;
        const double bodyRadius = 30;
        var baseRadius = hasBody ? bodyRadius : r;
        var isChamfered = type.TipStyle == PartTipStyle.Chamfer;

        var profile = PinProfile.Of(new PartType
        {
            Kind = type.Kind,
            Diameter = 2 * r,
            Height = total,
            BodyDiameter = hasBody ? 2 * bodyRadius : 0,
            ThinLength = thin,
            StepChamfer = stepChamfer,
            TipStyle = type.TipStyle,
            TipSize = tip
        });

        // Push rods come down from their plate, the pins stand up from theirs
        var flipped = type.IsPushRod;
        var faceY = flipped ? 68.0 : 202.0 - (fullHeight - total) / 2;
        double Y(double z) => flipped ? faceY + z : faceY - z;
        Point At(double x, double z) => new(x, Y(z));
        Rect Box(double x0, double z0, double x1, double z1) => new(At(x0, z0), At(x1, z1));

        // ----- Plate and board -----
        var plateBrush = Brush("ChartMuted", Brushes.Gray, 0.25);
        // Clearly narrower than the part, to show that the drill is a dimension of its own
        var holeRadius = baseRadius * 0.55;
        context.DrawRectangle(plateBrush, linePen, Box(centerX - plateHalf, -plateThickness, centerX - holeRadius, 0).Normalize());
        context.DrawRectangle(plateBrush, linePen, Box(centerX + holeRadius, -plateThickness, centerX + plateHalf, 0).Normalize());

        void Board(double x0, double z0, double x1) => context.DrawRectangle(BoardBrush, null, Box(x0, z0, x1, z0 + boardThickness).Normalize());

        var tipLane = total + 14;
        if (type.IsToolingPin)
        {
            var z = hasBody ? total - thin : total * 0.5;
            Board(centerX - 88, z, centerX - r - 2);
            Board(centerX + r + 2, z, centerX + 48);
        }
        else if (type.IsPreCenteringPin)
        {
            Board(centerX - 92, total * 0.5, centerX - r - 16);
        }
        else
        {
            Board(centerX - 48, total, centerX + 48);
            tipLane += boardThickness;
        }

        // ----- The part, from the face of its plate to its tip -----
        var right = profile.Stations.Select(s => At(centerX + s.Radius, s.Z)).ToList();
        var left = profile.Stations.Reverse().Select(s => At(centerX - s.Radius, s.Z)).ToList();
        var top = Enumerable.Empty<Point>();
        if (profile.HasRoundTip)
        {
            var last = profile.Stations[^1];
            top = Enumerable.Range(1, 23).Select(i => i * Math.PI / 24)
                .Select(a => At(centerX + last.Radius * Math.Cos(a), last.Z + last.Radius * Math.Sin(a)));
        }

        var outline = right.Concat(top).Concat(left).ToList();
        context.DrawGeometry(Brush("InfoColor", Brushes.SteelBlue, 0.45), linePen, Polygon(outline, true));

        // ----- Dimensions -----
        void Line(IPen pen, Point a, Point b) => context.DrawLine(pen, a, b);

        void Letter(Point center, string letter) => DrawLetter(context, center, letter);

        void Across(double lane, double fromZ, double radius, string letter, bool letterBeside = false)
        {
            Line(extensionPen, At(centerX - radius, fromZ), At(centerX - radius, lane));
            Line(extensionPen, At(centerX + radius, fromZ), At(centerX + radius, lane));
            Line(dimensionPen, At(centerX - radius, lane), At(centerX + radius, lane));
            Letter(At(letterBeside ? centerX + radius + 12 : centerX, lane), letter);
        }

        void Along(double x, double fromZ, double toZ, double fromX, string letter)
        {
            Line(extensionPen, At(fromX, fromZ), At(x, fromZ));
            Line(dimensionPen, At(x, fromZ), At(x, toZ));
            Letter(At(x, (fromZ + toZ) / 2), letter);
        }

        void Leader(double x, double z, string letter)
        {
            var end = At(centerX - baseRadius - 24, z);
            Line(dimensionPen, At(x, z), end);
            Letter(end, letter);
        }

        var straightTop = profile.HasRoundTip ? profile.Stations[^1].Z : isChamfered ? total - tip : total;
        Across(tipLane, straightTop, r, "A", letterBeside: true);

        var farLane = -plateThickness - 14;
        Across(farLane, -plateThickness, holeRadius, "C");
        if (hasBody)
            Across(farLane - 20, 0, bodyRadius, "B");

        Line(extensionPen, At(centerX + r, total), At(centerX + baseRadius + 54, total));
        Along(centerX + baseRadius + 54, 0, total, centerX + baseRadius, "D");
        if (hasBody)
            Along(centerX + baseRadius + 24, total - thin, total, centerX + r, "E");

        if (hasBody && type.IsPushRod)
            Leader(centerX - r - stepChamfer / 2, total - thin + stepChamfer / 2, "F");

        if (isChamfered)
            Leader(centerX - r + tip / 2, total - tip / 2, "G");

        if (type.IsPreCenteringPin)
        {
            var lane = total * 0.5 + boardThickness + 14;
            var (edge, pin) = (centerX - r - 16, centerX - r);
            Line(extensionPen, At(edge, total * 0.5 + boardThickness), At(edge, lane));
            Line(extensionPen, At(pin, lane - 8), At(pin, lane));
            Line(dimensionPen, At(edge, lane), At(pin, lane));
            Letter(At(edge - 12, lane), "H");
        }
    }
}
