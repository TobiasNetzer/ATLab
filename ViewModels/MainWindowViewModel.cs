using ATLab.Services;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using ATLab.Views;
using System.Threading.Tasks;
using ATLab.Interfaces;
using System;

namespace ATLab.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private CTIAController _cTIA;

    public MainWindowViewModel(CTIAController cTIA)
    {
        _cTIA = cTIA;
    }

    [RelayCommand]
    public async Task OpenAboutWindow()
    {
        var deviceInfoProvider = (_cTIA as IDeviceInfoProvider) ?? new EmptyDeviceInfoProvider();
        var aboutVM = new AboutWindowViewModel(deviceInfoProvider);
        var aboutWindow = new AboutWindow
        {
            DataContext = aboutVM
        };

        var desktop = Avalonia.Application.Current?.ApplicationLifetime
            as IClassicDesktopStyleApplicationLifetime;

        if (desktop?.MainWindow != null)
            await aboutWindow.ShowDialog(desktop.MainWindow);
        else
            aboutWindow.Show();
    }

}
