using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;
using ATLab.Interfaces;
using ATLab.Resources;

namespace ATLab.Services;

/// <summary>
/// Resource based localization (English, German). A single instance is used by bindings and code.
/// </summary>
public class LocalizationService : ILocalizationService
{
    private readonly ResourceManager _resourceManager = AppResources.ResourceManager;
    private CultureInfo _currentCulture = CultureInfo.CurrentUICulture;

    public static LocalizationService Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public CultureInfo CurrentCulture => _currentCulture;

    public IReadOnlyList<CultureInfo> SupportedCultures { get; } = new[]
    {
        new CultureInfo("en"),
        new CultureInfo("de")
    };

    public string this[string key] => GetString(key);

    public string GetString(string key)
    {
        return _resourceManager.GetString(key, _currentCulture) ?? $"[{key}]";
    }

    public void ChangeCulture(CultureInfo culture)
    {
        _currentCulture = culture;
        //CultureInfo.CurrentCulture = culture; // Only change UI culture, the current culture is used for formatting and parsing and should stem from the operating system
        CultureInfo.CurrentUICulture = culture;

        OnPropertyChanged(string.Empty);
        OnPropertyChanged(null);
        OnPropertyChanged("Item[]");
        OnPropertyChanged("Item");
        OnPropertyChanged(nameof(CurrentCulture));
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}