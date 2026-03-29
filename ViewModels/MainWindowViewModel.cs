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
    private readonly ISettingsService _settingsService;

    public string WindowTitle => $"ATLab - Project: {(string.IsNullOrEmpty(_projectService.CurrentFilePath) ? "Untitled" : Path.GetFileNameWithoutExtension(_projectService.CurrentFilePath))}{(_projectService.IsDirty ? "*" : "")}";
    
    [ObservableProperty]
    private TestHardwareRelayChannelsViewModel _testHardwareRelayChannelsViewModel;

    [ObservableProperty]
    private ViewModelBase _selectedTab;

    public TestingTabViewModel TestingTab { get; }
    public ConfigTabViewModel ConfigTab { get; }
    public ScriptingTabViewModel ScriptTab { get; }
    public AboutTabViewModel AboutTab { get; }
    public HardwareTabViewModel HardwareTab { get; }
    public DocumentationTabViewModel DocumentationTab { get; }

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
        ScriptingTabViewModel scriptingTabViewModel,
        AboutTabViewModel aboutTab,
        HardwareTabViewModel hardwareTab,
        DocumentationTabViewModel documentationTab,
        ISettingsService settingsService)
    {
        _errorService = errorService;
        _simulationService = simulationService;
        _projectService = projectService;
        TestHardwareRelayChannelsViewModel = testHardwareRelayChannelsViewModel;
        _settingsService = settingsService;

        TestingTab = testingTab;
        ConfigTab = configTab;
        ScriptTab = scriptingTabViewModel;
        AboutTab = aboutTab;
        HardwareTab = hardwareTab;
        DocumentationTab = documentationTab;

        _selectedTab = TestingTab;
        
        Tabs.Add(TestingTab);
        Tabs.Add(ConfigTab);
        Tabs.Add(DocumentationTab);
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
    
    private async Task NewFile() => await TestingTab.NewFile();
    
    private async Task LoadFile(string fileToLoad) => await TestingTab.LoadFile(fileToLoad);
    
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

        _settingsService.Settings.IsDarkMode = Application.Current!.RequestedThemeVariant == ThemeVariant.Dark;
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
    
    public async Task OnWindowOpened()
    {
        var lastFile = _settingsService.Settings.LastOpenedFile;

        if (File.Exists(lastFile))
        {
            try
            {
                await LoadFile(lastFile);
            }
            catch (Exception ex)
            {
                _errorService.Errors.Add(ex.ToString());
                await NewFile();
            }
        }
        else
        {
            await NewFile();
        }
    }
}
