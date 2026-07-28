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
    private readonly IHardwareInfo _hardwareInfo;
    private readonly ISettingsService _settingsService;
    private readonly IErrorService _errorService;
    private readonly IProjectController _projectController;
    private readonly ITestStepEditor _testStepEditor;
    private readonly ITestExecutionController _testExecutionController;
    private readonly ControlModuleService _controlModuleService;
    private readonly ProjectModel _projectModel;
    
    public ObservableCollection<TestStepViewModel> TestSteps { get; } = new();

    public List<CustomVariable> RuntimeVariables = new();
    
    [ObservableProperty]
    private TestStepViewModel? _selectedStep;
    
    [ObservableProperty]
    private ObservableCollection<TestStepViewModel> _selectedSteps = new();
    
    [ObservableProperty]
    private WorkspaceEditorViewModel _workspaceEditor;
    
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
    
    public bool CanPaste => _testStepEditor.CanPaste;

    public TestingTabViewModel(
        ISettingsService settingsService,
        IErrorService errorService,
        ProjectModel projectModel,
        WorkspaceEditorViewModel workspaceEditor,
        IProjectController projectController,
        ITestStepEditor testStepEditor,
        ITestExecutionController testExecutionController,
        ControlModuleService controlModuleService,
        IHardwareInfo hardwareInfo)
    {
        _hardwareInfo = hardwareInfo;
        _settingsService = settingsService;
        _errorService = errorService;
        _projectModel = projectModel;
        WorkspaceEditor = workspaceEditor;
        _projectController = projectController;
        _testStepEditor = testStepEditor;
        _testExecutionController = testExecutionController;

        _controlModuleService = controlModuleService;

        Title = "Test Environment";
        IsDevelopmentMode = settingsService.Settings.IsDevelopmentMode;

        _projectModel.TestSteps.CollectionChanged += ProjectTestStepsChanged;
        
        _projectModel.RuntimeVariableChanged += SynchronizeRuntimeVariables;
        
        _testExecutionController.HookExecutorEvents(this);
        
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
    }
    
    private void ProjectTestStepsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            var index = e.NewStartingIndex;

            foreach (TestStep step in e.NewItems!)
            {
                var vm = CreateViewModel(step);
                vm.PropertyChanged += OnStepPropertyChanged;

                TestSteps.Insert(index++, vm);
            }
        }

        if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            foreach (TestStep step in e.OldItems!)
            {
                var vm = TestSteps.First(x => ReferenceEquals(x.TestStep, step));

                vm.PropertyChanged -= OnStepPropertyChanged;

                TestSteps.Remove(vm);
            }
        }
        
        if (e.Action == NotifyCollectionChangedAction.Move)
        {
            var selected = SelectedStep;
            var selectedList = SelectedSteps.ToList();
            
            var item = TestSteps[e.OldStartingIndex];
            TestSteps.RemoveAt(e.OldStartingIndex);
            TestSteps.Insert(e.NewStartingIndex, item);

            SelectedStep = selected;
            SelectedSteps.Clear();
            foreach (var s in selectedList)
            {
                SelectedSteps.Add(s);
            }
        }

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var vm in TestSteps)
                vm.PropertyChanged -= OnStepPropertyChanged;

            TestSteps.Clear();
        }
    }
    
    private void SynchronizeRuntimeVariables()
    {
        RuntimeVariables.Clear();

        foreach (var variable in _projectModel.RuntimeVariables)
            RuntimeVariables.Add(variable.Clone());
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
        TestDuration = string.Empty;
        TestStatus = TestStatus.IDLE;
    }

    private void NotifyPasteChanged()
    {
        PasteTestStepsCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CanPaste));
    }

    private void OnStepPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not TestStepViewModel changedVm)
            return;

        var isTestStepProperty = string.IsNullOrWhiteSpace(e.PropertyName) ||
                                 typeof(TestStep).GetProperty(e.PropertyName) != null;

        if (isTestStepProperty)
            _projectModel.MarkDirty();

        if (changedVm != SelectedStep || !isTestStepProperty || string.IsNullOrWhiteSpace(e.PropertyName))
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
    
    partial void OnSelectedStepChanged(TestStepViewModel? value)
    {
        if (value?.TestStep == null) return;
        
        using (_projectModel.SuppressDirtyTracking())
        {
            try
            {
                WorkspaceEditor.LoadTestStep(value, TestSteps);
            }
            catch (Exception ex)
            {
                _errorService.AddError("Exception: " + ex.Message);
            }
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

        if (_projectModel.Settings.IsControlModuleEnabled)
            _controlModuleService.SetStatus(value);
    }
    
    partial void OnIsDevelopmentModeChanged(bool value)
    {
        _settingsService.Settings.IsDevelopmentMode = value;
    }
    
    private bool IsNotTestRunning() => TestStatus != TestStatus.RUNNING;
    private bool IsTestRunning() => TestStatus == TestStatus.RUNNING;
    private bool CanPasteTestStep() => IsNotTestRunning() && _testStepEditor.CanPaste;

    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private async Task NewFile()
    {
        await _projectController.NewProjectAsync();

        if (TestSteps.Count > 0)
            SelectedStep = TestSteps[0];
        else
            SelectedStep = null;
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

        if (TestSteps.Count > 0)
            SelectedStep = TestSteps[0];
        else
            SelectedStep = null;
        ResetTestCounters();
    }
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    public async Task LoadFile(string path)
    {
        await _projectController.LoadFileAsync(path);

        if (TestSteps.Count > 0)
            SelectedStep = TestSteps[0];
        else
            SelectedStep = null;
        ResetTestCounters();
    }

    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private void AddTestStep()
    {
        var step = _testStepEditor.AddStep(ComputeInsertIndex());

        Select(step);
    }
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private void DuplicateTestSteps()
    {
        if (SelectedSteps.Count == 0)
            return;

        var models = SelectedSteps
            .Select(x => x.TestStep)
            .ToList();

        var inserted = _testStepEditor.DuplicateSteps(
            models,
            ComputeInsertIndex());

        Select(inserted);
    }
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private void CopyTestSteps()
    {
        if (SelectedSteps.Count == 0)
            return;

        _testStepEditor.CopySteps(
            SelectedSteps.Select(x => x.TestStep));

        NotifyPasteChanged();
    }
    
    [RelayCommand(CanExecute = nameof(CanPasteTestStep))]
    private void PasteTestSteps()
    {
        var inserted = _testStepEditor.PasteSteps(ComputeInsertIndex());

        if (inserted.Count == 0)
            return;

        Select(inserted);

        NotifyPasteChanged();
    }
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private void CutTestSteps()
    {
        var removed = _testStepEditor.CutSteps(
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
        if (SelectedSteps.Count == 0)
            return;

        var models = SelectedSteps
            .Select(x => x.TestStep)
            .ToList();

        var index = _testStepEditor.RemoveSteps(models);

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
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private void MoveStepUp()
    {
        if (SelectedStep == null)
            return;

        var step = SelectedStep.TestStep;

        if (_testStepEditor.MoveStepUp(step))
            Select(step);
    }

    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private void MoveStepDown()
    {
        if (SelectedStep == null)
            return;

        var step = SelectedStep.TestStep;

        if (_testStepEditor.MoveStepDown(step))
            Select(step);
    }
    
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
    
    [RelayCommand(CanExecute = nameof(IsTestRunning))]
    private void RequestBreakRepeat() => _testExecutionController.RequestBreakRepeat();

    private TestStepViewModel CreateViewModel(TestStep step)
    {
        return new TestStepViewModel(step, _hardwareInfo);
    }
}