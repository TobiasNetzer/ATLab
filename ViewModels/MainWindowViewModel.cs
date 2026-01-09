using System;
using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using ATLab.Interfaces;
using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IErrorService _errorService;
    private readonly ISimulationService _simulationService;
    private readonly IProjectService _projectService;

    public string WindowTitle => $"ATLab - Project: {(string.IsNullOrEmpty(_projectService.CurrentFilePath) ? "Untitled" : Path.GetFileNameWithoutExtension(_projectService.CurrentFilePath))}{(_projectService.IsDirty ? "*" : "")}";
    
    [ObservableProperty]
    private TestHardwareRelayChannelsViewModel _testHardwareRelayChannelsViewModel;

    [ObservableProperty]
    private ViewModelBase _selectedTab;

    [ObservableProperty]
    private TestingTabViewModel _testingTab;

    [ObservableProperty]
    private TestBenchViewModel _testBench;
    
    [ObservableProperty]
    private ConfigTabViewModel _configTab;
    
    [ObservableProperty]
    private ScriptsManagerViewModel _scriptTab;

    public ObservableCollection<ViewModelBase> Tabs { get; } = new();

    public ObservableCollection<string> Errors => _errorService.Errors;
    
    public bool IsSimulation => _simulationService.IsSimulationMode;

    [ObservableProperty]
    private int _errorCount;

    [ObservableProperty]
    private bool _hasErrors;
    
    [ObservableProperty]
    private bool _isErrorFlyoutOpen;

    public MainWindowViewModel(IErrorService errorService,
        ISimulationService simulationService,
        IProjectService projectService,
        TestHardwareRelayChannelsViewModel testHardwareRelayChannelsViewModel, 
        TestingTabViewModel testingTab, 
        TestBenchViewModel testBench, 
        ConfigTabViewModel configTab,
        ScriptsManagerViewModel scriptsManagerViewModel)
    {
        _errorService = errorService;
        _simulationService = simulationService;
        _projectService = projectService;
        TestHardwareRelayChannelsViewModel = testHardwareRelayChannelsViewModel;

        TestingTab = testingTab;
        TestBench = testBench;
        ConfigTab = configTab;
        ScriptTab = scriptsManagerViewModel;

        _selectedTab = TestingTab;
        
        Tabs.Add(TestingTab);
        Tabs.Add(TestBench);
        Tabs.Add(ConfigTab);
        Tabs.Add(ScriptTab);
        
        _errorService.Errors.CollectionChanged += (_, __) =>
        {
            ErrorCount += 1; // Only show number of new errors
            HasErrors = ErrorCount > 0;
        };

        _projectService.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName is nameof(IProjectService.CurrentFilePath) or nameof(IProjectService.IsDirty))
            {
                OnPropertyChanged(nameof(WindowTitle));
            }
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
    
    [RelayCommand]
    private void CancelTest() => TestingTab.CancelTestCommand.Execute(null);

    [RelayCommand]
    private async Task StartTest() => await TestingTab.StartTestCommand.ExecuteAsync(null);
    
    [RelayCommand]
    private async Task StartSingleTest() => await TestingTab.StartSingleStepTestCommand.ExecuteAsync(null);
    
    public async Task LoadFile(string fileToLoad) => await TestingTab.LoadFile(fileToLoad);
    
    public event Action? RequestClose;
    
    [RelayCommand]
    private async Task Close()
    {
        if (await _projectService.ConfirmAndContinueIfDirtyAsync())
        {
            RequestClose?.Invoke();
        }
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        Application.Current!.RequestedThemeVariant =
            Application.Current!.RequestedThemeVariant == ThemeVariant.Light
                ? ThemeVariant.Dark
                : ThemeVariant.Light;
    }

    partial void OnSelectedTabChanged(ViewModelBase value)
    {
        switch (value)
        {
            case TestBenchViewModel:
                TestBench.LoadTestBenchTabState();
                break;
            case TestingTabViewModel:
                TestingTab.SelectedStepIndex = 0;
                break;
        }
    }
}
