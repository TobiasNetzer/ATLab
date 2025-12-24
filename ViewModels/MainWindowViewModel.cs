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

    private string? _lastSavedJson;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private string? _currentFilePath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private bool _isDirty;

    public string WindowTitle => $"ATLab - {(string.IsNullOrEmpty(CurrentFilePath) ? "Untitled" : Path.GetFileNameWithoutExtension(CurrentFilePath))}{(IsDirty ? "*" : "")}";
    
    [ObservableProperty]
    private TestConfigurationViewModel _testConfigurationViewModel;

    [ObservableProperty]
    private ViewModelBase? _selectedTab;

    [ObservableProperty]
    private TestingTabViewModel _testingTab;

    [ObservableProperty]
    private LabTabViewModel _labTab;
    
    [ObservableProperty]
    private ConfigTabViewModel _configTab;
    
    public ObservableCollection<string> Errors => _errorService.Errors;

    [ObservableProperty]
    private int _errorCount;

    [ObservableProperty]
    private bool _hasErrors;
    
    [ObservableProperty]
    private bool _isErrorFlyoutOpen;

    public MainWindowViewModel(IErrorService errorService,
        ITestHardware testHardware, 
        TestConfigurationViewModel testConfigurationViewModel, 
        TestingTabViewModel testingTab, 
        LabTabViewModel labTab, 
        ConfigTabViewModel configTab,
        ISettingsService settingsService)
    {
        _errorService = errorService;
        _testHardware = testHardware;
        _settingsService = settingsService;
        TestConfigurationViewModel = testConfigurationViewModel;

        TestingTab = testingTab;
        LabTab = labTab;
        ConfigTab = configTab;
        
        _errorService.Errors.CollectionChanged += (_, __) =>
        {
            ErrorCount += 1; // Only show number of new errors
            HasErrors = ErrorCount > 0;
        };
        
    }
    
    public MainWindowViewModel()
    {
        _testHardware = new CtiaHardware(new SimulationService());
        _errorService = new ErrorService();
        _settingsService = new SettingsService();
        var configurator = new TestStepConfiguratorViewModel();
        TestConfigurationViewModel = new TestConfigurationViewModel(_testHardware.HardwareInfo, configurator);
        
        TestStepPresenterViewModel testStepPresenter = new TestStepPresenterViewModel(_errorService, TestConfigurationViewModel, new TestExecutor(new DummyTestStepRunner()), configurator);
        TestingTab = new TestingTabViewModel(_errorService, TestConfigurationViewModel, testStepPresenter);
        LabTab = new LabTabViewModel(_errorService, _testHardware, TestConfigurationViewModel, new SimulationStateService { IsSimulationMode = true });
        ConfigTab = new ConfigTabViewModel(TestConfigurationViewModel);
        
        _errorService.Errors.CollectionChanged += (_, __) =>
        {
            ErrorCount += 1; // Only show number of new errors
            HasErrors = ErrorCount > 0;
        };

    }

    [RelayCommand]
    private async Task OpenAboutWindow()
    {
        var deviceInfoProvider = _testHardware.HardwareInfo;
        var aboutVm = new AboutWindowViewModel(deviceInfoProvider);
        var aboutWindow = new AboutWindow
        {
            DataContext = aboutVm
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

    [RelayCommand]
    private void NewFile()
    {
        TestingTab.TestStepPresenter.TestSteps.Clear();
        TestConfigurationViewModel.ResetToDefault();
        CurrentFilePath = null;
        _lastSavedJson = null;
        IsDirty = false;
    }

    private string CaptureCurrentStateJson()
    {
        var presenter = TestingTab.TestStepPresenter;
        foreach (var vm in presenter.TestSteps)
            vm.SyncBack();

        var dto = new AtlabFileDto
        {
            TestSteps = presenter.TestSteps.Select(vm => vm.Model).ToList(),
            StimChannelNames = TestConfigurationViewModel.GetStimNames(),
            ExtStimChannelNames = TestConfigurationViewModel.GetExtStimNames(),
            MeasChannelNames = TestConfigurationViewModel.GetMeasNames()
        };

        return JsonSerializer.Serialize(dto);
    }

    public void CheckForChanges()
    {
        if (_lastSavedJson == null) return;
        var currentJson = CaptureCurrentStateJson();
        IsDirty = currentJson != _lastSavedJson;
    }

    [RelayCommand]
    private async Task SaveFileAs()
    {
        var window = (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (window is null)
            return;

        var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            DefaultExtension = "atlab",
            SuggestedFileName = "Test.atlab",
            FileTypeChoices = new List<FilePickerFileType>
            {
                new FilePickerFileType("ATLab files") { Patterns = new[] { "*.atlab" } }
            }
        });

        if (file is not null)
        {
            var json = CaptureCurrentStateJson();
            await File.WriteAllTextAsync(file.Path.LocalPath, json);

            _lastSavedJson = json;
            CurrentFilePath = file.Path.LocalPath;
            IsDirty = false;
        }
    }
    
    [RelayCommand]
    private async Task SaveFile()
    {
        var presenter = TestingTab.TestStepPresenter;
        
        if (!string.IsNullOrWhiteSpace(CurrentFilePath))
        {
            foreach (var vm in presenter.TestSteps)
                vm.SyncBack();
            
            var dto = new AtlabFileDto
            {
                TestSteps = presenter.TestSteps.Select(vm => vm.Model).ToList(),
                StimChannelNames = TestConfigurationViewModel.GetStimNames(),
                ExtStimChannelNames = TestConfigurationViewModel.GetExtStimNames(),
                MeasChannelNames = TestConfigurationViewModel.GetMeasNames()
            };
            
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(dto, options);

            await File.WriteAllTextAsync(CurrentFilePath, json);
            return;
        }
        
        await SaveFileAs();
    }
    
    [RelayCommand]
    private async Task LoadFileWithDialog()
    {
        var window = (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (window is null)
            return;

        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new FilePickerFileType("ATLab files") { Patterns = new[] { "*.atlab" } }
            }
        });


        if (files.Count > 0)
        {
            var file = files[0];
            var json = await File.ReadAllTextAsync(file.Path.LocalPath);
            
            var dto = JsonSerializer.Deserialize<AtlabFileDto>(json);
            
            TestingTab.TestStepPresenter.TestSteps.Clear();
            if (dto != null)
            {
                foreach (var step in dto.TestSteps)
                    TestingTab.TestStepPresenter.TestSteps.Add(new TestStepViewModel(step, _testHardware.HardwareInfo));

                TestConfigurationViewModel.ApplyChannelNames(
                    dto.StimChannelNames,
                    dto.ExtStimChannelNames,
                    dto.MeasChannelNames
                );
            }

            _lastSavedJson = json;
            CurrentFilePath = file.Path.LocalPath;
            IsDirty = false;
            _settingsService.Settings.LastOpenedFile = file.Path.LocalPath;
        }
    }
    
    public async Task LoadFile(string fileToLoad)
    {
        var json =  await File.ReadAllTextAsync(fileToLoad);
        
        var dto = JsonSerializer.Deserialize<AtlabFileDto>(json);
        
        TestingTab.TestStepPresenter.TestSteps.Clear();
        if (dto != null)
        {
            foreach (var step in dto.TestSteps)
                TestingTab.TestStepPresenter.TestSteps.Add(new TestStepViewModel(step, _testHardware.HardwareInfo));

            TestConfigurationViewModel.ApplyChannelNames(
                dto.StimChannelNames,
                dto.ExtStimChannelNames,
                dto.MeasChannelNames
            );
        }

        CurrentFilePath = fileToLoad;
        
        _settingsService.Settings.LastOpenedFile = fileToLoad;
    }
}
