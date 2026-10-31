using System;
using ATLab.Services;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace ATLab.Helpers;

/// <summary>
/// XAML markup extension {helpers:Localize Key} that binds a text to the localization service, so it updates when
/// the language changes.
/// </summary>
public class LocalizeExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public LocalizeExtension() { }

    public LocalizeExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]")
        {
            Source = LocalizationService.Instance,
            Mode = BindingMode.OneWay
        };

        return binding;
    }
}