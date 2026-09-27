using System;
using Avalonia.Data.Converters;
using System.Globalization;
using Avalonia;

namespace ATLab.Converters
{
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                null => AvaloniaProperty.UnsetValue,
                bool b => !b,
                _ => AvaloniaProperty.UnsetValue
            };
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;
            return AvaloniaProperty.UnsetValue;
        }
    }
}