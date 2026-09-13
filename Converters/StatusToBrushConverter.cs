using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;
using ATLab.Enums;

namespace ATLab.Converters;

public class StatusToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ConnectionStatus status)
        {
            return status switch
            {
                ConnectionStatus.CONNECTED =>  new SolidColorBrush(Color.Parse("#FF00C853")),
                ConnectionStatus.FAILED =>  new SolidColorBrush(Color.Parse("#FFFF1744")),
                _ => Brushes.Gray,
            };
        }

        return Brushes.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

