using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Services;
using ATLab.CTIA;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class TestStepPresenterViewModel : ViewModelBase
{

    private readonly ITestExecutor _testExecutor;
    public ObservableCollection<TestStepViewModel> TestSteps { get; }

    [ObservableProperty]
    private TestConfigurationViewModel _testConfiguration;
    
    [ObservableProperty]
    private TestStepViewModel? _selectedStep;
    
    [ObservableProperty]
    private int _selectedStepIndex;
    
    [ObservableProperty]
    private TestStepConfiguratorViewModel _testStepConfiguratorViewModel;
    
    private readonly IErrorService _errorService;
    
    private CancellationTokenSource? _cts;

    public TestStepPresenterViewModel(IErrorService errorService, TestConfigurationViewModel testConfiguration, ITestExecutor testExecutor, TestStepConfiguratorViewModel testStepConfiguratorViewModel)
    {
        _errorService = errorService;
        TestSteps = new ObservableCollection<TestStepViewModel>();
        TestConfiguration = testConfiguration;
        _testExecutor = testExecutor;
        TestStepConfiguratorViewModel = testStepConfiguratorViewModel;
        
        HookExecutorEvents();
    }
    
    partial void OnSelectedStepChanged(TestStepViewModel? value)
    {
        if (value != null)
        {
            try
            {
                TestConfiguration.MeasChannelViewModel.LoadActiveMeasChannels(value.MatrixState);
                TestConfiguration.StimChannelViewModel.LoadRelayStates(value.StimState);
                TestConfiguration.ExtStimChannelViewModel.LoadRelayStates(value.ExtStimState);
                TestConfiguration.TestStepConfiguratorViewModel.LoadTestStep(value);
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
            TestSteps[i].Number = i + 1; // 1‑based numbering
        }
    }

    [RelayCommand]
    private void AddTestStep()
    {
        var indexToInsertNewStep = SelectedStepIndex + 1;
        TestSteps.Insert(indexToInsertNewStep, new TestStepViewModel(new TestStep(), TestConfiguration.HardwareInfo));
        RenumberTestSteps();
        SelectedStepIndex = indexToInsertNewStep;
    }
    
    [RelayCommand]
    private void DuplicateTestStep()
    {
        if (SelectedStep == null) return;
        
        var currentModel = SelectedStep.GetModel();
        var modelCopy = new TestStep
        {
            Name = currentModel.Name,
            LowerLimit = currentModel.LowerLimit,
            UpperLimit = currentModel.UpperLimit,
            NominalValue = currentModel.NominalValue,
            Comment = currentModel.Comment,
            StimState = currentModel.StimState != null ? new RelayGroupDto(currentModel.StimState) : null,
            ExtStimState = currentModel.ExtStimState != null ? new RelayGroupDto(currentModel.ExtStimState) : null,
            MatrixState = currentModel.MatrixState != null ? new RelayMatrix(currentModel.MatrixState) : null
        };

        var duplicatedStep = new TestStepViewModel(modelCopy, TestConfiguration.HardwareInfo);

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
            step.Result = result.ToString();
        };

        _testExecutor.TestCompleted += () =>
        {
            IsRunning = false;
        };
    }


    public TestStepPresenterViewModel()
    {
        _errorService = new ErrorService();
        TestSteps = new ObservableCollection<TestStepViewModel>();
        TestConfiguration = new TestConfigurationViewModel(new DummyHardwareInfo(), new TestStepConfiguratorViewModel());
        _testExecutor = new TestExecutor(new DummyTestStepRunner());
        TestStepConfiguratorViewModel = new TestStepConfiguratorViewModel();
    }
}
