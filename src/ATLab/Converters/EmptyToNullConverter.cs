using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ATLab.Converters;

/// <summary>Null for an empty or blank string, so a tooltip bound to a text does not open as an empty box.</summary>
public class EmptyToNullConverter : IValueConverter
{
    public static readonly EmptyToNullConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string s && !string.IsNullOrWhiteSpace(s) ? s : null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
