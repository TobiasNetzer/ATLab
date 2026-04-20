using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using Avalonia.Media.Imaging;

namespace ATLab.ViewModels;

public partial class MessageBoxViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _message = string.Empty;
    
    [ObservableProperty]
    private string _okText = "Ok";

    [ObservableProperty]
    private string _cancelText = "Cancel";

    [ObservableProperty]
    private bool _showCancel = true;

    public Bitmap? Bitmap { get; set; }

    public event Action<bool>? CloseRequested;

    [RelayCommand]
    private void Ok()
    {
        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(false);
    }
}