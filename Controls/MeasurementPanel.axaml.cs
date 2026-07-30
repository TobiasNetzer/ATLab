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

        double padding = (UpperLimit - LowerLimit) * 0.15; // 5% padding
        double visualMin = LowerLimit - padding;
        double visualMax = UpperLimit + padding;

        double scale(double v)
        {
            if (visualMax == visualMin)
                return 0;

            double t = (v - visualMin) / (visualMax - visualMin); // 0..1
            return barLeft + t * barWidth; // position in grid coords
        }

        double lowerX = scale(LowerLimit);
        double nominalX = scale(Nominal);
        double upperX = scale(UpperLimit);
        double measuredX = scale(MeasuredValue);

        // center markers (2px rect, 12px ellipse)
        double rectOffset = -1;
        double ellipseOffset = -6;

        ((TranslateTransform)LowerRect.RenderTransform).X = lowerX + rectOffset;
        ((TranslateTransform)NominalRect.RenderTransform).X = nominalX + rectOffset;
        ((TranslateTransform)UpperRect.RenderTransform).X = upperX + rectOffset;
        ((TranslateTransform)MeasuredMarker.RenderTransform).X = measuredX + ellipseOffset;

        ((TranslateTransform)LowerText.RenderTransform).X = lowerX;
        ((TranslateTransform)NominalText.RenderTransform).X = nominalX;
        ((TranslateTransform)UpperText.RenderTransform).X = upperX;
        ((TranslateTransform)MeasuredText.RenderTransform).X = measuredX;

        LowerText.Text = LowerLimit.ToString("0.###");
        NominalText.Text = Nominal.ToString("0.###");
        UpperText.Text = UpperLimit.ToString("0.###");
        MeasuredText.Text = MeasuredValue.ToString("0.###");

        bool inside = MeasuredValue >= LowerLimit && MeasuredValue <= UpperLimit;
        MeasuredMarker.Fill = inside ? Brushes.ForestGreen : Brushes.Red;
    }



}
