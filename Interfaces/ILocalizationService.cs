using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace ATLab.Interfaces;

/// <summary>
/// Provides the UI texts in the selected language and notifies bindings when the language changes.
/// </summary>
public interface ILocalizationService : INotifyPropertyChanged
{
    string this[string key] { get; }
    CultureInfo CurrentCulture { get; }
    IReadOnlyList<CultureInfo> SupportedCultures { get; }
    void ChangeCulture(CultureInfo culture);
    string GetString(string key);
}