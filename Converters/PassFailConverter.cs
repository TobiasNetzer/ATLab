using System;
using System.Globalization;
using ATLab.Models;
using Avalonia.Data.Converters;

namespace ATLab.Converters;

public class PassFailConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var passed = value is true;
        return passed ? Icons.Pass : Icons.Fail;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
