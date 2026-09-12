using ATLab.Enums;

namespace ATLab.Converters;

using Avalonia.Media;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

public class TestStatusToBrushConverter : IValueConverter
{
    public IBrush? IdleBrush { get; set; }
    public IBrush? RunningBrush { get; set; }
    public IBrush? PassedBrush { get; set; }
    public IBrush? FailedBrush { get; set; }
    public IBrush? CancelledBrush { get; set; }

    public object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is not TestStatus status)
            return IdleBrush;

        return status switch
        {
            TestStatus.IDLE      => IdleBrush,
            TestStatus.RUNNING   => RunningBrush,
            TestStatus.PASSED    => PassedBrush,
            TestStatus.FAILED    => FailedBrush,
            TestStatus.CANCELLED => CancelledBrush,
            _                    => IdleBrush
        };
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
        => throw new NotSupportedException();
}
