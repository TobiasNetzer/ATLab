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
            return new SolidColorBrush(Color.Parse("#FF2C2C30"));

        return status switch
        {
            TestStatus.IDLE      => new SolidColorBrush(Color.Parse("#FF2C2C30")),
            TestStatus.RUNNING   => new SolidColorBrush(Color.Parse("#992979FF")),
            TestStatus.PASSED    => new SolidColorBrush(Color.Parse("#FF00C853")),
            TestStatus.FAILED    => new SolidColorBrush(Color.Parse("#FFFF1744")),
            TestStatus.CANCELLED => Brushes.Orange,
            _ => new SolidColorBrush(Color.Parse("#FF2C2C30"))
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
