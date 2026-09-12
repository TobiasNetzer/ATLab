using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace ATLab.Converters;

public class ResultBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isValid)
        {
            return isValid ? new SolidColorBrush(Color.Parse("#FF00C853")) : new SolidColorBrush(Color.Parse("#FFFF1744"));
        }

        return new SolidColorBrush(Color.Parse("#FF2C2C30"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
