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

    public string WindowTitle => $"ATLab - {(string.IsNullOrEmpty(TestingTab.CurrentFilePath) ? "Untitled" : Path.GetFileNameWithoutExtension(TestingTab.CurrentFilePath))}{(TestingTab.IsDirty ? "*" : "")}";
    
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
    
    [ObservableProperty]
    private ScpiScriptsManagerViewModel _scriptTab;

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
        ScpiScriptsManagerViewModel scpiScriptsManagerViewModel)
    {
        _errorService = errorService;
        TestHardwareRelayChannelsViewModel = testHardwareRelayChannelsViewModel;

        TestingTab = testingTab;
        LabTab = labTab;
        ConfigTab = configTab;
        ScriptTab = scpiScriptsManagerViewModel;

        _selectedTab = TestingTab;
        
        Tabs.Add(TestingTab);
        Tabs.Add(LabTab);
        Tabs.Add(ConfigTab);
        Tabs.Add(ScriptTab);
        
        _errorService.Errors.CollectionChanged += (_, __) =>
        {
            ErrorCount += 1; // Only show number of new errors
            HasErrors = ErrorCount > 0;
        };

        TestingTab.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName is nameof(TestingTabViewModel.CurrentFilePath) or nameof(TestingTabViewModel.IsDirty))
            {
                OnPropertyChanged(nameof(WindowTitle));
            }
        };
    }
    
    public MainWindowViewModel()
    {
        var testHardware = new CtiaHardware(new SimulationService());
        _errorService = new ErrorService();
        var configurator = new TestStepConfiguratorViewModel();
        TestHardwareRelayChannelsViewModel = new TestHardwareRelayChannelsViewModel(testHardware.HardwareInfo);
        
        TestingTab = new TestingTabViewModel();
        LabTab = new LabTabViewModel(_errorService, testHardware, TestHardwareRelayChannelsViewModel, new SimulationStateService { IsSimulationMode = true });
        ConfigTab = new ConfigTabViewModel(TestHardwareRelayChannelsViewModel, configurator);
        ScriptTab = new ScpiScriptsManagerViewModel(new FileScpiScriptRepository(@"C:\Users\Tobias\Desktop"));

        _selectedTab = TestingTab;
        
        Tabs.Add(TestingTab);
        Tabs.Add(LabTab);
        Tabs.Add(ConfigTab);
        Tabs.Add(ScriptTab);
        
        _errorService.Errors.CollectionChanged += (_, __) =>
        {
            ErrorCount += 1; // Only show number of new errors
            HasErrors = ErrorCount > 0;
        };

    }

    [RelayCommand]
    private async Task OpenAboutWindow() => await TestingTab.OpenAboutWindow();
    
    partial void OnIsErrorFlyoutOpenChanged(bool value)
    {
        if (value)
        {
            ErrorCount = 0;
            HasErrors = false;
        }
    }

    [RelayCommand]
    private async Task NewFile() => await TestingTab.NewFile();

    [RelayCommand]
    private async Task SaveFileAs() => await TestingTab.SaveFileAs();
    
    [RelayCommand]
    private async Task SaveFile() => await TestingTab.SaveFile();
    
    [RelayCommand]
    private async Task LoadFileWithDialog() => await TestingTab.LoadFileWithDialog();
    
    public async Task LoadFile(string fileToLoad) => await TestingTab.LoadFile(fileToLoad);
    
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
                TestingTab.SelectedStepIndex = 0;
                break;
            case ScpiScriptsManagerViewModel:
                 ScriptTab.ReloadScriptsCommand.Execute(null);
                 break;
        }
    }
}
