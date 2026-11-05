using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace ATLab.FixtureDesigner.Localization;

/// <summary>
/// XAML markup extension {loc:Localize Key} that binds a text to the localizer, so it updates when the language
/// changes.
/// </summary>
public sealed class LocalizeExtension : MarkupExtension
{
    public LocalizeExtension() { }

    public LocalizeExtension(string key) => Key = key;

    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        new Binding($"[{Key}]") { Source = Localizer.Instance, Mode = BindingMode.OneWay };
}

/// <summary>Shows an enum value in the current language (resource key Enum_{Type}_{Value}).</summary>
public sealed class EnumTextConverter : IValueConverter
{
    public static EnumTextConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Enum e ? Localizer.Enum(e) : value;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// A number in a text box: empty or anything that is no number is 0. Parsed in the current culture, else invariant, so
/// both 1,5 and 1.5 work. The text boxes take their value when they lose focus (class Numeric), so a number being typed
/// (e.g. "-") is not taken before it is complete; the window then shows 0 in a box that holds no number.
/// </summary>
public sealed class NumberConverter : IValueConverter
{
    public static NumberConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is IFormattable number ? number.ToString(null, culture) : value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Parse(value?.ToString(), culture) ?? 0.0;

    /// <summary>The number in a text, or null if it is none.</summary>
    public static double? Parse(string? text, CultureInfo culture)
    {
        text = text?.Trim() ?? string.Empty;
        return double.TryParse(text, NumberStyles.Float, culture, out var number) ||
               double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number)
            ? number
            : null;
    }
}
