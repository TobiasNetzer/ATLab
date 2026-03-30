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
    private readonly SerialNumberEntryWindowViewModel _serialNumberEntryWindowViewModel;

    public SerialNumberDialogService(SerialNumberEntryWindowViewModel serialNumberEntryWindowViewModel)
    {
        _serialNumberEntryWindowViewModel = serialNumberEntryWindowViewModel;
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

        var dialog = new SerialNumberEntryWindow
        {
            DataContext = _serialNumberEntryWindowViewModel
        };

        var result = await dialog.ShowDialog<bool?>(owner);

        var serialNumber = _serialNumberEntryWindowViewModel.SerialNumber;
        _serialNumberEntryWindowViewModel.SerialNumber = string.Empty;

        return result == true ? serialNumber : null;
    }
}