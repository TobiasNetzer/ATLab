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
    
    public string? CurrentFilePath { get; private set; }
    
    [ObservableProperty]
    private TestConfigurationViewModel _testConfigurationViewModel;

    [ObservableProperty]
    private ViewModelBase? _selectedTab;

    [ObservableProperty]
    private Tabs.TestingTabViewModel _testingTab;

    [ObservableProperty]
    private Tabs.LabTabViewModel _labTab;
    
    [ObservableProperty]
    private Tabs.ConfigTabViewModel _configTab;
    
    public ObservableCollection<string> Errors => _errorService.Errors;

    [ObservableProperty]
    private int _errorCount;

    [ObservableProperty]
    private bool _hasErrors;
    
    [ObservableProperty]
    private bool _isErrorFlyoutOpen;

    public MainWindowViewModel(IErrorService errorService, ITestHardware testHardware)
    {
        _errorService = errorService;
        _testHardware = testHardware;
        TestConfigurationViewModel = new TestConfigurationViewModel(_testHardware.HardwareInfo);

        TestingTab = new Tabs.TestingTabViewModel(_errorService, TestConfigurationViewModel);
        LabTab = new Tabs.LabTabViewModel(_errorService, _testHardware, TestConfigurationViewModel);
        ConfigTab = new Tabs.ConfigTabViewModel(TestConfigurationViewModel);
        
        _errorService.Errors.CollectionChanged += (_, __) =>
        {
            ErrorCount += 1; // Only show number of new errors
            HasErrors = ErrorCount > 0;
        };
        
    }
    
    public MainWindowViewModel()
    {
        TestConfigurationViewModel = new TestConfigurationViewModel(new DummyHardwareInfo());
        _errorService = new ErrorService();
        
        TestingTab = new Tabs.TestingTabViewModel(_errorService, TestConfigurationViewModel);
        LabTab = new Tabs.LabTabViewModel(_errorService, new CtiaHardware(new SimulationService()), TestConfigurationViewModel);
        ConfigTab = new Tabs.ConfigTabViewModel(TestConfigurationViewModel);
        
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

        TestConfigurationViewModel = new TestConfigurationViewModel(_testHardware.HardwareInfo);

        TestingTab = new Tabs.TestingTabViewModel(_errorService, TestConfigurationViewModel);
        LabTab = new Tabs.LabTabViewModel(_errorService, _testHardware, TestConfigurationViewModel);
        ConfigTab = new Tabs.ConfigTabViewModel(TestConfigurationViewModel);

        CurrentFilePath = null;
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
            
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(dto, options);

            await File.WriteAllTextAsync(file.Path.LocalPath, json);

            CurrentFilePath = file.Path.LocalPath;
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

            CurrentFilePath = file.Path.LocalPath;
            
            App.SettingsService.Settings.LastOpenedFile = file.Path.LocalPath;
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
        
        App.SettingsService.Settings.LastOpenedFile = fileToLoad;
    }
}
