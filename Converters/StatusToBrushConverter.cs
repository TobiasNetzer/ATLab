using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;
using ATLab.Models;

namespace ATLab.Converters;

public class StatusToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ConnectionStatus status)
        {
            return status switch
            {
                ConnectionStatus.Connected => Brushes.LightGreen,
                ConnectionStatus.Failed => Brushes.Red,
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

