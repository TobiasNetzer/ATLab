using System;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace ATLab.Converters;

/// <summary>True when the bound enum value is none of the comma separated values in the parameter.</summary>
public class InvertedEnumToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return true; // default: enabled

        var enumType = value.GetType();
        var parts = parameter.ToString()!.Split(',', StringSplitOptions.RemoveEmptyEntries);

        // Return TRUE when NOT equal to any of the given values
        return !parts
            .Select(part => Enum.Parse(enumType, part.Trim(), ignoreCase: true))
            .Contains(value);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
