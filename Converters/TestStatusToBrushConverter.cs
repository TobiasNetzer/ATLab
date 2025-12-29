using ATLab.Enums;

namespace ATLab.Converters;

using Avalonia.Media;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

public class TestStatusToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TestStatus status)
            return Brushes.Gray;

        return status switch
        {
            TestStatus.IDLE      => Brushes.Gray,
            TestStatus.RUNNING   => Brushes.DodgerBlue,
            TestStatus.PASSED    => Brushes.LimeGreen,
            TestStatus.FAILED    => Brushes.Red,
            TestStatus.CANCELLED => Brushes.Orange,
            _ => Brushes.Gray
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
