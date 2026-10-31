using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace ATLab.Analyzer.Localization;

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