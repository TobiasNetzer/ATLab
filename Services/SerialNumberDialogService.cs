using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using ATLab.ViewModels;
using ATLab.Views;
using ATLab.Interfaces;

namespace ATLab.Services;

public class SerialNumberDialogService : ISerialNumberDialogService
{
    private Window? GetMainWindow()
    {
        var desktop = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        return desktop?.MainWindow;
    }

    public async Task<string?> AskForSerialNumberAsync()
    {
        var owner = GetMainWindow();
        if (owner == null) return null;

        var vm = new SerialNumberEntryBoxViewModel();
        var dialog = new SerialNumberEntryBox
        {
            DataContext = vm
        };

        var result = await dialog.ShowDialog<bool?>(owner);

        return result == true ? vm.SerialNumber : null;
    }
}