using System.IO;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.ViewModels;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using ShadUI;

namespace ATLab.Services;

public partial class MessageBoxService : ObservableObject, IMessageBoxService
{
    private readonly IErrorService _errorService;
    private readonly ControlModuleService _controlModuleService;
    private readonly MessageBoxViewModel _messageBoxViewModel;
    
    [ObservableProperty]
    private DialogManager _dialogManager;
    
    [ObservableProperty]
    private bool _isDialogOpen;
    
    public MessageBoxService(IErrorService errorService,
        ControlModuleService controlModuleService,
        MessageBoxViewModel messageBoxViewModel,
        DialogManager dialogManager)
    {
        _errorService = errorService;
        _controlModuleService = controlModuleService;
        _messageBoxViewModel = messageBoxViewModel;
        _dialogManager = dialogManager;
    }

    public async Task<bool> ShowConfirmationDestructiveAsync(
        string title,
        string message)
    {
        _messageBoxViewModel.Initialize(
            title,
            message,
            "Continue",
            "Cancel",
            DialogFunction.CONFIRMATION_DESTRUCTIVE);

        var tcs = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        
        IsDialogOpen = true;

        DialogManager
            .CreateDialog(_messageBoxViewModel)
            .WithMaxWidth(1000)
            .WithMinWidth(300)
            .Dismissible()
            .WithSuccessCallback(() =>
            {
                tcs.TrySetResult(true);
            })
            .WithCancelCallback(() =>
            {
                tcs.TrySetResult(false);
            })
            .Show();
    
        var result = await tcs.Task;
        
        IsDialogOpen = false;
        
        return result;
        
    }
    
    public async Task<bool> ShowConfirmationImageAsync(string title, string message, string imagePath, DialogFunction dialogFunction = DialogFunction.CONFIRMATION)
    {
        string okText;
        string cancelText;
        
        switch (dialogFunction)
        {
            case DialogFunction.CONFIRMATION:
                okText = "Continue";
                cancelText = "Cancel";
                break;
            case DialogFunction.USER_INPUT:
                okText = "Pass";
                cancelText = "Fail";
                break;
            default:
                return false;
        }
        
        var bitmap = null as Bitmap;
        if (!string.IsNullOrWhiteSpace(imagePath))
        {
            if (File.Exists(imagePath))
                bitmap = new Bitmap(imagePath);
            else
                _errorService.AddError($"Image not found: {imagePath}");
        }
        
        _controlModuleService.SetButtonColor(0, ControlModuleColors.LED_MODE_GREEN);
        _controlModuleService.SetButtonColor(1, ControlModuleColors.LED_MODE_RED);
        _controlModuleService.SetUserResponseMode(true);
            
        _messageBoxViewModel.Initialize(
            title,
            message,
            okText,
            cancelText,
            dialogFunction,
            bitmap);

        var tcs = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        
        IsDialogOpen = true;
        
        DialogManager
            .CreateDialog(_messageBoxViewModel)
            .WithMaxWidth(1000)
            .WithMinWidth(300)
            .Dismissible()
            .WithSuccessCallback(() =>
            {
                tcs.TrySetResult(true);
            })
            .WithCancelCallback(() =>
            {
                tcs.TrySetResult(false);
            })
            .Show();

        var result = await tcs.Task;
        
        IsDialogOpen = false;
        
        _controlModuleService.SetButtonColor(0, ControlModuleColors.LED_MODE_OFF);
        _controlModuleService.SetButtonColor(1, ControlModuleColors.LED_MODE_OFF);
        _controlModuleService.SetUserResponseMode(false);
        
        return result;
    }

    public async Task ShowMessageAsync(string title, string message)
    {
        _messageBoxViewModel.Initialize(
            title,
            message);
        
        var tcs = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        
        IsDialogOpen = true;

        DialogManager
            .CreateDialog(_messageBoxViewModel)
            .WithMaxWidth(1000)
            .WithMinWidth(300)
            .Dismissible()
            .WithSuccessCallback(() =>
            {
                tcs.TrySetResult(true);
            })
            .WithCancelCallback(() =>
            {
                tcs.TrySetResult(false);
            })
            .Show();
        
        await tcs.Task;
        
        IsDialogOpen = false;
    }
}