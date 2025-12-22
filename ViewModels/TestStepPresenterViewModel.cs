using System;
using System.Collections.ObjectModel;
using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class TestStepPresenterViewModel : ViewModelBase
{
    public ObservableCollection<TestStepViewModel> TestSteps { get; }

    [ObservableProperty]
    private TestConfigurationViewModel _testConfiguration;
    
    [ObservableProperty]
    private TestStepViewModel? _selectedStep;
    
    [ObservableProperty]
    private int _selectedStepIndex;
    
    private readonly IErrorService _errorService;
    
    public TestStepPresenterViewModel(IErrorService errorService, TestConfigurationViewModel testConfiguration)
    {
        _errorService = errorService;
        TestSteps = new ObservableCollection<TestStepViewModel>();
        TestConfiguration = testConfiguration;
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
        TestSteps.Add(new TestStepViewModel(new TestStep(), TestConfiguration.HardwareInfo));
        RenumberTestSteps();
    }
    
    [RelayCommand]
    private void RemoveTestStep()
    {
        TestSteps.RemoveAt(SelectedStepIndex);
        RenumberTestSteps();
    }
}
