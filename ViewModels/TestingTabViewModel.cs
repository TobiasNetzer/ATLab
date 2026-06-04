using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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
    private readonly IErrorService _errorService;

    private readonly IProjectController _projectController;
    private readonly ITestStepEditor _testStepEditor;
    private readonly ITestExecutionController _testExecutionController;
    private readonly ProjectSettings _projectSettings;
    private readonly ProjectDocumentation _projectDocumentation;
    private readonly DeviceUnderTestInfo _deviceUnderTestInfo;
    private readonly RuntimeVariableEditorViewModel _runtimeVariableEditor;
    private readonly ControlModuleService _controlModuleService;
    
    [ObservableProperty]
    private ObservableCollection<TestStepViewModel> _testSteps = new();

    public List<CustomVariable> RuntimeVariables { get; } = new();
    
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
    private ResponseMaskEditorViewModel _responseMaskEditor;
    
    [ObservableProperty]
    private ScriptSelectorViewModel _scriptSelector;
    
    [ObservableProperty]
    private CommandEditorViewModel _commandEditor;
    
    [ObservableProperty]
    private ShellCommandEditorViewModel _shellCommandEditor;
    
    [ObservableProperty]
    private ExpressionEditorViewModel _expressionEditor;
    
    [ObservableProperty]
    private FilePathEditorViewModel _filePathEditor;
    
    [ObservableProperty]
    private bool _isDevelopmentMode;
    
    [ObservableProperty]
    private int _numberFailedSteps;
    
    [ObservableProperty]
    private int _numberRunTests;
    
    [ObservableProperty]
    private int _numberPassedTests;

    [ObservableProperty]
    private double _passedPercentage;
    
    [ObservableProperty]
    private TestStatus _testStatus = TestStatus.IDLE;
    
    [ObservableProperty]
    private string _testDuration = string.Empty;
    
    [ObservableProperty]
    private string _serialNumber = string.Empty;
    
    [ObservableProperty]
    private string _user = Environment.UserName;

    public bool AllowResultSave { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public TimeSpan Elapsed => DateTimeOffset.Now - StartTime;

    private int _suppressChangesCount;
    
    private bool _isAnimationEnabled = true;
    public bool IsAnimationEnabled
    {
        get => _isAnimationEnabled;
        set
        {
            if (_isAnimationEnabled == value)
                return;
            
            _isAnimationEnabled = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AnimationDuration));
        }
    }
    
    private int _testProgress;

    public int TestProgress
    {
        get => _testProgress;
        set
        {
            if (_testProgress == value)
                return;
            
            _testProgress = value;
            IsAnimationEnabled = _testProgress != 0;
            OnPropertyChanged();
        }
    }
    public TimeSpan AnimationDuration => IsAnimationEnabled ? TimeSpan.FromMilliseconds(200) : TimeSpan.Zero;

    public TestingTabViewModel(
        ISettingsService settingsService,
        IErrorService errorService,
        TestHardwareRelayChannelsViewModel testHardwareRelayChannels,
        TestStepConfiguratorViewModel testStepConfiguratorViewModel,
        ResponseMaskEditorViewModel responseMaskEditor,
        ScriptSelectorViewModel scriptSelector,
        CommandEditorViewModel commandEditor,
        ShellCommandEditorViewModel shellCommandEditor,
        ExpressionEditorViewModel expressionEditor,
        FilePathEditorViewModel filePathEditor,
        IProjectController projectController,
        ITestStepEditor testStepEditor,
        ITestExecutionController testExecutionController,
        DeviceManagerViewModel deviceManager,
        ProjectSettings projectSettings,
        ProjectDocumentation projectDocumentation,
        DeviceUnderTestInfo deviceUnderTestInfo,
        RuntimeVariableEditorViewModel runtimeVariableEditor,
        ControlModuleService controlModuleService)
    {
        _settingsService = settingsService;
        _errorService = errorService;
        TestHardwareRelayChannels = testHardwareRelayChannels;
        TestStepConfiguratorViewModel = testStepConfiguratorViewModel;
        ResponseMaskEditor = responseMaskEditor;
        ScriptSelector = scriptSelector;
        CommandEditor = commandEditor;
        ShellCommandEditor = shellCommandEditor;
        ExpressionEditor = expressionEditor;
        FilePathEditor = filePathEditor;
        _projectController = projectController;
        _testStepEditor = testStepEditor;
        _testExecutionController = testExecutionController;
        _projectSettings = projectSettings;
        _projectDocumentation = projectDocumentation;
        _deviceUnderTestInfo = deviceUnderTestInfo;
        _runtimeVariableEditor = runtimeVariableEditor;
        _controlModuleService = controlModuleService;

        Title = "Test Environment";
        IsDevelopmentMode = settingsService.Settings.IsDevelopmentMode;

        TestSteps.CollectionChanged += (_, _) => CheckForChanges();
        TestHardwareRelayChannels.ConfigurationChanged += () => CheckForChanges();
        _projectSettings.SettingsChanged += () => CheckForChanges();
        deviceManager.Devices.CollectionChanged += DevicesChanged;
        _projectDocumentation.DocumentationChanged += () => CheckForChanges();
        _deviceUnderTestInfo.DeviceUnderTestInfoChanged += () => CheckForChanges();
        _runtimeVariableEditor.RuntimeVariables.CollectionChanged += RuntimeVariablesChanged;
        
        foreach (var device in deviceManager.Devices)
            SubscribeToDevice(device);
        _testExecutionController.HookExecutorEvents(this);
        
        _projectSettings.ControlModuleSettingChanged += _controlModuleService.Initialize;

        _controlModuleService.StartPressed += async () =>
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (StartTestCommand.CanExecute(null))
                    await StartTestCommand.ExecuteAsync(null);
            });
        };
        
        _controlModuleService.StopPressed += () =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                CancelTestCommand.ExecuteAsync(null);
            });
        };
    }

    public IDisposable SuppressDirtyTracking()
    {
        _suppressChangesCount++;
        return new ActionOnDispose(() => _suppressChangesCount--);
    }

    private void CheckForChanges()
    {
        if (_suppressChangesCount > 0)
            return;

        _projectController.MarkDirty();
    }

    public void AddInitialStep() => _testStepEditor.AddStep(this);

    public void ResetTestCounters()
    {
        NumberPassedTests = 0;
        NumberRunTests = 0;
        NumberFailedSteps = 0;
        TestDuration = string.Empty;
        TestStatus = TestStatus.IDLE;
    }

    public void NotifyPasteChanged() => PasteTestStepsCommand.NotifyCanExecuteChanged();

    public void OnStepPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not TestStepViewModel changedVm)
            return;
        
        var isTestStepProperty = string.IsNullOrWhiteSpace(e.PropertyName) || 
                                 typeof(TestStep).GetProperty(e.PropertyName) != null;

        if (isTestStepProperty)
            CheckForChanges();

        if (changedVm != SelectedStep)
            return;
        
        if (string.IsNullOrWhiteSpace(e.PropertyName))
            return;
        
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
    
    private void RuntimeVariablesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (CustomVariable variable in e.NewItems)
                SubscribeToVariable(variable);
        }

        if (e.OldItems != null)
        {
            foreach (CustomVariable variable in e.OldItems)
                UnsubscribeFromVariable(variable);
        }
        
        RuntimeVariables.Clear();
        foreach (var v in _runtimeVariableEditor.RuntimeVariables)
        {
            RuntimeVariables.Add(v.Clone());
        }

        CheckForChanges();
    }
    
    private void SubscribeToVariable(CustomVariable variable)
    {
        variable.PropertyChanged += VariableChanged;
    }

    private void UnsubscribeFromVariable(CustomVariable variable)
    {
        variable.PropertyChanged -= VariableChanged;
    }

    private void VariableChanged(object? sender, PropertyChangedEventArgs e)
    {
        RuntimeVariables.Clear();
        foreach (var v in _runtimeVariableEditor.RuntimeVariables)
        {
            RuntimeVariables.Add(v.Clone());
        }
        
        CheckForChanges();
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
        
        _suppressChangesCount++;
        try
        {
            TestHardwareRelayChannels.MeasChannelViewModel.LoadActiveMeasChannels(value.TestStep.MatrixState);
            TestHardwareRelayChannels.StimChannelViewModel.LoadRelayStates(value.TestStep.LiveStimState);
            TestHardwareRelayChannels.ExtStimChannelViewModel.LoadRelayStates(value.TestStep.LiveExtStimState);
            TestStepConfiguratorViewModel.LoadTestStep(value, TestSteps);
            ScriptSelector.LoadTestStep(value);
            CommandEditor.LoadTestStep(value);
            ShellCommandEditor.LoadTestStep(value.TestStep.ShellCommand);
            ExpressionEditor.LoadTestStep(value.TestStep);
            FilePathEditor.LoadTestStep(value.TestStep);
            ResponseMaskEditor.LoadTestStep(value);
        }
        catch (Exception ex)
        {
            _errorService.AddError("Exception: " + ex.Message);
        }
        finally
        {
            _suppressChangesCount--;
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

        if (_projectSettings.IsControlModuleEnabled)
            _controlModuleService.SetStatus(value);
    }
    
    partial void OnIsDevelopmentModeChanged(bool value)
    {
        _settingsService.Settings.IsDevelopmentMode = value;
    }
    
    private bool IsNotTestRunning() => TestStatus != TestStatus.RUNNING;
    private bool CanPasteTestStep() => IsNotTestRunning() && _testStepEditor.HasClipboard;

    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    public Task NewFile() => _projectController.NewProjectAsync(this);
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private Task SaveFile() => _projectController.SaveFileAsync(this);
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private Task SaveFileAs() => _projectController.SaveFileAsAsync(this);
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private Task LoadFileWithDialog() => _projectController.LoadFileWithDialogAsync(this);
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    public Task LoadFile(string path) => _projectController.LoadFileAsync(this, path);

    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private void AddTestStep() => _testStepEditor.AddStep(this);
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private void DuplicateTestSteps() => _testStepEditor.DuplicateSteps(this);
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private void CopyTestSteps() => _testStepEditor.CopySteps(this);
    
    [RelayCommand(CanExecute = nameof(CanPasteTestStep))]
    private void PasteTestSteps() => _testStepEditor.PasteSteps(this);
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private void CutTestSteps() => _testStepEditor.CutSteps(this);
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private void RemoveTestSteps() => _testStepEditor.RemoveSteps(this);
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private void MoveStepUp() => _testStepEditor.MoveStepUp(this);
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private void MoveStepDown() => _testStepEditor.MoveStepDown(this);
    

    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private Task StartTest() => _testExecutionController.StartTestAsync(this);
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private Task StartTestRepeat() => _testExecutionController.StartRepeatAsync(this);
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private Task StartTestFromSelection() => _testExecutionController.StartFromSelectionAsync(this);
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private Task StartSingleStepTest() => _testExecutionController.StartSingleStepAsync(this);
    
    [RelayCommand]
    private Task CancelTest() => _testExecutionController.CancelAsync();
}