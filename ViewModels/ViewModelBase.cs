using System;
using ATLab.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

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