using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class SerialNumberEntryBoxViewModel : ObservableObject
{
    [ObservableProperty]
    private string _serialNumber = string.Empty;

    public event Action<bool>? RequestClose;

    [RelayCommand]
    private void Ok() => RequestClose?.Invoke(true);

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(false);
}