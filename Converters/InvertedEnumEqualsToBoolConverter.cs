using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ATLab.Converters;

public class InvertedEnumEqualsToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return true; // default: enabled

        var enumType = value.GetType();
        var parameterValue = Enum.Parse(enumType, parameter.ToString()!);

        // Return TRUE when NOT equal
        return !value.Equals(parameterValue);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
