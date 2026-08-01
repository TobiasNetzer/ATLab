using System.Globalization;
using ATLab.Helpers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace ATLab.Controls;

public partial class MeasurementPanel : UserControl
{
    public static readonly StyledProperty<double> LowerLimitProperty =
        AvaloniaProperty.Register<MeasurementPanel, double>(nameof(LowerLimit));

    public static readonly StyledProperty<double> UpperLimitProperty =
        AvaloniaProperty.Register<MeasurementPanel, double>(nameof(UpperLimit));

    public static readonly StyledProperty<string> MeasuredValueProperty =
        AvaloniaProperty.Register<MeasurementPanel, string>(nameof(MeasuredValue));
    
    public static readonly StyledProperty<string> UnitProperty =
        AvaloniaProperty.Register<MeasurementPanel, string>(nameof(Unit));

    public string Unit
    {
        get => GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
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

    public string MeasuredValue
    {
        get => GetValue(MeasuredValueProperty);
        set => SetValue(MeasuredValueProperty, value);
    }

    public MeasurementPanel()
    {
        InitializeComponent();

        UnitProperty.Changed.AddClassHandler<MeasurementPanel>((panel, _) => panel.UpdateVisuals());
        LowerLimitProperty.Changed.AddClassHandler<MeasurementPanel>((panel, _) => panel.UpdateVisuals());
        UpperLimitProperty.Changed.AddClassHandler<MeasurementPanel>((panel, _) => panel.UpdateVisuals());
        MeasuredValueProperty.Changed.AddClassHandler<MeasurementPanel>((panel, _) => panel.UpdateVisuals());


        Root.SizeChanged += (_, __) => UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (Bar.Bounds.Width <= 0)
            return;

        var barLeft = Bar.Bounds.X;
        var barWidth = Bar.Bounds.Width;
        var barCenterY = Bar.Bounds.Y + Bar.Bounds.Height / 2;

        const int edgePadding = 180;

        var lowerX = barLeft + edgePadding;
        var upperX = barLeft + barWidth - edgePadding;

        if (!double.TryParse(MeasuredValue, NumberStyles.Any, CultureInfo.CurrentCulture, out var measured))
        {
            measured = 0;
            MeasuredMarker.IsVisible = false;
            MeasuredText.Text = string.Empty;
        }
        else
        {
            MeasuredText.Text = !string.IsNullOrWhiteSpace(Unit)
                ? UnitParser.Format(measured, Unit)
                : measured.ToString(CultureInfo.CurrentCulture);
            
            var measuredX = Scale(measured);
            MeasuredMarker.IsVisible = true;
            Canvas.SetTop(MeasuredMarker, barCenterY - MeasuredMarker.Height / 2);
            Canvas.SetLeft(MeasuredMarker, measuredX - MeasuredMarker.Width / 2);
        }
        
        Canvas.SetTop(LowerRect, barCenterY - LowerRect.Height / 2);
        Canvas.SetTop(UpperRect, barCenterY - UpperRect.Height / 2);
        
        Canvas.SetLeft(LowerRect, lowerX - LowerRect.Width / 2);
        Canvas.SetLeft(UpperRect, upperX - UpperRect.Width / 2);
        
        LowerText.Text = !string.IsNullOrWhiteSpace(Unit)
            ? UnitParser.Format(LowerLimit, Unit)
            : LowerLimit.ToString(CultureInfo.CurrentCulture);
        
        UpperText.Text = !string.IsNullOrWhiteSpace(Unit)
            ? UnitParser.Format(UpperLimit, Unit)
            : UpperLimit.ToString(CultureInfo.CurrentCulture);
        
        var inside = measured >= LowerLimit && measured <= UpperLimit;
        MeasuredMarker.Fill = inside ? Brushes.LimeGreen : Brushes.Red;
        MeasuredText.Foreground = inside ? Brushes.LimeGreen : Brushes.Red;
        return;

        double Scale(double v)
        {
            var barRight = barLeft + barWidth;

            var usableLeft  = barLeft + edgePadding;
            var usableRight = barRight - edgePadding;
            var usableWidth = usableRight - usableLeft;
            
            if (UpperLimit.Equals(LowerLimit))
                return barLeft + barWidth / 2;
            
            if (v <= LowerLimit)
                return barLeft;

            if (v >= UpperLimit)
                return barRight;
            
            var t = (v - LowerLimit) / (UpperLimit - LowerLimit);
            return usableLeft + t * usableWidth;
        }

    }

}