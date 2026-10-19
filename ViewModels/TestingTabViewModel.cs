using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
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
    private readonly IHardwareInfo _hardwareInfo;
    private readonly ISettingsService _settingsService;
    private readonly ILoggingService _loggingService;
    private readonly IProjectController _projectController;
    private readonly ITestStepEditor _testStepEditor;
    private readonly ITestModuleEditor _testModuleEditor;
    private readonly ControlModuleService _controlModuleService;
    private readonly ProjectModel _projectModel;
    private readonly ITestExecutor _testExecutor;
    private readonly ISerialNumberDialogService _serialNumberDialogService;
    private readonly ITestResultExportService _testResultExportService;
    private readonly IMessageBoxService _messageBoxService;
    
    private readonly List<CustomVariable> _runtimeVariables = new();
    private readonly ObservableCollection<TestStepViewModel> _emptySteps = new();

    // Flattened steps of all enabled modules, captured when a test run starts
    private List<TestStepViewModel> _executionSteps = new();
    private readonly SystemVariables _systemVariables = new();
    
    public IStatusService StatusService { get; }
    
    public ObservableCollection<TestModuleViewModel> Modules { get; } = new();

    // Steps of the selected module, displayed in the data grid
    public ObservableCollection<TestStepViewModel> TestSteps => SelectedModule?.TestSteps ?? _emptySteps;

    private IEnumerable<TestStepViewModel> AllTestSteps => Modules.SelectMany(m => m.TestSteps);

    public WorkspaceEditorViewModel WorkspaceEditor { get; }

    [ObservableProperty]
    private TestModuleViewModel? _selectedModule;

    [ObservableProperty]
    private TestStepViewModel? _selectedStep;
    
    [ObservableProperty]
    private ObservableCollection<TestStepViewModel> _selectedSteps = new();
    
    [ObservableProperty]
    private bool _isDevelopmentMode;
    
    [ObservableProperty]
    private bool _isDebugMode;
    
    [ObservableProperty]
    private bool _isShowContinueButton;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestSingleStepContinueCommand))]
    private bool _isSingleStepContinueRequested;
    
    [ObservableProperty]
    private bool _isShowMeasurementPanel;
    
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
    private string _testDuration = "0s";
    
    [ObservableProperty]
    private string _serialNumber = "-";
    
    [ObservableProperty]
    private string _user = Environment.UserName;

    [ObservableProperty]
    private string _deviceUnderTestSummary = "-";

    public DeviceUnderTestInfo DeviceUnderTestInfo => _projectModel.DeviceUnderTestInfo;
    
    [ObservableProperty]
    private bool _startTestBusy;
    
    [ObservableProperty]
    private bool _startFromStepBusy;
    
    [ObservableProperty]
    private bool _startRepeatTestBusy;
    
    [ObservableProperty]
    private bool _startSingleStepBusy;

    [ObservableProperty]
    private bool _startModuleTestBusy;
    
    private bool _isPartialRun;

    private bool _allowResultSave;
    private DateTimeOffset _startTime;
    private TimeSpan Elapsed => DateTimeOffset.Now - _startTime;

    private bool IsAnimationEnabled
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AnimationDuration));
        }
    } = true;

    public int TestProgress
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;
            IsAnimationEnabled = field != 0;
            OnPropertyChanged();
        }
    }

    public TimeSpan AnimationDuration => IsAnimationEnabled ? TimeSpan.FromMilliseconds(200) : TimeSpan.Zero;

    public TestingTabViewModel(
        ISettingsService settingsService,
        ILoggingService loggingService,
        ProjectModel projectModel,
        WorkspaceEditorViewModel workspaceEditor,
        IProjectController projectController,
        ITestStepEditor testStepEditor,
        ITestModuleEditor testModuleEditor,
        ControlModuleService controlModuleService,
        IHardwareAccessor hardwareAccessor,
        ITestExecutor testExecutor,
        ISerialNumberDialogService serialNumberDialogService,
        ITestResultExportService testResultExportService,
        IMessageBoxService messageBoxService,
        IStatusService statusService)
    {
        _hardwareInfo = hardwareAccessor.Hardware.HardwareInfo;
        _settingsService = settingsService;
        _loggingService = loggingService;
        _projectModel = projectModel;
        WorkspaceEditor = workspaceEditor;
        _projectController = projectController;
        _testStepEditor = testStepEditor;
        _testModuleEditor = testModuleEditor;
        _testExecutor = testExecutor;
        _serialNumberDialogService = serialNumberDialogService;
        _testResultExportService = testResultExportService;
        _messageBoxService = messageBoxService;
        StatusService = statusService;

        _controlModuleService = controlModuleService;

        SetLocalizedTitle("Tab_Testing");
        IsDevelopmentMode = settingsService.Settings.IsDevelopmentMode;
        IsShowMeasurementPanel = settingsService.Settings.IsShowMeasurementPanel;

        _projectModel.Modules.CollectionChanged += ProjectModulesChanged;
        foreach (var module in _projectModel.Modules)
            Modules.Add(CreateModuleViewModel(module));
        
        _projectModel.RuntimeVariableChanged += SynchronizeRuntimeVariables;
        SynchronizeRuntimeVariables();

        _projectModel.DeviceUnderTestInfo.DeviceUnderTestInfoChanged += UpdateDeviceUnderTestSummary;
        UpdateDeviceUnderTestSummary();

        HookTestEvents();
        
        _projectModel.Settings.ControlModuleSettingChanged += () => 
        {
            if (_projectModel.Settings.IsControlModuleEnabled)
                _controlModuleService.Initialize();
            else
                _controlModuleService.Dispose();
        };
 
        _controlModuleService.StartPressed += async () =>
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (StartTestCommand.CanExecute(null))
                    await StartTestCommand.ExecuteAsync(null);
                
                RequestBreakRepeatCommand.Execute(null);
            });
        };
        
        _controlModuleService.StopPressed += () =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                CancelTestCommand.ExecuteAsync(null);
            });
        };
        
        _testExecutor.SingleStepContinueRequestedChanged += OnSingleStepContinueRequestedChanged;
    }
    
    private void ProjectModulesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
            {
                var index = e.NewStartingIndex;

                foreach (TestModule module in e.NewItems!)
                    Modules.Insert(index++, CreateModuleViewModel(module));

                break;
            }
            case NotifyCollectionChangedAction.Remove:
            {
                foreach (TestModule module in e.OldItems!)
                {
                    var vm = Modules.First(x => ReferenceEquals(x.Module, module));
                    RemoveModuleViewModel(vm);
                }

                break;
            }
            case NotifyCollectionChangedAction.Move:
            {
                var selected = SelectedModule;
                Modules.Move(e.OldStartingIndex, e.NewStartingIndex);
                SelectedModule = selected;
                break;
            }
            case NotifyCollectionChangedAction.Replace:
            case NotifyCollectionChangedAction.Reset:
            {
                foreach (var vm in Modules.ToList())
                    RemoveModuleViewModel(vm);

                foreach (var module in _projectModel.Modules)
                    Modules.Add(CreateModuleViewModel(module));

                break;
            }
        }

        NotifyFailedStepCommandsChanged();
    }

    private TestModuleViewModel CreateModuleViewModel(TestModule module)
    {
        var vm = new TestModuleViewModel(module, _hardwareInfo);
        vm.StepPropertyChanged += OnStepPropertyChanged;
        vm.TestSteps.CollectionChanged += OnModuleTestStepsChanged;
        return vm;
    }

    private void RemoveModuleViewModel(TestModuleViewModel vm)
    {
        vm.StepPropertyChanged -= OnStepPropertyChanged;
        vm.TestSteps.CollectionChanged -= OnModuleTestStepsChanged;
        vm.Detach();
        Modules.Remove(vm);

        if (SelectedModule == vm)
            SelectedModule = null;

        if (SelectedStep?.Module == vm.Module)
            SelectedStep = null;
    }

    private TestModuleViewModel? FindModuleViewModel(TestModule module)
    {
        return Modules.FirstOrDefault(x => ReferenceEquals(x.Module, module));
    }

    private void SelectFirstModule()
    {
        SelectedModule = Modules.FirstOrDefault();
        SelectedStep = TestSteps.FirstOrDefault();
    }

    private void SynchronizeRuntimeVariables()
    {
        _runtimeVariables.Clear();

        foreach (var variable in _projectModel.RuntimeVariables)
            _runtimeVariables.Add(variable.Clone());

        _runtimeVariables.AddRange(_systemVariables.All);
    }

    private void UpdateDeviceUnderTestSummary()
    {
        var info = _projectModel.DeviceUnderTestInfo;

        var parts = new[]
            {
                info.DeviceName,
                string.IsNullOrWhiteSpace(info.Revision) ? null : $"Rev. {info.Revision}",
                info.Variant,
                info.PartNumber
            }
            .Where(p => !string.IsNullOrWhiteSpace(p));

        var summary = string.Join(" · ", parts);
        DeviceUnderTestSummary = string.IsNullOrEmpty(summary) ? "-" : summary;
    }

    private void UpdateSystemVariables(TestStepViewModel? step)
    {
        _systemVariables.SerialNumber.Value = SerialNumber;
        _systemVariables.User.Value = User;
        _systemVariables.ProjectName.Value = _projectModel.ProjectName;
        _systemVariables.FailedSteps.Value = NumberFailedSteps.ToString(CultureInfo.InvariantCulture);
        _systemVariables.UpdateTimestamp(DateTimeOffset.Now, Elapsed);

        if (step != null)
        {
            _systemVariables.ModuleName.Value = step.Module.Name;
            _systemVariables.StepNumber.Value = step.TestStep.Number.ToString(CultureInfo.InvariantCulture);
        }
    }
    
    private int ComputeInsertIndex()
    {
        if (SelectedSteps.Count == 0)
            return TestSteps.Count;

        return SelectedSteps
            .Select(x => TestSteps.IndexOf(x))
            .Max() + 1;
    }

    private TestStepViewModel FindViewModel(TestStep model)
    {
        return TestSteps.First(x => ReferenceEquals(x.TestStep, model));
    }

    private void Select(TestStep model)
    {
        SelectedSteps.Clear();

        var vm = FindViewModel(model);

        SelectedSteps.Add(vm);
        SelectedStep = vm;
    }

    private void Select(IEnumerable<TestStep> models)
    {
        SelectedSteps.Clear();

        foreach (var model in models)
            SelectedSteps.Add(FindViewModel(model));

        SelectedStep = SelectedSteps.LastOrDefault();
    }

    private void ResetTestCounters()
    {
        NumberPassedTests = 0;
        NumberRunTests = 0;
        NumberFailedSteps = 0;
        PassedPercentage = 100;
        TestDuration = "0s";
        TestStatus = TestStatus.IDLE;
    }

    private void NotifyModuleCommandsChanged()
    {
        RemoveModuleCommand.NotifyCanExecuteChanged();
        DuplicateModuleCommand.NotifyCanExecuteChanged();
        MoveModuleUpCommand.NotifyCanExecuteChanged();
        MoveModuleDownCommand.NotifyCanExecuteChanged();
    }

    private void NotifyPasteChanged()
    {
        PasteTestStepsCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(_testStepEditor.CanPaste));
    }
    
    private void OnModuleTestStepsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        NotifyMoveStepCommandsChanged();
        NotifyFailedStepCommandsChanged();
    }

    private void NotifyMoveStepCommandsChanged()
    {
        MoveStepUpCommand.NotifyCanExecuteChanged();
        MoveStepDownCommand.NotifyCanExecuteChanged();
    }

    private void NotifyFailedStepCommandsChanged()
    {
        GoToPreviousFailedStepCommand.NotifyCanExecuteChanged();
        GoToNextFailedStepCommand.NotifyCanExecuteChanged();
    }

    private static bool IsFailedStep(TestStepViewModel step) => step.IsExecuted && !step.IsPassed;
    
    private TestStepViewModel? FindFailedStep(bool forward)
    {
        var steps = AllTestSteps.ToList();
        var current = SelectedStep != null ? steps.IndexOf(SelectedStep) : -1;

        return forward
            ? steps.Skip(current + 1).FirstOrDefault(IsFailedStep)
            : steps.Take(Math.Max(current, 0)).LastOrDefault(IsFailedStep);
    }

    private void SelectFailedStep(bool forward)
    {
        var step = FindFailedStep(forward);

        if (step == null)
            return;

        if (SelectedModule?.Module != step.Module)
            SelectedModule = FindModuleViewModel(step.Module);

        Select(step.TestStep);
    }

    private bool _isUpdatingMultipleSteps;

    private void OnStepPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TestStepViewModel.IsExecuted) or nameof(TestStepViewModel.IsPassed))
            NotifyFailedStepCommandsChanged();

        if (_isUpdatingMultipleSteps || _isChangingSelection || sender is not TestStepViewModel changedVm)
            return;

        var isTestStepProperty = string.IsNullOrWhiteSpace(e.PropertyName) ||
                                 typeof(TestStep).GetProperty(e.PropertyName) != null;
        
        if (isTestStepProperty)
            _projectModel.MarkDirty();

        if (changedVm != SelectedStep || !isTestStepProperty || string.IsNullOrWhiteSpace(e.PropertyName))
            return;

        _isUpdatingMultipleSteps = true;
        try
        {
            foreach (var vm in SelectedSteps)
            {
                if (vm == changedVm)
                    continue;

                CopyChangedProperty(changedVm.TestStep, vm.TestStep, e.PropertyName);
            }
        }
        finally
        {
            _isUpdatingMultipleSteps = false;
        }
    }
    
    private static void CopyChangedProperty(TestStep source, TestStep target, string propertyName)
    {
        var prop = typeof(TestStep).GetProperty(propertyName);
        if (prop == null)
            return;

        var sourceValue = prop.GetValue(source);
        var targetValue = prop.GetValue(target);

        if (prop.CanWrite)
        {
            if (Equals(sourceValue, targetValue))
                return;

            var newValue = sourceValue switch
            {
                PassFailAction p => new PassFailAction(p),
                ScriptCommand s => new ScriptCommand(s),
                ResponseMask r => new ResponseMask(r),
                ShellCommand sc => new ShellCommand(sc),
                RelayMatrix rm => new RelayMatrix(rm),
                RelayGroup rg => new RelayGroup(rg),
                _ => sourceValue
            };

            prop.SetValue(target, newValue);
        }
        else if (sourceValue is ObservableCollection<CustomVariable> sourceCollection &&
                 targetValue is ObservableCollection<CustomVariable> targetCollection)
        {
            targetCollection.Clear();
            foreach (var item in sourceCollection)
            {
                targetCollection.Add(item.Clone());
            }
        }
    }
    
    private bool _isChangingSelection;
    
    partial void OnSelectedStepChanged(TestStepViewModel? value)
    {
        // Without a selection the (disabled) editor shows a blank step that isn't part of the project,
        // so it doesn't keep displaying the previously selected step
        var stepToLoad = value ?? new TestStepViewModel(new TestStep(), new TestModule(), _hardwareInfo);

        NotifyMoveStepCommandsChanged();
        NotifyFailedStepCommandsChanged();

        _isChangingSelection = true;
        try
        {
            using (_projectModel.SuppressDirtyTracking())
            {
                try
                {
                    WorkspaceEditor.LoadTestStep(stepToLoad, AllTestSteps);
                }
                catch (Exception ex)
                {
                    _loggingService.Error("Exception: " + ex.Message);
                }
            }
        }
        finally
        {
            _isChangingSelection = false;
        }
    }

    partial void OnSelectedModuleChanged(TestModuleViewModel? value)
    {
        OnPropertyChanged(nameof(TestSteps));
        SelectedSteps.Clear();

        if (value != null && SelectedStep?.Module != value.Module)
            SelectedStep = value.TestSteps.FirstOrDefault();

        AddTestStepCommand.NotifyCanExecuteChanged();
        PasteTestStepsCommand.NotifyCanExecuteChanged();
        StartModuleTestCommand.NotifyCanExecuteChanged();
        NotifyModuleCommandsChanged();
        NotifyMoveStepCommandsChanged();
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
        NotifyFailedStepCommandsChanged();
        NewFileCommand.NotifyCanExecuteChanged();
        SaveFileCommand.NotifyCanExecuteChanged();
        LoadFileWithDialogCommand.NotifyCanExecuteChanged();
        SaveFileAsCommand.NotifyCanExecuteChanged();
        StartTestFromSelectionCommand.NotifyCanExecuteChanged();
        StartTestRepeatCommand.NotifyCanExecuteChanged();
        StartTestCommand.NotifyCanExecuteChanged();
        StartSingleStepTestCommand.NotifyCanExecuteChanged();
        StartModuleTestCommand.NotifyCanExecuteChanged();
        RequestBreakRepeatCommand.NotifyCanExecuteChanged();
        RequestSingleStepContinueCommand.NotifyCanExecuteChanged();
        ToggleDebugModeCommand.NotifyCanExecuteChanged();
        ToggleDevelopmentModeCommand.NotifyCanExecuteChanged();
        AddModuleCommand.NotifyCanExecuteChanged();
        NotifyModuleCommandsChanged();

        if (_projectModel.Settings.IsControlModuleEnabled)
            _controlModuleService.SetStatus(value);
    }
    
    partial void OnIsDevelopmentModeChanged(bool value)
    {
        _settingsService.Settings.IsDevelopmentMode = value;
    }
    
    partial void OnIsShowMeasurementPanelChanged(bool value)
    {
        _settingsService.Settings.IsShowMeasurementPanel = value;
    }
    
    private void OnSingleStepContinueRequestedChanged(bool value)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            IsSingleStepContinueRequested = value;
        });
    }
    
    private bool IsNotTestRunning() => TestStatus != TestStatus.RUNNING;
    private bool IsDialogClosed() => !_messageBoxService.IsDialogOpen && !_serialNumberDialogService.IsDialogOpen;
    private bool CanRequestBreakRepeat() => !_messageBoxService.IsDialogOpen && TestStatus == TestStatus.RUNNING && !_serialNumberDialogService.IsDialogOpen;
    private bool CanRequestSingleStepContinue() => IsDebugMode && TestStatus == TestStatus.RUNNING && !IsSingleStepContinueRequested;
    private bool IsModuleSelected() => IsNotTestRunning() && SelectedModule != null;
    private bool CanPasteTestStep() => IsModuleSelected() && _testStepEditor.CanPaste;
    private bool CanRemoveModule() => IsModuleSelected() && Modules.Count > 1;
    private bool CanMoveModuleUp() => IsModuleSelected() && Modules.IndexOf(SelectedModule!) > 0;
    private bool CanMoveModuleDown() => IsModuleSelected() && Modules.IndexOf(SelectedModule!) < Modules.Count - 1;
    private bool CanGoToPreviousFailedStep() => IsNotTestRunning() && FindFailedStep(forward: false) != null;
    private bool CanGoToNextFailedStep() => IsNotTestRunning() && FindFailedStep(forward: true) != null;
    private bool CanMoveStepUp() => IsModuleSelected() && SelectedStep != null && TestSteps.IndexOf(SelectedStep) > 0;
    private bool CanMoveStepDown() => IsModuleSelected() && SelectedStep != null && TestSteps.IndexOf(SelectedStep) is var index && index >= 0 && index < TestSteps.Count - 1;

    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private async Task NewFile()
    {
        await _projectController.NewProjectAsync();

        SelectFirstModule();
        
        ResetTestCounters();
    }
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private Task SaveFile() => _projectController.SaveFileAsync();
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private Task SaveFileAs() => _projectController.SaveFileAsAsync();
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private async Task LoadFileWithDialog()
    {
        await _projectController.LoadFileWithDialogAsync();

        SelectFirstModule();
        
        ResetTestCounters();
    }
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    public async Task LoadFile(string path)
    {
        await _projectController.LoadFileAsync(path);

        SelectFirstModule();
        
        ResetTestCounters();
    }

    [RelayCommand(CanExecute = nameof(IsModuleSelected))]
    private void AddTestStep()
    {
        if (SelectedModule == null)
            return;

        var step = _testStepEditor.AddStep(SelectedModule.Module, ComputeInsertIndex());

        Select(step);
    }
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private void DuplicateTestSteps()
    {
        if (SelectedModule == null || SelectedSteps.Count == 0)
            return;

        var models = SelectedSteps
            .Select(x => x.TestStep)
            .ToList();

        var inserted = _testStepEditor.DuplicateSteps(
            SelectedModule.Module,
            models,
            ComputeInsertIndex());

        Select(inserted);
    }
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private void CopyTestSteps()
    {
        if (SelectedModule == null || SelectedSteps.Count == 0)
            return;

        _testStepEditor.CopySteps(
            SelectedModule.Module,
            SelectedSteps.Select(x => x.TestStep));

        NotifyPasteChanged();
    }
    
    [RelayCommand(CanExecute = nameof(CanPasteTestStep))]
    private void PasteTestSteps()
    {
        if (SelectedModule == null)
            return;

        var inserted = _testStepEditor.PasteSteps(SelectedModule.Module, ComputeInsertIndex());

        if (inserted.Count == 0)
            return;

        Select(inserted);

        NotifyPasteChanged();
    }
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private void CutTestSteps()
    {
        if (SelectedModule == null)
            return;

        var removed = _testStepEditor.CutSteps(
            SelectedModule.Module,
            SelectedSteps.Select(x => x.TestStep));

        if (removed.Count == 0)
            return;

        SelectedSteps.Clear();
        SelectedStep = null;

        NotifyPasteChanged();
    }
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private void RemoveTestSteps()
    {
        if (SelectedModule == null || SelectedSteps.Count == 0)
            return;

        var models = SelectedSteps
            .Select(x => x.TestStep)
            .ToList();

        var index = _testStepEditor.RemoveSteps(SelectedModule.Module, models);

        if (index >= 0)
        {
            Select(TestSteps[index].TestStep);
        }
        else
        {
            SelectedSteps.Clear();
            SelectedStep = null;
        }
    }
    
    [RelayCommand(CanExecute = nameof(CanGoToPreviousFailedStep))]
    private void GoToPreviousFailedStep() => SelectFailedStep(forward: false);

    [RelayCommand(CanExecute = nameof(CanGoToNextFailedStep))]
    private void GoToNextFailedStep() => SelectFailedStep(forward: true);

    [RelayCommand(CanExecute = nameof(CanMoveStepUp))]
    private void MoveStepUp()
    {
        if (SelectedModule == null || SelectedStep == null)
            return;

        var step = SelectedStep.TestStep;

        if (_testStepEditor.MoveStepUp(SelectedModule.Module, step))
            Select(step);
    }

    [RelayCommand(CanExecute = nameof(CanMoveStepDown))]
    private void MoveStepDown()
    {
        if (SelectedModule == null || SelectedStep == null)
            return;

        var step = SelectedStep.TestStep;

        if (_testStepEditor.MoveStepDown(SelectedModule.Module, step))
            Select(step);
    }

    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private void AddModule()
    {
        var index = SelectedModule != null
            ? Modules.IndexOf(SelectedModule) + 1
            : Modules.Count;

        var module = _testModuleEditor.AddModule(index);

        SelectedModule = FindModuleViewModel(module);
        NotifyModuleCommandsChanged();
    }

    [RelayCommand(CanExecute = nameof(IsModuleSelected))]
    private void DuplicateModule()
    {
        if (SelectedModule == null)
            return;

        var clone = _testModuleEditor.DuplicateModule(SelectedModule.Module);

        SelectedModule = FindModuleViewModel(clone);
        NotifyModuleCommandsChanged();
    }

    [RelayCommand(CanExecute = nameof(CanRemoveModule))]
    private async Task RemoveModule()
    {
        if (SelectedModule == null)
            return;

        var module = SelectedModule.Module;
        var localization = LocalizationService.Instance;

        var confirmed = await _messageBoxService.ShowConfirmationDestructiveAsync(
            localization["Title_DeleteModule"],
            string.Format(localization["Text_DeleteModule"], module.Name));

        if (!confirmed)
            return;

        var index = _testModuleEditor.RemoveModule(module);

        SelectedModule = index >= 0 ? Modules[index] : null;
        NotifyModuleCommandsChanged();
    }

    [RelayCommand(CanExecute = nameof(CanMoveModuleUp))]
    private void MoveModuleUp()
    {
        if (SelectedModule != null && _testModuleEditor.MoveModuleUp(SelectedModule.Module))
            NotifyModuleCommandsChanged();
    }

    [RelayCommand(CanExecute = nameof(CanMoveModuleDown))]
    private void MoveModuleDown()
    {
        if (SelectedModule != null && _testModuleEditor.MoveModuleDown(SelectedModule.Module))
            NotifyModuleCommandsChanged();
    }

    private List<TestStepViewModel> BuildExecutionSteps()
    {
        return Modules
            .Where(m => m.Module.IsEnabled)
            .SelectMany(m => m.TestSteps)
            .ToList();
    }
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private async Task StartTestAsync()
    {
        using (_projectModel.SuppressDirtyTracking())
        {
            StartTestBusy = true;
            ResetAllResults();
            NumberFailedSteps = 0;
            TestProgress = 0;
            TestDuration = "0s";
            
            if (!await RequestSerialNumber())
            {
                StartTestBusy = false;
                return;
            }

            TestStatus = TestStatus.RUNNING;
            _executionSteps = BuildExecutionSteps();

            _allowResultSave = true;
            IsShowContinueButton = true;
            await _testExecutor.StartTestAsync(_executionSteps, 0, _runtimeVariables);
            IsShowContinueButton = false;
            _allowResultSave = false;
            StartTestBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private async Task StartTestRepeat()
    {
        using (_projectModel.SuppressDirtyTracking())
        {
            StartRepeatTestBusy = true;
            ResetAllResults();
            NumberFailedSteps = 0;
            TestProgress = 0;
            TestDuration = "0s";
            
            if (!await RequestSerialNumber())
            {
                StartRepeatTestBusy = false;
                return;
            }
            
            TestStatus = TestStatus.RUNNING;
            _executionSteps = BuildExecutionSteps();

            _allowResultSave = true;
            IsShowContinueButton = true;
            await _testExecutor.StartRepeatTestAsync(_executionSteps, 0, _runtimeVariables);
            IsShowContinueButton = false;
            _allowResultSave = false;
            StartRepeatTestBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private async Task StartTestFromSelection()
    {
        using (_projectModel.SuppressDirtyTracking())
        {
            var module = SelectedStep != null ? FindModuleViewModel(SelectedStep.Module) : SelectedModule;

            if (module == null)
                return;
            
            _executionSteps = module.TestSteps.ToList();
            var index = SelectedStep != null ? Math.Max(_executionSteps.IndexOf(SelectedStep), 0) : 0;

            StartFromStepBusy = true;
            _isPartialRun = true;
            ResetAllResults();
            NumberFailedSteps = 0;
            TestStatus = TestStatus.RUNNING;
            TestProgress = 0;
            TestDuration = "0s";

            IsShowContinueButton = true;
            try
            {
                await _testExecutor.StartTestAsync(_executionSteps, index, _runtimeVariables);
            }
            finally
            {
                IsShowContinueButton = false;
                _isPartialRun = false;
                StartFromStepBusy = false;
            }
        }
    }

    [RelayCommand(CanExecute = nameof(IsModuleSelected))]
    private async Task StartModuleTest()
    {
        if (SelectedModule == null)
            return;

        using (_projectModel.SuppressDirtyTracking())
        {
            StartModuleTestBusy = true;
            _isPartialRun = true;
            ResetAllResults();
            NumberFailedSteps = 0;
            TestProgress = 0;
            TestDuration = "0s";
            TestStatus = TestStatus.RUNNING;

            // The selected module runs even if it is disabled for the full test
            _executionSteps = SelectedModule.TestSteps.ToList();

            IsShowContinueButton = true;
            try
            {
                await _testExecutor.StartTestAsync(_executionSteps, 0, _runtimeVariables);
            }
            finally
            {
                IsShowContinueButton = false;
                _isPartialRun = false;
                StartModuleTestBusy = false;
            }
        }
    }

    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private async Task StartSingleStepTest()
    {
        if (SelectedStep == null)
            return;

        using (_projectModel.SuppressDirtyTracking())
        {
            StartSingleStepBusy = true;
            NumberFailedSteps = 0;
            TestProgress = 0;
            TestDuration = "0s";
            TestStatus = TestStatus.RUNNING;
            _startTime = DateTimeOffset.Now;
            UpdateSystemVariables(SelectedStep);

            await _testExecutor.StartSingleStepTest(SelectedStep, _runtimeVariables);

            TestStatus = TestStatus.IDLE;
            StartSingleStepBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(IsDialogClosed))]
    private Task CancelTest() => _testExecutor.CancelTest();

    [RelayCommand(CanExecute = nameof(CanRequestBreakRepeat))]
    private void RequestBreakRepeat() => _testExecutor.RequestBreakRepeat();
    
    [RelayCommand(CanExecute = nameof(CanRequestSingleStepContinue))]
    private void RequestSingleStepContinue() => _testExecutor.RequestSingleStepContinue();
    
    [RelayCommand]
    private void ToggleMeasurementPanel() => IsShowMeasurementPanel = !IsShowMeasurementPanel;
    
    [RelayCommand]
    private void ToggleDevelopmentMode() => IsDevelopmentMode = !IsDevelopmentMode;

    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private void ToggleDebugMode()
    {
        IsDebugMode = !IsDebugMode;
        _testExecutor.SetDebugMode(IsDebugMode);
    }

    private void ResetAllResults()
    {
        TestStatus = TestStatus.IDLE;
        foreach (var module in Modules)
            module.ResetResults();
    }

    private async Task<bool> RequestSerialNumber()
    {
        if (_projectModel.Settings.IsUseSerialNumber)
        {
            var serial = await _serialNumberDialogService.AskForSerialNumberAsync();

            if (serial == null)
            {
                SerialNumber = "-";
                return false;
            }

            SerialNumber = serial;
        }
        else
        {
            SerialNumber = "-";
        }

        return true;
    }
    
    private void UpdatePassedPercentage()
    {
        if (NumberRunTests > 0)
            PassedPercentage = Math.Round((double)NumberPassedTests / NumberRunTests * 100, 2);
        else
            PassedPercentage = 0;
    }
    
    private void HookTestEvents()
    {
        _testExecutor.TestStarted += () =>
        {
            _startTime = DateTimeOffset.Now;
        };

        _testExecutor.StepStarted += (index, step) =>
        {
            var module = FindModuleViewModel(step.Module);

            if (module != null && SelectedModule != module)
                SelectedModule = module;

            foreach (var m in Modules)
                m.IsRunning = m == module;

            SelectedStep = step;
            UpdateSystemVariables(step);
        };

        _testExecutor.StepCompleted += (index, step) =>
        {
            TestDuration = $"{Elapsed.TotalSeconds:F2}s";
            TestProgress = _executionSteps.Count == 0
                ? 0
                : (int)Math.Round((double)(index + 1) / _executionSteps.Count * 100);

            if (!step.IsPassed)
                NumberFailedSteps++;

            UpdateSystemVariables(step);

            step.IsExecuted = true;
        };
        
        _testExecutor.StepRepeated += () =>
        {
            TestDuration = $"{Elapsed.TotalSeconds:F2}s";
            var index = SelectedStep != null ? _executionSteps.IndexOf(SelectedStep) : -1;
            TestProgress = _executionSteps.Count == 0 || index == -1
                ? 0
                : (int)Math.Round((double)(index + 1) / _executionSteps.Count * 100);
        };

        _testExecutor.TestCompleted += async () =>
        {
            TestDuration = $"{Elapsed.TotalSeconds:F2}s";
            TestProgress = 100;

            foreach (var module in Modules)
                module.IsRunning = false;

            if (_isPartialRun)
            {
                if (TestStatus == TestStatus.RUNNING)
                    TestStatus = NumberFailedSteps > 0 ? TestStatus.FAILED : TestStatus.PASSED;

                return;
            }

            NumberRunTests++;

            if (TestStatus == TestStatus.CANCELED)
            {
                UpdatePassedPercentage();
                return;
            }

            if (TestStatus == TestStatus.EXECUTION_ERROR)
            {
                UpdatePassedPercentage();
                return;
            }

            if (_allowResultSave)
            {
                var testInfo = new TestInfo()
                {
                    ProjectName = _projectModel.ProjectName,
                    Operator = User,
                    Duration = TestDuration,
                    SerialNumber = SerialNumber,
                    DeviceUnderTestInfo = _projectModel.DeviceUnderTestInfo
                };
                
                await _testResultExportService.SaveAsync(_executionSteps, testInfo, NumberFailedSteps);
            }

            if (NumberFailedSteps > 0)
            {
                TestStatus = TestStatus.FAILED;
                UpdatePassedPercentage();
                return;
            }

            TestStatus = TestStatus.PASSED;
            NumberPassedTests++;
            UpdatePassedPercentage();
        };

        _testExecutor.TestCanceled += () =>
        {
            TestStatus = TestStatus.CANCELED;
        };
        
        _testExecutor.ExecutionError += () =>
        {
            TestStatus = TestStatus.EXECUTION_ERROR;
        };

        _testExecutor.TestRepeated += () =>
        {
            ResetAllResults();
            TestProgress = 0;
            NumberFailedSteps = 0;
            TestStatus = TestStatus.RUNNING;
        };
    }
}