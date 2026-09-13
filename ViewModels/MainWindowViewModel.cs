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
using ShadUI;

namespace ATLab.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ITestHardware _testHardware;
    private readonly IErrorService _errorService;
    private readonly IProjectDocumentService _projectDocumentService;
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

    public ObservableCollection<string> Errors => _errorService.Errors;
    
    public bool IsSimulation => _applicationState.IsSimulationMode;

    [ObservableProperty]
    private bool _hasErrors;
    
    [ObservableProperty]
    private bool _isErrorFlyoutOpen;

    [ObservableProperty]
    private string? _matrixChannel;
    
    [ObservableProperty]
    private DialogManager _dialogManager;
    
    [ObservableProperty]
    private ToastManager _toastManager;

    public MainWindowViewModel(ITestHardware testHardware,
        IErrorService errorService,
        IProjectDocumentService projectDocumentService,
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
        _errorService = errorService;
        _projectDocumentService = projectDocumentService;
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
        
        _errorService.Errors.CollectionChanged += (_, __) =>
        {
            HasErrors = true;
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
        
        HasErrors = false;
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
            _errorService.AddError(result.ErrorMessage);
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
        switch (value)
        {
            case TestingTabViewModel:
                TestingTab.SelectedStep = TestingTab.TestSteps.Count > 0 ? TestingTab.TestSteps[0] : null;
                break;
        }
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