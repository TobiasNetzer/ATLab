using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace ATLab.Analyzer.Localization;

/// <summary>
/// Resource based localization (English, German), like ATLab's LocalizationService. Only the UI culture changes;
/// numbers and dates keep the format of the operating system.
/// </summary>
public sealed class Localizer : INotifyPropertyChanged
{
    private static readonly ResourceManager Resources =
        new("ATLab.Analyzer.Resources.Strings", typeof(Localizer).Assembly);

    private Localizer()
    {
        Culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de" ? SupportedCultures[1] : SupportedCultures[0];
    }

    public static Localizer Instance { get; } = new();

    public IReadOnlyList<CultureInfo> SupportedCultures { get; } = [new CultureInfo("en"), new CultureInfo("de")];

    public CultureInfo Culture { get; private set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Raised after the language changed, for texts built in code.</summary>
    public event EventHandler? CultureChanged;

    /// <summary>Indexer for the {loc:Localize Key} bindings.</summary>
    public string this[string key] => Get(key);

    public static string Get(string key) => Resources.GetString(key, Instance.Culture) ?? $"[{key}]";

    /// <summary>Text with placeholders; numbers are formatted in the culture of the operating system.</summary>
    public static string Format(string key, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, Get(key), args);

    /// <summary>Text of an enum value from the key Enum_{Type}_{Value}; the plain name if there is no text yet.</summary>
    public static string Enum(Enum value) =>
        Resources.GetString($"Enum_{value.GetType().Name}_{value}", Instance.Culture) ?? value.ToString();

    public void ChangeCulture(CultureInfo culture)
    {
        if (culture.TwoLetterISOLanguageName == Culture.TwoLetterISOLanguageName)
            return;

        Culture = culture;
        CultureInfo.CurrentUICulture = culture;

        // Indexer bindings react to different names depending on the binding type, so raise all of them like ATLab
        foreach (var name in new[] { string.Empty, null, "Item", "Item[]", nameof(Culture) })
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        CultureChanged?.Invoke(this, EventArgs.Empty);
    }
}