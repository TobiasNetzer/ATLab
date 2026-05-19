using System;
using System.Collections.Generic;
using System.Globalization;
using ATLab.Helpers;
using Avalonia.Data.Converters;

namespace ATLab.Converters;

public class UnitFormatConverter : IMultiValueConverter
{
    public object Convert(IList<object?>? values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is not { Count: >= 1 } || values[0] is not double val) return string.Empty;
        
        var unit = values.Count >= 2 ? values[1]?.ToString() : null;
        
        return string.IsNullOrEmpty(unit) ? val.ToString(culture) : UnitParser.Format(val, unit);
    }
}