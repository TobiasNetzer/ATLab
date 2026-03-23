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

    public TestingTabViewModel TestingTab { get; }
    public ConfigTabViewModel ConfigTab { get; }
    public ScriptsManagerViewModel ScriptTab { get; }
    public AboutTabViewModel AboutTab { get; }
    public HardwareTabViewModel HardwareTab { get; }

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
        ConfigTabViewModel configTab,
        ScriptsManagerViewModel scriptsManagerViewModel,
        AboutTabViewModel aboutTab,
        HardwareTabViewModel hardwareTab)
    {
        _errorService = errorService;
        _simulationService = simulationService;
        _projectService = projectService;
        TestHardwareRelayChannelsViewModel = testHardwareRelayChannelsViewModel;

        TestingTab = testingTab;
        ConfigTab = configTab;
        ScriptTab = scriptsManagerViewModel;
        AboutTab = aboutTab;
        HardwareTab = hardwareTab;

        _selectedTab = TestingTab;
        
        Tabs.Add(TestingTab);
        Tabs.Add(ConfigTab);
        Tabs.Add(ScriptTab);
        Tabs.Add(HardwareTab);
        Tabs.Add(AboutTab);
        
        ScriptTab.ReloadScriptsCommand.ExecuteAsync(null);
        
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
    
    partial void OnIsErrorFlyoutOpenChanged(bool value)
    {
        if (!value)
            return;
        
        ErrorCount = 0;
        HasErrors = false;
    }
    
    public async Task NewFile() => await TestingTab.NewFile();
    
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
    private static void ToggleTheme()
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
            case TestingTabViewModel:
                TestingTab.SelectedStepIndex = 0;
                break;
        }
    }
}
