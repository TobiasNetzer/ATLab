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
            return isValid ? Brushes.LightGreen : Brushes.Red;
        }

        return Brushes.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
