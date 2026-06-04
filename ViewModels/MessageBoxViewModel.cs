using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using ATLab.Services;
using Avalonia.Media.Imaging;

namespace ATLab.ViewModels;

public partial class MessageBoxViewModel : ViewModelBase, IDisposable
{
    private readonly ControlModuleService _controlModuleService;
    
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
    
    private event Action PassHandler;
    private event Action CancelHandler;
    
    public MessageBoxViewModel(ControlModuleService controlModuleService)
    {
        _controlModuleService = controlModuleService;
        
        PassHandler += async () => 
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                OkCommand.Execute(null);
            });
            
        CancelHandler += async () => 
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                CancelCommand.Execute(null);
            });
        
        _controlModuleService.PassPressed += PassHandler;
        _controlModuleService.FailPressed += CancelHandler;
    }

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
    
    public void Dispose()
    {
        _controlModuleService.PassPressed -= PassHandler;
        _controlModuleService.FailPressed -= CancelHandler;
    }
}