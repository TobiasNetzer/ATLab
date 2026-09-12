using Avalonia.Media;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ATLab.Converters;

public class BoolToValueConverter : IValueConverter
{
    public IBrush? TrueValue { get; set; }
    public IBrush? FalseValue { get; set; }

    public object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return value is true ? TrueValue : FalseValue;
    }

    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}