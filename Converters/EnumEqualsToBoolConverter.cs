using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ATLab.Converters;

public class EnumEqualsToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        // parameter comes in as a string → parse it to the enum type
        var enumType = value.GetType();
        var parameterValue = Enum.Parse(enumType, parameter.ToString()!);

        return value.Equals(parameterValue);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
