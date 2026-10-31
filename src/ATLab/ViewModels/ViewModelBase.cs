using System;
using ATLab.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

/// <summary>Base of all view models. Provides a title that follows the selected language.</summary>
public partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    /// <summary>False hides and locks the tab header in the main window so the tab cannot be selected.</summary>
    [ObservableProperty]
    private bool _isTabEnabled = true;

    protected void SetLocalizedTitle(string resourceKey, Func<string, string>? formatTitle = null)
    {
        void Update()
        {
            var text = LocalizationService.Instance[resourceKey];
            Title = formatTitle != null ? formatTitle(text) : text;
        }

        Update();
        LocalizationService.Instance.PropertyChanged += (_, _) => Update();
    }
}