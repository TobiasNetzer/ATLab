using System.IO;
using System.Threading.Tasks;
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
    public MessageBoxService(IErrorService errorService)
    {
        _errorService = errorService;
    }
    
    private Window? GetMainWindow()
    {
        var desktop = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        return desktop?.MainWindow;
    }

    public async Task<bool> ShowConfirmationAsync(string title, string message, string okText = "Ok", string cancelText = "Cancel")
    {
        var owner = GetMainWindow();
        if (owner == null) return false;

        var vm = new MessageBoxViewModel
        {
            Title = title,
            Message = message,
            OkText = okText,
            CancelText = cancelText,
            ShowCancel = true
        };

        var mb = new MessageBox
        {
            DataContext = vm
        };

        await mb.ShowDialog(owner);
        return mb.Result == MessageBox.MessageBoxResult.Ok;
    }
    
    public async Task<bool> ShowConfirmationImageAsync(string title, string message, string imagePath, string okText = "Ok", string cancelText = "Cancel")
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

        var vm = new MessageBoxViewModel
        {
            Title = title,
            Message = message,
            OkText = okText,
            CancelText = cancelText,
            ShowCancel = true,
            Bitmap = bitmap
        };

        var mb = new MessageBox
        {
            DataContext = vm
        };

        await mb.ShowDialog(owner);
        return mb.Result == MessageBox.MessageBoxResult.Ok;
    }

    public async Task ShowMessageAsync(string title, string message)
    {
        var owner = GetMainWindow();
        if (owner == null) return;

        var vm = new MessageBoxViewModel
        {
            Title = title,
            Message = message,
            ShowCancel = false
        };

        var mb = new MessageBox
        {
            DataContext = vm
        };

        await mb.ShowDialog(owner);
    }
}