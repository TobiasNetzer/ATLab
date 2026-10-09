using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace ATLab.Interfaces;

public interface ILocalizationService : INotifyPropertyChanged
{
    string this[string key] { get; }
    CultureInfo CurrentCulture { get; }
    IReadOnlyList<CultureInfo> SupportedCultures { get; }
    void ChangeCulture(CultureInfo culture);
    string GetString(string key);
}