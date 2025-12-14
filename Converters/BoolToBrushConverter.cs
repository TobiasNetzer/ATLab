using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ATLab.Converters;

public class BoolToBrushConverter : IValueConverter
{
    public IBrush TrueBrush { get; set; } = Brushes.Red;
    public IBrush FalseBrush { get; set; } = Brushes.Gray;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool and true ? TrueBrush : FalseBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Avalonia.Data.BindingOperations.DoNothing;
    }
}