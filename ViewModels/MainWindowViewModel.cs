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

namespace ATLab.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private CTIAController _cTIA;

    public ObservableCollection<ViewModelBase> Tabs { get; set; }

    [ObservableProperty]
    private ViewModelBase? selectedTab;

    [ObservableProperty]
    private TestingTabViewModel? testingTab;

        [ObservableProperty]
    private LabTabViewModel? labTab;

    public event EventHandler<bool>? StateChanged;

    [ObservableProperty]
    private bool state;

    partial void OnStateChanged(bool value)
    {
        StateChanged?.Invoke(this, value);
        _ = SetStim();
    }

    public MainWindowViewModel(CTIAController cTIA)
    {
        _cTIA = cTIA;
        Tabs = new ObservableCollection<ViewModelBase>
        {
            new TestingTabViewModel { Title = "Testing" },
            new LabTabViewModel { Title = "Lab" }
        };
        SelectedTab = Tabs.FirstOrDefault();

        testingTab = new TestingTabViewModel();
        labTab = new LabTabViewModel();
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

    [RelayCommand]
    public async Task SetStim()
    {
        if(State)
            await _cTIA.Set.SetExtStimCh(1);
        else await _cTIA.Clr.ClearExtStimCh(1);
    }
}
