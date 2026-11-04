using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.Records;
using CommunityToolkit.Mvvm.ComponentModel;
using ShadUI;

namespace ATLab.ViewModels;

/// <summary>
/// Shell of the main window: the tabs, the dialog and toast hosts, theme and language switching and the user guide.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ITestHardware _testHardware;
    private readonly ILoggingService _loggingService;
    private readonly ISettingsService _settingsService;
    private readonly ILocalizationService _localizationService;
    private readonly ProjectModel _projectModel;
    private readonly ApplicationState _applicationState;
    private readonly AppSettingsDialogViewModel _appSettingsDialog;
    private readonly IMessageBoxService _messageBoxService;

    private const string OnlineUserGuideUrl = "https://github.com/TobiasNetzer/ATLab/blob/main/docs/user-guide";

    public string WindowTitle => IsProjectOpen
        ? $"ATLab - {_projectModel.ProjectName}{(_projectModel.IsDirty ? "*" : "")}"
        : "ATLab";

    public bool IsProjectOpen => _projectModel.IsProjectOpen;

    [ObservableProperty]
    private TestHardwareRelayChannelsViewModel _testHardwareRelayChannelsViewModel;

    [ObservableProperty]
    private ViewModelBase _selectedTab;

    public TestingTabViewModel TestingTab { get; }
    public ConfigTabViewModel ConfigTab { get; }
    public ScriptingTabViewModel ScriptTab { get; }
    public AboutTabViewModel AboutTab { get; }
    public HardwareTabViewModel HardwareTab { get; }
    public BoundaryScanTabViewModel BoundaryScanTab { get; }
    public DocumentationTabViewModel DocumentationTab { get; }

    public ObservableCollection<ViewModelBase> Tabs { get; } = new();

    public bool IsTestingTabSelected => ReferenceEquals(SelectedTab, TestingTab);
    public bool IsTestingViewVisible => IsTestingTabSelected && IsProjectOpen;
    public bool ShowWelcome => IsTestingTabSelected && !IsProjectOpen && !IsLoadingProject;

    /// <summary>A project file is being opened, or the tabs are prepared for it; hidden while a message is shown.</summary>
    public bool IsLoadingProject => _applicationState.IsLoadingProject || IsPreparingViews;
    public bool ShowLoadingDialog => IsLoadingProject && !_messageBoxService.IsDialogOpen;
    public string LoadingProjectName => _applicationState.IsLoadingProject ? _applicationState.LoadingProjectName : _lastLoadingProjectName;

    private string _lastLoadingProjectName = string.Empty;

    /// <summary>
    /// The window lays out the tabs that are not shown, so the first click on one of them is not slow. The loading
    /// dialog stays meanwhile.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoadingProject), nameof(ShowLoadingDialog), nameof(ShowWelcome), nameof(LoadingProjectName))]
    private bool _isPreparingViews;

    /// <summary>Raised when a project was opened from a file, for the window to prepare its tabs.</summary>
    public event Action? ProjectOpened;
    public bool IsConfigTabSelected => ReferenceEquals(SelectedTab, ConfigTab);
    public bool IsDocumentationTabSelected => ReferenceEquals(SelectedTab, DocumentationTab);
    public bool IsScriptTabSelected => ReferenceEquals(SelectedTab, ScriptTab);
    public bool IsHardwareTabSelected => ReferenceEquals(SelectedTab, HardwareTab);
    public bool IsBoundaryScanTabSelected => ReferenceEquals(SelectedTab, BoundaryScanTab);
    public bool IsAboutTabSelected => ReferenceEquals(SelectedTab, AboutTab);

    public ObservableCollection<LogEntry> Events => _loggingService.Events;

    public bool IsSimulation => _applicationState.IsSimulationMode;

    [ObservableProperty]
    private string? _matrixChannel;

    [ObservableProperty]
    private DialogManager _dialogManager;

    [ObservableProperty]
    private ToastManager _toastManager;

    public MainWindowViewModel(
        IHardwareAccessor hardwareAccessor,
        ILoggingService loggingService,
        TestHardwareRelayChannelsViewModel testHardwareRelayChannelsViewModel, 
        TestingTabViewModel testingTab, 
        ConfigTabViewModel configTab,
        ScriptingTabViewModel scriptingTabViewModel,
        AboutTabViewModel aboutTab,
        HardwareTabViewModel hardwareTab,
        BoundaryScanTabViewModel boundaryScanTab,
        DocumentationTabViewModel documentationTab,
        ISettingsService settingsService,
        ILocalizationService localizationService,
        ProjectModel projectModel,
        ApplicationState applicationState,
        AppSettingsDialogViewModel appSettingsDialog,
        IMessageBoxService messageBoxService,
        DialogManager dialogManager,
        ToastManager toastManager)
    {
        _testHardware = hardwareAccessor.Hardware;
        _loggingService = loggingService;
        TestHardwareRelayChannelsViewModel = testHardwareRelayChannelsViewModel;
        _settingsService = settingsService;
        _localizationService = localizationService;
        _projectModel = projectModel;
        _applicationState = applicationState;
        _appSettingsDialog = appSettingsDialog;
        _messageBoxService = messageBoxService;
        DialogManager = dialogManager;
        ToastManager = toastManager;

        TestingTab = testingTab;
        ConfigTab = configTab;
        ScriptTab = scriptingTabViewModel;
        AboutTab = aboutTab;
        HardwareTab = hardwareTab;
        BoundaryScanTab = boundaryScanTab;
        DocumentationTab = documentationTab;

        _selectedTab = TestingTab;

        Tabs.Add(TestingTab);
        Tabs.Add(ConfigTab);
        Tabs.Add(DocumentationTab);
        Tabs.Add(ScriptTab);
        Tabs.Add(BoundaryScanTab);
        Tabs.Add(HardwareTab);
        Tabs.Add(AboutTab);

        UpdateTabAccess();

        _applicationState.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(ApplicationState.LoadingProjectPath))
            {
                // A file was read: the name stays in the dialog while the tabs are prepared
                if (_applicationState.LoadingProjectPath is { } path)
                    _lastLoadingProjectName = Path.GetFileName(path);
                else if (_projectModel.IsProjectOpen && !string.IsNullOrEmpty(_projectModel.FilePath))
                    ProjectOpened?.Invoke();

                OnPropertyChanged(nameof(IsLoadingProject));
                OnPropertyChanged(nameof(ShowLoadingDialog));
                OnPropertyChanged(nameof(LoadingProjectName));
                OnPropertyChanged(nameof(ShowWelcome));
            }
        };

        if (_messageBoxService is System.ComponentModel.INotifyPropertyChanged messageBoxNotifier)
        {
            messageBoxNotifier.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(IMessageBoxService.IsDialogOpen))
                    OnPropertyChanged(nameof(ShowLoadingDialog));
            };
        }

        _projectModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName is nameof(ProjectModel.FilePath) or
                nameof(ProjectModel.IsDirty))
            {
                OnPropertyChanged(nameof(WindowTitle));
            }
            else if (e.PropertyName is nameof(ProjectModel.IsProjectOpen))
            {
                OnPropertyChanged(nameof(WindowTitle));
                OnPropertyChanged(nameof(IsProjectOpen));
                OnPropertyChanged(nameof(IsTestingViewVisible));
                OnPropertyChanged(nameof(ShowWelcome));
                UpdateTabAccess();
            }
        };

        TestingTab.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName is nameof(TestingTabViewModel.TestStatus))
            {
                FindMatrixChannelCommand.NotifyCanExecuteChanged();
                OpenAppSettingsCommand.NotifyCanExecuteChanged();
            }
            else if (e.PropertyName is nameof(TestingTabViewModel.IsDevelopmentMode))
            {
                UpdateTabAccess();
            }
        };
    }

    private void UpdateTabAccess()
    {
        // The development tabs need an open project; without one only Test Execution (welcome screen) and Info remain
        var isEditable = TestingTab.IsDevelopmentMode && IsProjectOpen;

        foreach (var tab in Tabs)
        {
            tab.IsTabEnabled = isEditable || ReferenceEquals(tab, TestingTab) || ReferenceEquals(tab, AboutTab);
        }

        if (!SelectedTab.IsTabEnabled)
            SelectedTab = TestingTab;
    }

    private async Task LoadFile(string fileToLoad) => await TestingTab.LoadFile(fileToLoad);

    // Probing talks to the test hardware, which must not happen in the middle of a run
    private bool IsNotTestRunning() => TestingTab.TestStatus != TestStatus.RUNNING;

    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
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

    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private void OpenAppSettings()
    {
        _appSettingsDialog.Load();

        DialogManager
            .CreateDialog(_appSettingsDialog)
            .Dismissible()
            .Show();
    }

    [RelayCommand]
    private void OpenUserGuide()
    {
        var language = _localizationService.CurrentCulture.TwoLetterISOLanguageName
            .Equals("de", StringComparison.OrdinalIgnoreCase) ? "de" : "en";

        var docsFolder = Path.Combine(AppContext.BaseDirectory, "docs");
        var localizedGuide = Path.Combine(docsFolder, $"ATLab-User-Guide-{language}.pdf");
        var englishGuide = Path.Combine(docsFolder, "ATLab-User-Guide-en.pdf");

        string target;
        if (File.Exists(localizedGuide))
        {
            target = localizedGuide;
        }
        else if (File.Exists(englishGuide))
        {
            target = englishGuide;
        }
        else
        {
            _loggingService.Warning($"User guide not found in '{docsFolder}'. Opening the online user guide.");
            target = $"{OnlineUserGuideUrl}/{language}/README.md";
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = target,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _loggingService.Error($"Could not open the user guide '{target}': {ex.Message}");
        }
    }

    partial void OnSelectedTabChanged(ViewModelBase value)
    {
        OnPropertyChanged(nameof(IsTestingTabSelected));
        OnPropertyChanged(nameof(IsTestingViewVisible));
        OnPropertyChanged(nameof(ShowWelcome));
        OnPropertyChanged(nameof(IsConfigTabSelected));
        OnPropertyChanged(nameof(IsDocumentationTabSelected));
        OnPropertyChanged(nameof(IsScriptTabSelected));
        OnPropertyChanged(nameof(IsHardwareTabSelected));
        OnPropertyChanged(nameof(IsBoundaryScanTabSelected));
        OnPropertyChanged(nameof(IsAboutTabSelected));
    }

    /// <summary>
    /// Opens the project given on the command line, otherwise the last opened project. If neither opens, the welcome
    /// screen stays until the user creates or opens a project.
    /// </summary>
    public async Task OnWindowOpened()
    {
        var args = Environment.GetCommandLineArgs();
        var fileFromArgs = args.Length > 1 ? args[1] : null;

        if (!string.IsNullOrWhiteSpace(fileFromArgs) && await ExistsAsync(fileFromArgs))
        {
            await TryLoadFile(fileFromArgs);
            return;
        }

        var lastFile = _settingsService.Settings.LastOpenedFile;

        if (!string.IsNullOrWhiteSpace(lastFile) && await ExistsAsync(lastFile))
            await TryLoadFile(lastFile);
    }

    /// <summary>
    /// Checks a file off the UI thread: on a network share that is not reachable this can take seconds, the loading
    /// dialog is shown meanwhile instead of a frozen welcome screen.
    /// </summary>
    private async Task<bool> ExistsAsync(string path)
    {
        _applicationState.LoadingProjectPath = path;
        try
        {
            return await Task.Run(() => File.Exists(path));
        }
        finally
        {
            _applicationState.LoadingProjectPath = null;
        }
    }

    private async Task TryLoadFile(string path)
    {
        try
        {
            await LoadFile(path);
        }
        catch (Exception ex)
        {
            _loggingService.Error(ex.ToString());
        }
    }
}