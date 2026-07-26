using System;
using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;
using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IErrorService _errorService;
    private readonly ISimulationService _simulationService;
    private readonly IProjectFileService _projectFileService;
    private readonly ISettingsService _settingsService;
    private readonly ProjectModel _projectModel;

    public string WindowTitle => $"ATLab - Project: {_projectModel.ProjectName}{(_projectModel.IsDirty ? "*" : "")}";
    
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
        IProjectFileService projectFileService,
        TestHardwareRelayChannelsViewModel testHardwareRelayChannelsViewModel, 
        TestingTabViewModel testingTab, 
        ConfigTabViewModel configTab,
        ScriptingTabViewModel scriptingTabViewModel,
        AboutTabViewModel aboutTab,
        HardwareTabViewModel hardwareTab,
        DocumentationTabViewModel documentationTab,
        ISettingsService settingsService,
        ProjectModel projectModel)
    {
        _errorService = errorService;
        _simulationService = simulationService;
        _projectFileService = projectFileService;
        TestHardwareRelayChannelsViewModel = testHardwareRelayChannelsViewModel;
        _settingsService = settingsService;
        _projectModel = projectModel;

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
        
        _errorService.Errors.CollectionChanged += (_, __) =>
        {
            ErrorCount += 1; // Only show number of new errors
            HasErrors = ErrorCount > 0;
        };

        _projectModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName is nameof(ProjectModel.FilePath) or
                nameof(ProjectModel.IsDirty))
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
        if (await _projectFileService.ConfirmAndContinueIfDirtyAsync())
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
        await ScriptTab.ReloadScriptsCommand.ExecuteAsync(null);
        
        var args = Environment.GetCommandLineArgs();
        var fileFromArgs = args.Length > 1 ? args[1] : null;

        if (!string.IsNullOrWhiteSpace(fileFromArgs) && File.Exists(fileFromArgs))
        {
            try
            {
                await LoadFile(fileFromArgs);
                return;
            }
            catch (Exception ex)
            {
                _errorService.Errors.Add(ex.ToString());
                await NewFile();
                return;
            }
        }
        
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