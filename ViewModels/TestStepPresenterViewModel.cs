using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Services;
using ATLab.Models;
using ATLab.Views;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class TestStepPresenterViewModel : ViewModelBase
{

    private readonly ITestExecutor _testExecutor;

    [ObservableProperty]
    private ObservableCollection<TestStepViewModel> _testSteps;

    [ObservableProperty]
    private TestHardwareRelayChannelsViewModel _testHardwareRelayChannels;
    
    [ObservableProperty]
    private TestStepViewModel? _selectedStep;
    
    [ObservableProperty]
    private int _selectedStepIndex;
    
    [ObservableProperty]
    private TestStepConfiguratorViewModel _testStepConfiguratorViewModel;
    
    private readonly IErrorService _errorService;
    private readonly IFileDialogService _fileDialogService;
    private readonly ISettingsService _settingsService;
    private readonly IFileService _fileService;
    private readonly IMessageBoxService _messageBoxService;
    
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private string? _currentFilePath;

    [ObservableProperty]
    private bool _isDirty;

    private string? _lastSavedJson;

    public TestStepPresenterViewModel(
        IErrorService errorService, 
        TestHardwareRelayChannelsViewModel testHardwareRelayChannels, 
        ITestExecutor testExecutor, 
        TestStepConfiguratorViewModel testStepConfiguratorViewModel,
        IFileDialogService fileDialogService,
        ISettingsService settingsService,
        IFileService fileService,
        IMessageBoxService messageBoxService)
    {
        _errorService = errorService;
        TestSteps = new ObservableCollection<TestStepViewModel>();
        TestHardwareRelayChannels = testHardwareRelayChannels;
        _testExecutor = testExecutor;
        TestStepConfiguratorViewModel = testStepConfiguratorViewModel;
        _fileDialogService = fileDialogService;
        _settingsService = settingsService;
        _fileService = fileService;
        _messageBoxService = messageBoxService;
        
        HookExecutorEvents();
        
        TestSteps.CollectionChanged += (_, _) => CheckForChanges();
        TestHardwareRelayChannels.ConfigurationChanged += () => CheckForChanges();

        _lastSavedJson = CaptureCurrentStateJson();
    }
    
    partial void OnSelectedStepChanged(TestStepViewModel? value)
    {
        if (value?.TestStep != null)
        {
            try
            {
                TestHardwareRelayChannels.MeasChannelViewModel.LoadActiveMeasChannels(value.TestStep.MatrixState!);
                TestHardwareRelayChannels.StimChannelViewModel.LoadRelayStates(value.TestStep.LiveStimState!);
                TestHardwareRelayChannels.ExtStimChannelViewModel.LoadRelayStates(value.TestStep.LiveExtStimState!);
                TestStepConfiguratorViewModel.LoadTestStep(value);
            }
            catch (Exception ex)
            {
                _errorService.AddError("Exception: " + ex.Message);
            }
            
        }
    }

    private void RenumberTestSteps()
    {
        for (int i = 0; i < TestSteps.Count; i++)
        {
            TestSteps[i].TestStep.Number = i + 1; // 1‑based numbering
        }
    }

    [RelayCommand]
    private void AddTestStep()
    {
        var indexToInsertNewStep = SelectedStepIndex < 0 ? 0 : SelectedStepIndex + 1;
        var newStep = new TestStepViewModel(new TestStep(), TestHardwareRelayChannels.HardwareInfo);
        newStep.PropertyChanged += (_, _) => CheckForChanges();
        TestSteps.Insert(indexToInsertNewStep, newStep);
        RenumberTestSteps();
        SelectedStepIndex = indexToInsertNewStep;
    }
    
    [RelayCommand]
    private void DuplicateTestStep()
    {
        if (SelectedStep == null) return;

        SelectedStep.TestStep.UpdateDtos();
        var currentModel = SelectedStep.TestStep;
        var modelCopy = new TestStep
        {
            Name = currentModel.Name,
            LowerLimit = currentModel.LowerLimit,
            UpperLimit = currentModel.UpperLimit,
            NominalValue = currentModel.NominalValue,
            Comment = currentModel.Comment,
            Delay = currentModel.Delay,
            StimState = currentModel.StimState != null ? new RelayGroupDto(currentModel.StimState) : null,
            ExtStimState = currentModel.ExtStimState != null ? new RelayGroupDto(currentModel.ExtStimState) : null,
            MatrixState = currentModel.MatrixState != null ? new RelayMatrix(currentModel.MatrixState) : null
        };

        var duplicatedStep = new TestStepViewModel(modelCopy, TestHardwareRelayChannels.HardwareInfo);
        duplicatedStep.PropertyChanged += (_, _) => CheckForChanges();

        var indexToInsert = SelectedStepIndex + 1;
        TestSteps.Insert(indexToInsert, duplicatedStep);

        RenumberTestSteps();
        SelectedStepIndex = indexToInsert;
    }
    
    [RelayCommand]
    private void RemoveTestStep()
    {
        if (SelectedStepIndex >= 0 && SelectedStepIndex < TestSteps.Count)
        {
            TestSteps.RemoveAt(SelectedStepIndex);
            RenumberTestSteps();
        }
    }
    
    [RelayCommand]
    private void MoveStepUp()
    {
        if (SelectedStep == null || SelectedStepIndex <= 0) return;

        var stepToMove = SelectedStep;
        int oldIndex = SelectedStepIndex;
        int newIndex = oldIndex - 1;

        TestSteps.RemoveAt(oldIndex);
        TestSteps.Insert(newIndex, stepToMove);
        
        RenumberTestSteps();
        SelectedStepIndex = newIndex;
    }

    [RelayCommand]
    private void MoveStepDown()
    {
        if (SelectedStep == null || SelectedStepIndex < 0 || SelectedStepIndex >= TestSteps.Count - 1) return;

        var stepToMove = SelectedStep;
        int oldIndex = SelectedStepIndex;
        int newIndex = oldIndex + 1;

        TestSteps.RemoveAt(oldIndex);
        TestSteps.Insert(newIndex, stepToMove);

        RenumberTestSteps();
        SelectedStepIndex = newIndex;
    }
    
    [ObservableProperty]
    private bool _isRunning;
    
    private bool CanStartTest() => !IsRunning;
    
    [RelayCommand(CanExecute = nameof(CanStartTest))]
    private async Task StartTestAsync()
    {
        if (TestSteps.Count == 0)
        {
            _errorService.AddError("No test steps configured.");
            return;
        }

        IsRunning = true;
        _cts = new CancellationTokenSource();

        try
        {
            await _testExecutor.ExecuteAsync(TestSteps, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            _errorService.AddError("Test was cancelled by user.");
        }
        catch (Exception ex)
        {
            _errorService.AddError("Test execution failed: " + ex.Message);
        }
        finally
        {
            IsRunning = false;
            _cts.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private void CancelTest()
    {
        _cts?.Cancel();
    }

    private void HookExecutorEvents()
    {
        _testExecutor.StepStarted += (index, step) =>
        {
            //SelectedStepIndex = index;
            //SelectedStep = step;
        };

        _testExecutor.StepCompleted += (index, step, result) =>
        {
            step.Result = result.MeasuredValue.ToString("F3");
        };

        _testExecutor.TestCompleted += () =>
        {
            IsRunning = false;
        };
    }

    [RelayCommand]
    public async Task NewFile()
    {
        if (IsDirty)
        {
            var confirm = await _messageBoxService.ShowConfirmationAsync("Unsaved Changes", "You have unsaved changes. Do you want to continue and lose your changes?");
            if (!confirm) return;
        }

        TestSteps.Clear();
        TestHardwareRelayChannels.ResetToDefault();
        CurrentFilePath = null;
        _lastSavedJson = CaptureCurrentStateJson();
        IsDirty = false;
    }

    private string CaptureCurrentStateJson()
    {
        foreach (var vm in TestSteps)
            vm.TestStep.UpdateDtos();

        var dto = new AtlabFileDto
        {
            TestSteps = TestSteps.Select(vm => vm.TestStep).ToList(),
            StimChannelNames = TestHardwareRelayChannels.GetStimNames(),
            ExtStimChannelNames = TestHardwareRelayChannels.GetExtStimNames(),
            MeasChannelNames = TestHardwareRelayChannels.GetMeasNames()
        };

        return _fileService.Serialize(dto);
    }

    private void CheckForChanges()
    {
        if (_lastSavedJson == null) return;
        var currentJson = CaptureCurrentStateJson();
        IsDirty = currentJson != _lastSavedJson;
    }

    [RelayCommand]
    public async Task OpenAboutWindow()
    {
        var deviceInfoProvider = TestHardwareRelayChannels.HardwareInfo;
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

    [RelayCommand]
    public async Task SaveFileAs()
    {
        var file = await _fileDialogService.SaveFileAsync("ATLab files", "Test.atlab", "atlab", new[] { "atlab" });

        if (file is not null)
        {
            var json = CaptureCurrentStateJson();
            await File.WriteAllTextAsync(file.Path.LocalPath, json);

            _lastSavedJson = json;
            CurrentFilePath = file.Path.LocalPath;
            _settingsService.Settings.LastOpenedFile = file.Path.LocalPath;
            IsDirty = false;
        }
    }

    [RelayCommand]
    public async Task SaveFile()
    {
        if (!string.IsNullOrWhiteSpace(CurrentFilePath))
        {
            var json = CaptureCurrentStateJson();
            await File.WriteAllTextAsync(CurrentFilePath, json);
            _lastSavedJson = json;
            IsDirty = false;
            return;
        }

        await SaveFileAs();
    }

    [RelayCommand]
    public async Task LoadFileWithDialog()
    {
        if (IsDirty)
        {
            var result = await _messageBoxService.ShowConfirmationAsync("Unsaved Changes", "You have unsaved changes. Do you want to continue and lose your changes?");
            if (!result) return;
        }

        var file = await _fileDialogService.OpenFileAsync("ATLab files", new[] { "atlab" });

        if (file is not null)
        {
            await LoadFile(file.Path.LocalPath);
        }
    }


    public async Task LoadFile(string fileToLoad)
    {
        try
        {
            var json = await File.ReadAllTextAsync(fileToLoad);
            var dto = _fileService.Deserialize(json);

            if (dto != null)
            {
                TestSteps.Clear();
                foreach (var step in dto.TestSteps)
                {
                    var stepVm = new TestStepViewModel(step, TestHardwareRelayChannels.HardwareInfo);
                    stepVm.PropertyChanged += (_, _) => CheckForChanges();
                    TestSteps.Add(stepVm);
                }

                TestHardwareRelayChannels.ApplyChannelNames(dto.StimChannelNames, dto.ExtStimChannelNames, dto.MeasChannelNames);

                CurrentFilePath = fileToLoad;
                _lastSavedJson = json;
                _settingsService.Settings.LastOpenedFile = fileToLoad;
                IsDirty = false;
            }
        }
        catch (Exception ex)
        {
            _errorService.AddError("Failed to load file: " + ex.Message);
        }
    }

    public TestStepPresenterViewModel()
    {
        _errorService = new ErrorService();
        TestSteps = new ObservableCollection<TestStepViewModel>();
        TestHardwareRelayChannels = new TestHardwareRelayChannelsViewModel(new DummyHardwareInfo());
        _testExecutor = new TestExecutor(new DummyTestStepRunner());
        TestStepConfiguratorViewModel = new TestStepConfiguratorViewModel();
        _fileDialogService = new FileDialogService();
        _settingsService = new SettingsService();
        _fileService = new FileService();
        _messageBoxService = new MessageBoxService();
    }
}
