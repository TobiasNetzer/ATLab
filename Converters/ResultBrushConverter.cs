using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace ATLab.Converters;

/// <summary>Colors a result: valid, invalid, or unknown when the value is not a bool (e.g. null).</summary>
public class ResultBrushConverter : IValueConverter
{
    public IBrush? ValidBrush { get; set; }
    public IBrush? InvalidBrush { get; set; }
    public IBrush? UnknownBrush { get; set; }

    public object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is not bool isValid)
            return UnknownBrush;

        return isValid ? ValidBrush : InvalidBrush;
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
