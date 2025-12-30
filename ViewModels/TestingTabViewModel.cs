using System;
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

    private bool _isRepeatedExecution;

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
    private bool _isEditingMode;
    
    [ObservableProperty]
    private int _numberFailedSteps;

    [ObservableProperty]
    private int _testProgress;
    
    [ObservableProperty]
    private TestStatus _testStatus = TestStatus.IDLE;

    public TestingTabViewModel(
        ISettingsService settingsService,
        IErrorService errorService, 
        TestHardwareRelayChannelsViewModel testHardwareRelayChannels, 
        ITestExecutor testExecutor, 
        TestStepConfiguratorViewModel testStepConfiguratorViewModel,
        IProjectService projectService,
        SerialDeviceManagerViewModel serialDeviceManager,
        ScriptSelectorViewModel scriptSelector,
        ProjectSettingsViewModel projectSettingsViewModel)
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
        if (value?.TestStep != null)
        {
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
    }

    partial void OnTestStatusChanged(TestStatus value)
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
            RepeatUntilPass = currentModel.RepeatUntilPass,
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
    
    private bool IsTestRunning() => TestStatus != TestStatus.RUNNING;
    
    [RelayCommand(CanExecute = nameof(IsTestRunning))]
    private async Task StartTestAsync()
    {
        TestStatus = TestStatus.RUNNING;
        NumberFailedSteps = 0;
        TestProgress = 0;
        
        await _testExecutor.StartTestAsync(TestSteps);

    }

    [RelayCommand]
    private void CancelTest()
    {
        _testExecutor.CancelTest();
    }

    private void HookExecutorEvents()
    {
        _testExecutor.StepStarted += (index, step) =>
        {
            SelectedStepIndex = index;
        };

        _testExecutor.StepCompleted += (index, step) =>
        {
            TestProgress = TestSteps.Count == 0 ? 0 : (int)Math.Round((double)(SelectedStepIndex + 1) / TestSteps.Count * 100);
            
            if (!step.IsValid)
            {
                NumberFailedSteps++;
            }
        };

        _testExecutor.TestCompleted += (cancelled) =>
        {
            TestProgress = 100;
            if (cancelled)
            {
                TestStatus = TestStatus.CANCELLED;
                return;
            }
            TestStatus = NumberFailedSteps > 0 ? TestStatus.FAILED : TestStatus.PASSED;
        };
    }

    [RelayCommand(CanExecute = nameof(IsTestRunning))]
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
            DefaultTolerance = _projectSettingsViewModel.ToleranceValue
        };
    }

    private void CheckForChanges()
    {
        _projectService.IsStateChanged(CaptureCurrentState());
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
        var dto = CaptureCurrentState();
        await _projectService.SaveAsAsync(dto);
    }

    [RelayCommand(CanExecute = nameof(IsTestRunning))]
    public async Task SaveFile()
    {
        var dto = CaptureCurrentState();
        await _projectService.SaveAsync(dto);
    }

    [RelayCommand(CanExecute = nameof(IsTestRunning))]
    public async Task LoadFileWithDialog()
    {
        var dto = await _projectService.OpenFileAsync();
        if (dto != null)
        {
            ApplyDto(dto);
        }
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
    }

    private void ApplyDto(AtlabFileDto dto)
    {
        TestSteps.Clear();
        foreach (var stepVm in dto.TestSteps.Select(step => new TestStepViewModel(step, TestHardwareRelayChannels.HardwareInfo)))
        {
            stepVm.PropertyChanged += (_, _) => CheckForChanges();
            TestSteps.Add(stepVm);
        }
        
        _projectSettingsViewModel.ToleranceValue = dto.DefaultTolerance;
        
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
}
