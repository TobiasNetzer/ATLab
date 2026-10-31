using System.IO;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.ViewModels;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using ShadUI;

namespace ATLab.Services;

/// <summary>
/// Modal dialogs through the ShadUI dialog host. Dialogs of a test run can also be answered with the control
/// module.
/// </summary>
public partial class MessageBoxService : ObservableObject, IMessageBoxService
{
    private readonly ILoggingService _loggingService;
    private readonly ControlModuleService _controlModuleService;
    private readonly MessageBoxViewModel _messageBoxViewModel;
    
    [ObservableProperty]
    private DialogManager _dialogManager;
    
    [ObservableProperty]
    private bool _isDialogOpen;
    
    public MessageBoxService(ILoggingService loggingService,
        ControlModuleService controlModuleService,
        MessageBoxViewModel messageBoxViewModel,
        DialogManager dialogManager)
    {
        _loggingService = loggingService;
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
            LocalizationService.Instance["ButtonLabel_Continue"],
            LocalizationService.Instance["ButtonLabel_Cancel"],
            DialogFunction.CONFIRMATION_DESTRUCTIVE);

        var tcs = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        
        IsDialogOpen = true;

        DialogManager
            .CreateDialog(_messageBoxViewModel)
            .WithMaxWidth(1000)
            .WithMinWidth(300)
            .Dismissible()
            .WithSuccessCallback(() => tcs.TrySetResult(true))
            .WithCancelCallback(() => tcs.TrySetResult(false))
            .Show();
    
        var result = await tcs.Task;
        
        IsDialogOpen = false;
        
        return result;
        
    }
    
    public Task<bool> ShowConfirmationImageAsync(
        string title,
        string message,
        string imagePath,
        DialogFunction dialogFunction = DialogFunction.CONFIRMATION)
    {
        return ShowImageDialogAsync(
            title,
            message,
            imagePath,
            string.Empty,
            false,
            dialogFunction);
    }

    public Task<bool> ShowResultConfirmationImageAsync(
        string title,
        string message,
        string imagePath,
        string? resultText,
        bool isPassed = false,
        DialogFunction dialogFunction = DialogFunction.CONFIRMATION)
    {
        return ShowImageDialogAsync(
            title,
            message,
            imagePath,
            resultText,
            isPassed,
            dialogFunction);
    }
    
    public async Task<string?> ShowInputImageAsync(
        string title,
        string message,
        string imagePath)
    {
        var result = await ShowImageDialogAsync(
            title,
            message,
            imagePath,
            string.Empty,
            false,
            DialogFunction.USER_INPUT);

        return result ? _messageBoxViewModel.InputText : null;
    }

    public async Task<string?> ShowPasswordInputAsync(string title, string message)
    {
        _messageBoxViewModel.Initialize(
            title,
            message,
            LocalizationService.Instance["ButtonLabel_Continue"],
            LocalizationService.Instance["ButtonLabel_Cancel"],
            DialogFunction.PASSWORD_INPUT);

        var tcs = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        IsDialogOpen = true;

        DialogManager
            .CreateDialog(_messageBoxViewModel)
            .WithMaxWidth(1000)
            .WithMinWidth(300)
            .Dismissible()
            .WithSuccessCallback(() => tcs.TrySetResult(true))
            .WithCancelCallback(() => tcs.TrySetResult(false))
            .Show();

        var result = await tcs.Task;

        IsDialogOpen = false;

        var password = _messageBoxViewModel.InputText;
        _messageBoxViewModel.InputText = string.Empty;

        return result ? password : null;
    }

    private async Task<bool> ShowImageDialogAsync(
        string title,
        string message,
        string imagePath,
        string? resultText,
        bool isPassed,
        DialogFunction dialogFunction)
    {
        string okText;
        string cancelText;

        switch (dialogFunction)
        {
            case DialogFunction.CONFIRMATION:
                okText = LocalizationService.Instance["ButtonLabel_Continue"];
                cancelText = LocalizationService.Instance["ButtonLabel_Cancel"];
                break;

            case DialogFunction.USER_CONFIRMATION:
                okText = LocalizationService.Instance["ButtonLabel_Pass"];
                cancelText = LocalizationService.Instance["ButtonLabel_Fail"];
                break;

            case DialogFunction.USER_INPUT:
                okText = LocalizationService.Instance["ButtonLabel_Continue"];
                cancelText = LocalizationService.Instance["ButtonLabel_Cancel"];
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
                _loggingService.Error($"Image not found: {imagePath}");
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
            bitmap,
            resultText,
            isPassed);

        var tcs = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        IsDialogOpen = true;

        DialogManager
            .CreateDialog(_messageBoxViewModel)
            .WithMaxWidth(1000)
            .WithMinWidth(300)
            .Dismissible()
            .WithSuccessCallback(() => tcs.TrySetResult(true))
            .WithCancelCallback(() => tcs.TrySetResult(false))
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
            message,
            LocalizationService.Instance["ButtonLabel_Continue"],
            LocalizationService.Instance["ButtonLabel_Cancel"]);
        
        var tcs = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        
        IsDialogOpen = true;

        DialogManager
            .CreateDialog(_messageBoxViewModel)
            .WithMaxWidth(1000)
            .WithMinWidth(300)
            .Dismissible()
            .WithSuccessCallback(() => tcs.TrySetResult(true))
            .WithCancelCallback(() => tcs.TrySetResult(false))
            .Show();
        
        await tcs.Task;
        
        IsDialogOpen = false;
    }
}