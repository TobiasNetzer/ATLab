using System;
using System.Collections.Generic;
using System.Globalization;
using ATLab.Helpers;
using Avalonia.Data.Converters;

namespace ATLab.Converters;

public class UnitFormatConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values != null && values.Count >= 1 && values[0] is double val)
        {
            string? unit = values.Count >= 2 ? values[1]?.ToString() : null;
            return UnitParser.Format(val, unit);
        }
        return string.Empty;
    }
}
