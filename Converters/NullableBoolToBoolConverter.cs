using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ATLab.Converters;

public class NullableBoolToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value as bool? ?? false;
    }
    
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}