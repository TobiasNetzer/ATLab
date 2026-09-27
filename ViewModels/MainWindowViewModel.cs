using System;
using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.Records;
using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using ShadUI;

namespace ATLab.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ITestHardware _testHardware;
    private readonly ILoggingService _loggingService;
    private readonly ISettingsService _settingsService;
    private readonly ProjectModel _projectModel;
    private readonly ApplicationState _applicationState;

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
    
    public bool IsTestingTabSelected => ReferenceEquals(SelectedTab, TestingTab);
    public bool IsConfigTabSelected => ReferenceEquals(SelectedTab, ConfigTab);
    public bool IsDocumentationTabSelected => ReferenceEquals(SelectedTab, DocumentationTab);
    public bool IsScriptTabSelected => ReferenceEquals(SelectedTab, ScriptTab);
    public bool IsHardwareTabSelected => ReferenceEquals(SelectedTab, HardwareTab);
    public bool IsAboutTabSelected => ReferenceEquals(SelectedTab, AboutTab);

    public ObservableCollection<LogEntry> Events => _loggingService.Events;
    
    public bool IsSimulation => _applicationState.IsSimulationMode;

    [ObservableProperty]
    private string? _matrixChannel;
    
    [ObservableProperty]
    private DialogManager _dialogManager;
    
    [ObservableProperty]
    private ToastManager _toastManager;

    public MainWindowViewModel(ITestHardware testHardware,
        ILoggingService loggingService,
        TestHardwareRelayChannelsViewModel testHardwareRelayChannelsViewModel, 
        TestingTabViewModel testingTab, 
        ConfigTabViewModel configTab,
        ScriptingTabViewModel scriptingTabViewModel,
        AboutTabViewModel aboutTab,
        HardwareTabViewModel hardwareTab,
        DocumentationTabViewModel documentationTab,
        ISettingsService settingsService,
        ProjectModel projectModel,
        ApplicationState applicationState,
        DialogManager dialogManager,
        ToastManager toastManager)
    {
        _testHardware = testHardware;
        _loggingService = loggingService;
        TestHardwareRelayChannelsViewModel = testHardwareRelayChannelsViewModel;
        _settingsService = settingsService;
        _projectModel = projectModel;
        _applicationState = applicationState;
        DialogManager = dialogManager;
        ToastManager = toastManager;

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

        _projectModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName is nameof(ProjectModel.FilePath) or
                nameof(ProjectModel.IsDirty))
            {
                OnPropertyChanged(nameof(WindowTitle));
            }
        };
    }
    
    private async Task NewFile() => await TestingTab.NewFileCommand.ExecuteAsync(null);
    
    private async Task LoadFile(string fileToLoad) => await TestingTab.LoadFile(fileToLoad);
    
    [RelayCommand]
    private async Task FindMatrixChannel()
    {
        var result = await _testHardware.FindMeasChannel();

        if (result.IsSuccess)
        {
            MatrixChannel = result.Value == 0 ? "-" : result.Value.ToString();
        }
        else
        {
            _loggingService.Error(result.ErrorMessage);
            MatrixChannel = "External probe not detected";
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
        OnPropertyChanged(nameof(IsTestingTabSelected));
        OnPropertyChanged(nameof(IsConfigTabSelected));
        OnPropertyChanged(nameof(IsDocumentationTabSelected));
        OnPropertyChanged(nameof(IsScriptTabSelected));
        OnPropertyChanged(nameof(IsHardwareTabSelected));
        OnPropertyChanged(nameof(IsAboutTabSelected));
    }
    
    public async Task OnWindowOpened()
    {
        
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
                _loggingService.Error(ex.ToString());
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
                _loggingService.Error(ex.ToString());
                await NewFile();
            }
        }
        else
        {
            await NewFile();
        }
    }
}