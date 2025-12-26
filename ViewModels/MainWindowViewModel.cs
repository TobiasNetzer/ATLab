using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using ATLab.Views;
using System.Threading.Tasks;
using ATLab.CTIA;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.Services;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IErrorService _errorService;
    private readonly ITestHardware _testHardware;
    private readonly ISettingsService _settingsService;

    public string WindowTitle => $"ATLab - {(string.IsNullOrEmpty(TestingTab.TestStepPresenter.CurrentFilePath) ? "Untitled" : Path.GetFileNameWithoutExtension(TestingTab.TestStepPresenter.CurrentFilePath))}{(TestingTab.TestStepPresenter.IsDirty ? "*" : "")}";
    
    [ObservableProperty]
    private TestHardwareRelayChannelsViewModel _testHardwareRelayChannelsViewModel;

    [ObservableProperty]
    private ViewModelBase _selectedTab;

    [ObservableProperty]
    private TestingTabViewModel _testingTab;

    [ObservableProperty]
    private LabTabViewModel _labTab;
    
    [ObservableProperty]
    private ConfigTabViewModel _configTab;

    public ObservableCollection<ViewModelBase> Tabs { get; } = new();

    public ObservableCollection<string> Errors => _errorService.Errors;

    [ObservableProperty]
    private int _errorCount;

    [ObservableProperty]
    private bool _hasErrors;
    
    [ObservableProperty]
    private bool _isErrorFlyoutOpen;

    public MainWindowViewModel(IErrorService errorService,
        ITestHardware testHardware, 
        TestHardwareRelayChannelsViewModel testHardwareRelayChannelsViewModel, 
        TestingTabViewModel testingTab, 
        LabTabViewModel labTab, 
        ConfigTabViewModel configTab,
        ISettingsService settingsService)
    {
        _errorService = errorService;
        _testHardware = testHardware;
        _settingsService = settingsService;
        TestHardwareRelayChannelsViewModel = testHardwareRelayChannelsViewModel;

        TestingTab = testingTab;
        LabTab = labTab;
        ConfigTab = configTab;

        _selectedTab = TestingTab;
        
        Tabs.Add(TestingTab);
        Tabs.Add(LabTab);
        Tabs.Add(ConfigTab);
        
        _errorService.Errors.CollectionChanged += (_, __) =>
        {
            ErrorCount += 1; // Only show number of new errors
            HasErrors = ErrorCount > 0;
        };

        TestingTab.TestStepPresenter.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName is nameof(TestStepPresenterViewModel.CurrentFilePath) or nameof(TestStepPresenterViewModel.IsDirty))
            {
                OnPropertyChanged(nameof(WindowTitle));
            }
        };
    }
    
    public MainWindowViewModel()
    {
        _testHardware = new CtiaHardware(new SimulationService());
        _errorService = new ErrorService();
        _settingsService = new SettingsService();
        var configurator = new TestStepConfiguratorViewModel();
        TestHardwareRelayChannelsViewModel = new TestHardwareRelayChannelsViewModel(_testHardware.HardwareInfo);
        
        TestStepPresenterViewModel testStepPresenter = new TestStepPresenterViewModel(
            _errorService, 
            TestHardwareRelayChannelsViewModel, 
            new TestExecutor(new DummyTestStepRunner()), 
            configurator,
            new FileDialogService(),
            _settingsService,
            new FileService(),
            new MessageBoxService());
            
        TestingTab = new TestingTabViewModel(_errorService, TestConfigurationViewModel, testStepPresenter);
        LabTab = new LabTabViewModel(_errorService, _testHardware, TestConfigurationViewModel, new SimulationStateService { IsSimulationMode = true });
        ConfigTab = new ConfigTabViewModel(TestConfigurationViewModel);

        _selectedTab = TestingTab;
        
        Tabs.Add(TestingTab);
        Tabs.Add(LabTab);
        Tabs.Add(ConfigTab);
        
        _errorService.Errors.CollectionChanged += (_, __) =>
        {
            ErrorCount += 1; // Only show number of new errors
            HasErrors = ErrorCount > 0;
        };

    }

    [RelayCommand]
    private async Task OpenAboutWindow() => await TestingTab.TestStepPresenter.OpenAboutWindow();
    
    partial void OnIsErrorFlyoutOpenChanged(bool value)
    {
        if (value)
        {
            ErrorCount = 0;
            HasErrors = false;
        }
    }

    [RelayCommand]
    private async Task NewFile() => await TestingTab.TestStepPresenter.NewFile();

    [RelayCommand]
    private async Task SaveFileAs() => await TestingTab.TestStepPresenter.SaveFileAs();
    
    [RelayCommand]
    private async Task SaveFile() => await TestingTab.TestStepPresenter.SaveFile();
    
    [RelayCommand]
    private async Task LoadFileWithDialog() => await TestingTab.TestStepPresenter.LoadFileWithDialog();
    
    public async Task LoadFile(string fileToLoad) => await TestingTab.TestStepPresenter.LoadFile(fileToLoad);
    
    public event Action? RequestClose;
    
    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke();
    }

    partial void OnSelectedTabChanged(ViewModelBase value)
    {
        switch (value)
        {
            case LabTabViewModel:
                LabTab.LoadLabTabState();
                break;
            case TestingTabViewModel:
                TestingTab.TestStepPresenter.SelectedStepIndex = 0;
                break;
        }
    }
}
