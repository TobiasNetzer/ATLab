using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class TestingTabViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly ITestExecutor _testExecutor;
    private readonly IErrorService _errorService;
    private readonly IProjectService _projectService;
    private readonly DeviceManagerViewModel _deviceManager;
    private readonly ProjectSettingsViewModel _projectSettingsViewModel;
    private readonly ISerialNumberDialogService _serialNumberDialogService;
    private readonly TestResultExportService _testResultExportService;
    
    private List<TestStep>? _copiedSteps;

    [ObservableProperty]
    private ObservableCollection<TestStepViewModel> _testSteps;

    [ObservableProperty]
    private TestHardwareRelayChannelsViewModel _testHardwareRelayChannels;
    
    [ObservableProperty]
    private TestStepViewModel? _selectedStep;
    
    [ObservableProperty]
    private ObservableCollection<TestStepViewModel> _selectedSteps = new();
    
    [ObservableProperty]
    private int _selectedStepIndex;
    
    [ObservableProperty]
    private TestStepConfiguratorViewModel _testStepConfiguratorViewModel;

    [ObservableProperty]
    private ScriptSelectorViewModel _scriptSelector;
    
    [ObservableProperty]
    private ShellCommandEditorViewModel _shellCommandEditor;
    
    [ObservableProperty]
    private bool _isDevelopmentMode;
    
    [ObservableProperty]
    private int _numberFailedSteps;
    
    [ObservableProperty]
    private string _user = Environment.UserName;
    
    [ObservableProperty]
    private int _numberRunTests;
    
    [ObservableProperty]
    private int _numberPassedTests;
    
    [ObservableProperty]
    private TestStatus _testStatus = TestStatus.IDLE;

    private DateTimeOffset StartTime { get; set; }
    private TimeSpan Elapsed => DateTimeOffset.Now - StartTime;
    
    [ObservableProperty]
    private string _testDuration = string.Empty;
    
    [ObservableProperty]
    private string _serialNumber = string.Empty;

    private bool _isAnimationEnabled = true;
    public bool IsAnimationEnabled
    {
        get => _isAnimationEnabled;
        set
        {
            if (_isAnimationEnabled != value)
            {
                _isAnimationEnabled = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AnimationDuration));
            }
        }
    }
    
    private int _testProgress;

    public int TestProgress
    {
        get => _testProgress;
        set
        {
            if (_testProgress != value)
            {
                _testProgress = value;
                IsAnimationEnabled = _testProgress != 0;
                OnPropertyChanged();
            }
        }
    }


    public TimeSpan AnimationDuration => IsAnimationEnabled ? TimeSpan.FromMilliseconds(250) : TimeSpan.Zero;

    private bool _allowResultSave;

    public TestingTabViewModel(
        ISettingsService settingsService,
        IErrorService errorService, 
        TestHardwareRelayChannelsViewModel testHardwareRelayChannels, 
        ITestExecutor testExecutor, 
        TestStepConfiguratorViewModel testStepConfiguratorViewModel,
        IProjectService projectService,
        DeviceManagerViewModel deviceManager,
        ScriptSelectorViewModel scriptSelector,
        ShellCommandEditorViewModel shellCommandEditor,
        ProjectSettingsViewModel projectSettingsViewModel,
        ISerialNumberDialogService serialNumberDialogService,
        TestResultExportService testResultExportService)
    {
        _settingsService = settingsService;
        _errorService = errorService;
        TestSteps = new ObservableCollection<TestStepViewModel>();
        TestHardwareRelayChannels = testHardwareRelayChannels;
        _testExecutor = testExecutor;
        TestStepConfiguratorViewModel = testStepConfiguratorViewModel;
        ScriptSelector = scriptSelector;
        ShellCommandEditor = shellCommandEditor;
        _projectService = projectService;
        _deviceManager = deviceManager;
        _projectSettingsViewModel = projectSettingsViewModel;
        _serialNumberDialogService = serialNumberDialogService;
        _testResultExportService = testResultExportService;
        
        Title = "Testing";
        
        IsDevelopmentMode = settingsService.Settings.IsDevelopmentMode;
        
        HookExecutorEvents();
        
        TestSteps.CollectionChanged += (_, _) => CheckForChanges();
        TestHardwareRelayChannels.ConfigurationChanged += () => CheckForChanges();
        _projectSettingsViewModel.ConfigurationChanged += () => CheckForChanges();
        _deviceManager.Devices.CollectionChanged += DevicesChanged;
        
        foreach (var device in _deviceManager.Devices)
            SubscribeToDevice(device);
        
        _projectService.UpdateLastSavedState(CaptureCurrentState());
    }
    
    private void CheckForChanges()
    {
        _projectService.IsStateChanged(CaptureCurrentState());
    }

    private void OnStepPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        CheckForChanges();

        if (sender is not TestStepViewModel changedVm)
            return;
        
        if (changedVm != SelectedStep)
            return;
        
        if (string.IsNullOrWhiteSpace(e.PropertyName))
            return;
        
        var isTestStepProperty = typeof(TestStep).GetProperty(e.PropertyName) != null;
        if (!isTestStepProperty)
            return;

        foreach (var vm in SelectedSteps)
        {
            if (vm == changedVm)
                continue;

            CopyChangedProperty(changedVm.TestStep, vm.TestStep, e.PropertyName);
        }
    }
    
    private static void CopyChangedProperty(TestStep source, TestStep target, string propertyName)
    {
        var prop = typeof(TestStep).GetProperty(propertyName);
        if (prop == null || !prop.CanWrite)
            return;

        var value = prop.GetValue(source);
        prop.SetValue(target, value);
    }
    
    private void DevicesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (Device device in e.NewItems)
                SubscribeToDevice(device);
        }

        if (e.OldItems != null)
        {
            foreach (Device device in e.OldItems)
                UnsubscribeFromDevice(device);
        }

        CheckForChanges();
    }

    private void SubscribeToDevice(Device device)
    {
        device.PropertyChanged += DeviceChanged;
        device.Configuration.PropertyChanged += DeviceConfigurationChanged;
    }

    private void UnsubscribeFromDevice(Device device)
    {
        device.PropertyChanged -= DeviceChanged;
        device.Configuration.PropertyChanged -= DeviceConfigurationChanged;
    }

    private void DeviceChanged(object? sender, PropertyChangedEventArgs e)
    {
        CheckForChanges();
    }

    private void DeviceConfigurationChanged(object? sender, PropertyChangedEventArgs e)
    {
        CheckForChanges();
    }
    
    partial void OnSelectedStepChanged(TestStepViewModel? value)
    {
        if (value?.TestStep == null) return;
        
        try
        {
            TestHardwareRelayChannels.MeasChannelViewModel.LoadActiveMeasChannels(value.TestStep.MatrixState);
            TestHardwareRelayChannels.StimChannelViewModel.LoadRelayStates(value.TestStep.LiveStimState);
            TestHardwareRelayChannels.ExtStimChannelViewModel.LoadRelayStates(value.TestStep.LiveExtStimState);
            TestStepConfiguratorViewModel.LoadTestStep(value);
            ScriptSelector.LoadTestStep(value);
            ShellCommandEditor.LoadTestStep(value.TestStep.ShellCommand);
        }
        catch (Exception ex)
        {
            _errorService.AddError("Exception: " + ex.Message);
        }
    }

    partial void OnTestStatusChanged(TestStatus value)
    {
        AddTestStepCommand.NotifyCanExecuteChanged();
        DuplicateTestStepsCommand.NotifyCanExecuteChanged();
        CutTestStepsCommand.NotifyCanExecuteChanged();
        CopyTestStepsCommand.NotifyCanExecuteChanged();
        PasteTestStepsCommand.NotifyCanExecuteChanged();
        RemoveTestStepsCommand.NotifyCanExecuteChanged();
        MoveStepUpCommand.NotifyCanExecuteChanged();
        MoveStepDownCommand.NotifyCanExecuteChanged();
        NewFileCommand.NotifyCanExecuteChanged();
        SaveFileCommand.NotifyCanExecuteChanged();
        LoadFileWithDialogCommand.NotifyCanExecuteChanged();
        SaveFileAsCommand.NotifyCanExecuteChanged();
        StartTestFromSelectionCommand.NotifyCanExecuteChanged();
        StartTestRepeatCommand.NotifyCanExecuteChanged();
        StartTestCommand.NotifyCanExecuteChanged();
        StartSingleStepTestCommand.NotifyCanExecuteChanged();
    }
    
    partial void OnIsDevelopmentModeChanged(bool value)
    {
        _settingsService.Settings.IsDevelopmentMode = value;
    }
    
    private bool IsNotTestRunning() => TestStatus != TestStatus.RUNNING;
    private bool CanPasteTestStep() => IsNotTestRunning() && _copiedSteps != null;

    private void RenumberTestSteps()
    {
        for (int i = 0; i < TestSteps.Count; i++)
        {
            TestSteps[i].TestStep.Number = i + 1; // 1‑based numbering
        }
    }

    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private void AddTestStep()
    {
        int indexToInsertNewStep;

        if (SelectedSteps.Count == 0)
        {
            indexToInsertNewStep = 0;
        }
        else
        {
            var lastSelected = SelectedSteps
                .Select(s => TestSteps.IndexOf(s))
                .Where(i => i >= 0)
                .Max();

            indexToInsertNewStep = lastSelected + 1;
        }
        
        if (indexToInsertNewStep > TestSteps.Count)
            indexToInsertNewStep = TestSteps.Count;
        
        var newStep = new TestStepViewModel(new TestStep(), TestHardwareRelayChannels.HardwareInfo);
        newStep.PropertyChanged += OnStepPropertyChanged;

        TestSteps.Insert(indexToInsertNewStep, newStep);

        RenumberTestSteps();
        
        SelectedStep = newStep;
    }

    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private void DuplicateTestSteps()
    {
        if (SelectedSteps.Count == 0)
            return;
        
        var lastSelectedIndex = SelectedSteps
            .Select(s => TestSteps.IndexOf(s))
            .Where(i => i >= 0)
            .Max();

        int insertIndex = lastSelectedIndex + 1;
        
        var stepsToDuplicate = SelectedSteps
            .OrderBy(s => TestSteps.IndexOf(s))
            .ToList();

        var newDuplicates = new List<TestStepViewModel>();

        foreach (var step in stepsToDuplicate)
        {
            step.TestStep.UpdateDtos();
            
            var modelCopy = step.TestStep.Clone();
            
            var duplicatedStep = new TestStepViewModel(modelCopy, TestHardwareRelayChannels.HardwareInfo);
            duplicatedStep.PropertyChanged += (_, _) => CheckForChanges();
            
            TestSteps.Insert(insertIndex, duplicatedStep);
            newDuplicates.Add(duplicatedStep);

            insertIndex++;
        }

        RenumberTestSteps();
        
        SelectedSteps.Clear();
        foreach (var dup in newDuplicates)
            SelectedSteps.Add(dup);
        
        SelectedStep = newDuplicates.Last();
    }
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private void CopyTestSteps()
    {
        if (SelectedSteps.Count == 0)
            return;
        
        foreach (var step in SelectedSteps)
            step.TestStep.UpdateDtos();
        
        _copiedSteps = SelectedSteps
            .OrderBy(s => TestSteps.IndexOf(s))
            .Select(s => s.TestStep.Clone())
            .ToList();

        PasteTestStepsCommand.NotifyCanExecuteChanged();
    }
    
    [RelayCommand(CanExecute = nameof(CanPasteTestStep))]
    private void PasteTestSteps()
    {
        if (_copiedSteps == null || _copiedSteps.Count == 0)
            return;
        
        int insertIndex;

        if (SelectedSteps.Count == 0)
        {
            insertIndex = 0;
        }
        else
        {
            insertIndex = SelectedSteps
                .Select(s => TestSteps.IndexOf(s))
                .Where(i => i >= 0)
                .Max() + 1;
        }

        var pasted = new List<TestStepViewModel>();

        foreach (var model in _copiedSteps)
        {
            var modelCopy = model.Clone();

            var vm = new TestStepViewModel(modelCopy, TestHardwareRelayChannels.HardwareInfo);
            vm.PropertyChanged += OnStepPropertyChanged;

            TestSteps.Insert(insertIndex, vm);
            pasted.Add(vm);

            insertIndex++;
        }

        RenumberTestSteps();
        
        SelectedSteps.Clear();
        foreach (var p in pasted)
            SelectedSteps.Add(p);

        SelectedStep = pasted.Last();
    }
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private void RemoveTestSteps()
    {
        var toRemove = SelectedSteps
            .OrderByDescending(s => TestSteps.IndexOf(s))
            .ToList();

        foreach (var step in toRemove)
        {
            step.PropertyChanged -= OnStepPropertyChanged;
            TestSteps.Remove(step);
        }

        RenumberTestSteps();
    }
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private void CutTestSteps()
    {
        if (SelectedSteps.Count == 0)
            return;
        
        foreach (var step in SelectedSteps)
            step.TestStep.UpdateDtos();
        
        _copiedSteps = SelectedSteps
            .OrderBy(s => TestSteps.IndexOf(s))
            .Select(s => s.TestStep.Clone())
            .ToList();
        
        var toRemove = SelectedSteps
            .OrderByDescending(s => TestSteps.IndexOf(s)) // remove bottom-up
            .ToList();

        foreach (var step in toRemove)
        {
            step.PropertyChanged -= OnStepPropertyChanged;
            TestSteps.Remove(step);
        }

        RenumberTestSteps();
        
        SelectedSteps.Clear();
        SelectedStep = null;
        
        PasteTestStepsCommand.NotifyCanExecuteChanged();
    }
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
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

    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
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
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private async Task StartTestFromSelectionAsync()
    {
        NumberFailedSteps = 0;
        TestStatus = TestStatus.RUNNING;
        TestProgress = 0;
        await _testExecutor.StartTestAsync(TestSteps, SelectedStepIndex);
    }
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private async Task StartTestRepeatAsync()
    {
        if (!await ShowSerialNumberRequestWindow())
            return;
        
        NumberFailedSteps = 0;
        TestStatus = TestStatus.RUNNING;
        TestProgress = 0;
        SelectedStepIndex = 0;
              
        _allowResultSave = true;
        await _testExecutor.StartRepeatTestAsync(TestSteps, SelectedStepIndex);
        _allowResultSave = false;
    }
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private async Task StartTestAsync()
    {
        if (!await ShowSerialNumberRequestWindow())
            return;
        
        TestStatus = TestStatus.RUNNING;
        NumberFailedSteps = 0;
        TestProgress = 0;
        SelectedStepIndex = 0;

        _allowResultSave = true;
        await _testExecutor.StartTestAsync(TestSteps, SelectedStepIndex);
        _allowResultSave = false;
    }

    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private async Task StartSingleStepTest()
    {
        if(SelectedStep == null) return;
        
        NumberFailedSteps = 0;
        TestProgress = 0;
        TestDuration = string.Empty;
        TestStatus = TestStatus.RUNNING;
        await _testExecutor.StartSingleStepTest(SelectedStep);
        TestStatus = TestStatus.IDLE;
    }

    [RelayCommand]
    private async Task CancelTest()
    {
        await _testExecutor.CancelTest();
    }

    private async Task<bool> ShowSerialNumberRequestWindow()
    {
        if (_projectSettingsViewModel.UseSerialNumber)
        {
            var serial = await _serialNumberDialogService.AskForSerialNumberAsync();

            if (serial == null)
            {
                SerialNumber = string.Empty;
                return false;
            }
            
            SerialNumber = serial;

        }
        else
        {
            SerialNumber = string.Empty;
        }

        return true;
    }

    private void HookExecutorEvents()
    {
        _testExecutor.TestStarted += () =>
        {
            StartTime = DateTimeOffset.Now;
        };
        
        _testExecutor.StepStarted += (index, step) =>
        {
            SelectedStepIndex = index;
            
        };
        
        _testExecutor.StepExecuted += () =>
        {
            TestDuration = $"{Elapsed.TotalSeconds:F2}s";
        };

        _testExecutor.StepCompleted += (index, step) =>
        {
            TestDuration = $"{Elapsed.TotalSeconds:F2}s";
            TestProgress = TestSteps.Count == 0 ? 0 : (int)Math.Round((double)(SelectedStepIndex + 1) / TestSteps.Count * 100);
            
            if (!step.IsPassed)
            {
                NumberFailedSteps++;
            }
        };

        _testExecutor.TestCompleted += () =>
        {
            TestDuration = $"{Elapsed.TotalSeconds:F2}s";
            TestProgress = 100;
            NumberRunTests++;

            if (TestStatus == TestStatus.CANCELLED)
                return;
            
            if (_allowResultSave)
                _ = _testResultExportService.SaveAsync(TestSteps, SerialNumber, NumberFailedSteps);

            if (NumberFailedSteps > 0)
            {
                TestStatus = TestStatus.FAILED;
                return;
            }
            
            TestStatus = TestStatus.PASSED;
            NumberPassedTests++;
        };

        _testExecutor.TestCancelled += () =>
        {
            TestStatus = TestStatus.CANCELLED;
        };

        _testExecutor.TestRepeated += () =>
        {
            TestProgress = 0;
            TestStatus = TestStatus.RUNNING;
        };
    }

    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    public async Task NewFile()
    {
        if (await _projectService.NewProjectAsync())
        {
            TestSteps.Clear();
            TestHardwareRelayChannels.ResetToDefault();
            _projectSettingsViewModel.ResetToDefault();
            _deviceManager.Devices.Clear();
            _projectService.UpdateLastSavedState(CaptureCurrentState());
            SelectedStepIndex = -1;
            AddTestStep();
            NumberPassedTests = 0;
            NumberRunTests = 0;
            TestDuration = string.Empty;
            TestStatus = TestStatus.IDLE;
        }
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
            Devices = _deviceManager.Devices.ToList(),
            DefaultTolerance = _projectSettingsViewModel.ToleranceValue,
            UseSerialNumber = _projectSettingsViewModel.UseSerialNumber,
            SaveTestResults = _projectSettingsViewModel.SaveTestResult,
            SaveTestResultOptions =  _projectSettingsViewModel.SaveTestResultOptions,
            SaveTestResultFilePath = _projectSettingsViewModel.SaveTestResultFilePath,
        };
    }

    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    public async Task SaveFileAs()
    {
        var dto = CaptureCurrentState();
        await _projectService.SaveAsAsync(dto);
    }

    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    public async Task SaveFile()
    {
        var dto = CaptureCurrentState();
        await _projectService.SaveAsync(dto);
    }

    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    public async Task LoadFileWithDialog()
    {
        try
        {
            var dto = await _projectService.OpenFileAsync();
            if (dto != null)
            {
                ApplyDto(dto);
            }
        }
        catch (Exception ex)
        {
            _errorService.AddError("Failed to load file: " + ex.Message);
        }
        
        NumberPassedTests = 0;
        NumberRunTests = 0;
        TestDuration = string.Empty;
        TestStatus = TestStatus.IDLE;
    }
    
    public async Task LoadFile(string fileToLoad)
    {
        try
        {
            var dto = await _projectService.LoadAsync(fileToLoad);
            if (dto != null)
            {
                ApplyDto(dto);
            }
        }
        catch (Exception ex)
        {
            _errorService.AddError("Failed to load file: " + ex.Message);
        }
        
        NumberPassedTests = 0;
        NumberRunTests = 0;
        TestDuration = string.Empty;
        TestStatus = TestStatus.IDLE;
    }

    private void ApplyDto(AtlabFileDto dto)
    {
        TestSteps.Clear();
        foreach (var stepVm in dto.TestSteps.Select(step => new TestStepViewModel(step, TestHardwareRelayChannels.HardwareInfo)))
        {
            stepVm.PropertyChanged += OnStepPropertyChanged;
            TestSteps.Add(stepVm);
        }
        
        _projectSettingsViewModel.ToleranceValue = dto.DefaultTolerance;
        _projectSettingsViewModel.UseSerialNumber = dto.UseSerialNumber;
        _projectSettingsViewModel.SaveTestResult = dto.SaveTestResults;
        _projectSettingsViewModel.SaveTestResultOptions = dto.SaveTestResultOptions;
        _projectSettingsViewModel.SaveTestResultFilePath = dto.SaveTestResultFilePath;
        
        TestHardwareRelayChannels.ApplyChannelNames(dto.StimChannelNames, dto.ExtStimChannelNames, dto.MeasChannelNames);

        _deviceManager.Devices.Clear();
        foreach (var device in dto.Devices)
        {
            _deviceManager.Devices.Add(device);
        }
        
        SelectedStepIndex = 0;
    }
}
