using System.IO;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Views;
using ATLab.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;

namespace ATLab.Services;

public class MessageBoxService : IMessageBoxService
{
    private readonly IErrorService _errorService;
    private readonly ControlModuleService _controlModuleService;
    
    public MessageBoxService(IErrorService errorService,
        ControlModuleService controlModuleService)
    {
        _errorService = errorService;
        _controlModuleService = controlModuleService;
    }
    
    private Window? GetMainWindow()
    {
        var desktop = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        return desktop?.MainWindow;
    }

    public async Task<bool> ShowConfirmationAsync(string title, string message, string okText = "Ok", string cancelText = "Cancel", bool useControlModule = false)
    {
        var owner = GetMainWindow();
        if (owner == null) return false;

        if (useControlModule)
        {
            _controlModuleService.SetButtonColor(0, ControlModuleColors.LED_MODE_GREEN);
            _controlModuleService.SetButtonColor(1, ControlModuleColors.LED_MODE_RED);
            _controlModuleService.SetUserResponseMode(true);
        }

        using var vm = new MessageBoxViewModel(_controlModuleService);
        vm.Title = title;
        vm.Message = message;
        vm.OkText = okText;
        vm.CancelText = cancelText;
        vm.ShowCancel = true;

        var mb = new MessageBox
        {
            DataContext = vm
        };

        await mb.ShowDialog(owner);

        if (useControlModule)
        {
            _controlModuleService.SetButtonColor(0, ControlModuleColors.LED_MODE_OFF);
            _controlModuleService.SetButtonColor(1, ControlModuleColors.LED_MODE_OFF);
            _controlModuleService.SetUserResponseMode(false);
        }

        return mb.Result == MessageBox.MessageBoxResult.Ok;
    }
    
    public async Task<bool> ShowConfirmationImageAsync(string title, string message, string imagePath, string okText = "Ok", string cancelText = "Cancel", bool useControlModule = false)
    {
        var owner = GetMainWindow();
        if (owner == null) return false;
        
        var bitmap = null as Bitmap;
        if (!string.IsNullOrWhiteSpace(imagePath))
        {
            if (File.Exists(imagePath))
                bitmap = new Bitmap(imagePath);
            else
                _errorService.AddError($"Image not found: {imagePath}");
        }

        if (useControlModule)
        {
            _controlModuleService.SetButtonColor(0, ControlModuleColors.LED_MODE_GREEN);
            _controlModuleService.SetButtonColor(1, ControlModuleColors.LED_MODE_RED);
            _controlModuleService.SetUserResponseMode(true);
        }
        

        using var vm = new MessageBoxViewModel(_controlModuleService);
        vm.Title = title;
        vm.Message = message;
        vm.OkText = okText;
        vm.CancelText = cancelText;
        vm.ShowCancel = true;
        vm.Bitmap = bitmap;

        var mb = new MessageBox
        {
            DataContext = vm
        };

        await mb.ShowDialog(owner);

        if (useControlModule)
        {
            _controlModuleService.SetButtonColor(0, ControlModuleColors.LED_MODE_OFF);
            _controlModuleService.SetButtonColor(1, ControlModuleColors.LED_MODE_OFF);
            _controlModuleService.SetUserResponseMode(false);
        }
        
        return mb.Result == MessageBox.MessageBoxResult.Ok;
    }

    public async Task ShowMessageAsync(string title, string message, bool useControlModule = false)
    {
        var owner = GetMainWindow();
        if (owner == null) return;

        if (useControlModule)
        {
            _controlModuleService.SetButtonColor(0, ControlModuleColors.LED_MODE_GREEN);
            _controlModuleService.SetUserResponseMode(true);
        }
        
        using var vm = new MessageBoxViewModel(_controlModuleService);
        vm.Title = title;
        vm.Message = message;
        vm.ShowCancel = false;

        var mb = new MessageBox
        {
            DataContext = vm
        };

        await mb.ShowDialog(owner);

        if (useControlModule)
        {
            _controlModuleService.SetButtonColor(0, ControlModuleColors.LED_MODE_OFF);
            _controlModuleService.SetUserResponseMode(false);
        }
    }
}