using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using ATLab.Views;
using System.Threading.Tasks;
using ATLab.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ITestHardware _testHardware;
    
    private TestHardwareRelayChannelsViewModel _testHardwareRelayChannelsViewModel;
    
    [ObservableProperty]
    private string _statusMessage;

    [ObservableProperty]
    private ViewModelBase? _selectedTab;

    [ObservableProperty]
    private Tabs.TestingTabViewModel _testingTab;

    [ObservableProperty]
    private Tabs.LabTabViewModel _labTab;
    
    [ObservableProperty]
    private Tabs.ConfigTabViewModel _configTab;

    public MainWindowViewModel(ITestHardware testHardware)
    {
        _testHardware = testHardware;
        _testHardwareRelayChannelsViewModel = new TestHardwareRelayChannelsViewModel(_testHardware);

        TestingTab = new Tabs.TestingTabViewModel();
        LabTab = new Tabs.LabTabViewModel(_testHardware, _testHardwareRelayChannelsViewModel);
        ConfigTab = new Tabs.ConfigTabViewModel(_testHardwareRelayChannelsViewModel);
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
