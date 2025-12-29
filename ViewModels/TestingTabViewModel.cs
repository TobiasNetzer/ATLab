using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.Views;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class TestingTabViewModel : ViewModelBase
{

    private readonly ITestExecutor _testExecutor;
    private readonly IErrorService _errorService;
    private readonly IFileDialogService _fileDialogService;
    private readonly ISettingsService _settingsService;
    private readonly IFileService _fileService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly SerialDeviceManagerViewModel _serialDeviceManager;

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

    [ObservableProperty]
    private ScriptSelectorViewModel _scriptSelector;
    
    [ObservableProperty]
    private bool _isRunning;
    
    [ObservableProperty]
    private int _numberFailedSteps;
    
    [ObservableProperty]
    private bool _isTestFailed;

    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private string? _currentFilePath;

    [ObservableProperty]
    private bool _isDirty;

    private string? _lastSavedJson;

    public TestingTabViewModel(
        IErrorService errorService, 
        TestHardwareRelayChannelsViewModel testHardwareRelayChannels, 
        ITestExecutor testExecutor, 
        TestStepConfiguratorViewModel testStepConfiguratorViewModel,
        IFileDialogService fileDialogService,
        ISettingsService settingsService,
        IFileService fileService,
        IMessageBoxService messageBoxService,
        SerialDeviceManagerViewModel serialDeviceManager,
        ScriptSelectorViewModel scriptSelector)
    {
        _errorService = errorService;
        TestSteps = new ObservableCollection<TestStepViewModel>();
        TestHardwareRelayChannels = testHardwareRelayChannels;
        _testExecutor = testExecutor;
        TestStepConfiguratorViewModel = testStepConfiguratorViewModel;
        _scriptSelector = scriptSelector;
        _fileDialogService = fileDialogService;
        _settingsService = settingsService;
        _fileService = fileService;
        _messageBoxService = messageBoxService;
        _serialDeviceManager = serialDeviceManager;
        
        Title = "Testing";
        
        HookExecutorEvents();
        
        TestSteps.CollectionChanged += (_, _) => CheckForChanges();
        TestHardwareRelayChannels.ConfigurationChanged += () => CheckForChanges();
        _serialDeviceManager.SerialDevices.CollectionChanged += (_, _) => CheckForChanges();

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
                ScriptSelector.LoadTestStep(value);
            }
            catch (Exception ex)
            {
                _errorService.AddError("Exception: " + ex.Message);
            }
            
        }
    }

    partial void OnIsRunningChanged(bool value)
    {
        AddTestStepCommand.NotifyCanExecuteChanged();
        DuplicateTestStepCommand.NotifyCanExecuteChanged();
        RemoveTestStepCommand.NotifyCanExecuteChanged();
        MoveStepUpCommand.NotifyCanExecuteChanged();
        MoveStepDownCommand.NotifyCanExecuteChanged();
        NewFileCommand.NotifyCanExecuteChanged();
        SaveFileCommand.NotifyCanExecuteChanged();
        OpenAboutWindowCommand.NotifyCanExecuteChanged();
        LoadFileWithDialogCommand.NotifyCanExecuteChanged();
        SaveFileAsCommand.NotifyCanExecuteChanged();
    }

    private void RenumberTestSteps()
    {
        for (int i = 0; i < TestSteps.Count; i++)
        {
            TestSteps[i].TestStep.Number = i + 1; // 1‑based numbering
        }
    }

    [RelayCommand(CanExecute = nameof(IsTestRunning))]
    private void AddTestStep()
    {
        var indexToInsertNewStep = SelectedStepIndex < 0 ? 0 : SelectedStepIndex + 1;
        if (indexToInsertNewStep > TestSteps.Count) indexToInsertNewStep = TestSteps.Count;
        var newStep = new TestStepViewModel(new TestStep(), TestHardwareRelayChannels.HardwareInfo);
        newStep.PropertyChanged += (_, _) => CheckForChanges();
        TestSteps.Insert(indexToInsertNewStep, newStep);
        RenumberTestSteps();
        SelectedStepIndex = indexToInsertNewStep;
    }
    
    [RelayCommand(CanExecute = nameof(IsTestRunning))]
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
            Unit = currentModel.Unit,
            Comment = currentModel.Comment,
            Delay = currentModel.Delay,
            EvaluationSource = currentModel.EvaluationSource,
            TargetDevice = currentModel.TargetDevice,
            ScriptId = currentModel.ScriptId,
            ScriptVariables = new ObservableCollection<ScriptVariable>(currentModel.ScriptVariables.Select(v => v.Clone())),
            StimState = currentModel.StimState != null ? new RelayGroupDto(currentModel.StimState) : null,
            ExtStimState = currentModel.ExtStimState != null ? new RelayGroupDto(currentModel.ExtStimState) : null,
            MatrixState = new RelayMatrix(currentModel.MatrixState)
        };

        var duplicatedStep = new TestStepViewModel(modelCopy, TestHardwareRelayChannels.HardwareInfo);
        duplicatedStep.PropertyChanged += (_, _) => CheckForChanges();

        var indexToInsert = SelectedStepIndex + 1;
        TestSteps.Insert(indexToInsert, duplicatedStep);

        RenumberTestSteps();
        SelectedStepIndex = indexToInsert;
    }
    
    [RelayCommand(CanExecute = nameof(IsTestRunning))]
    private void RemoveTestStep()
    {
        if (SelectedStepIndex >= 0 && SelectedStepIndex < TestSteps.Count)
        {
            TestSteps.RemoveAt(SelectedStepIndex);
            RenumberTestSteps();
        }
    }
    
    [RelayCommand(CanExecute = nameof(IsTestRunning))]
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

    [RelayCommand(CanExecute = nameof(IsTestRunning))]
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
    
    private bool IsTestRunning() => !IsRunning;
    
    [RelayCommand(CanExecute = nameof(IsTestRunning))]
    private async Task StartTestAsync()
    {
        if (TestSteps.Count == 0)
        {
            _errorService.AddError("No test steps configured.");
            return;
        }

        IsRunning = true;
        NumberFailedSteps = 0;
        IsTestFailed = false;
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
            SelectedStepIndex = index;
        };

        _testExecutor.StepCompleted += (index, step) =>
        {
            if (!step.IsValid)
            {
                NumberFailedSteps++;
            }
        };

        _testExecutor.TestCompleted += () =>
        {
            IsRunning = false;
            if (NumberFailedSteps > 0)
            {
                IsTestFailed = true;
            }
        };
    }

    [RelayCommand(CanExecute = nameof(IsTestRunning))]
    public async Task NewFile()
    {
        if (IsDirty)
        {
            var confirm = await _messageBoxService.ShowConfirmationAsync("Unsaved Changes", "You have unsaved changes. Do you want to continue and lose your changes?");
            if (!confirm) return;
        }

        TestSteps.Clear();
        TestHardwareRelayChannels.ResetToDefault();
        _serialDeviceManager.SerialDevices.Clear();
        CurrentFilePath = null;
        _lastSavedJson = CaptureCurrentStateJson();
        IsDirty = false;
    }

    private AtlabFileDto CaptureCurrentState()
    {
        foreach (var vm in TestSteps)
            vm.TestStep.UpdateDtos();

        return new AtlabFileDto
        {
            TestSteps = TestSteps.Select(vm => vm.TestStep).ToList(),
            StimChannelNames = TestHardwareRelayChannels.GetStimNames(),
            ExtStimChannelNames = TestHardwareRelayChannels.GetExtStimNames(),
            MeasChannelNames = TestHardwareRelayChannels.GetMeasNames(),
            SerialDevices = _serialDeviceManager.SerialDevices.ToList()
        };
    }

    private string CaptureCurrentStateJson()
    {
        var dto = CaptureCurrentState();
        return _fileService.Serialize(dto);
    }

    private void CheckForChanges()
    {
        if (_lastSavedJson == null) return;
        var currentJson = CaptureCurrentStateJson();
        IsDirty = currentJson != _lastSavedJson;
    }

    [RelayCommand(CanExecute = nameof(IsTestRunning))]
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

    [RelayCommand(CanExecute = nameof(IsTestRunning))]
    public async Task SaveFileAs()
    {
        var file = await _fileDialogService.SaveFileAsync("ATLab files", "Test.atlab", "atlab", new[] { "atlab" });

        if (file is not null)
        {
            var dto = CaptureCurrentState();
            await _fileService.SaveAsync(file.Path.LocalPath, dto);

            _lastSavedJson = _fileService.Serialize(dto);
            CurrentFilePath = file.Path.LocalPath;
            _settingsService.Settings.LastOpenedFile = file.Path.LocalPath;
            IsDirty = false;
        }
    }

    [RelayCommand(CanExecute = nameof(IsTestRunning))]
    public async Task SaveFile()
    {
        if (!string.IsNullOrWhiteSpace(CurrentFilePath))
        {
            var dto = CaptureCurrentState();
            await _fileService.SaveAsync(CurrentFilePath, dto);
            _lastSavedJson = _fileService.Serialize(dto);
            IsDirty = false;
            return;
        }

        await SaveFileAs();
    }

    [RelayCommand(CanExecute = nameof(IsTestRunning))]
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
            var dto = await _fileService.LoadAsync(fileToLoad);

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
                
                _serialDeviceManager.SerialDevices.Clear();

                foreach (var device in dto.SerialDevices)
                {
                    _serialDeviceManager.SerialDevices.Add(device);
                }

                CurrentFilePath = fileToLoad;
                _lastSavedJson = _fileService.Serialize(dto);
                _settingsService.Settings.LastOpenedFile = fileToLoad;
                IsDirty = false;
                SelectedStepIndex = 0;
            }
        }
        catch (Exception ex)
        {
            _errorService.AddError("Failed to load file: " + ex.Message);
        }
    }
}
