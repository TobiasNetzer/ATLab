using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ATLab.Converters;

/// <summary>Dims an element (50 % opacity) when the bound value is true, e.g. for ignored steps.</summary>
public class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? 0.5 : 1.0;
    }
    
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}