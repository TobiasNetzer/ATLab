using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.Views;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class TestingTabViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly ITestExecutor _testExecutor;
    private readonly IErrorService _errorService;
    private readonly IProjectService _projectService;
    private readonly SerialDeviceManagerViewModel _serialDeviceManager;
    private readonly ProjectSettingsViewModel _projectSettingsViewModel;
    private readonly ISerialNumberDialogService _serialNumberDialogService;
    
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
    private bool _isEditingMode;
    
    [ObservableProperty]
    private int _numberFailedSteps;

    [ObservableProperty]
    private int _testProgress;
    
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

    public TestingTabViewModel(
        ISettingsService settingsService,
        IErrorService errorService, 
        TestHardwareRelayChannelsViewModel testHardwareRelayChannels, 
        ITestExecutor testExecutor, 
        TestStepConfiguratorViewModel testStepConfiguratorViewModel,
        IProjectService projectService,
        SerialDeviceManagerViewModel serialDeviceManager,
        ScriptSelectorViewModel scriptSelector,
        ProjectSettingsViewModel projectSettingsViewModel,
        ISerialNumberDialogService serialNumberDialogService)
    {
        _settingsService = settingsService;
        _errorService = errorService;
        TestSteps = new ObservableCollection<TestStepViewModel>();
        TestHardwareRelayChannels = testHardwareRelayChannels;
        _testExecutor = testExecutor;
        TestStepConfiguratorViewModel = testStepConfiguratorViewModel;
        _scriptSelector = scriptSelector;
        _projectService = projectService;
        _serialDeviceManager = serialDeviceManager;
        _projectSettingsViewModel = projectSettingsViewModel;
        _serialNumberDialogService = serialNumberDialogService;
        
        Title = "Testing";
        
        IsEditingMode = settingsService.Settings.IsEditingMode;
        
        HookExecutorEvents();
        
        TestSteps.CollectionChanged += (_, _) => CheckForChanges();
        TestHardwareRelayChannels.ConfigurationChanged += () => CheckForChanges();
        _projectSettingsViewModel.ConfigurationChanged += () => CheckForChanges();
        _serialDeviceManager.SerialDevices.CollectionChanged += (_, _) => CheckForChanges();
        
        _projectService.UpdateLastSavedState(CaptureCurrentState());
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
        OpenAboutWindowCommand.NotifyCanExecuteChanged();
        LoadFileWithDialogCommand.NotifyCanExecuteChanged();
        SaveFileAsCommand.NotifyCanExecuteChanged();
        StartTestFromSelectionCommand.NotifyCanExecuteChanged();
        StartTestRepeatCommand.NotifyCanExecuteChanged();
        StartTestCommand.NotifyCanExecuteChanged();
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
            
            var modelCopy = CopyTestStepModel(step.TestStep);
            
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
            .Select(s => CopyTestStepModel(s.TestStep))
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
            var modelCopy = CopyTestStepModel(model);

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
            .Select(s => CopyTestStepModel(s.TestStep))
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
        NumberFailedSteps = 0;
        TestStatus = TestStatus.RUNNING;
        TestProgress = 0;
        SelectedStepIndex = 0;
                
        await _testExecutor.StartRepeatTestAsync(TestSteps, SelectedStepIndex);
    }
    
    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
    private async Task StartTestAsync()
    {
        if (_projectSettingsViewModel.UseSerialNumber)
        {
            var serial = await _serialNumberDialogService.AskForSerialNumberAsync();

            if (serial == null)
            {
                SerialNumber = string.Empty;
                return;
            }
            
            SerialNumber = serial;

        }
        else
        {
            SerialNumber = string.Empty;
        }
        
        TestStatus = TestStatus.RUNNING;
        NumberFailedSteps = 0;
        TestProgress = 0;
        SelectedStepIndex = 0;

        await _testExecutor.StartTestAsync(TestSteps, SelectedStepIndex);
    }

    [RelayCommand]
    private void CancelTest()
    {
        _testExecutor.CancelTest();
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
            _serialDeviceManager.SerialDevices.Clear();
            _projectService.UpdateLastSavedState(CaptureCurrentState());
            SelectedStepIndex = -1;
            AddTestStep();
            NumberPassedTests = 0;
            NumberRunTests = 0;
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
            SerialDevices = _serialDeviceManager.SerialDevices.ToList(),
            DefaultTolerance = _projectSettingsViewModel.ToleranceValue,
            UseSerialNumber = _projectSettingsViewModel.UseSerialNumber,
        };
    }

    private void CheckForChanges()
    {
        _projectService.IsStateChanged(CaptureCurrentState());
    }

    private void OnStepPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        CheckForChanges();
    }

    [RelayCommand(CanExecute = nameof(IsNotTestRunning))]
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
        
        TestHardwareRelayChannels.ApplyChannelNames(dto.StimChannelNames, dto.ExtStimChannelNames, dto.MeasChannelNames);

        _serialDeviceManager.SerialDevices.Clear();
        foreach (var device in dto.SerialDevices)
        {
            _serialDeviceManager.SerialDevices.Add(device);
        }
        
        SelectedStepIndex = 0;
    }

    partial void OnIsEditingModeChanged(bool value)
    {
        _settingsService.Settings.IsEditingMode = value;
    }

    private TestStep CopyTestStepModel(TestStep step)
    {
        return new TestStep
        {
            Name = step.Name,
            LowerLimit = step.LowerLimit,
            UpperLimit = step.UpperLimit,
            NominalValue = step.NominalValue,
            Unit = step.Unit,
            Comment = step.Comment,
            ShowCommentOnTestStart = step.ShowCommentOnTestStart,
            CustomMessageBoxImagePath = step.CustomMessageBoxImagePath,
            Delay = step.Delay,
            EvaluationSource = step.EvaluationSource,
            RepeatUntilPass = step.RepeatUntilPass,
            TargetDevice = step.TargetDevice,
            ScriptId = step.ScriptId,
            ScriptVariables = new ObservableCollection<ScriptVariable>(step.ScriptVariables.Select(v => v.Clone())),
            StimState = step.StimState != null ? new RelayGroupDto(step.StimState) : null,
            ExtStimState = step.ExtStimState != null ? new RelayGroupDto(step.ExtStimState) : null,
            MatrixState = new RelayMatrix(step.MatrixState)
        };
    }
}
