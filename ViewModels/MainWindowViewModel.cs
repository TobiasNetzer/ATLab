using System.Collections.ObjectModel;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using ATLab.Views;
using System.Threading.Tasks;
using ATLab.CTIA;
using ATLab.Interfaces;
using ATLab.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IErrorService _errorService;
    private readonly ITestHardware _testHardware;
    
    private TestHardwareRelayChannelsViewModel _testHardwareRelayChannelsViewModel;

    [ObservableProperty]
    private ViewModelBase? _selectedTab;

    [ObservableProperty]
    private Tabs.TestingTabViewModel _testingTab;

    [ObservableProperty]
    private Tabs.LabTabViewModel _labTab;
    
    [ObservableProperty]
    private Tabs.ConfigTabViewModel _configTab;
    
    public ObservableCollection<string> Errors => _errorService.Errors;

    [ObservableProperty]
    private int _errorCount;

    [ObservableProperty]
    private bool _hasErrors;
    
    [ObservableProperty]
    private bool _isErrorFlyoutOpen;

    public MainWindowViewModel(IErrorService errorService, ITestHardware testHardware)
    {
        _errorService = errorService;
        _testHardware = testHardware;
        _testHardwareRelayChannelsViewModel = new TestHardwareRelayChannelsViewModel(_testHardware.HardwareInfo);

        TestingTab = new Tabs.TestingTabViewModel(_errorService, _testHardware, _testHardwareRelayChannelsViewModel);
        LabTab = new Tabs.LabTabViewModel(_errorService, _testHardware, _testHardwareRelayChannelsViewModel);
        ConfigTab = new Tabs.ConfigTabViewModel(_testHardwareRelayChannelsViewModel);
        
        _errorService.Errors.CollectionChanged += (_, __) =>
        {
            ErrorCount += 1; // Only show number of new errors
            HasErrors = ErrorCount > 0;
        };
        
    }
    
    public MainWindowViewModel()
    {
        _testHardwareRelayChannelsViewModel = new TestHardwareRelayChannelsViewModel(_testHardware.HardwareInfo);
        _errorService = new ErrorService();
        
        TestingTab = new Tabs.TestingTabViewModel(_errorService, new CtiaHardware(new SimulationService()), _testHardwareRelayChannelsViewModel);
        LabTab = new Tabs.LabTabViewModel(_errorService, new CtiaHardware(new SimulationService()), _testHardwareRelayChannelsViewModel);
        ConfigTab = new Tabs.ConfigTabViewModel(_testHardwareRelayChannelsViewModel);
        
        _errorService.Errors.CollectionChanged += (_, __) =>
        {
            ErrorCount += 1; // Only show number of new errors
            HasErrors = ErrorCount > 0;
        };
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
    
    partial void OnIsErrorFlyoutOpenChanged(bool value)
    {
        if (value)
        {
            ErrorCount = 0;
            HasErrors = false;
        }
    }
}
