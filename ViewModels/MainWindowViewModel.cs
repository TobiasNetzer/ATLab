using ATLab.Services;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using ATLab.Views;
using System;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;
using ATLab.CTIA;

namespace ATLab.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private ITestHardware _testHardware;

    [ObservableProperty]
    private ViewModelBase? selectedTab;

    [ObservableProperty]
    private TestingTabViewModel testingTab;

    [ObservableProperty]
    private StimChannelViewModel labTab;

    public MainWindowViewModel(ITestHardware testHardware)
    {
        _testHardware = testHardware;

        testingTab = new TestingTabViewModel();
        labTab = new StimChannelViewModel(_testHardware);
    }

    [RelayCommand]
    public async Task OpenAboutWindow()
    {
        var deviceInfoProvider = _testHardware.HardwareInfo;
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
