using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.ViewModels;
using ATLab.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using ShadUI;

namespace ATLab.Services;

public partial class SerialNumberDialogService : ObservableObject, ISerialNumberDialogService
{
    private readonly SerialNumberEntryDialogViewModel _serialNumberEntryDialogViewModel;
    private readonly ControlModuleService _controlModuleService;
    
    [ObservableProperty]
    private DialogManager _dialogManager;
    
    [ObservableProperty]
    private bool _isDialogOpen;

    public SerialNumberDialogService(
        SerialNumberEntryDialogViewModel serialNumberEntryDialogViewModel,
        ControlModuleService controlModuleService,
        DialogManager dialogManager)
    {
        _serialNumberEntryDialogViewModel = serialNumberEntryDialogViewModel;
        _controlModuleService = controlModuleService;
        DialogManager = dialogManager;
    }

    public async Task<string?> AskForSerialNumberAsync()
    {
        
        _controlModuleService.SetButtonColor(0, ControlModuleColors.LED_MODE_GREEN);
        _controlModuleService.SetButtonColor(1, ControlModuleColors.LED_MODE_RED);
        _controlModuleService.SetUserResponseMode(true);
        
        IsDialogOpen = true;
        
        var tcs = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        
        DialogManager
            .CreateDialog(_serialNumberEntryDialogViewModel)
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

        var serialNumber = _serialNumberEntryDialogViewModel.SerialNumber;
        _serialNumberEntryDialogViewModel.SerialNumber = string.Empty;

        return result ? serialNumber : null;
    }
}