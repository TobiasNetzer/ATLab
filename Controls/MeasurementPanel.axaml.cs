using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace ATLab.Controls;

public partial class MeasurementPanel : UserControl
{
    public static readonly StyledProperty<double> NominalProperty =
        AvaloniaProperty.Register<MeasurementPanel, double>(nameof(Nominal));

    public static readonly StyledProperty<double> LowerLimitProperty =
        AvaloniaProperty.Register<MeasurementPanel, double>(nameof(LowerLimit));

    public static readonly StyledProperty<double> UpperLimitProperty =
        AvaloniaProperty.Register<MeasurementPanel, double>(nameof(UpperLimit));

    public static readonly StyledProperty<double> MeasuredValueProperty =
        AvaloniaProperty.Register<MeasurementPanel, double>(nameof(MeasuredValue));

    public double Nominal
    {
        get => GetValue(NominalProperty);
        set => SetValue(NominalProperty, value);
    }

    public double LowerLimit
    {
        get => GetValue(LowerLimitProperty);
        set => SetValue(LowerLimitProperty, value);
    }

    public double UpperLimit
    {
        get => GetValue(UpperLimitProperty);
        set => SetValue(UpperLimitProperty, value);
    }

    public double MeasuredValue
    {
        get => GetValue(MeasuredValueProperty);
        set => SetValue(MeasuredValueProperty, value);
    }

    public MeasurementPanel()
    {
        InitializeComponent();

        NominalProperty.Changed.AddClassHandler<MeasurementPanel>((panel, _) => panel.UpdateVisuals());
        LowerLimitProperty.Changed.AddClassHandler<MeasurementPanel>((panel, _) => panel.UpdateVisuals());
        UpperLimitProperty.Changed.AddClassHandler<MeasurementPanel>((panel, _) => panel.UpdateVisuals());
        MeasuredValueProperty.Changed.AddClassHandler<MeasurementPanel>((panel, _) => panel.UpdateVisuals());


        Root.SizeChanged += (_, __) => UpdateVisuals();
    }

private void UpdateVisuals()
{
    if (Bar.Bounds.Width <= 0)
        return;

    double barLeft = Bar.Bounds.X;
    double barWidth = Bar.Bounds.Width;

    double padding = (UpperLimit - LowerLimit) * 0.15;
    double visualMin = LowerLimit - padding;
    double visualMax = UpperLimit + padding;

    double scale(double v)
    {
        double t = (v - visualMin) / (visualMax - visualMin);
        return barLeft + t * barWidth;
    }

    double lowerX = scale(LowerLimit);
    double nominalX = scale(Nominal);
    double upperX = scale(UpperLimit);
    double measuredX = scale(MeasuredValue);

    double barCenterY = Bar.Bounds.Y + Bar.Bounds.Height / 2;

    // Vertical positions
    Canvas.SetTop(LowerRect, barCenterY - LowerRect.Height / 2);
    Canvas.SetTop(NominalRect, barCenterY - NominalRect.Height / 2);
    Canvas.SetTop(UpperRect, barCenterY - UpperRect.Height / 2);
    Canvas.SetTop(MeasuredMarker, barCenterY - MeasuredMarker.Height / 2);

    double textAbove = barCenterY - 30;

    Canvas.SetTop(LowerText, textAbove);
    Canvas.SetTop(NominalText, textAbove);
    Canvas.SetTop(UpperText, textAbove);
    Canvas.SetTop(MeasuredText, textAbove);

    // Horizontal positions (centered)
    Canvas.SetLeft(LowerRect, lowerX - LowerRect.Width / 2);
    Canvas.SetLeft(NominalRect, nominalX - NominalRect.Width / 2);
    Canvas.SetLeft(UpperRect, upperX - UpperRect.Width / 2);
    Canvas.SetLeft(MeasuredMarker, measuredX - MeasuredMarker.Width / 2);

    Canvas.SetLeft(LowerText, lowerX - LowerText.Bounds.Width / 2);
    Canvas.SetLeft(NominalText, nominalX - NominalText.Bounds.Width / 2);
    Canvas.SetLeft(UpperText, upperX - UpperText.Bounds.Width / 2);
    Canvas.SetLeft(MeasuredText, measuredX - MeasuredText.Bounds.Width / 2);

    // Text
    LowerText.Text = LowerLimit.ToString("0.###");
    NominalText.Text = Nominal.ToString("0.###");
    UpperText.Text = UpperLimit.ToString("0.###");
    MeasuredText.Text = MeasuredValue.ToString("0.###");

    bool inside = MeasuredValue >= LowerLimit && MeasuredValue <= UpperLimit;
    MeasuredMarker.Fill = inside ? Brushes.ForestGreen : Brushes.Red;
}



}
