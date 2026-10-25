using System;
using System.Globalization;
using ATLab.Services;
using Avalonia.Data.Converters;

namespace ATLab.Converters;

/// <summary>Shows an enum value with its localized text from the resource key Enum_<Type>_<Value>.</summary>
public sealed class EnumLocalizationConverter : IValueConverter
{
    public object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is not Enum e)
            return value;
    
        var key = $"Enum_{e.GetType().Name}_{e}";
    
        return LocalizationService.Instance.GetString(key);
    }

    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}