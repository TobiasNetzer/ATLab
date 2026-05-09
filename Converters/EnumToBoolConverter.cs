using System;
using System.Globalization;
using System.Linq;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace ATLab.Converters;

public class EnumToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        var enumType = value.GetType();
        var parts = parameter.ToString()!.Split(',', StringSplitOptions.RemoveEmptyEntries);

        return parts
            .Select(part => Enum.Parse(enumType, part.Trim(), ignoreCase: true))
            .Contains(value);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool b || !b)
            return BindingOperations.DoNothing;
        
        var parts = parameter!.ToString()!.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return Enum.Parse(targetType, parts[0].Trim(), ignoreCase: true);

    }
}