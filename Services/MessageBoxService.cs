using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Views;
using ATLab.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace ATLab.Services;

public class MessageBoxService : IMessageBoxService
{
    private Window? GetMainWindow()
    {
        var desktop = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        return desktop?.MainWindow;
    }

    public async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var owner = GetMainWindow();
        if (owner == null) return false;

        var vm = new MessageBoxViewModel
        {
            Title = title,
            Message = message,
            ShowCancel = true
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
