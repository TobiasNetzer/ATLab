using System.Threading.Tasks;
using ATLab.Enums;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using ATLab.ViewModels;
using ATLab.Views;
using ATLab.Interfaces;

namespace ATLab.Services;

public class SerialNumberDialogService : ISerialNumberDialogService
{
    private readonly SerialNumberEntryWindowViewModel _serialNumberEntryWindowViewModel;
    private readonly ControlModuleService _controlModuleService;

    public SerialNumberDialogService(
        SerialNumberEntryWindowViewModel serialNumberEntryWindowViewModel,
        ControlModuleService controlModuleService)
    {
        _serialNumberEntryWindowViewModel = serialNumberEntryWindowViewModel;
        _controlModuleService = controlModuleService;
    }
    private Window? GetMainWindow()
    {
        var desktop = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        return desktop?.MainWindow;
    }

    public async Task<string?> AskForSerialNumberAsync()
    {
        var owner = GetMainWindow();
        if (owner == null) return null;
        
        _controlModuleService.SetButtonColor(0, ControlModuleColors.LED_MODE_GREEN);
        _controlModuleService.SetButtonColor(1, ControlModuleColors.LED_MODE_RED);
        _controlModuleService.SetUserResponseMode(true);

        var dialog = new SerialNumberEntryWindow
        {
            DataContext = _serialNumberEntryWindowViewModel
        };

        var result = await dialog.ShowDialog<bool?>(owner);
        
        _controlModuleService.SetButtonColor(0, ControlModuleColors.LED_MODE_OFF);
        _controlModuleService.SetButtonColor(1, ControlModuleColors.LED_MODE_OFF);
        _controlModuleService.SetUserResponseMode(false);

        var serialNumber = _serialNumberEntryWindowViewModel.SerialNumber;
        _serialNumberEntryWindowViewModel.SerialNumber = string.Empty;

        return result == true ? serialNumber : null;
    }
}