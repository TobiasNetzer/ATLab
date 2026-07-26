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
    private int _selectedStepIndex;
    
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

    public TestingTabViewModel(
        ISettingsService settingsService,
        IErrorService errorService,
        ProjectModel projectModel,
        WorkspaceEditorViewModel workspaceEditor,
        IProjectController projectController,
        ITestStepEditor testStepEditor,
        ITestExecutionController testExecutionController,
        ControlModuleService controlModuleService)
    {
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
        
        _projectModel.RuntimeVariables.CollectionChanged += SynchronizeRuntimeVariables;
        
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
                var vm = _testStepEditor.CreateViewModel(step);
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
            TestSteps.Move(e.OldStartingIndex, e.NewStartingIndex);
        }

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var vm in TestSteps)
                vm.PropertyChanged -= OnStepPropertyChanged;

            TestSteps.Clear();
        }
    }
    
    private void SynchronizeRuntimeVariables(object? sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
    {
        RuntimeVariables.Clear();

        foreach (var variable in _projectModel.RuntimeVariables)
            RuntimeVariables.Add(variable.Clone());
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
    private bool CanPasteTestStep() => IsNotTestRunning() && _testStepEditor.HasClipboard;

    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    public Task NewFile() => _projectController.NewProjectAsync(this);
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private Task SaveFile() => _projectController.SaveFileAsync();
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private Task SaveFileAs() => _projectController.SaveFileAsAsync();
    
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
    
    [RelayCommand(CanExecute = nameof(IsTestRunning))]
    private void RequestBreakRepeat() => _testExecutionController.RequestBreakRepeat();
}