using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using ATLab.Enums;
using ATLab.Services;
using Avalonia.Media.Imaging;
using ShadUI;

namespace ATLab.ViewModels;

public partial class MessageBoxViewModel : ViewModelBase, IDisposable
{
    private readonly ControlModuleService _controlModuleService;
    private readonly DialogManager _dialogManager;
    
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _message = string.Empty;
    
    [ObservableProperty]
    private string _okText = "Ok";

    [ObservableProperty]
    private string _cancelText = "Cancel";

    [ObservableProperty]
    private DialogFunction _dialogFunction = DialogFunction.INFORMATION;

    public Bitmap? Bitmap { get; set; }
    
    private event Action PassHandler;
    private event Action CancelHandler;
    
    public MessageBoxViewModel(ControlModuleService controlModuleService,
        DialogManager dialogManager)
    {
        _controlModuleService = controlModuleService;
        _dialogManager = dialogManager;
        
        PassHandler += async () => 
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                SubmitCommand.Execute(null);
            });
            
        CancelHandler += async () => 
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                CancelCommand.Execute(null);
            });
        
        _controlModuleService.PassPressed += PassHandler;
        _controlModuleService.FailPressed += CancelHandler;
    }

    public void Initialize(string title, string message, string okText = "Continue", string cancelText = "Cancel", DialogFunction dialogFunction = DialogFunction.INFORMATION, Bitmap? bitmap = null)
    {
        Title = title;
        Message = message;
        OkText = okText;
        CancelText = cancelText;
        DialogFunction = dialogFunction;
        Bitmap = bitmap;
    }

    [RelayCommand]
    private void Submit()
    {
        _dialogManager.Close(this, new CloseDialogOptions { Success = true });
    }

    [RelayCommand]
    private void Cancel()
    {
        _dialogManager.Close(this);
    }
    
    public void Dispose()
    {
        _controlModuleService.PassPressed -= PassHandler;
        _controlModuleService.FailPressed -= CancelHandler;
    }
}